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
 [BepInPlugin(Id,"Ledger",Version)]
 [BepInDependency("urakka.kingdominc.foundation", "0.5.0")]
 public sealed partial class LedgerPlugin:BaseUnityPlugin
 {
  public const string Id="urakka.kingdominc.ledgermod",Version="0.5.1";
  Harmony harmony;
  static LedgerPlugin instance;
  internal static bool Diagnostic=>Environment.GetCommandLineArgs().Contains("-ledger-smoke");
  internal static void Warn(string message)=>instance?.Logger.LogWarning(message);
  ITechNode research;
  IWorkspaceNodeType workspace;
  internal static ITechNode ResearchRegistration=>instance?.research;
  internal static IWorkspaceNodeType WorkspaceRegistration=>instance?.workspace;
  void Awake()
  {
   instance=this;
   try
   {
    RegisterFoundation();
    harmony=new Harmony(Id);harmony.PatchAll(Assembly.GetExecutingAssembly());
    LedgerObserver.Install(harmony);
    if(Diagnostic){Application.runInBackground=true;StartCoroutine(RuntimeCheck());}
   }
   catch{workspace?.Dispose();research?.Dispose();harmony?.UnpatchSelf();throw;}
  }
  void Start(){Logger.LogInfo("Ledger ready through Foundation: native Mods research, registered workspace and zero-storage accounting.");}
  void OnDestroy(){workspace?.Dispose();research?.Dispose();harmony?.UnpatchSelf();if(instance==this)instance=null;}
  void RegisterFoundation()
  {
   var accountant=TechNode.Get("accountant")??throw new InvalidOperationException("Ledger requires native Accountant research.");
   // Keep legacy IDs so existing unlocked research, node types and pipe lanes load unchanged.
   research=FoundationApi.CreateTechNode(new TechNodeDefinition(Ledger.TechId,"Ledger",Vector2.zero,
    "Track items, full production costs and actual currency profit. Zero storage; configure 1–5 input/output pairs and second/minute/hour windows with the gear.",
    research:new TechNodeOptions(costTier:accountant.CostTier,costMultiplier:.5,requirements:new[]{accountant.Identifier},iconName:"Icons_149")));
   // Foundation 0.4.0 exposes tiers but not custom research currency collections.
   // Its public native bridge preserves Ledger's existing Accountant-based price.
   var native=((INativeTechNode)research).NativeNode;
   native.CostCurrency=accountant.CostCurrency;native.ResetCost();
   workspace=FoundationApi.RegisterWorkspaceNode(new WorkspaceNodeDefinition(Ledger.TypeId,"Ledger",()=>new Ledger(),Ledger.TechId,
    size:new Vector2Int(Ledger.Width,Ledger.Height),description:Ledger.NodeDescription,iconName:"Icons_149",costMultiplier:.25));
  }
  // Native deserialization restores IO positions after DataFromJson. Normalize legacy
  // 40-unit spacing only after that step, keeping saved pipe/lane identities intact.
  [HarmonyPatch(typeof(FlowElement),nameof(FlowElement.FromJson))]static class LoadedLayout
  {
   static void Postfix(FlowElement __result){if(__result is Ledger ledger)ledger.SetPairs(ledger.Inputs.Count);}
  }
  [HarmonyPatch(typeof(FlowIO),nameof(FlowIO.CanAcceptItem))]static class Acceptance
  {
   static bool Prefix(FlowIO __instance,InventoryItem item,ref bool __result)
   {
    if(!(__instance is FlowInput)||!(__instance.Parent is Ledger ledger))return true;
    __result=item!=null&&Ledger.Resolve(ledger,__instance.ParentIndex,item,out _,out _,out _);return false;
   }
  }
  [HarmonyPatch(typeof(FlowInput),nameof(FlowInput.InputItem))]static class Delivery
  {
   static bool Prefix(FlowInput __instance,InventoryItem item,ref bool __result){if(!(__instance.Parent is Ledger ledger))return true;__result=ledger.Forward(__instance.ParentIndex,item);return false;}
  }
  // ClearIO normally writes directly into adjacent buffers, bypassing InputItem.
  // Keep native deletion/recipe-change behavior from placing items inside Ledger.
  [HarmonyPatch(typeof(FlowElement),nameof(FlowElement.ClearIO))]static class Clearing
  {
   static void Prefix(FlowElement __instance)=>PrepareClear(__instance);
  }
  internal static void PrepareClear(FlowElement element)
  {
   foreach(var input in element.Inputs)
   {
    var origin=input.ConnectedPipe?.ConnectedOutput;
    if(!(origin?.Parent is Ledger))continue;
    var seen=new HashSet<Ledger>();
    while(origin?.Parent is Ledger previous)
    {
     if(!seen.Add(previous)){origin=null;break;}
     origin=previous.Inputs[origin.ParentIndex].ConnectedPipe?.ConnectedOutput;
    }
    // Return queued arrivals to the real producer, never a Ledger output.
    if(origin!=null)for(int i=input.BufferedItems.Count-1;i>=0;i--)origin.BufferedItems.Insert(0,input.BufferedItems[i]);
    input.BufferedItems.Clear();
   }
   foreach(var output in element.Outputs)
   {
    var target=output.ConnectedPipe?.ConnectedInput;
    if(!(target?.Parent is Ledger))continue;
    while(output.HasBuffer&&target.CanAcceptItem(output.AvailableItem)&&target.InputItem(output.AvailableItem))output.ConsumeItem();
   }
  }
  [HarmonyPatch(typeof(WorldFlowIO),nameof(WorldFlowIO.SetIO))]static class PortColors
  {
   static void Postfix(WorldFlowIO __instance,FlowIO io)
   {
    if(!(io.Parent is Ledger))return;
    var sprites=(Sprite[])AccessTools.Field(typeof(WorldFlowIO),io is FlowOutput?"_outputSprites":"_inputSprites").GetValue(__instance);
    ((Image)AccessTools.Field(typeof(WorldFlowIO),"_sprite").GetValue(__instance)).sprite=sprites[2];
   }
  }
  // Foundation owns the generic world prefab, but 0.4.0 has no public view/bind
  // callback. This scoped adapter adds Ledger's display to the bound native view.
  [HarmonyPatch(typeof(WorldFlowElement),nameof(WorldFlowElement.SetElement))]static class WorkspaceView
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
  internal static T Field<T>(WorldFlowElement world,string name)where T:class=>AccessTools.Field(typeof(WorldFlowElement),name).GetValue(world)as T;
 }
}
