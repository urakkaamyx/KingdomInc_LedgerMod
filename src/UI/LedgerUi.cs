using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed class LedgerUi
    {
        internal Image Box(Transform parent, string name, float x, float y, float width, float height, string sprite)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = Sprite(sprite);
            image.type = Image.Type.Sliced;
            image.color = image.sprite ? Color.white : new Color(.2f, .15f, .13f);
            return image;
        }

        internal Button Button(Transform parent, string text, float x, float y, float width, float height, Action action)
        {
            var image = Box(parent, text + "Button", x, y, width, height, "Button03");
            image.pixelsPerUnitMultiplier = 1.5f;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1, .94f, .76f);
            colors.pressedColor = new Color(.7f, .65f, .5f);
            colors.disabledColor = new Color(.5f, .5f, .5f, .65f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            var label = Text(image.transform, text, 0, 0, width, height, 18);
            label.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => action());
            return button;
        }

        internal static readonly Color Paper = new Color(1, .94f, .79f);
        internal static readonly Color Muted = new Color(.72f, .67f, .57f);
        internal static readonly Color Gold = new Color(.94f, .73f, .38f);
        internal static readonly Color Green = new Color(.62f, .85f, .57f);
        internal static readonly Color Red = new Color(.96f, .52f, .44f);
        internal static readonly Color Blue = new Color(.57f, .75f, .91f);
        internal readonly TMP_FontAsset Font;
        readonly Sprite[] sprites;
        internal LedgerUi(TMP_FontAsset font)
        {
            Font = font;
            sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        }

        internal Image Fill(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal Image Icon(Transform parent, Sprite sprite, float x, float y, float size)
        {
            var image = Rect(parent, "Icon", x, y, size, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite;
            return image;
        }

        internal static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        internal void Rule(Transform parent, float x, float y, float width) => Fill(parent, "Rule", x, y, width, 1, new Color(.68f, .51f, .32f, .4f));
        internal void Select(Button button, bool selected)
        {
            ((Image)button.targetGraphic).sprite = Sprite(selected ? "Button02" : "Button03");
            button.GetComponentInChildren<TMP_Text>().color = selected ? Gold : Paper;
        }

        internal Sprite Sprite(string name) => sprites.FirstOrDefault(s => s.name == name);
        internal TMP_Text Text(Transform parent, string text, float x, float y, float width, float height, float size)
        {
            var label = Rect(parent, text, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font;
            label.fontSize = size;
            label.color = Paper;
            label.text = text;
            label.raycastTarget = false;
            label.richText = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }
    }
}
