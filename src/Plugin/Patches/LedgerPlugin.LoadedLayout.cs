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
        [HarmonyPatch(typeof(FlowElement),nameof(FlowElement.FromJson))]static partial class LoadedLayout
        {
            static void Postfix(FlowElement __result){if(__result is Ledger ledger)ledger.SetPairs(ledger.Inputs.Count);}
        }
    }
}
