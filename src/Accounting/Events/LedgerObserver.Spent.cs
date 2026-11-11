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
        static void Spent(Currency __0,long __1,bool __result){if(current!=null&&__result&&__1>0)CostTrace.Add(current.Spent,__0.ToString(),__1);}
    }
}
