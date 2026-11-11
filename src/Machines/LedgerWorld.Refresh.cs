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
        internal void Refresh()

        {

         if(!initialized)return;RefreshTimer();var summary=ledger.History.Summary(ledger.Period);var items=summary.Items.OrderBy(p=>LedgerHistory.ItemName(p.Key)).ToArray();

         int visibleRows=Math.Max(1,items.Length);EnsureItemRows(visibleRows);int itemBottom=Ledger.ItemTop+visibleRows*Ledger.RowHeight;

         string unit=ledger.Period==1?"second":ledger.Period==60?"minute":"hour";

         period.text="ITEMS PASSED / LAST "+unit.ToUpperInvariant();

         var currencies=summary.Currencies;bool money=currencies.Length>0;
         int extra=Math.Max(0,(currencies.Length+1)/2-1)*20;int height=Ledger.DisplayHeight(ledger.Inputs.Count,items.Length,currencies.Length);
         bool showContent=items.Length>0||height>=128;

         if(quantity)quantity.gameObject.SetActive(showContent);

         for(int i=0;i<itemRows.Count;i++)
         {
          var row=itemRows[i];bool showRow=items.Length>0?i<visibleRows:(showContent&&i==0);row.Root.gameObject.SetActive(showRow);bool exists=i<items.Length;row.Icon.enabled=exists;
          if(row.Shade)row.Shade.enabled=exists;
          row.Name.text=exists?LedgerHistory.ItemName(items[i].Key):showRow?"No items passed yet":"";
          row.Amount.text=exists?LedgerHistory.Number(items[i].Value):"";
          if(exists)row.Icon.sprite=ItemType.Get(items[i].Key)?.PreviewItem.Icon;
         }

         moneyStrip.gameObject.SetActive(money);throughput.gameObject.SetActive(!money&&showContent);

         throughput.text=LedgerHistory.Number(summary.Items.Values.Sum())+" items passed · "+summary.Items.Count+" types";

         moneyStrip.anchoredPosition=new Vector2(30,-itemBottom-4);throughput.rectTransform.anchoredPosition=new Vector2(32,-itemBottom);
         moneyStrip.sizeDelta=new Vector2(ContentWidth,38+extra);body.sizeDelta=new Vector2(Ledger.Width,height);

         var size=new Vector2Int(Ledger.Width,height);
         if(ledger.Size!=size||((RectTransform)transform).sizeDelta!=(Vector2)size)
         {
          ledger.Size=size;((RectTransform)transform).sizeDelta=size;
          if(ledger.World.Collider)ledger.World.Collider.UpdateSize();
          foreach(var io in ledger.IO)if(io.ConnectedPipe?.World)io.ConnectedPipe.World.UpdateLinePositions();
         }

         for(int i=0;i<currencies.Length;i++)

         {

          if(moneyRows.Count<=i)

          {

           var row=new NodeCurrencyRow();row.Root=LedgerUi.Rect(moneyStrip,"CurrencyRow"+i,(i%2)*CurrencyStride,17+(i/2)*20,CurrencyWidth,20);

           row.Icon=ui.Icon(row.Root,null,0,3,14);row.Net=ui.Text(row.Root,"",20,0,CurrencyWidth-20,20,12);row.Net.name="Net";row.Net.enableAutoSizing=true;row.Net.fontSizeMin=9;row.Net.fontSizeMax=12;

           var hover=ui.Fill(row.Root,"CurrencyHelp",0,0,CurrencyWidth,20,Color.clear);hover.raycastTarget=true;row.Tip=hover.gameObject.AddComponent<TooltipSource>();moneyRows.Add(row);

          }

          string currency=currencies[i];var r=moneyRows[i];r.Root.name=currency;r.Root.gameObject.SetActive(true);

          if(Enum.TryParse(currency,out Currency type)){r.Icon.sprite=type.GetIcon();r.Icon.enabled=true;}

          summary.Costs.Money.TryGetValue(currency,out var cost);summary.Revenue.TryGetValue(currency,out var revenue);decimal net=revenue-cost;

          r.Net.text=(summary.ProfitComplete?"":"~")+LedgerHistory.Amount(currency,net,true);r.Net.color=net<0?LedgerUi.Red:net>0?LedgerUi.Green:LedgerUi.Muted;

          r.Tip.SetTooltip(LedgerHistory.CurrencyName(currency),"Operating costs: "+LedgerHistory.Amount(currency,-cost,true)+"\nRevenue: "+LedgerHistory.Amount(currency,revenue,true)+"\nProfit / loss: "+r.Net.text+(summary.ProfitComplete?"":"\n~ Estimated from known income and costs; upstream cost history is incomplete."));

         }

         for(int i=currencies.Length;i<moneyRows.Count;i++)moneyRows[i].Root.gameObject.SetActive(false);

        }
    }
}
