using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Items;

namespace KingdomLedger
{
    internal static partial class LedgerObserver
    {
        static void FinishEnchanting(Scope scope,Batch batch,InventoryItem[] outputs,bool first)
        {
         if(batch.Receipts==null)return;
         // Update can finish this job and pay for its next job in the same call.
         // scope.Spent belongs to that next job; only StartEnchanting attributes it.
         var completionSpend=new Dictionary<string,decimal>();
         foreach(var entry in batch.Receipts.Values)
         {
          if(!entry.ledger.TryGetTarget(out var ledger))continue;
          ledger.Endpoint(Stage(scope.Element),completionSpend,Math.Max(1,outputs.Length));
          if(scope.Earned.Count>0)ledger.Sale(entry.trace,scope.Earned,false);
         }
         foreach(var output in outputs)
         {
          var source=new Tickets();
          foreach(var entry in batch.Receipts.Values)
          {
           // Native enchantments return their original equipment; material recipes
           // create new objects. Carry receipts in both cases, splitting the paid
           // basis across a batch and assigning no duplicate cost to productivity.
           var trace=first?entry.trace.Copy(1m/outputs.Length):new CostTrace();
           trace.Stage(Stage(scope.Element),completionSpend,1);
           source.Values.Add((entry.ledger,trace));
          }
          tickets.Remove(output);tickets.Add(output,source);
         }
        }
    }
}
