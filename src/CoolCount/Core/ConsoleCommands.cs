using System.Collections.Generic;

namespace CoolCount.Core
{
    /// <summary>
    /// "coolcount" prints what the overlays would show for every item you carry, so the mod can
    /// be checked from the BepInEx log without a screenshot. "coolcount effects" and
    /// "coolcount foods" print the state the numbers come from.
    /// </summary>
    public static class ConsoleCommands
    {
        public static void Register()
        {
            new Terminal.ConsoleCommand("coolcount", "CoolCount: list cooldowns (effects | foods)",
                delegate (Terminal.ConsoleEventArgs args)
                {
                    string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";
                    Player player = Player.m_localPlayer;
                    if (player == null)
                    {
                        Say(args.Context, "CoolCount: no local player.");
                        return;
                    }
                    switch (sub)
                    {
                        case "effects": Effects(args.Context, player); break;
                        case "foods": Foods(args.Context, player); break;
                        default: Items(args.Context, player); break;
                    }
                });
        }

        private static void Say(Terminal ctx, string line)
        {
            ctx.AddString(line);
            CoolCountPlugin.Log.LogInfo(line);
        }

        private static void Items(Terminal ctx, Player player)
        {
            Inventory inv = player.GetInventory();
            if (inv == null)
                return;
            int shown = 0;
            foreach (ItemDrop.ItemData item in inv.GetAllItems())
            {
                Cooldown cd;
                if (!Cooldowns.TryGet(player, item, out cd))
                    continue;
                shown++;
                Say(ctx, string.Format("CoolCount: [{0},{1}] {2}: {3} left of {4} ({5})",
                    item.m_gridPos.x, item.m_gridPos.y, Localization.instance.Localize(item.m_shared.m_name),
                    float.IsInfinity(cd.Remaining) ? "forever" : cd.Remaining.ToString("0.0") + "s",
                    cd.Total.ToString("0"), Localization.instance.Localize(cd.Source ?? "")));
            }
            Say(ctx, "CoolCount: " + shown + " item(s) on cooldown.");
        }

        private static void Effects(Terminal ctx, Player player)
        {
            List<StatusEffect> effects = player.GetSEMan().GetStatusEffects();
            foreach (StatusEffect se in effects)
            {
                if (se == null)
                    continue;
                Say(ctx, string.Format("CoolCount: effect {0} category=\"{1}\" ttl={2} left={3} hidden={4}",
                    se.m_name, se.m_category, se.m_ttl.ToString("0"),
                    se.m_ttl > 0f ? se.GetRemaningTime().ToString("0.0") : "inf", se.m_hidden));
            }
            Say(ctx, "CoolCount: " + effects.Count + " status effect(s).");
        }

        private static void Foods(Terminal ctx, Player player)
        {
            List<Player.Food> foods = player.m_foods;
            foreach (Player.Food food in foods)
            {
                if (food == null || food.m_item == null)
                    continue;
                float burn = food.m_item.m_shared.m_foodBurnTime;
                Say(ctx, string.Format("CoolCount: food {0} time={1}/{2} again in {3}",
                    Localization.instance.Localize(food.m_item.m_shared.m_name), food.m_time.ToString("0"),
                    burn.ToString("0"), food.CanEatAgain() ? "now" : (food.m_time - burn * 0.5f).ToString("0.0") + "s"));
            }
            Say(ctx, "CoolCount: " + foods.Count + " food(s) active.");
        }
    }
}
