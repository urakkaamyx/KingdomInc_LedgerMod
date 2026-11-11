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
        static void Endpoint(Tickets source,FlowElement destination,IDictionary<string,decimal> spend)
        {
         if(source==null)return;
         foreach(var entry in source.Values)
          if(entry.ledger.TryGetTarget(out var ledger))ledger.Endpoint(Stage(destination),spend);
        }
    }
}
