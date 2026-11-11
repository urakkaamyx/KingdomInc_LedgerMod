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
        const float Width=880,Height=732,ReportWidth=798,ReportHeight=258;

        Ledger ledger;LedgerWorld owner;LedgerUi ui;RectTransform panel,content,details,financePanel,counters;

        TMP_Text financeEmpty,unitsColumn;float reportY,currentHeight=Height;

        readonly List<FinanceRow> financeRows=new List<FinanceRow>();

        TMP_Text count,subtitle,activity,status,notice,firstColumn,lastColumn;

        GameObject historyHelp;

        Button minus,plus;Button[] periods,tabs;

        readonly List<ReportRow> rows=new List<ReportRow>();

        MoneyCard earned,spent,profit;

        Canvas referenceCanvas;CanvasScaler scaler;ScrollRect scroll;

        bool restoreInput;int openedFrame,tab=1;float refresh;

        internal bool IsOpen=>gameObject.activeSelf;



        // Reuse visible rows while the live ledger updates. Scrolling and pointer

        // targets stay stable; a busy chain does not rebuild a UI tree every frame.













































































        void RefreshFinance(LedgerSummary summary,bool money)

        {

         var currencies=summary.Currencies;financePanel.gameObject.SetActive(money);counters.gameObject.SetActive(!money);

         float h=money?Math.Max(104,44+currencies.Length*36):104;financePanel.sizeDelta=new Vector2(824,h);

         currentHeight=Height+h-104;panel.sizeDelta=new Vector2(Width,currentHeight);details.anchoredPosition=new Vector2(0,-270-(h-104));

         financeEmpty.gameObject.SetActive(currencies.Length==0);

         for(int i=0;i<currencies.Length;i++)

         {

          while(financeRows.Count<=i)financeRows.Add(new FinanceRow(ui,financePanel,financeRows.Count));

          string currency=currencies[i];var row=financeRows[i];row.Root.name=currency;row.Root.gameObject.SetActive(true);

          row.Name.text=LedgerHistory.CurrencyName(currency);if(Enum.TryParse(currency,out Currency type)){row.Icon.sprite=type.GetIcon();row.Icon.enabled=true;}

          summary.Revenue.TryGetValue(currency,out var revenue);summary.Costs.Money.TryGetValue(currency,out var cost);decimal net=revenue-cost;

          row.Revenue.Show(currency,revenue,revenue<0?LedgerUi.Red:LedgerUi.Paper,true);row.Cost.Show(currency,-cost,cost>0?LedgerUi.Red:LedgerUi.Gold,true);

          row.Net.Show(currency,net,summary.ProfitComplete?(net<0?LedgerUi.Red:net>0?LedgerUi.Green:LedgerUi.Muted):LedgerUi.Muted,true,!summary.ProfitComplete);

          row.Tip.SetTooltip(LedgerHistory.CurrencyName(currency),"Revenue: "+LedgerHistory.Amount(currency,revenue,true)+"\nOperating costs: "+LedgerHistory.Amount(currency,-cost,true)+"\nNet: "+(summary.ProfitComplete?LedgerHistory.Amount(currency,net,true):"Unknown: incomplete upstream history"));

          SetLayer(row.Root,gameObject.layer);

         }

         for(int i=currencies.Length;i<financeRows.Count;i++)financeRows[i].Root.gameObject.SetActive(false);

        }
    }
}
