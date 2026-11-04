using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using HarmonyLib;
using UnityEngine;

namespace KingdomLedger
{
 internal static partial class LedgerObserver
 {
  // Requisition contracts can consume several items before minting a reward.
  // Retain only their outstanding value and weak ledger receipts, never items.
  sealed class RequisitionShare {internal decimal Value;internal Tickets Source;internal bool Credited;}
  sealed class RequisitionPool
  {
   internal readonly Queue<RequisitionShare> Shares=new Queue<RequisitionShare>();
   internal decimal Value;
   internal void Add(decimal amount,Tickets source=null)
   {
    if(amount<=0)return;
    Shares.Enqueue(new RequisitionShare{Value=amount,Source=source});Value+=amount;
   }
  }
  static ConditionalWeakTable<RequisitionOffice,RequisitionPool> requisitions=new ConditionalWeakTable<RequisitionOffice,RequisitionPool>();
  static void ResetIncome()=>requisitions=new ConditionalWeakTable<RequisitionOffice,RequisitionPool>();
  internal static bool IsIncomeReceiver(FlowElement e)=>e is Shop||e is LuxuryShop||e is RequisitionOffice||e is Disenchanter||e is Restaurant||e is MercenaryGuild;
  static T Native<T>(object owner,string name)=>(T)AccessTools.Field(owner.GetType(),name).GetValue(owner);
  static Dictionary<string,decimal> IncomeBalances(FlowElement e)
  {
   var values=new Dictionary<string,decimal>();
   if(e is Shop shop)values["Gold"]=shop.CollectedGold;
   if(e is RequisitionOffice office)values["Requisition"]=office.CollectedRequisition;
   if(e is Disenchanter disenchanter)values["Mana"]=disenchanter.CollectedMana;
   if(e is Restaurant restaurant){values["Gold"]=restaurant.CollectedGold;values["Crown"]=restaurant.CollectedCrowns;}
   return values;
  }
  static void CaptureIncome(Scope scope)
  {
   if(!IsIncomeReceiver(scope.Element))return;
   scope.IncomeBefore=IncomeBalances(scope.Element);
   if(scope.Element is RequisitionOffice office){scope.StoredValue=office.StoredValue;scope.PassiveTimer=Native<float>(office,"_passiveTimer");}
   if(scope.Element is Disenchanter disenchanter)scope.PassiveTimer=Native<float>(disenchanter,"_passiveTimer");
  }
  static void Credit(Tickets source,IDictionary<string,decimal> income,bool first=true)
  {
   if(source==null||!income.Values.Any(v=>v>0))return;
   foreach(var entry in source.Values)
    if(entry.ledger.TryGetTarget(out var ledger))ledger.Sale(entry.trace,income,first);
  }
  static void Endpoint(Tickets source,FlowElement destination,IDictionary<string,decimal> spend)
  {
   if(source==null)return;
   foreach(var entry in source.Values)
    if(entry.ledger.TryGetTarget(out var ledger))ledger.Endpoint(Stage(destination),spend);
  }
  static bool FinishIncome(Scope scope)
  {
   var element=scope.Element;if(!IsIncomeReceiver(element))return false;
   // Earned currency plus the change in its native collection tray is the
   // new reward. Flushing an existing tray contributes zero, even in Update.
   var income=new Dictionary<string,decimal>(scope.Earned);
   foreach(var value in IncomeBalances(element))CostTrace.Add(income,value.Key,value.Value);
   foreach(var value in scope.IncomeBefore)CostTrace.Add(income,value.Key,-value.Value);
   if(element is RequisitionOffice office){FinishRequisition(scope,office,income);return true;}
   if(scope.Method=="Update"&&element is Disenchanter disenchanter&&scope.PassiveTimer+scope.Delta>=1f)
    CostTrace.Add(income,"Mana",-Native<int>(disenchanter,"_passiveGain"));
   if(scope.Consumed.Count==1)
   {
    var item=scope.Consumed[0];tickets.TryGetValue(item,out var source);
    Endpoint(source,element,scope.Spent);
    Credit(source,income.Where(p=>p.Value>0).ToDictionary(p=>p.Key,p=>p.Value));
    tickets.Remove(item);
   }
   return true;
  }
  static void FinishRequisition(Scope scope,RequisitionOffice office,Dictionary<string,decimal> income)
  {
   if(scope.Method!="Update")return;
   int unit=Native<int>(office,"_valuePerReq");if(unit<=0)return;
   var pool=requisitions.GetOrCreateValue(office);
   // A loaded residual, or value changed by another mod, has no attributable
   // history. Preserve it as unowned rather than giving it to the next item.
   if(pool.Value!=scope.StoredValue){pool.Shares.Clear();pool.Value=0;pool.Add(scope.StoredValue);}
   income.TryGetValue("Requisition",out var minted);
   decimal added=office.StoredValue-scope.StoredValue+minted*unit;
   decimal passive=scope.PassiveTimer+scope.Delta>=1f&&scope.IncomeBefore["Requisition"]<office.MaxRequisition
    ?Mathf.RoundToInt(unit*Native<float>(office,"_passiveGain")):0;
   passive=Math.Min(Math.Max(0,added),passive);pool.Add(passive);
   if(scope.Consumed.Count==1)
   {
    var item=scope.Consumed[0];tickets.TryGetValue(item,out var source);
    Endpoint(source,office,scope.Spent);pool.Add(Math.Max(0,added-passive),source);tickets.Remove(item);
   }
   else pool.Add(Math.Max(0,added-passive));
   decimal remaining=Math.Max(0,minted)*unit;
   while(remaining>0&&pool.Shares.Count>0)
   {
    var share=pool.Shares.Peek();decimal amount=Math.Min(remaining,share.Value);
    Credit(share.Source,new Dictionary<string,decimal>{{"Requisition",amount/unit}},!share.Credited);share.Credited=true;
    remaining-=amount;share.Value-=amount;pool.Value-=amount;
    if(share.Value==0)pool.Shares.Dequeue();
   }
  }
 }
}
