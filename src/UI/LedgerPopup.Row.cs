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
        ReportRow Row(int index,string name,string detail,string amount,string cost,Sprite icon=null,Color? color=null,string help=null,float height=44)

        {

         while(rows.Count<=index)rows.Add(new ReportRow(ui,content,rows.Count));

         var row=rows[index];row.Root.anchoredPosition=new Vector2(0,-reportY);row.Root.sizeDelta=new Vector2(ReportWidth,height);reportY+=height;row.Background.rectTransform.sizeDelta=new Vector2(ReportWidth,height-1);row.Cost.rectTransform.sizeDelta=new Vector2(202,height-8);row.Cost.rectTransform.anchoredPosition=new Vector2(580,-4);row.Cost.alignment=TextAlignmentOptions.TopRight;row.Cost.fontSize=16;row.Root.gameObject.SetActive(true);row.Name.text=name;row.Detail.text=detail;

         row.Name.rectTransform.anchoredPosition=new Vector2(icon?52:16,detail.Length>0?-2:-9);

         row.Detail.rectTransform.anchoredPosition=new Vector2(icon?52:16,-25);

         row.Name.color=color??LedgerUi.Paper;row.Detail.color=LedgerUi.Muted;row.Amount.text=amount;row.Cost.text=cost;row.Cost.color=LedgerUi.Red;

         row.Icon.sprite=icon;row.Icon.enabled=icon;

         row.Tooltip.SetTooltip(name,help??(string.IsNullOrEmpty(detail)?name:detail));

         SetLayer(row.Root,gameObject.layer);return row;

        }
    }
}
