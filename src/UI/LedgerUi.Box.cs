using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed partial class LedgerUi
    {
        internal Image Box(Transform parent, string name, float x, float y, float width, float height, string sprite)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = Sprite(sprite); image.type = Image.Type.Sliced;
            image.color = image.sprite ? Color.white : new Color(.2f, .15f, .13f); return image;
        }
    }
}
