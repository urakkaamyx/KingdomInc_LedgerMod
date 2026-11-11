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
        static void Credit(Tickets source,IDictionary<string,decimal> income,bool first=true)
        {
         if(source==null||!income.Values.Any(v=>v>0))return;
         foreach(var entry in source.Values)
          if(entry.ledger.TryGetTarget(out var ledger))ledger.Sale(entry.trace,income,first);
        }
    }
}
