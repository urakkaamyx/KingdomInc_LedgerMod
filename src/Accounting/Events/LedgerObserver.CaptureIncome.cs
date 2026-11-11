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
        static void CaptureIncome(Scope scope)
        {
         if(!IsIncomeReceiver(scope.Element))return;
         scope.IncomeBefore=IncomeBalances(scope.Element);
         if(scope.Element is RequisitionOffice office){scope.StoredValue=office.StoredValue;scope.PassiveTimer=Native<float>(office,"_passiveTimer");}
         if(scope.Element is Disenchanter disenchanter)scope.PassiveTimer=Native<float>(disenchanter,"_passiveTimer");
        }
    }
}
