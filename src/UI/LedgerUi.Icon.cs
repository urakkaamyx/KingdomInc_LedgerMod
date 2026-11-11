using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed partial class LedgerUi
    {
        internal Image Icon(Transform parent, Sprite sprite, float x, float y, float size)
        {
            var image=Rect(parent,"Icon",x,y,size,size).gameObject.AddComponent<Image>();
            image.sprite=sprite; image.preserveAspect=true; image.raycastTarget=false; image.enabled=sprite; return image;
        }
    }
}
