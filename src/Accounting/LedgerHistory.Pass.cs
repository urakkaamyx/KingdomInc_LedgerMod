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
        internal void Pass(InventoryItem item,int lane,FlowElement destination)
        {
         var value=Current().Value;CostTrace.Add(value.Items,item.Type.Identifier,1);
         var trace=LedgerObserver.Trace(item);value.Costs.Merge(trace);if(!trace.Complete)value.UnknownPassed++;
         foreach(var reward in trace.Rewards)CostTrace.Add(value.Revenue,reward.Key,reward.Value);
         LedgerObserver.Ticket(item,this,trace);
        }
    }
}
