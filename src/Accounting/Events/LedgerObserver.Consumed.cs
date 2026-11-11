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
        static void Consumed(FlowIO __instance,InventoryItem __result)
        {
         if(current!=null&&__instance is FlowInput&&__instance.Parent==current.Element&&__result!=null)current.Consumed.Add(__result);
        }
    }
}
