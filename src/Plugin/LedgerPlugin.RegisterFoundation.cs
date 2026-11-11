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
        void RegisterFoundation()
        {
         var accountant=TechNode.Get("accountant")??throw new InvalidOperationException("Ledger requires native Accountant research.");
         // Keep legacy IDs so existing unlocked research, node types and pipe lanes load unchanged.
         research=context.Register(new TechNodeDefinition(Ledger.TechId,"Ledger",Id,"Ledger",Vector2.zero,
          "Track items, full production costs and actual currency profit. Zero storage; configure 1–5 input/output pairs and second/minute/hour windows with the gear.",
          research:new TechNodeOptions(costTier:accountant.CostTier,costMultiplier:.5,requirements:new[]{accountant.Identifier},iconName:"Icons_149")));
         // Preserve the native Accountant currency and price used by existing saves.
         var native=((INativeTechNode)research).NativeNode;
         native.CostCurrency=accountant.CostCurrency;native.ResetCost();
         workspace=context.Register(new WorkspaceNodeDefinition(Ledger.TypeId,"Ledger",()=>new Ledger(),Ledger.TechId,
          size:new Vector2Int(Ledger.Width,Ledger.Height),description:Ledger.NodeDescription,iconName:"Icons_149",costMultiplier:.25));
        }
    }
}
