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
        static T Native<T>(object owner,string name)=>(T)AccessTools.Field(owner.GetType(),name).GetValue(owner);
    }
}
