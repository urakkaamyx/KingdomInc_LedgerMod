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
        [HarmonyPatch(typeof(WorldFlowIO),nameof(WorldFlowIO.SetIO))]static partial class PortColors
        {
            static void Postfix(WorldFlowIO __instance,FlowIO io)
            {
             if(!(io.Parent is Ledger))return;
             var sprites=(Sprite[])AccessTools.Field(typeof(WorldFlowIO),io is FlowOutput?"_outputSprites":"_inputSprites").GetValue(__instance);
             ((Image)AccessTools.Field(typeof(WorldFlowIO),"_sprite").GetValue(__instance)).sprite=sprites[2];
            }
        }
    }
}
