using System;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using LightJson;
using TMPro;
using UnityEngine;

namespace KingdomLedger
{
 public sealed partial class LedgerPlugin
 {
  void LayoutModelChecks()
  {
   var layout=new Ledger();
   for(int count=1;count<=5;count++)
   {
    layout.SetPairs(count);
    Check(layout.Size==new Vector2Int(384,Math.Max(128,count*64))&&layout.Inputs.Select((io,i)=>io.Position==-10-i*64).All(v=>v)&&layout.Outputs.Select((io,i)=>io.Position==10+i*64).All(v=>v),"Grid-aligned dimensions and native port spacing for "+count+" Ledger pairs");
   }
   var source=new Collector();var sink=new Stockpile();var flow=new FlowDiagram{Label="Legacy Ledger layout"};
   flow.Elements.Add(source);flow.Elements.Add(layout);flow.Elements.Add(sink);
   flow.Pipes.Add(Connect(source.Outputs[0],layout.Inputs[4]));flow.Pipes.Add(Connect(layout.Outputs[4],sink.Inputs[0]));
   layout.Mirrored=true;layout.SetPeriod(3600);
   var saved=flow.ToJson();var data=saved["Elements"][1];
   for(int i=0;i<5;i++){data["Inputs"][i].AsJsonObject["Position"]=-60-i*40;data["Outputs"][i].AsJsonObject["Position"]=60+i*40;}
   var restored=FlowDiagram.FromJson(saved);var loaded=restored.Elements.OfType<Ledger>().Single();
   Check(loaded.Mirrored&&loaded.Period==3600&&loaded.Size==new Vector2Int(384,320)&&loaded.Inputs[4].Position==-266&&loaded.Outputs[4].Position==266,"Legacy saved IO spacing is normalized after native deserialization with settings preserved");
   Check(loaded.Inputs[4].ConnectedPipe.ConnectedOutput.Parent is Collector&&loaded.Outputs[4].ConnectedPipe.ConnectedInput.Parent is Stockpile&&restored.Pipes.Count==2,"Legacy saved pipes retain their original lane identities after layout migration");
  }

  void LayoutWorldChecks(Ledger ledger,LedgerWorld view,string directory)
  {
   var clock=LedgerHistory.Clock;double now=clock()+100000;LedgerHistory.Clock=()=>now;
   int pairs=ledger.Inputs.Count,period=ledger.Period;bool mirrored=ledger.Mirrored;
   WorldFlowElement nativeWorld=null;
   try
   {
    ledger.SetPeriod(60);
    var distributor=new Distributor{Position=ledger.Position+new Vector2Int(448,0)};
    while(distributor.Outputs.Count<5)distributor.AddOutputSlot();
    nativeWorld=UnityEngine.Object.Instantiate(FlowPrefabManager.Instance.Get("Distributor").Prefab,ledger.World.transform.parent);
    nativeWorld.SetElement(distributor);
    for(int count=1;count<=5;count++)
    {
     Check(ledger.SetPairs(count),"Physical layout can select "+count+" port pairs");view.Refresh();Canvas.ForceUpdateCanvases();
     var size=((RectTransform)ledger.World.transform).sizeDelta;
     Check(size==new Vector2(384,Math.Max(128,count*64))&&ledger.World.GetComponentsInChildren<WorldFlowIO>().Count(io=>io.IO?.Parent==ledger)==count*2,"Actual node and collider footprint follow "+count+" port rows without reserved item rows");
     for(int i=0;i<count;i++)
     {
      var input=ledger.World.transform.InverseTransformPoint(ledger.Inputs[i].World.transform.position);
      var output=ledger.World.transform.InverseTransformPoint(ledger.Outputs[i].World.transform.position);
      var native=nativeWorld.transform.InverseTransformPoint(distributor.Outputs[i].World.transform.position);
      Check(Math.Abs(input.x)<.01&&Math.Abs(output.x-384)<.01&&Math.Abs(input.y-native.y)<.01&&Math.Abs(output.y-native.y)<.01,"Ledger pair "+(i+1)+" aligns with native Distributor ports at "+count+" pairs");
     }
     var collider=(BoxCollider2D)HarmonyLib.AccessTools.Field(typeof(FlowCollider),"_collider").GetValue(ledger.World.Collider);
     Check(collider.size==size-new Vector2(2,2),"Collision bounds match the resized "+count+"-pair node");
     Capture(Path.Combine(directory,"ledger-layout-"+count+"-pairs.png"));
    }
    ledger.Mirrored=true;ledger.World.UpdateIO();Canvas.ForceUpdateCanvases();
    Check(ledger.Inputs.All(io=>Math.Abs(ledger.World.transform.InverseTransformPoint(io.World.transform.position).x-384)<.01)&&ledger.Outputs.All(io=>Math.Abs(ledger.World.transform.InverseTransformPoint(io.World.transform.position).x)<.01),"Mirroring preserves row heights and swaps ports onto the aligned edges");
    ledger.Mirrored=false;ledger.SetPairs(1);
    var item=Item();Seed(item);ledger.History.Pass(item,0,new Stockpile());view.Refresh();
    Check(ledger.Size==new Vector2Int(384,192)&&view.transform.Find("LedgerDisplay/ItemRow0").gameObject.activeSelf&&!view.transform.Find("LedgerDisplay/ItemRow1").gameObject.activeSelf,"Single-item paid report removes blank rows and grows only enough grid cells for its values");
    var strip=(RectTransform)view.transform.Find("LedgerDisplay/MonetaryTotals");
    Check(-strip.anchoredPosition.y+strip.sizeDelta.y<=ledger.Size.y-6&&view.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==LedgerHistory.ItemName("iron_sword")),"Compact layout retains passed items and keeps profit values within the frame");
    Capture(Path.Combine(directory,"ledger-layout-paid.png"));
    now+=61;view.Refresh();Check(ledger.Size==new Vector2Int(384,128),"Expired report rows release extra grid cells without leaving stale node or collision size");
   }
   finally
   {
    if(nativeWorld)UnityEngine.Object.Destroy(nativeWorld.gameObject);
    LedgerHistory.Clock=clock;ledger.Mirrored=mirrored;ledger.SetPairs(pairs);ledger.SetPeriod(period);view.Refresh();
   }
  }
 }
}
