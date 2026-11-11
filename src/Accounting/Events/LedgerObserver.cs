using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Items;

namespace KingdomLedger
{
    internal static partial class LedgerObserver
    {
        static void StartEnchanting(Scope scope,Batch batch)
        {
         var sources=new Tickets();
         // A recipe may consume several inputs from the same Ledger or from chained
         // Ledgers. Record its actual payment once per distinct report, never per input.
         foreach(var item in scope.Consumed.Distinct())
         {
          if(!tickets.TryGetValue(item,out var receipt))continue;
          foreach(var entry in receipt.Values)
          {
           if(!entry.ledger.TryGetTarget(out var ledger))continue;
           var existing=sources.Values.Find(v=>v.ledger.TryGetTarget(out var h)&&ReferenceEquals(h,ledger));
           if(existing.trace!=null)existing.trace.Merge(entry.trace);
           else sources.Values.Add((new WeakReference<LedgerHistory>(ledger),entry.trace.Copy()));
          }
          tickets.Remove(item);
         }
         if(sources.Values.Count==0)return;
         batch.Receipts=sources;
         foreach(var entry in sources.Values)
         {
          entry.trace.Stage(Stage(scope.Element),scope.Spent,0);
          if(entry.ledger.TryGetTarget(out var ledger))ledger.Endpoint(Stage(scope.Element),scope.Spent,0);
         }
        }
    }
}
