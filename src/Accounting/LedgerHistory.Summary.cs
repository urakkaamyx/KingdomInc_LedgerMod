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
        internal LedgerSummary Summary(int seconds)
        {
         Current();long first=(long)Math.Floor(Clock())-seconds+1;var result=new LedgerSummary();
         foreach(var bucket in buckets.Where(b=>b.Second>=first))
         {
          var v=bucket.Value;foreach(var p in v.Items)CostTrace.Add(result.Items,p.Key,p.Value);
          foreach(var p in v.Revenue)CostTrace.Add(result.Revenue,p.Key,p.Value);
          foreach(var p in v.SoldCosts)CostTrace.Add(result.SoldCosts,p.Key,p.Value);
          result.Costs.Merge(v.Costs);result.Sold+=v.Sold;result.UnknownPassed+=v.UnknownPassed;result.UnknownSold+=v.UnknownSold;result.SaleSeen|=v.SaleSeen;
         }
         return result;
        }
    }
}
