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

namespace KingdomLedger
{
 [BepInPlugin(Id,"Ledger",Version)]
 public sealed partial class LedgerPlugin:BaseUnityPlugin
 {
  public const string Id="urakka.kingdominc.ledgermod",Version="0.4.3";
  Harmony harmony;
  static LedgerPlugin instance;
  internal static bool Diagnostic=>Environment.GetCommandLineArgs().Contains("-ledger-smoke");
  internal static void Warn(string message)=>instance?.Logger.LogWarning(message);
  void Awake(){instance=this;harmony=new Harmony(Id);harmony.PatchAll(Assembly.GetExecutingAssembly());if(Diagnostic){Application.runInBackground=true;StartCoroutine(RuntimeCheck());}}
  void Start(){EnsureResearch();LedgerObserver.Install(harmony);Logger.LogInfo("Ledger ready: zero-storage paired lanes, rolling item/cost report and observed destination income.");}
  void OnDestroy(){harmony?.UnpatchSelf();instance=null;}
  internal static void EnsureResearch()
  {
   if(TechNode.Get(Ledger.TechId)!=null)return;
   var previous=TechNode.Get("accountant")??TechNode.Get("collector");
   TechNode.Add(new TechNode(Ledger.TechId){Name="Ledger",Description="Track items, full production costs and actual currency profit. Zero storage; configure 1–5 input/output pairs and second/minute/hour windows with the gear.",NodeType=TechNodeType.FlowElement,Previous=previous,RelativePosition=new Vector2Int(300,-180),IconName="Icons_149",CostTier=previous.CostTier,CostCurrency=previous.CostCurrency,CostMultiplier=.5});
  }
  [HarmonyPatch(typeof(GamePlayer),MethodType.Constructor)]static class Research {static void Prefix()=>EnsureResearch();}
  [HarmonyPatch(typeof(FlowElement),nameof(FlowElement.Create))]static class Factory
  {
   static bool Prefix(string name,ref FlowElement __result)
   {
    if(name==Ledger.TypeId){__result=new Ledger();return false;}
    var type=typeof(FlowElement).Assembly.GetType("Assets.Source.Flow.Elements."+name);
    if(type==null)return true;__result=(FlowElement)Activator.CreateInstance(type);return false;
   }
  }
  // Native deserialization restores IO positions after DataFromJson. Normalize legacy
  // 40-unit spacing only after that step, keeping saved pipe/lane identities intact.
  [HarmonyPatch(typeof(FlowElement),nameof(FlowElement.FromJson))]static class LoadedLayout
  {
   static void Postfix(FlowElement __result){if(__result is Ledger ledger)ledger.SetPairs(ledger.Inputs.Count);}
  }
  [HarmonyPatch(typeof(FlowElement),nameof(FlowElement.GetAllTypes))]static class Catalog
  {
   static Ledger preview;
   static void Postfix(ref IEnumerable<FlowElement> __result)=>__result=Include(__result);
   static IEnumerable<FlowElement> Include(IEnumerable<FlowElement> source){bool found=false;foreach(var e in source){found|=e is Ledger;yield return e;}if(!found)yield return preview??(preview=new Ledger());}
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
  [HarmonyPatch(typeof(FlowPrefabManager),nameof(FlowPrefabManager.Get))]static class Prefab
  {
   static FlowPrefabManager owner;static FlowPrefabSet custom;
   static bool Prefix(FlowPrefabManager __instance,string id,ref FlowPrefabSet __result)
   {
    if(id!=Ledger.TypeId)return true;
    if(!custom||owner!=__instance)
    {
     var original=__instance.Get("Collector");var root=new GameObject("LedgerAssets");root.transform.SetParent(__instance.transform,false);root.SetActive(false);
     custom=UnityEngine.Object.Instantiate(original,root.transform,false);custom.Identifier=Ledger.TypeId;custom.name=Ledger.TypeId;
     var world=UnityEngine.Object.Instantiate(original.Prefab,root.transform,false);world.name="LedgerPrefab";
     ((RectTransform)world.transform).sizeDelta=new Vector2(Ledger.Width,Ledger.Height);
     if(world.GetComponent<WorldFlowCollector>())UnityEngine.Object.DestroyImmediate(world.GetComponent<WorldFlowCollector>());
     var background=Field<Image>(world,"_background");var label=Field<TMP_Text>(world,"_label");var border=Field<Image>(world,"_labelContainer");
     foreach(var graphic in world.GetComponentsInChildren<Graphic>(true))if(graphic!=background&&graphic!=label&&graphic!=border)graphic.enabled=false;
     foreach(var b in world.GetComponentsInChildren<Button>(true))b.enabled=false;
     foreach(var t in world.GetComponentsInChildren<TooltipSource>(true))t.enabled=false;
     world.gameObject.AddComponent<LedgerWorld>();AccessTools.Field(typeof(FlowPrefabSet),"<Prefab>k__BackingField").SetValue(custom,world);owner=__instance;
    }
    __result=custom;return false;
   }
  }
  internal static T Field<T>(WorldFlowElement world,string name)where T:class=>AccessTools.Field(typeof(WorldFlowElement),name).GetValue(world)as T;
 }
}
