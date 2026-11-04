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
  internal sealed class Batch {internal CostTrace Cost;internal bool Charged;internal Tickets Receipts;}
  sealed class OutfitBatch {internal InventoryItem Human;internal CostTrace Cost;}
  internal sealed class Tickets {internal readonly List<(WeakReference<LedgerHistory> ledger,CostTrace trace)> Values=new List<(WeakReference<LedgerHistory>,CostTrace)>();}
  internal sealed class Scope
  {
   internal Scope Parent; internal FlowElement Element;internal string Method;
   internal HashSet<InventoryItem> Outputs;
   internal CraftingInput Input;
   internal InventoryItem Human,Equipment;
   internal int Queued;
   internal Dictionary<string,decimal> IncomeBefore;internal int StoredValue;internal float Delta,PassiveTimer;
   internal readonly List<InventoryItem> Consumed=new List<InventoryItem>();
   internal readonly Dictionary<string,decimal> Spent=new Dictionary<string,decimal>(),Earned=new Dictionary<string,decimal>();
  }
  static ConditionalWeakTable<InventoryItem,CostTrace> traces=new ConditionalWeakTable<InventoryItem,CostTrace>();
  static ConditionalWeakTable<CraftingInput,Batch> batches=new ConditionalWeakTable<CraftingInput,Batch>();
  static ConditionalWeakTable<Outfitter,OutfitBatch> outfits=new ConditionalWeakTable<Outfitter,OutfitBatch>();
  static ConditionalWeakTable<FlowElement,Queue<CostTrace>> queued=new ConditionalWeakTable<FlowElement,Queue<CostTrace>>();
  static ConditionalWeakTable<InventoryItem,Tickets> tickets=new ConditionalWeakTable<InventoryItem,Tickets>();
  [ThreadStatic] static Scope current;
  static GamePlayer player;
  static readonly HashSet<string> Routers=new HashSet<string>{"Collector","Distributor","Manifold","Filter","TrackSwitch","Stockpile","Inspector","SingleButton","Timer","Accountant","Appraiser","Portal"};
  internal static bool IsRoutingStage(string stage)=>Routers.Contains(stage.Split(new[]{" / "},StringSplitOptions.None)[0])||stage==Ledger.TypeId;
  internal static void Reset()
  {
   player=GamePlayer.Current;traces=new ConditionalWeakTable<InventoryItem,CostTrace>();batches=new ConditionalWeakTable<CraftingInput,Batch>();outfits=new ConditionalWeakTable<Outfitter,OutfitBatch>();
   queued=new ConditionalWeakTable<FlowElement,Queue<CostTrace>>();tickets=new ConditionalWeakTable<InventoryItem,Tickets>();current=null;ResetIncome();
  }
  internal static CostTrace Trace(InventoryItem item)=>traces.TryGetValue(item,out var t)?t:new CostTrace{Complete=false};
  internal static void SetTrace(InventoryItem item,CostTrace trace){traces.Remove(item);traces.Add(item,trace);}
  internal static void Ticket(InventoryItem item,LedgerHistory ledger,CostTrace trace)
  {
   var t=tickets.GetOrCreateValue(item);
   // Chained Ledgers are each credited once when the actual destination sells.
   if(!t.Values.Any(v=>v.ledger.TryGetTarget(out var h)&&ReferenceEquals(h,ledger)))t.Values.Add((new WeakReference<LedgerHistory>(ledger),trace.Copy()));
  }
  internal static void Install(Harmony harmony)
  {
   var methods=typeof(FlowElement).Assembly.GetTypes().Where(t=>typeof(FlowElement).IsAssignableFrom(t))
    .SelectMany(t=>t.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
    .Where(m=>!m.IsAbstract&&(m.Name=="Update"||m.Name=="DoManualCraft"||m.Name=="QueueCraft")).Distinct();
   foreach(var method in methods)harmony.Patch(method,prefix:Patch(nameof(Enter)),finalizer:Patch(nameof(Exit)));
   harmony.Patch(AccessTools.Method(typeof(FlowIO),"ConsumeItem",Type.EmptyTypes),postfix:Patch(nameof(Consumed)));
   harmony.Patch(AccessTools.Method(typeof(GamePlayer),"SpendCurrency",new[]{typeof(Currency),typeof(long)}),postfix:Patch(nameof(Spent)));
   harmony.Patch(AccessTools.Method(typeof(GamePlayer),"AddCurrency",new[]{typeof(Currency),typeof(long),typeof(bool)}),postfix:Patch(nameof(Earned)));
  }
  static HarmonyMethod Patch(string name)=>new HarmonyMethod(typeof(LedgerObserver).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic));
  static void Enter(FlowElement __instance,MethodBase __originalMethod,object[] __args,out Scope __state)
  {
   __state=null;
   try
   {
    if(GamePlayer.Current==null)return;
    if(!ReferenceEquals(player,GamePlayer.Current))Reset();
    string method=__originalMethod.Name;
    if(Routers.Contains(__instance.ElementType)||(current?.Element==__instance&&method!="QueueCraft"))return;
    __state=new Scope{Parent=current,Element=__instance,Method=method,
     Outputs=new HashSet<InventoryItem>(__instance.Outputs.SelectMany(o=>o.BufferedItems)),
     Input=(__instance as RecipeElement)?.Input,Human=(__instance as Outfitter)?.CurrentHuman,Equipment=(__instance as Outfitter)?.CurrentEquip,
     Queued=QueueCount(__instance),Delta=method=="Update"&&__args.Length>0&&__args[0] is float delta?delta:0};
    CaptureIncome(__state);
    current=__state;
   }
   catch(Exception e){LedgerPlugin.Warn("Observer start: "+e.Message);}
  }
  internal static string Stage(FlowElement element)
  {
   if(element is RecipeElement r&&r.Recipe!=null)return element.ElementType+" / "+Translation.Translate(r.Recipe.DisplayName);
   if(element is ResourceElement s&&s.Resource!=null)return element.ElementType+" / "+Translation.Translate(s.Resource.PreviewItem.DisplayName);
   if(element is Forest f&&f.SelectedResource!=null)return "Forest / "+Translation.Translate(f.SelectedResource.PreviewItem.DisplayName);
   if(IsIncomeReceiver(element))return element.ElementType+" / "+Translation.Translate(element.Label);
   return element.ElementType;
  }
  static int QueueCount(FlowElement e)=>e is ResourceElement r?r.QueuedCount:e is Forest f?f.QueuedCount:0;
  static Exception Exit(Exception __exception,Scope __state)
  {
   if(__state==null)return __exception;
   try{if(__exception==null)Finish(__state);}
   catch(Exception e){LedgerPlugin.Warn("Observer finish: "+e.Message);}
   finally{current=__state.Parent;}
   return __exception;
  }
  static CostTrace Inputs(IEnumerable<InventoryItem> items)
  {
   var result=new CostTrace();foreach(var item in items)result.Merge(Trace(item));return result;
  }
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
  static void Consumed(FlowIO __instance,InventoryItem __result)
  {
   if(current!=null&&__instance is FlowInput&&__instance.Parent==current.Element&&__result!=null)current.Consumed.Add(__result);
  }
  static void Spent(Currency __0,long __1,bool __result){if(current!=null&&__result&&__1>0)CostTrace.Add(current.Spent,__0.ToString(),__1);}
  static void Earned(Currency __0,long __1,bool __2){if(current!=null&&__2&&__1>0)CostTrace.Add(current.Earned,__0.ToString(),__1);}
 }
}
