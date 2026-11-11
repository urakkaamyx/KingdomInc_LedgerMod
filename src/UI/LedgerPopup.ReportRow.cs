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
        sealed partial class ReportRow
        {
            internal RectTransform Root;internal Image Icon,Background;internal TMP_Text Name,Detail,Amount,Cost;internal TooltipSource Tooltip;

            internal ReportRow(LedgerUi ui,Transform parent,int index)

            {

             Root=LedgerUi.Rect(parent,"ReportRow"+index,0,index*44,ReportWidth,44);

             Background=ui.Fill(Root,"RowShade",0,0,ReportWidth,43,new Color(0,0,0,index%2==0?.15f:.04f));

             // This surface owns an optional tooltip; the rest of the row remains a scroll target.

             Background.raycastTarget=true;Tooltip=Background.gameObject.AddComponent<TooltipSource>();

             Icon=ui.Icon(Root,null,12,7,30);

             Name=ui.Text(Root,"",52,2,412,25,18);

             Detail=ui.Text(Root,"",52,25,412,17,12);Detail.color=LedgerUi.Muted;

             Amount=ui.Text(Root,"",466,0,100,44,18);Amount.alignment=TextAlignmentOptions.MidlineRight;

             Cost=ui.Text(Root,"",580,0,202,44,17);Cost.alignment=TextAlignmentOptions.MidlineRight;Cost.color=LedgerUi.Gold;

            }
        }
    }
}
