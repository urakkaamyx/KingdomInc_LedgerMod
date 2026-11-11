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
