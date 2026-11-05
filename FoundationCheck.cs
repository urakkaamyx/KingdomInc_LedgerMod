using System;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using BepInEx.Bootstrap;
using HarmonyLib;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
 public sealed partial class LedgerPlugin
 {
  void FoundationModelChecks()
  {
   Check(Chainloader.PluginInfos.TryGetValue("urakka.kingdominc.foundation",out var dependency)&&dependency.Metadata.Version>=new Version(0,5,0),"Foundation 0.5.0+ loads before Ledger through its hard plugin dependency");
   Check(FoundationApi.TechNodes.TryGet(Ledger.TechId,out var tech)&&ReferenceEquals(tech,ResearchRegistration)&&tech is INativeTechNode,"Ledger research is owned by the public Foundation registry");
   Check(FoundationApi.WorkspaceNodes.TryGet(Ledger.TypeId,out var type)&&ReferenceEquals(type,WorkspaceRegistration)&&type.CreateInstance() is Ledger,"Public Foundation workspace registration creates real Ledger models");
   var native=((INativeTechNode)tech).NativeNode;
   Check(native.Identifier=="ledger_mod"&&native.Previous==null&&native.Root==null&&native.ForceRootNode&&native.AllRequirements.Any(t=>t.Identifier=="accountant"),"Standalone Mods research preserves the legacy research ID and native Accountant requirement");
   var player=GamePlayer.Current;
   Check(!type.CreateInstance().IsPurchaseable(player),"Unresearched Foundation Ledger cannot be bought");
   player.AddTech("accountant");Check(!type.CreateInstance().IsPurchaseable(player),"Accountant alone does not skip Ledger research");
   player.AddTech(Ledger.TechId);Check(type.CreateInstance().IsPurchaseable(player),"Purchasing the registered Ledger research unlocks native construction");
   Check(new Ledger().CostCurrency.SequenceEqual(CurrencyCollection.GoldMedal),"Foundation migration retains Ledger's existing building currencies");
   Check(native.CostCurrency.SequenceEqual(TechNode.Get("accountant").CostCurrency)&&native.CostMultiplier==.5,"Foundation native bridge preserves Accountant-based research currency and multiplier");
   foreach(var method in new[]{AccessTools.Method(typeof(FlowElement),nameof(FlowElement.Create)),AccessTools.Method(typeof(FlowElement),nameof(FlowElement.GetAllTypes)),AccessTools.Method(typeof(FlowPrefabManager),nameof(FlowPrefabManager.Get))})
   {
    var owners=Harmony.GetPatchInfo(method)?.Owners;
    Check(owners!=null&&owners.Contains("urakka.kingdominc.foundation")&&!owners.Contains(Id),"Foundation exclusively handles Ledger integration for "+method.DeclaringType.Name+"."+method.Name);
   }
   var legacy=new Ledger{Position=new Vector2Int(128,-64),Mirrored=true};legacy.SetPairs(5);legacy.SetPeriod(3600);
   var source=new Collector();var destination=new Stockpile();var diagram=new FlowDiagram{Label="Legacy Ledger Foundation roundtrip"};diagram.Elements.Add(source);diagram.Elements.Add(legacy);diagram.Elements.Add(destination);diagram.Pipes.Add(Connect(source.Outputs[0],legacy.Inputs[4]));diagram.Pipes.Add(Connect(legacy.Outputs[4],destination.Inputs[0]));
   player.Flows.Add(diagram);
   var saved=player.ToJson();var restored=GamePlayer.FromJson(saved);var loaded=restored.Flows.Last().Elements.OfType<Ledger>().Single();
   Check(restored.HasTech("ledger_mod")&&loaded.ElementType=="Ledger"&&loaded.Period==3600&&loaded.Inputs.Count==5&&loaded.Mirrored&&loaded.Position==legacy.Position,"Full native kingdom JSON retains legacy Ledger unlock, node identity and settings through Foundation");
   Check(loaded.Inputs[4].ConnectedPipe?.ConnectedOutput.Parent is Collector&&loaded.Outputs[4].ConnectedPipe?.ConnectedInput.Parent is Stockpile,"Full kingdom roundtrip restores saved Ledger lane connections through Foundation's factory");
   player.Flows.Remove(diagram);GamePlayer.Current=player;
  }

  void FoundationWorldChecks(string directory)
  {
   var ledger=GamePlayer.Current.Flows[0].Elements.OfType<Ledger>().First();
   var prefab=FlowPrefabManager.Instance.Get(Ledger.TypeId);
   Check(prefab.Prefab.name=="WorkspaceNodePrefab:Ledger"&&prefab.Prefab.GetType()==typeof(WorldFlowElement),"Ledger uses Foundation's generic native workspace prefab");
   Check(!prefab.Prefab.GetComponent<LedgerWorld>()&&ledger.World.GetComponents<LedgerWorld>().Length==1,"Ledger display is attached once at binding without cloning or replacing Foundation assets");
   Check(ledger.World.GetComponentsInChildren<WorldFlowIO>().All(io=>io.IO.Parent!=ledger||io.gameObject.activeInHierarchy),"Custom Ledger presentation leaves Foundation's native ports active");
   try
   {
    GameUI.Instance.ToggleTechMenu(true);var tree=TechTreeMenu.Instance;
    Check(!tree.Nodes.Any(n=>n.Tech.Identifier==Ledger.TechId),"Ledger is absent from Foundation's Base Game research tab");
    var mods=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ModsTab"&&b.gameObject.activeInHierarchy);mods.onClick.Invoke();
    Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="Mod:"+Id&&b.gameObject.activeInHierarchy).onClick.Invoke();
    Check(tree.Nodes.Count(n=>n.Tech.Identifier==Ledger.TechId)==1,"Foundation Mods tab renders exactly one native Ledger research node");
    var node=tree.Nodes.Single(n=>n.Tech.Identifier==Ledger.TechId);
    node.UpdateNodeStatus();
    var nativePrefab=(UITechNode)AccessTools.Field(typeof(TechTreeMenu),"_elementNodePrefab").GetValue(tree);
    Check(node.GetType()==nativePrefab.GetType()&&((RectTransform)node.transform).sizeDelta==((RectTransform)nativePrefab.transform).sizeDelta&&node.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text=="Ledger"),"Ledger research uses the native building technology card and label");
    CaptureFoundationTree(tree,Path.Combine(directory,"ledger-foundation-tech-tree.png"));
    var baseGame=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="Base GameTab"&&b.gameObject.activeInHierarchy);baseGame.onClick.Invoke();
    Check(!tree.Nodes.Any(n=>n.Tech.Identifier==Ledger.TechId)&&tree.Nodes.Any(),"Switching back restores native research without a duplicate Ledger card");
   }
   finally{GameUI.Instance.ToggleTechMenu(false);WorldFlowCamera.Instance.SetZoomLevel(3);}
  }
  static void CaptureFoundationTree(TechTreeMenu tree,string path)
  {
   var camera=(Camera)AccessTools.Field(typeof(TechTreeMenu),"_camera").GetValue(tree);
   var overlays=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
   var cameras=overlays.Select(c=>c.worldCamera).ToArray();var distances=overlays.Select(c=>c.planeDistance).ToArray();var orders=overlays.Select(c=>c.sortingOrder).ToArray();
   var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(Screen.width,Screen.height,24);var oldTarget=camera.targetTexture;int mask=camera.cullingMask;
   var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
   try
   {
    foreach(var overlay in overlays){overlay.renderMode=RenderMode.ScreenSpaceCamera;overlay.worldCamera=camera;overlay.planeDistance=10;overlay.sortingOrder=30000;}
    camera.cullingMask=-1;camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();File.WriteAllBytes(path,ImageConversion.EncodeToPNG(texture));
   }
   finally
   {
    camera.targetTexture=oldTarget;camera.cullingMask=mask;
    for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=distances[i];overlays[i].sortingOrder=orders[i];}
    Canvas.ForceUpdateCanvases();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(texture);
   }
  }
 }
}
