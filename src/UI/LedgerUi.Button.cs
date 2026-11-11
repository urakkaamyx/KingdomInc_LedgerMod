using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed partial class LedgerUi
    {
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
}
