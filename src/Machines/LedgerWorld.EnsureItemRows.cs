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
        void EnsureItemRows(int count)
        {
         while(itemRows.Count<count)
         {
          int i=itemRows.Count;var row=new ItemRow();row.Root=LedgerUi.Rect(body,"ItemRow"+i,30,Ledger.ItemTop+i*Ledger.RowHeight,ContentWidth,24);
          if(i%2==0)row.Shade=ui.Fill(row.Root,"Shade",0,0,ContentWidth,24,new Color(0,0,0,.12f));
          row.Icon=ui.Icon(row.Root,null,6,2,20);row.Icon.name="ItemIcon"+i;
          row.Name=ui.Text(row.Root,"",38,0,ContentWidth-124,24,17);
          row.Amount=ui.Text(row.Root,"",ContentWidth-82,0,72,24,17);row.Amount.alignment=TextAlignmentOptions.MidlineRight;row.Amount.color=LedgerUi.Gold;
          itemRows.Add(row);
         }
        }
    }
}
