using UnityEngine;

namespace CoolCount.Core
{
    /// <summary>The countdown string and its colour, OmniCC's rules with the game's format as default.</summary>
    public static class TimeText
    {
        public static string Format(float seconds)
        {
            if (seconds <= 0f)
                return "";

            float tenths = PluginConfig.TenthsBelow.Value;
            if (tenths > 0f && seconds < tenths)
                return seconds.ToString("0.0");

            if (PluginConfig.Format.Value == TimeFormat.Game)
                return StatusEffect.GetTimeString(seconds);

            int s = Mathf.CeilToInt(seconds);
            if (s >= 3600)
                return Mathf.RoundToInt(seconds / 3600f) + "h";
            if (s >= 60)
                return Mathf.RoundToInt(seconds / 60f) + "m";
            return s.ToString();
        }

        public static Color ColorFor(float seconds)
        {
            if (seconds < PluginConfig.SoonBelow.Value)
                return PluginConfig.SoonColor.Value;
            if (seconds < 60f)
                return PluginConfig.SecondsColor.Value;
            if (seconds < 3600f)
                return PluginConfig.MinutesColor.Value;
            return PluginConfig.HoursColor.Value;
        }
    }
}
