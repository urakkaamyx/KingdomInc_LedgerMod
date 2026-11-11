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
        internal void Refresh()

        {

         var s=ledger.History.Summary(ledger.Period);count.text=ledger.Inputs.Count+(ledger.Inputs.Count==1?" pair":" pairs");

         minus.interactable=ledger.Inputs.Count>1;plus.interactable=ledger.Inputs.Count<5;

         var seconds=new[]{1,60,3600};for(int i=0;i<3;i++)ui.Select(periods[i],ledger.Period==seconds[i]);

         for(int i=0;i<2;i++)ui.Select(tabs[i],tab==i);

         string unit=ledger.Period==1?"second":ledger.Period==60?"minute":"hour";

         bool money=ledger.ShowFinancials(s);

         subtitle.text="Last "+unit;

         RefreshFinance(s,money);

         if(!money)

         {

          earned.Count("Items passed",LedgerHistory.Number(s.Items.Values.Sum()),"Last "+unit);

          spent.Count("Item types",s.Items.Count.ToString(),"Distinct items passed");

          profit.Count("Connected pairs",ledger.Outputs.Count(o=>o.ConnectedPipe!=null)+" / "+ledger.Outputs.Count,"Matched inputs / outputs");

         }

         activity.text=LedgerHistory.Number(s.Items.Values.Sum())+" items passed  ·  "+s.Items.Count+" types";

         historyHelp.SetActive(money);

         status.text=!money?"":s.UnknownPassed>0?"Partial cost history  ?":s.Items.Count==0?"Waiting for production":"Cost history complete";

         status.color=s.UnknownPassed>0?LedgerUi.Gold:LedgerUi.Muted;

         tabs[1].GetComponentInChildren<TMP_Text>().text=money?"Chain costs":"Chain items";

         firstColumn.text=tab==0?"ITEM":"PRODUCTION CHAIN";

         unitsColumn.text="UNITS";

         lastColumn.text=tab==0?"":money?"OBSERVED SPEND":"";

         // Reset presentation when pooled rows change between tabs.

         for(int i=0;i<rows.Count;i++){rows[i].Name.fontSize=18;rows[i].Background.color=new Color(0,0,0,i%2==0?.15f:.04f);}

         int n=0;reportY=0;

         if(tab==0)

         {

          foreach(var p in s.Items.OrderBy(p=>LedgerHistory.ItemName(p.Key)))Row(n++,LedgerHistory.ItemName(p.Key),"","× "+LedgerHistory.Number(p.Value),"",ItemType.Get(p.Key)?.PreviewItem.Icon);

          if(n==0)Row(n++,"No items passed yet","Items appear here as they move through Ledger.","","",ui.Sprite("Icons_149"),LedgerUi.Muted);

         }

         else if(tab==1)

         {

          var chain=LedgerChain.Read(ledger);var listed=new HashSet<string>();

          n=Heading(n,"PRODUCTION STAGES");

          foreach(var entry in chain.Stages){n=ChainStage(n,entry.Stage,s,money,entry);listed.Add(entry.Stage);}

          foreach(var key in s.Costs.Stages.Keys.Where(k=>!listed.Contains(k)).OrderBy(k=>k))n=ChainStage(n,key,s,money);

          var resources=s.Costs.Resources.Where(p=>p.Value>0).OrderBy(p=>LedgerHistory.ItemName(p.Key)).ToArray();

          if(resources.Length>0)n=Heading(n,"RAW RESOURCES");

          foreach(var resource in resources)Row(n++,LedgerHistory.ItemName(resource.Key),"",LedgerHistory.Number(resource.Value),"",ItemType.Get(resource.Key)?.PreviewItem.Icon);

          if(n==1)Row(n++,"No production observations yet","Recorded stages and resources will appear here.","","",null,LedgerUi.Muted);



         }

         for(int i=n;i<rows.Count;i++)rows[i].Root.gameObject.SetActive(false);

         content.sizeDelta=new Vector2(ReportWidth,Math.Max(ReportHeight,reportY));

        }
    }
}
