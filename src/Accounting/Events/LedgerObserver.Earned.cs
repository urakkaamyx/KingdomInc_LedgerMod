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
        static void Earned(Currency __0,long __1,bool __2){if(current!=null&&__2&&__1>0)CostTrace.Add(current.Earned,__0.ToString(),__1);}
    }
}
