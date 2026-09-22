using System.Collections.Generic;

namespace CoolCount.Core
{
    public struct Cooldown
    {
        /// <summary>Seconds until the item can be used. PositiveInfinity when the blocking effect never expires.</summary>
        public float Remaining;

        /// <summary>Full length of the cooldown, for the sweep. 0 when unknown.</summary>
        public float Total;

        /// <summary>What is blocking it, for the console.</summary>
        public string Source;
    }

    /// <summary>
    /// Mirrors the game's own gating so the overlay never lies about what you can use.
    ///
    /// Nothing is drinkable unless it is a Consumable: Humanoid.CanConsumeItem, which
    /// Player.CanConsumeItem calls first, rejects every other item type outright. Mead *bases*
    /// are Material and carry the finished mead's m_consumeStatusEffect (which is why their
    /// tooltip names the effect), so that field alone is not enough to call an item gated.
    ///
    /// Meads: Player.CanConsumeItem refuses an item while the player has its consume status
    /// effect, by name hash, or any effect sharing its m_category (the wiki's "exclusion
    /// groups"). There is no separate cooldown timer; the block ends when that effect ends, so
    /// the countdown is the effect's remaining time. An empty category matches nothing.
    ///
    /// Food: Player.CanEat allows a food you are digesting only once Food.CanEatAgain, i.e. its
    /// timer (which counts down from m_foodBurnTime) is under half. A food you are not digesting
    /// is refused only when all three slots are full and none is past half.
    /// </summary>
    public static class Cooldowns
    {
        public static bool TryGet(Player player, ItemDrop.ItemData item, out Cooldown cd)
        {
            cd = default(Cooldown);
            if (player == null || item == null || item.m_shared == null)
                return false;

            ItemDrop.ItemData.SharedData shared = item.m_shared;
            if (shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
                return false;

            if (shared.m_consumeStatusEffect != null && PluginConfig.ShowMeads.Value
                && FromStatusEffect(player, shared.m_consumeStatusEffect, out cd))
                return Accept(ref cd);

            if (IsFood(shared) && PluginConfig.ShowFood.Value && FromFood(player, item, out cd))
                return Accept(ref cd);

            return false;
        }

        public static bool IsFood(ItemDrop.ItemData.SharedData shared)
        {
            return shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable
                && (shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f);
        }

        private static bool Accept(ref Cooldown cd)
        {
            if (cd.Remaining <= 0f)
                return false;
            float min = PluginConfig.MinDuration.Value;
            return min <= 0f || cd.Total <= 0f || cd.Total >= min;
        }

        private static bool FromStatusEffect(Player player, StatusEffect gate, out Cooldown cd)
        {
            cd = default(Cooldown);
            SEMan seman = player.GetSEMan();
            if (seman == null)
                return false;

            int hash = gate.NameHash();
            string category = gate.m_category;
            bool useCategory = !string.IsNullOrEmpty(category);

            List<StatusEffect> effects = seman.GetStatusEffects();
            bool found = false;
            for (int i = 0; i < effects.Count; i++)
            {
                StatusEffect se = effects[i];
                if (se == null)
                    continue;
                if (se.NameHash() != hash && !(useCategory && se.m_category == category))
                    continue;

                float remaining = se.m_ttl > 0f ? se.GetRemaningTime() : float.PositiveInfinity;
                if (!found || remaining > cd.Remaining)
                {
                    cd.Remaining = remaining;
                    cd.Total = se.m_ttl;
                    cd.Source = se.m_name;
                    found = true;
                }
            }
            return found;
        }

        private static bool FromFood(Player player, ItemDrop.ItemData item, out Cooldown cd)
        {
            cd = default(Cooldown);
            List<Player.Food> foods = player.m_foods;
            if (foods == null || foods.Count == 0)
                return false;

            string name = item.m_shared.m_name;
            Player.Food own = null;
            Player.Food soonest = null;
            float soonestLeft = float.MaxValue;
            bool anyReady = false;

            for (int i = 0; i < foods.Count; i++)
            {
                Player.Food food = foods[i];
                if (food == null || food.m_item == null || food.m_item.m_shared == null)
                    continue;
                if (food.m_item.m_shared.m_name == name)
                    own = food;

                float left = food.m_time - food.m_item.m_shared.m_foodBurnTime * 0.5f;
                if (left <= 0f)
                    anyReady = true;
                else if (left < soonestLeft)
                {
                    soonestLeft = left;
                    soonest = food;
                }
            }

            if (own != null)
            {
                float half = own.m_item.m_shared.m_foodBurnTime * 0.5f;
                cd.Remaining = own.m_time - half;
                cd.Total = half;
                cd.Source = own.m_item.m_shared.m_name;
                return cd.Remaining > 0f;
            }

            if (PluginConfig.FoodSlotsFull.Value && foods.Count >= 3 && !anyReady && soonest != null)
            {
                cd.Remaining = soonestLeft;
                cd.Total = soonest.m_item.m_shared.m_foodBurnTime * 0.5f;
                cd.Source = "$msg_isfull";
                return true;
            }
            return false;
        }
    }
}
