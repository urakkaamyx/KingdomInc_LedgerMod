using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed class LedgerUi
    {
        internal static readonly Color Paper = new Color(1, .94f, .79f);
        internal static readonly Color Muted = new Color(.72f, .67f, .57f);
        internal static readonly Color Gold = new Color(.94f, .73f, .38f);
        internal static readonly Color Green = new Color(.62f, .85f, .57f);
        internal static readonly Color Red = new Color(.96f, .52f, .44f);
        internal static readonly Color Blue = new Color(.57f, .75f, .91f);
        internal readonly TMP_FontAsset Font;
        readonly Sprite[] sprites;
        internal LedgerUi(TMP_FontAsset font) { Font = font; sprites = Resources.FindObjectsOfTypeAll<Sprite>(); }
        internal Sprite Sprite(string name) => sprites.FirstOrDefault(s => s.name == name);
        internal static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        internal Image Box(Transform parent, string name, float x, float y, float width, float height, string sprite)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = Sprite(sprite); image.type = Image.Type.Sliced;
            image.color = image.sprite ? Color.white : new Color(.2f, .15f, .13f); return image;
        }
        internal Image Fill(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var image = Rect(parent,name,x,y,width,height).gameObject.AddComponent<Image>();
            image.color=color; image.raycastTarget=false; return image;
        }
        internal Image Icon(Transform parent, Sprite sprite, float x, float y, float size)
        {
            var image=Rect(parent,"Icon",x,y,size,size).gameObject.AddComponent<Image>();
            image.sprite=sprite; image.preserveAspect=true; image.raycastTarget=false; image.enabled=sprite; return image;
        }
        internal void Rule(Transform parent,float x,float y,float width) => Fill(parent,"Rule",x,y,width,1,new Color(.68f,.51f,.32f,.4f));
        internal void Select(Button button,bool selected)
        {
            ((Image)button.targetGraphic).sprite=Sprite(selected?"Button02":"Button03");
            button.GetComponentInChildren<TMP_Text>().color=selected?Gold:Paper;
        }
        internal TMP_Text Text(Transform parent, string text, float x, float y, float width, float height, float size)
        {
            var label = Rect(parent, text, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font; label.fontSize = size; label.color = Paper; label.text = text;
            label.raycastTarget = false; label.richText = false; label.textWrappingMode = TextWrappingModes.NoWrap;
            label.alignment = TextAlignmentOptions.MidlineLeft; label.overflowMode=TextOverflowModes.Ellipsis; return label;
        }
        internal Button Button(Transform parent, string text, float x, float y, float width, float height, Action action)
        {
            var image = Box(parent, text + "Button", x, y, width, height, "Button03"); image.pixelsPerUnitMultiplier = 1.5f;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, .94f, .76f);
            colors.pressedColor = new Color(.7f, .65f, .5f); colors.disabledColor = new Color(.5f, .5f, .5f, .65f); colors.fadeDuration=.08f; button.colors = colors;
            var label = Text(image.transform, text, 0, 0, width, height, 18); label.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => action()); return button;
        }
    }

    // UI mesh icon: coarse square pixels avoid relying on a font's gear glyph.
    public sealed class LedgerGearGraphic : MaskableGraphic
    {
        static readonly string[] Pixels = {
            "0000001111000000", "0000001111000000", "0011001111001100", "0011111111111100",
            "0001111111111000", "0001111001111000", "1111110000111111", "1111100000011111",
            "1111100000011111", "1111110000111111", "0001111001111000", "0001111111111000",
            "0011111111111100", "0011001111001100", "0000001111000000", "0000001111000000"
        };
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); var rect = rectTransform.rect; float dx = rect.width / 16, dy = rect.height / 16;
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                if (Pixels[y][x] != '1') continue;
                int index = helper.currentVertCount;
                float left = rect.xMin + x * dx, top = rect.yMax - y * dy;
                helper.AddVert(new Vector3(left, top), color, Vector2.zero);
                helper.AddVert(new Vector3(left + dx, top), color, Vector2.zero);
                helper.AddVert(new Vector3(left + dx, top - dy), color, Vector2.zero);
                helper.AddVert(new Vector3(left, top - dy), color, Vector2.zero);
                helper.AddTriangle(index, index + 1, index + 2); helper.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
