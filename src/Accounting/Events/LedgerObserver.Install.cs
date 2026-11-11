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
        internal static void Install(Harmony harmony)
        {
         var methods=typeof(FlowElement).Assembly.GetTypes().Where(t=>typeof(FlowElement).IsAssignableFrom(t))
          .SelectMany(t=>t.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
          .Where(m=>!m.IsAbstract&&(m.Name=="Update"||m.Name=="DoManualCraft"||m.Name=="QueueCraft")).Distinct();
         foreach(var method in methods)harmony.Patch(method,prefix:Patch(nameof(Enter)),finalizer:Patch(nameof(Exit)));
         harmony.Patch(AccessTools.Method(typeof(FlowIO),"ConsumeItem",Type.EmptyTypes),postfix:Patch(nameof(Consumed)));
         harmony.Patch(AccessTools.Method(typeof(GamePlayer),"SpendCurrency",new[]{typeof(Currency),typeof(long)}),postfix:Patch(nameof(Spent)));
         harmony.Patch(AccessTools.Method(typeof(GamePlayer),"AddCurrency",new[]{typeof(Currency),typeof(long),typeof(bool)}),postfix:Patch(nameof(Earned)));
        }
    }
}
