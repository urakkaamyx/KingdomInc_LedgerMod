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
        sealed partial class RequisitionShare
        {
            internal decimal Value;internal Tickets Source;internal bool Credited;
        }
    }
}
