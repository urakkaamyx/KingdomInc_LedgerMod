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
        static CostTrace Inputs(IEnumerable<InventoryItem> items)
        {
         var result=new CostTrace();foreach(var item in items)result.Merge(Trace(item));return result;
        }
    }
}
