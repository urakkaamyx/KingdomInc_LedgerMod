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
        internal static string Net(Dictionary<string,decimal> revenue,Dictionary<string,decimal> cost)
        {
         var net=new Dictionary<string,decimal>(revenue);foreach(var p in cost)CostTrace.Add(net,p.Key,-p.Value);
         return Money(net);
        }
    }
}
