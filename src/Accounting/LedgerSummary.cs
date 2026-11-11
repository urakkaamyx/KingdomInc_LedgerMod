using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomLedger
{
    internal sealed partial class LedgerSummary
    {
        internal readonly Dictionary<string,decimal> Items=new Dictionary<string,decimal>(), Revenue=new Dictionary<string,decimal>(), SoldCosts=new Dictionary<string,decimal>();
        internal readonly CostTrace Costs=new CostTrace();
        internal long Sold,UnknownPassed,UnknownSold;
        internal bool SaleSeen;
        internal bool ProfitComplete=>UnknownPassed==0&&UnknownSold==0;
        internal bool HasMoneyActivity=>Costs.Money.Values.Any(v=>v!=0)||Revenue.Values.Any(v=>v!=0);
        internal string[] Currencies=>Costs.Money.Where(p=>p.Value!=0).Select(p=>p.Key).Union(Revenue.Where(p=>p.Value!=0).Select(p=>p.Key)).OrderBy(k=>k=="Gold"?0:1).ThenBy(k=>k).ToArray();
        internal string Profit=>!ProfitComplete?"Profit unavailable: missing cost history":LedgerHistory.Net(Revenue,Costs.Money);
    }
}
