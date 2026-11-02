using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using LightJson;
using UnityEngine;

namespace KingdomLedger
{
 public sealed class Ledger : FlowElement
 {
  public const string TypeId="Ledger", TechId="ledger_mod";
  public const int Width=400, Height=284;
  public int Period { get; private set; }=60;
  public override string ElementType=>TypeId;
  public override string Label=>"Ledger";
  public override string IconName=>"Icons_149";
  public override ElementCategory Category=>ElementCategory.Utility;
  public override bool HasCraftButton=>false;
  public override int UpgradeSlots=>0;
  internal readonly LedgerHistory History=new LedgerHistory();
  public Ledger()
  {
   RequiredTech=TechId; CostCurrency=CurrencyCollection.GoldMedal; CostMultiplier=.25;
   Description="A zero-storage production ledger. Items enter only when their matching output can deliver immediately. Gear: reporting period, paired ports and full chain costs. Connected Item/Luxury Shop sales determine profit.";
   SetPairs(1);
  }
  public void SetPeriod(int seconds) { Period=seconds==1||seconds==3600?seconds:60; }
  public bool SetPairs(int count)
  {
   if(count<1||count>5)return false;
   for(int i=count;i<Inputs.Count;i++)
    if(Inputs[i].ConnectedPipe!=null||Outputs[i].ConnectedPipe!=null)return false;
   while(Inputs.Count<count)
   {
    int i=Inputs.Count;
    Inputs.Add(new FlowInput(this,i){BufferSize=0,Filter=item=>true,Position=-60-40*i});
    Outputs.Add(new FlowOutput(this,i){BufferSize=0,Position=60+40*i});
   }
   if(Inputs.Count>count){Inputs.RemoveRange(count,Inputs.Count-count); Outputs.RemoveRange(count,Outputs.Count-count);}
   Size=new Vector2Int(Width,Height);
   if(World)World.UpdateIO();
   return true;
  }
  public override void Update(float delta) { }
  public override bool HandleReopenBuyMenu(){if(World)World.GetComponent<LedgerWorld>()?.OpenSettings();return false;}
  internal static bool Resolve(Ledger start,int lane,InventoryItem item,out FlowInput receiver,out List<(Ledger ledger,int lane)> path,out string reason)
  {
   receiver=null;path=new List<(Ledger,int)>();var seen=new HashSet<Ledger>();
   var ledger=start;
   while(ledger!=null)
   {
    if(!seen.Add(ledger)||seen.Count>128){reason="Ledger loop blocked";return false;}
    if(lane<0||lane>=ledger.Outputs.Count){reason="Invalid output";return false;}
    path.Add((ledger,lane));
    var output=ledger.Outputs[lane];var pipe=output.ConnectedPipe;
    if(pipe==null||pipe.ConnectedOutput!=output||pipe.ConnectedInput==null||pipe.ConnectedInput.ConnectedPipe!=pipe)
     {reason="Output "+(lane+1)+": disconnected";return false;}
    receiver=pipe.ConnectedInput;
    if(receiver.Parent is Ledger next){ledger=next;lane=receiver.ParentIndex;continue;}
    if(!receiver.CanBuffer){reason="Output "+(lane+1)+": full";return false;}
    if(receiver.Parent is Stockpile store && store.Contents.Count+store.Inputs.Sum(i=>i.BufferedItems.Count)>=store.Capacity)
     {reason="Stockpile full";return false;}
    if(receiver.Parent is Shop shop && shop.CollectedGold>=shop.MaxGold)
     {reason="Shop till full";return false;}
    if(item!=null&&!receiver.CanAcceptItem(item)){reason="Item rejected by destination";return false;}
    reason=null;return true;
   }
   reason="No destination";return false;
  }
  internal bool Forward(int lane,InventoryItem item)
  {
   if(item==null||!Resolve(this,lane,item,out var receiver,out var path,out _))return false;
   if(!receiver.InputItem(item))return false;
   // The native incoming pipe consumes the parent's item only after true is returned.
   // Ledger never adds the item to any input/output buffer.
   try{foreach(var entry in path)entry.ledger.History.Pass(item,entry.lane,receiver.Parent);}
   catch(Exception e){LedgerPlugin.Warn("Transfer completed; reporting failed: "+e.Message);}
   return true;
  }
  internal string Destination(int lane)
  {
   Resolve(this,lane,null,out var receiver,out _,out var reason);
   if(receiver==null)return reason??"Disconnected";
   return (IsShop(receiver.Parent)?"Sell shop: ":"Not selling: ")+receiver.Parent.ElementType+(reason==null?"":" · "+reason);
  }
  internal static bool IsShop(FlowElement e)=>e is Shop||e is LuxuryShop;
  internal bool ShowFinancials(LedgerSummary summary)=>HasShopOutput||summary.HasMoneyActivity||LedgerChain.Read(this).HasOperatingCosts;
  internal bool HasShopOutput
  {
   get
   {
    for(int i=0;i<Outputs.Count;i++)
    {
     Resolve(this,i,null,out var receiver,out _,out _);
     // Full shops still count as connected; admission and display mode are separate.
     if(receiver!=null&&IsShop(receiver.Parent))return true;
    }
    return false;
   }
  }
  protected override void CopySettingsTo(FlowElement copy){var l=(Ledger)copy;l.SetPairs(Inputs.Count);l.SetPeriod(Period);}
  protected override void DataToJson(JsonObject data){data["LedgerPairs"]=Inputs.Count;data["LedgerPeriod"]=Period;}
  protected override void DataFromJson(JsonObject data,FlowDiagram parent){SetPairs(data["LedgerPairs"].IsInteger?Math.Max(1,Math.Min(5,data["LedgerPairs"].AsInteger)):1);SetPeriod(data["LedgerPeriod"].AsInteger);}
 }
}
