using UnityEngine;
using UnityEngine.UI;

namespace LandNav
{
    /// <summary>Builds small world-space panels (diegetic signs) at runtime. No screen-space UI is used.</summary>
    public static class WorldUI
    {
        static Font _font;
        public static Font DefaultFont
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public class Panel
        {
            public GameObject root;
            public Image background;
            public Text title;
            public Text body;
        }

        /// <summary>
        /// Create a panel of the given physical size (metres). Canvas units are millimetres so text sizes read naturally.
        /// </summary>
        public static Panel CreatePanel(string name, Transform parent, Vector2 sizeMetres, Color bg, int titleSize = 44, int bodySize = 30)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f; // sharper text in VR
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizeMetres * 1000f;
            rt.localScale = Vector3.one * 0.001f;

            var panel = new Panel { root = go };

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(go.transform, false);
            Stretch((RectTransform)bgGo.transform, 0);
            panel.background = bgGo.GetComponent<Image>();
            panel.background.color = bg;

            panel.title = MakeText("Title", go.transform, titleSize, FontStyle.Bold, TextAnchor.UpperLeft);
            var trt = (RectTransform)panel.title.transform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(-60, titleSize * 1.6f);
            trt.anchoredPosition = new Vector2(0, -24);

            panel.body = MakeText("Body", go.transform, bodySize, FontStyle.Normal, TextAnchor.UpperLeft);
            var brt = (RectTransform)panel.body.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(30, 24);
            brt.offsetMax = new Vector2(-30, -(titleSize * 1.6f + 36));
            return panel;
        }

        static Text MakeText(string name, Transform parent, int size, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = DefaultFont;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }

        static void Stretch(RectTransform rt, float pad)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }

        public static Color ToColor(FlagColour c) => c switch
        {
            FlagColour.Blue => new Color(0.12f, 0.37f, 0.84f),
            FlagColour.Red => new Color(0.84f, 0.13f, 0.13f),
            FlagColour.Yellow => new Color(0.95f, 0.76f, 0.0f),
            FlagColour.Green => new Color(0.09f, 0.61f, 0.23f),
            _ => Color.grey
        };

        public static string Hex(FlagColour c) => "#" + ColorUtility.ToHtmlStringRGB(ToColor(c));
    }

    /// <summary>Short procedurally generated sounds, so no audio assets are needed (and none are harsh).</summary>
    public static class Tones
    {
        static AudioClip _correct, _incorrect, _plant, _select, _complete;

        public static AudioClip Correct => _correct ??= Make("correct", new[] { 523.25f, 659.25f, 783.99f }, 0.13f, 0.35f);
        public static AudioClip Incorrect => _incorrect ??= Make("incorrect", new[] { 392f, 329.63f }, 0.18f, 0.25f);
        public static AudioClip Plant => _plant ??= Thud();
        public static AudioClip Select => _select ??= Make("select", new[] { 880f }, 0.05f, 0.15f);
        public static AudioClip Complete => _complete ??= Make("complete", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.16f, 0.35f);

        static AudioClip Make(string name, float[] notes, float noteLen, float volume)
        {
            const int rate = 44100;
            int per = (int)(rate * noteLen);
            int tail = (int)(rate * 0.25f);
            var data = new float[per * notes.Length + tail];
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < per + tail && n * per + i < data.Length; i++)
                {
                    float t = i / (float)rate;
                    float env = Mathf.Min(1f, t * 200f) * Mathf.Exp(-t * 6f);
                    data[n * per + i] += volume * env * (Mathf.Sin(2 * Mathf.PI * notes[n] * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * notes[n] * t));
                }
            }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Thud()
        {
            const int rate = 44100;
            var data = new float[(int)(rate * 0.25f)];
            var rng = new System.Random(3);
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 28f);
                data[i] = 0.45f * env * (Mathf.Sin(2 * Mathf.PI * 90f * t) + 0.35f * (float)(rng.NextDouble() * 2 - 1));
            }
            var clip = AudioClip.Create("plant", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
