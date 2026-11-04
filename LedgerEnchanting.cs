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
