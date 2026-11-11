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
        sealed partial class MoneyCard
        {
            readonly TMP_Text value,detail,title;

            internal MoneyCard(LedgerUi ui,Transform parent,string name,float x)

            {

             var card=ui.Box(parent,name+"Card",x,0,266,104,"DefaultBorderDark");

             title=ui.Text(card.transform,name.ToUpperInvariant(),16,10,234,22,14);title.color=LedgerUi.Muted;

             value=ui.Text(card.transform,"",16,34,234,38,25);detail=ui.Text(card.transform,"",16,76,234,18,13);detail.color=LedgerUi.Muted;

            }
        }
    }
}
