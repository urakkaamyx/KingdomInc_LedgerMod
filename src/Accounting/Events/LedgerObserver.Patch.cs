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
        static HarmonyMethod Patch(string name)=>new HarmonyMethod(typeof(LedgerObserver).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic));
    }
}
