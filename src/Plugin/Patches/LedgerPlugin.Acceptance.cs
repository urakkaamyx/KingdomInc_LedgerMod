using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Behaviour.Flow;
using Assets.Behaviour.Flow.Elements;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;
using Behaviour.UI.Tooltip;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KingdomFoundation.Api;

namespace KingdomLedger
{
    public sealed partial class LedgerPlugin:BaseUnityPlugin
    {
        [HarmonyPatch(typeof(FlowIO),nameof(FlowIO.CanAcceptItem))]static partial class Acceptance
        {
            static bool Prefix(FlowIO __instance,InventoryItem item,ref bool __result)
            {
             if(!(__instance is FlowInput)||!(__instance.Parent is Ledger ledger))return true;
             __result=item!=null&&Ledger.Resolve(ledger,__instance.ParentIndex,item,out _,out _,out _);return false;
            }
        }
    }
}
