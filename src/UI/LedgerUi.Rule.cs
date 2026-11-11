using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed partial class LedgerUi
    {
        internal void Rule(Transform parent,float x,float y,float width) => Fill(parent,"Rule",x,y,width,1,new Color(.68f,.51f,.32f,.4f));
    }
}
