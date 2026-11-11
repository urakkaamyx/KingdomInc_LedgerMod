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
        internal static bool IsIncomeReceiver(FlowElement e)=>e is Shop||e is LuxuryShop||e is RequisitionOffice||e is Disenchanter||e is Restaurant||e is MercenaryGuild;
    }
}
