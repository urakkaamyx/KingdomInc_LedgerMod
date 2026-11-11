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
        sealed partial class RequisitionPool
        {
            internal readonly Queue<RequisitionShare> Shares=new Queue<RequisitionShare>();
            internal decimal Value;
            internal void Add(decimal amount,Tickets source=null)
            {
             if(amount<=0)return;
             Shares.Enqueue(new RequisitionShare{Value=amount,Source=source});Value+=amount;
            }
        }
    }
}
