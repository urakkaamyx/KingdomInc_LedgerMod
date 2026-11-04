using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomLedger
{
 internal sealed class CostTrace
 {
  internal bool Complete=true;
  internal readonly Dictionary<string,decimal> Money=new Dictionary<string,decimal>(), Rewards=new Dictionary<string,decimal>(), Resources=new Dictionary<string,decimal>(), Stages=new Dictionary<string,decimal>();
  internal readonly Dictionary<string,Dictionary<string,decimal>> StageMoney=new Dictionary<string,Dictionary<string,decimal>>();
  internal static void Add(Dictionary<string,decimal> d,string key,decimal amount){d.TryGetValue(key,out var old);d[key]=old+amount;}
  internal void Merge(CostTrace t,decimal scale=1)
  {
   if(t==null){Complete=false;return;} Complete &=t.Complete;
   foreach(var p in t.Money)Add(Money,p.Key,p.Value*scale);
   foreach(var p in t.Rewards)Add(Rewards,p.Key,p.Value*scale);
   foreach(var p in t.Resources)Add(Resources,p.Key,p.Value*scale);
   foreach(var p in t.Stages)Add(Stages,p.Key,p.Value*scale);
   foreach(var stage in t.StageMoney){if(!StageMoney.TryGetValue(stage.Key,out var values))StageMoney[stage.Key]=values=new Dictionary<string,decimal>();foreach(var p in stage.Value)Add(values,p.Key,p.Value*scale);}
  }
  internal CostTrace Copy(decimal scale=1){var result=new CostTrace();result.Merge(this,scale);return result;}
  internal void Stage(string name,Dictionary<string,decimal> spend,decimal quantity=1)
  {
   Add(Stages,name,quantity);if(!StageMoney.TryGetValue(name,out var row))StageMoney[name]=row=new Dictionary<string,decimal>();
   foreach(var p in spend){Add(Money,p.Key,p.Value);Add(row,p.Key,p.Value);}
  }
 }
 internal sealed class LedgerSummary
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
 internal sealed class LedgerHistory
 {
  // One aggregate per active second; no item references or unbounded event log.
  internal class LedgerBucket
  {
   internal long Second;
   internal readonly LedgerSummary Value=new LedgerSummary();
  }
  readonly Queue<LedgerBucket> buckets=new Queue<LedgerBucket>();
  LedgerBucket latest;
  internal static Func<double> Clock=()=>UnityEngine.Time.unscaledTimeAsDouble;
  LedgerBucket Current()
  {
   long second=(long)Math.Floor(Clock());
   while(buckets.Count>0&&buckets.Peek().Second<second-3600)buckets.Dequeue();
   if(buckets.Count==0||latest.Second!=second){latest=new LedgerBucket{Second=second};buckets.Enqueue(latest);}
   return latest;
  }
  internal void Pass(InventoryItem item,int lane,FlowElement destination)
  {
   var value=Current().Value;CostTrace.Add(value.Items,item.Type.Identifier,1);
   var trace=LedgerObserver.Trace(item);value.Costs.Merge(trace);if(!trace.Complete)value.UnknownPassed++;
   foreach(var reward in trace.Rewards)CostTrace.Add(value.Revenue,reward.Key,reward.Value);
   LedgerObserver.Ticket(item,this,trace);
  }
  internal void Sale(CostTrace trace,IDictionary<string,decimal> revenue,bool first=true)
  {
   var value=Current().Value;if(first)value.Sold++;value.SaleSeen=true;
   if(!trace.Complete)value.UnknownSold++;
   if(first)foreach(var p in trace.Money)CostTrace.Add(value.SoldCosts,p.Key,p.Value);
   foreach(var p in revenue)CostTrace.Add(value.Revenue,p.Key,p.Value);
  }
  internal void Endpoint(string stage,IDictionary<string,decimal> spend)
  {
   Current().Value.Costs.Stage(stage,new Dictionary<string,decimal>(spend),1);
  }
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
  internal static string Number(decimal n)=>n.ToString("#,0.##",CultureInfo.InvariantCulture);
  internal static string Gold(decimal amount)
  {
   var value=Math.Abs(amount);decimal gold=Math.Floor(value/10000m),silver=Math.Floor(value%10000m/100m),copper=value%100m;
   return (amount<0?"-":"")+Number(gold)+"g "+Number(silver)+"s "+Number(copper)+"c";
  }
  internal static string Amount(string currency,decimal amount,bool signed=false)=>currency=="Gold"?(signed&&amount>0?"+":"")+Gold(amount):(signed&&amount>0?"+":"")+Number(amount);
  internal static string CurrencyName(string currency)=>Enum.TryParse(currency,out Currency value)?Assets.Source.Util.Translation.Translate(value.GetDisplayName()):currency;
  internal static string Money(IEnumerable<KeyValuePair<string,decimal>> values)
  {
   var text=values.Where(p=>p.Value!=0).OrderBy(p=>p.Key).Select(p=>p.Key=="Gold"?Gold(p.Value):Number(p.Value)+" "+CurrencyName(p.Key));
   return string.Join(" · ",text.DefaultIfEmpty("0g 0s 0c"));
  }
  internal static string Net(Dictionary<string,decimal> revenue,Dictionary<string,decimal> cost)
  {
   var net=new Dictionary<string,decimal>(revenue);foreach(var p in cost)CostTrace.Add(net,p.Key,-p.Value);
   return Money(net);
  }
  internal static string ItemName(string id)=>Assets.Source.Util.Translation.Translate(ItemType.Get(id)?.PreviewItem.DisplayName??id);
 }
}
