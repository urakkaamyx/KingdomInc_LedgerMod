using System;

using System.Collections.Generic;

using System.Linq;

using Assets.Behaviour.Flow;

using Assets.Behaviour.UI;

using Assets.Behaviour.UI.Shared;

using Assets.Source.Items;

using Assets.Source.Player;

using Assets.Source.Util;

using Behaviour.UI.Tooltip;

using TMPro;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.InputSystem;

using UnityEngine.UI;

using GameInput=Assets.Behaviour.Input.PlayerInput;

namespace KingdomLedger
{
    public sealed partial class LedgerPopup:UIPopupMenu
    {
        sealed partial class FinanceRow
        {
            internal RectTransform Root;internal TMP_Text Name;internal Image Icon;internal LedgerMoneyValue Revenue,Cost,Net;internal TooltipSource Tip;

            internal FinanceRow(LedgerUi ui,Transform parent,int index)

            {

             Root=LedgerUi.Rect(parent,"CurrencyRow"+index,0,36+index*36,824,36);

             var shade=ui.Fill(Root,"Shade",8,0,808,35,new Color(0,0,0,index%2==0?.13f:.03f));shade.raycastTarget=true;Tip=shade.gameObject.AddComponent<TooltipSource>();

             Icon=ui.Icon(Root,null,14,8,20);Name=ui.Text(Root,"",42,0,136,36,16);

             Revenue=new LedgerMoneyValue(ui,Root,"Revenue",184,0,196,36);Cost=new LedgerMoneyValue(ui,Root,"Cost",389,0,196,36);Net=new LedgerMoneyValue(ui,Root,"Net",594,0,214,36);

            }
        }
    }
}
