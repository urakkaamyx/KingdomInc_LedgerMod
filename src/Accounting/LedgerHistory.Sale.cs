using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomLedger
{
    internal sealed partial class LedgerHistory
    {
        internal void Sale(CostTrace trace,IDictionary<string,decimal> revenue,bool first=true)
        {
         var value=Current().Value;if(first)value.Sold++;value.SaleSeen=true;
         if(!trace.Complete)value.UnknownSold++;
         if(first)foreach(var p in trace.Money)CostTrace.Add(value.SoldCosts,p.Key,p.Value);
         foreach(var p in revenue)CostTrace.Add(value.Revenue,p.Key,p.Value);
        }
    }
}
