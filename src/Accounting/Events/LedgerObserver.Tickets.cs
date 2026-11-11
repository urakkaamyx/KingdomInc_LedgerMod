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
        internal sealed partial class Tickets
        {
            internal readonly List<(WeakReference<LedgerHistory> ledger,CostTrace trace)> Values=new List<(WeakReference<LedgerHistory>,CostTrace)>();
        }
    }
}
