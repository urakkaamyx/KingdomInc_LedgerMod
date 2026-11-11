using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed partial class LedgerUi
    {
        internal Image Fill(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var image = Rect(parent,name,x,y,width,height).gameObject.AddComponent<Image>();
            image.color=color; image.raycastTarget=false; return image;
        }
    }
}
