using BepInEx.Configuration;
using UnityEngine;

namespace CoolCount
{
    public enum TimeFormat
    {
        /// <summary>The game's own status-effect style: "1:59", then "45".</summary>
        Game,

        /// <summary>OmniCC style: "2h", "3m", then "45".</summary>
        Compact
    }

    public static class PluginConfig
    {
        // General
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> ShowInInventory;
        public static ConfigEntry<bool> ShowInContainers;
        public static ConfigEntry<bool> ShowInHotbar;
        public static ConfigEntry<bool> ShowMeads;
        public static ConfigEntry<bool> ShowFood;
        public static ConfigEntry<bool> FoodSlotsFull;
        public static ConfigEntry<float> MinDuration;

        // Text
        public static ConfigEntry<int> FontSize;
        public static ConfigEntry<TimeFormat> Format;
        public static ConfigEntry<float> TenthsBelow;
        public static ConfigEntry<float> SoonBelow;
        public static ConfigEntry<Color> SoonColor;
        public static ConfigEntry<Color> SecondsColor;
        public static ConfigEntry<Color> MinutesColor;
        public static ConfigEntry<Color> HoursColor;

        // Icon
        public static ConfigEntry<bool> DimIcon;
        public static ConfigEntry<Color> DimColor;
        public static ConfigEntry<bool> Sweep;
        public static ConfigEntry<Color> SweepColor;

        // Logging
        public static ConfigEntry<bool> Verbose;

        private static ConfigurationManagerAttributes Attr(int order, bool advanced = false)
        {
            return new ConfigurationManagerAttributes { Order = order, IsAdvanced = advanced };
        }

        public static void Bind(ConfigFile cfg)
        {
            Enabled = cfg.Bind("General", "Enabled", true,
                new ConfigDescription("Master switch. Off hides every overlay without unloading the mod.", null, Attr(100)));

            ShowInInventory = cfg.Bind("General", "ShowInInventory", true,
                new ConfigDescription("Draw cooldowns on your own inventory grid.", null, Attr(95)));

            ShowInContainers = cfg.Bind("General", "ShowInContainers", true,
                new ConfigDescription(
                    "Also draw them on the chest, cart and ship grids, measured against your own " +
                    "active effects: a mead in a chest shows when you could drink it.",
                    null, Attr(90)));

            ShowInHotbar = cfg.Bind("General", "ShowInHotbar", true,
                new ConfigDescription("Draw cooldowns on the hotbar under the health bar.", null, Attr(85)));

            ShowMeads = cfg.Bind("General", "Meads", true,
                new ConfigDescription(
                    "Meads and anything else that applies a status effect when consumed. The game " +
                    "blocks these while that effect, or any effect in the same exclusion group, is " +
                    "still running; the countdown is that effect's remaining time.",
                    null, Attr(80)));

            ShowFood = cfg.Bind("General", "Food", true,
                new ConfigDescription(
                    "Food you are already digesting. It can be eaten again once its timer drops " +
                    "below half; the countdown is the time until then.",
                    null, Attr(75)));

            FoodSlotsFull = cfg.Bind("General", "FoodSlotsFull", true,
                new ConfigDescription(
                    "When all three food slots are taken and none is past half, every other food " +
                    "in the bag counts down to the moment one frees up.",
                    null, Attr(70)));

            MinDuration = cfg.Bind("General", "MinDuration", 0f,
                new ConfigDescription("Ignore cooldowns whose full length is shorter than this many seconds.",
                    new AcceptableValueRange<float>(0f, 60f), Attr(65, advanced: true)));

            FontSize = cfg.Bind("Text", "FontSize", 22,
                new ConfigDescription("Size of the countdown.", new AcceptableValueRange<int>(8, 48), Attr(60)));

            Format = cfg.Bind("Text", "Format", TimeFormat.Game,
                new ConfigDescription("Game matches the status-effect icons (1:59). Compact is OmniCC's (2m).", null, Attr(58)));

            TenthsBelow = cfg.Bind("Text", "TenthsBelow", 0f,
                new ConfigDescription("Show tenths of a second under this many seconds (4.3). 0 turns it off.",
                    new AcceptableValueRange<float>(0f, 10f), Attr(56)));

            SoonBelow = cfg.Bind("Text", "SoonBelow", 5f,
                new ConfigDescription("Use the Soon colour under this many seconds.",
                    new AcceptableValueRange<float>(0f, 60f), Attr(54)));

            SoonColor = cfg.Bind("Text", "SoonColor", new Color(1f, 0.35f, 0.35f, 1f),
                new ConfigDescription("Colour of the last few seconds.", null, Attr(52)));

            SecondsColor = cfg.Bind("Text", "SecondsColor", new Color(1f, 1f, 0.45f, 1f),
                new ConfigDescription("Colour under one minute.", null, Attr(50)));

            MinutesColor = cfg.Bind("Text", "MinutesColor", Color.white,
                new ConfigDescription("Colour under one hour.", null, Attr(48)));

            HoursColor = cfg.Bind("Text", "HoursColor", new Color(0.75f, 0.75f, 0.75f, 1f),
                new ConfigDescription("Colour from one hour up.", null, Attr(46)));

            DimIcon = cfg.Bind("Icon", "Dim", false,
                new ConfigDescription(
                    "Darken the whole item icon on top of the sweep. Off by default: the sweep " +
                    "already darkens the part of the cooldown still to run, and dimming as well " +
                    "leaves the icon too dark to recognise. An item blocked by an effect with no " +
                    "end is always dimmed, since there is no sweep or countdown to show.",
                    null, Attr(40)));

            DimColor = cfg.Bind("Icon", "DimColor", new Color(0.45f, 0.45f, 0.45f, 1f),
                new ConfigDescription("Tint applied to a dimmed icon.", null, Attr(38)));

            Sweep = cfg.Bind("Icon", "Sweep", true,
                new ConfigDescription("Draw the radial sweep that shrinks clockwise as the cooldown runs out.", null, Attr(36)));

            SweepColor = cfg.Bind("Icon", "SweepColor", new Color(0f, 0f, 0f, 0.5f),
                new ConfigDescription("Colour and opacity of the sweep.", null, Attr(34)));

            Verbose = cfg.Bind("Logging", "Verbose", false,
                new ConfigDescription("Log overlay creation and every patch error to the BepInEx log.", null, Attr(5, advanced: true)));
        }
    }
}
