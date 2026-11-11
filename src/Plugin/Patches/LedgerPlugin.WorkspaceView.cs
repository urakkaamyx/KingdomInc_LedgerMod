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
        [HarmonyPatch(typeof(WorldFlowElement),nameof(WorldFlowElement.SetElement))]static partial class WorkspaceView
        {
            static void Postfix(WorldFlowElement __instance,FlowElement element)
            {
             if(!(element is Ledger)||__instance.GetComponent<LedgerWorld>())return;
             foreach(var name in new[]{"_icon","_progress","_productivity","_outputFull","_craftButton","_upgradeSlots"})
             {
              var component=AccessTools.Field(typeof(WorldFlowElement),name)?.GetValue(__instance)as Component;
              if(component)component.gameObject.SetActive(false);
             }
             __instance.gameObject.AddComponent<LedgerWorld>();
            }
        }
    }
}
