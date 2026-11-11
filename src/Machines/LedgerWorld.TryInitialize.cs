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
        bool TryInitialize()

        {

         if(initialized)return true;

         var world=GetComponent<WorldFlowElement>();var element=world?world.Element as Ledger:null;

         if(element==null)return false;

         var font=LedgerPlugin.Field<TMP_Text>(world,"_label")?.font;if(!font)return false;

         ledger=element;ui=new LedgerUi(font);

         body=LedgerUi.Rect(transform,"LedgerDisplay",0,0,Ledger.Width,Ledger.Height);

         var gear=ui.Button(body,"",Ledger.Width-42,0,30,30,OpenSettings);gear.name="LedgerGear";

         var cog=LedgerUi.Rect(gear.transform,"Cog",5,5,20,20).gameObject.AddComponent<LedgerGearGraphic>();cog.color=LedgerUi.Paper;cog.raycastTarget=false;

         gear.gameObject.AddComponent<TooltipSource>().SetTooltip("Ledger settings","Reporting window, input/output pairs, entire chain costs and currency profit.");

         period=ui.Text(body,"",32,35,Ledger.Width-130,24,13);period.color=LedgerUi.Muted;

         quantity=ui.Text(body,"QTY",Ledger.Width-100,35,66,24,12);quantity.color=LedgerUi.Muted;quantity.alignment=TextAlignmentOptions.MidlineRight;

         var timer=ui.Fill(body,"WindowTimer",30,Ledger.TimerTop,ContentWidth,4,new Color(.12f,.085f,.10f,.9f));timer.raycastTarget=true;
         timerTip=timer.gameObject.AddComponent<TooltipSource>();timerFill=ui.Fill(timer.transform,"Fill",0,0,0,4,LedgerUi.Gold).rectTransform;

         EnsureItemRows(1);

         moneyStrip=LedgerUi.Rect(body,"MonetaryTotals",30,216,ContentWidth,38);ui.Rule(moneyStrip,0,0,ContentWidth);

         var label=ui.Text(moneyStrip,"PROFIT / LOSS",0,3,ContentWidth,14,10);label.color=LedgerUi.Muted;

         throughput=ui.Text(body,"",32,Ledger.ItemTop+Ledger.RowHeight,ContentWidth-4,24,14);throughput.name="PassthroughTotal";throughput.color=LedgerUi.Muted;

         initialized=true;LayoutNativeBackdrops(world);Refresh();return true;

        }
    }
}
