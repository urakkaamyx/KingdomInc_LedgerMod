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
        static Exception Exit(Exception __exception,Scope __state)
        {
         if(__state==null)return __exception;
         try{if(__exception==null)Finish(__state);}
         catch(Exception e){LedgerPlugin.Warn("Observer finish: "+e.Message);}
         finally{current=__state.Parent;}
         return __exception;
        }
    }
}
