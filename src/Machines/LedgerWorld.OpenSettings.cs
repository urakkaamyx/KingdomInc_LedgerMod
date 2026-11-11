using System;

using System.Linq;

using System.Collections.Generic;

using Assets.Source.Player;

using Assets.Behaviour.Flow;

using Assets.Behaviour.UI;

using Assets.Source.Items;

using Behaviour.UI.Tooltip;

using TMPro;

using UnityEngine;

using UnityEngine.UI;

namespace KingdomLedger
{
    public sealed partial class LedgerWorld:MonoBehaviour
    {
        public void OpenSettings()

        {

         // Placement calls HandleReopenBuyMenu immediately after SetElement, before

         // Unity runs Start. Build once here, or retain the request until bound.

         if(!GameUI.Instance||!TryInitialize()){openWhenReady=true;return;}

         openWhenReady=false;

         if(!Settings)

         {

          var toolbar=HarmonyLib.AccessTools.Field(typeof(GameUI),"_editHotbar").GetValue(GameUI.Instance)as RectTransform;

          var canvas=toolbar?toolbar.GetComponentInParent<Canvas>()?.rootCanvas:GetComponentInParent<Canvas>()?.rootCanvas;

          var root=new GameObject("LedgerSettings",typeof(RectTransform));root.SetActive(false);

          try

          {

           var menu=root.AddComponent<LedgerPopup>();menu.Initialize(this,ledger,ui.Font,canvas?canvas.transform:null);

           Settings=menu;

          }

          catch{Destroy(root);throw;}

         }

         Settings.Open();

        }
    }
}
