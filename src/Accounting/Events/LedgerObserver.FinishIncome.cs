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
    }
}
