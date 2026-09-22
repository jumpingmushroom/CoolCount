using CoolCount.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoolCount.UI
{
    /// <summary>
    /// The OmniCC bit: a child of the slot's icon Image holding a radial sweep and a countdown
    /// label. Parenting under the icon gives the exact icon rectangle for free and hides the
    /// overlay whenever the game hides the icon. The label is a copy of the slot's own stack
    /// count text, so font, material and outline match the game's without any asset of ours.
    /// Neither child is a raycast target, so clicks, drags and hover reach the slot as before.
    /// </summary>
    public sealed class SlotOverlay : MonoBehaviour
    {
        private const string ObjectName = "CoolCount";

        private static Sprite _white;

        private Image _icon;
        private Image _sweep;
        private TMP_Text _text;
        private bool _dimmed;

        /// <summary>The overlay already on this icon, or null. Cheap: icons normally have no children.</summary>
        public static SlotOverlay Find(Image icon)
        {
            if (icon == null)
                return null;
            Transform t = icon.transform;
            if (t.childCount == 0)
                return null;
            Transform child = t.Find(ObjectName);
            return child != null ? child.GetComponent<SlotOverlay>() : null;
        }

        public static SlotOverlay Ensure(Image icon, TMP_Text textTemplate)
        {
            SlotOverlay overlay = Find(icon);
            if (overlay != null)
                return overlay;

            GameObject go = new GameObject(ObjectName, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(icon.transform, false);
            Stretch(rt);

            overlay = go.AddComponent<SlotOverlay>();
            overlay._icon = icon;
            overlay.Build(textTemplate);

            if (PluginConfig.Verbose.Value)
                CoolCountPlugin.Log.LogDebug("overlay created under " + icon.transform.parent.name + "/" + icon.name);
            return overlay;
        }

        public static void HideIfAny(Image icon)
        {
            SlotOverlay overlay = Find(icon);
            if (overlay != null)
                overlay.Hide();
        }

        private void Build(TMP_Text template)
        {
            GameObject sweepGo = new GameObject("sweep", typeof(RectTransform));
            RectTransform srt = (RectTransform)sweepGo.transform;
            srt.SetParent(transform, false);
            Stretch(srt);
            _sweep = sweepGo.AddComponent<Image>();
            _sweep.sprite = WhiteSprite();
            _sweep.type = Image.Type.Filled;
            _sweep.fillMethod = Image.FillMethod.Radial360;
            _sweep.fillOrigin = (int)Image.Origin360.Top;
            _sweep.fillClockwise = false;
            _sweep.raycastTarget = false;
            _sweep.color = PluginConfig.SweepColor.Value;

            if (template != null)
            {
                GameObject textGo = Instantiate(template.gameObject, transform);
                textGo.name = "time";
                textGo.SetActive(true);
                RectTransform trt = (RectTransform)textGo.transform;
                Stretch(trt);
                _text = textGo.GetComponent<TMP_Text>();
                if (_text != null)
                {
                    _text.enabled = true;
                    _text.text = "";
                    _text.alignment = TextAlignmentOptions.Center;
                    _text.enableAutoSizing = false;
                    _text.textWrappingMode = TextWrappingModes.NoWrap;
                    _text.overflowMode = TextOverflowModes.Overflow;
                    _text.raycastTarget = false;
                    _text.fontSize = PluginConfig.FontSize.Value;
                }
            }

            gameObject.SetActive(false);
        }

        public void Show(Cooldown cd)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            bool infinite = float.IsInfinity(cd.Remaining);

            if (_sweep != null)
            {
                bool sweep = PluginConfig.Sweep.Value && !infinite && cd.Total > 0f;
                _sweep.enabled = sweep;
                if (sweep)
                {
                    _sweep.fillAmount = Mathf.Clamp01(cd.Remaining / cd.Total);
                    _sweep.color = PluginConfig.SweepColor.Value;
                }
            }

            if (_text != null)
            {
                if (infinite)
                    _text.text = "";
                else
                {
                    _text.text = TimeText.Format(cd.Remaining);
                    _text.color = TimeText.ColorFor(cd.Remaining);
                    _text.fontSize = PluginConfig.FontSize.Value;
                }
            }

            // With no end there is neither sweep nor countdown, so the tint is the only signal.
            if ((PluginConfig.DimIcon.Value || infinite) && _icon != null)
            {
                _icon.color = PluginConfig.DimColor.Value;
                _dimmed = true;
            }
            else if (_dimmed)
                Undim();
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            if (_dimmed)
                Undim();
        }

        private void Undim()
        {
            _dimmed = false;
            if (_icon != null)
                _icon.color = Color.white;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        /// <summary>A filled Image needs a sprite; with none it draws a plain quad and ignores the fill.</summary>
        private static Sprite WhiteSprite()
        {
            if (_white != null)
                return _white;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[16];
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            _white = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            _white.hideFlags = HideFlags.HideAndDontSave;
            return _white;
        }
    }
}
