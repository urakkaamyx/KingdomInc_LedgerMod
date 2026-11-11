using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed partial class LedgerUi
    {
        internal void Select(Button button,bool selected)
        {
            ((Image)button.targetGraphic).sprite=Sprite(selected?"Button02":"Button03");
            button.GetComponentInChildren<TMP_Text>().color=selected?Gold:Paper;
        }
    }
}
