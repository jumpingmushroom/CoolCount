using System;
using System.Collections.Generic;
using CoolCount.Core;
using CoolCount.UI;
using HarmonyLib;
using TMPro;
using UnityEngine.UI;

namespace CoolCount.Patches
{
    internal static class Guard
    {
        private static int _errors;

        public static void Fail(string where, Exception ex)
        {
            if (_errors++ < 5 || PluginConfig.Verbose.Value)
                CoolCountPlugin.Log.LogWarning(where + " threw: " + ex);
        }
    }

    /// <summary>
    /// The game redraws every slot each frame the grid is visible (InventoryGrid.UpdateGui) and
    /// every frame for the hotbar (HotkeyBar.UpdateIcons). Both patches run after that redraw,
    /// read the same item list the game just used, and paint or clear the overlay on each slot.
    /// Nothing the game wrote is changed except the icon tint while dimmed.
    /// </summary>
    internal static class SlotPainter
    {
        public static void Paint(Image icon, TMP_Text template, Player who, ItemDrop.ItemData item)
        {
            if (icon == null)
                return;
            Cooldown cd;
            if (Cooldowns.TryGet(who, item, out cd))
                SlotOverlay.Ensure(icon, template).Show(cd);
            else
                SlotOverlay.HideIfAny(icon);
        }

        public static void PaintGrid(InventoryGrid grid, Player player)
        {
            List<InventoryElement> elements = grid.m_elements;
            if (elements == null)
                return;

            // Container grids are updated with a null player; the local player's effects are
            // still what decide whether the mead in the chest could be drunk.
            bool enabled = PluginConfig.Enabled.Value && PluginConfig.ShowInInventory.Value
                && (player != null || PluginConfig.ShowInContainers.Value);
            Player who = player != null ? player : Player.m_localPlayer;
            Inventory inv = grid.m_inventory;

            if (!enabled || who == null || inv == null)
            {
                for (int i = 0; i < elements.Count; i++)
                    SlotOverlay.HideIfAny(elements[i].m_icon);
                return;
            }

            for (int i = 0; i < elements.Count; i++)
            {
                if (!elements[i].m_used)
                    SlotOverlay.HideIfAny(elements[i].m_icon);
            }

            int width = inv.GetWidth();
            List<ItemDrop.ItemData> items = inv.GetAllItems();
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                InventoryElement element = grid.GetElement(item.m_gridPos.x, item.m_gridPos.y, width);
                if (element != null)
                    Paint(element.m_icon, element.m_amount, who, item);
            }
        }

        public static void PaintHotbar(HotkeyBar bar, Player player)
        {
            var elements = bar.m_elements;
            if (elements == null || elements.Count == 0)
                return;

            bool enabled = PluginConfig.Enabled.Value && PluginConfig.ShowInHotbar.Value && player != null;
            if (!enabled)
            {
                for (int i = 0; i < elements.Count; i++)
                    SlotOverlay.HideIfAny(elements[i].m_icon);
                return;
            }

            for (int i = 0; i < elements.Count; i++)
            {
                if (!elements[i].m_used)
                    SlotOverlay.HideIfAny(elements[i].m_icon);
            }

            List<ItemDrop.ItemData> items = bar.m_items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                int x = item.m_gridPos.x;
                if (x < 0 || x >= elements.Count)
                    continue;
                var element = elements[x];
                Paint(element.m_icon, element.m_amount, player, item);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class InventoryGrid_UpdateGui_Patch
    {
        private static void Postfix(InventoryGrid __instance, Player player)
        {
            try
            {
                SlotPainter.PaintGrid(__instance, player);
            }
            catch (Exception ex)
            {
                Guard.Fail("InventoryGrid.UpdateGui postfix", ex);
            }
        }
    }

    [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
    internal static class HotkeyBar_UpdateIcons_Patch
    {
        private static void Postfix(HotkeyBar __instance, Player player)
        {
            try
            {
                SlotPainter.PaintHotbar(__instance, player);
            }
            catch (Exception ex)
            {
                Guard.Fail("HotkeyBar.UpdateIcons postfix", ex);
            }
        }
    }
}
