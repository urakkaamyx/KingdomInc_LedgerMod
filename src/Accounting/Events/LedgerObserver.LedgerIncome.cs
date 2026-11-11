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











        static ConditionalWeakTable<RequisitionOffice,RequisitionPool> requisitions=new ConditionalWeakTable<RequisitionOffice,RequisitionPool>();
        static void ResetIncome()=>requisitions=new ConditionalWeakTable<RequisitionOffice,RequisitionPool>();
    }
}
