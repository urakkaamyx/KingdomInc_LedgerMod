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
        internal static void Ticket(InventoryItem item,LedgerHistory ledger,CostTrace trace)
        {
         var t=tickets.GetOrCreateValue(item);
         // Chained Ledgers are each credited once when the actual destination sells.
         if(!t.Values.Any(v=>v.ledger.TryGetTarget(out var h)&&ReferenceEquals(h,ledger)))t.Values.Add((new WeakReference<LedgerHistory>(ledger),trace.Copy()));
        }
    }
}
