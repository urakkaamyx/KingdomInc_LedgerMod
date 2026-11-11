using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;

namespace KingdomLedger
{
    internal static partial class LedgerObserver
    {
        static void Finish(Scope s)
        {
         var e=s.Element;
         if((e is ResourceElement||e is Forest)&&s.Method=="QueueCraft")
         {
          if(QueueCount(e)>s.Queued)
          {
           var trace=new CostTrace();trace.Stage(Stage(e),s.Spent,0);
           queued.GetOrCreateValue(e).Enqueue(trace);
           if(s.Parent?.Element==e)s.Parent.Queued++;
          }
          return;
         }
         var outputs=e.Outputs.SelectMany(o=>o.BufferedItems).Where(i=>!s.Outputs.Contains(i)).Distinct().ToArray();
         if(FinishIncome(s))return;
         if(e is Outfitter outfitter)
         {
          // Outfitter consumes both inputs at the START of a job, then emits the
          // same human on a later update. Keep that job's provenance between frames.
          if(outfitter.CurrentHuman!=null&&!ReferenceEquals(outfitter.CurrentHuman,s.Human))
          {
           var trace=Inputs(s.Consumed);
           if(!s.Consumed.Contains(outfitter.CurrentHuman)||!s.Consumed.Contains(outfitter.CurrentEquip))trace.Complete=false;
           trace.Stage(Stage(e),s.Spent,0);outfits.Remove(outfitter);
           outfits.Add(outfitter,new OutfitBatch{Human=outfitter.CurrentHuman,Cost=trace});
          }
          foreach(var output in outputs)
          {
           CostTrace trace;
           if(outfits.TryGetValue(outfitter,out var job)&&ReferenceEquals(output,job.Human))trace=job.Cost.Copy();
           else if(ReferenceEquals(output,s.Human))trace=Inputs(new[]{s.Human,s.Equipment}.Where(i=>i!=null));
           else if(s.Consumed.Contains(output))trace=Inputs(s.Consumed);
           else{trace=Trace(output).Copy();trace.Complete=false;}
           trace.Stage(Stage(e),s.Spent,1);SetTrace(output,trace);
          }
          if(outfitter.CurrentHuman==null)outfits.Remove(outfitter);
          return;
         }
         if(e is RecipeElement recipe)
         {
          if(outputs.Length>0||(e is Enchanter&&s.Input!=null&&s.Earned.Count>0))
          {
           CostTrace trace;Batch batch=null;
           bool observed=s.Input!=null&&batches.TryGetValue(s.Input,out batch),first=observed&&!batch.Charged;
           if(observed)
           {
            trace=batch.Charged?new CostTrace():batch.Cost.Copy();batch.Charged=true;
           }
           else trace=Inputs(s.Input?.AllItems??Enumerable.Empty<InventoryItem>());
           if(s.Input==null||!batches.TryGetValue(s.Input,out _))trace.Complete=false;
           foreach(var reward in s.Earned)CostTrace.Add(trace.Rewards,reward.Key,reward.Value);
           trace.Stage(Stage(e),new Dictionary<string,decimal>(),outputs.Length);
           foreach(var output in outputs)SetTrace(output,trace.Copy(1m/outputs.Length));
           if(e is Enchanter&&observed)FinishEnchanting(s,batch,outputs,first);
          }
          if(recipe.Input!=null&&!ReferenceEquals(recipe.Input,s.Input))
          {
           var trace=Inputs(s.Consumed);trace.Stage(Stage(e),s.Spent,0);
           var next=new Batch{Cost=trace};
           if(e is Enchanter)StartEnchanting(s,next);
           batches.Remove(recipe.Input);batches.Add(recipe.Input,next);
          }
          return;
         }
         if((e is ResourceElement||e is Forest)&&outputs.Length>0)
         {
          var trace=new CostTrace();
          bool paidCycle=QueueCount(e)<s.Queued;
          if(paidCycle)
          {
           if(queued.TryGetValue(e,out var q)&&q.Count>0)trace.Merge(q.Dequeue());
           else trace.Complete=false;
          }
          // Productivity outputs have no additional ingredient/operating spend.
          // The paid output bears the paid resource ticket; no cost is duplicated.
          trace.Stage(Stage(e),new Dictionary<string,decimal>(),outputs.Length);
          foreach(var output in outputs){var unit=trace.Copy(1m/outputs.Length);CostTrace.Add(unit.Resources,output.Type.Identifier,1);SetTrace(output,unit);}
          return;
         }
         if(outputs.Length>0)
         {
          var trace=Inputs(s.Consumed);trace.Complete=false;trace.Stage(Stage(e),s.Spent,outputs.Length);
          foreach(var output in outputs)
          {
           // A delayed utility may return an existing object with valuable upstream
           // history. Preserve it even when this utility's extra costs are unknown.
           if(traces.TryGetValue(output,out var known))
           {
            var retained=known.Copy();retained.Complete=false;
            retained.Stage(Stage(e),s.Spent.ToDictionary(p=>p.Key,p=>p.Value/outputs.Length),1);SetTrace(output,retained);
           }
           else SetTrace(output,trace.Copy(1m/outputs.Length));
          }
         }
        }
    }
}
