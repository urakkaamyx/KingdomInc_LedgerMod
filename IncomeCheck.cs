using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
 public sealed partial class LedgerPlugin
 {
  static decimal Revenue(Ledger ledger,string currency)
  {
   ledger.History.Summary(60).Revenue.TryGetValue(currency,out var value);return value;
  }
  static void NativeSet(object instance,string field,object value)=>AccessTools.Field(instance.GetType(),field).SetValue(instance,value);
  void IncomeChecks(Ledger main,LedgerWorld view,string directory)
  {
   var oldClock=LedgerHistory.Clock;LedgerHistory.Clock=()=>oldClock()+1000;
   var oldOutput=main.Outputs[0].ConnectedPipe;var oldInput=main.Inputs[0].ConnectedPipe;
   var created=new List<FlowElement>();
   T Place<T>(T element)where T:FlowElement
   {
    element.Position=new Vector2Int(1600+created.Count*450,900);WorldFlowManager.Instance.CreateNewElement(element);created.Add(element);return element;
   }
   RequisitionOffice Office(int unit=0)
   {
    var office=Place(new RequisitionOffice());var contracts=GamePlayer.Current.RequisitionTypes;
    contracts.Add(ItemType.Get("iron_sword"));office.SetSelectedContract(contracts.Count-1);office.MaxRequisition=100000;
    if(unit>0)NativeSet(office,"_valuePerReq",unit);return office;
   }
   Ledger To(FlowElement destination){var ledger=new Ledger();Connect(ledger.Outputs[0],destination.Inputs[0]);return ledger;}
   void Deliver(Ledger ledger,RequisitionOffice office)
   {
    var item=Item();Seed(item,100);Check(ledger.Inputs[0].InputItem(item),"Contract item passes straight into the Requisition Office");office.Update(0);
   }
   try
   {
    var office=Office();Connect(main.Outputs[0],office.Inputs[0]);
    Check(main.HasIncomeOutput&&main.ShowFinancials(main.History.Summary(60))&&!main.HasShopOutput,"Requisition Office enables currency reporting without being a shop");
    int value=checked((int)Item().Type.BaseValue);int unit=Currency.Requisition.GetGoldValue();int count=Math.Max(1,(unit+value-1)/value);
    for(int i=0;i<count;i++)Deliver(main,office);
    Check(office.CollectedRequisition>0&&Revenue(main,"Requisition")==office.CollectedRequisition,"Native Requisition rewards credit the contributing production chain");
    var report=main.History.Summary(60);
    Check(report.Costs.Money["Gold"]==count*100&&report.Currencies.SequenceEqual(new[]{"Gold","Requisition"}),"Requisition income and upstream Gold costs remain separate affected currencies");
    Check(report.Costs.Stages.Keys.Any(k=>k.StartsWith("RequisitionOffice / ")),"Currency-consuming destination appears as an observed chain stage");
    decimal before=Revenue(main,"Requisition");office.DoManualCraft();
    Check(office.CollectedRequisition==0&&Revenue(main,"Requisition")==before,"Collecting stored Requisition does not count the reward twice");
    NativeSet(office,"_hasWorker",true);office.CollectedRequisition=7;
    decimal wallet=Currency.Requisition.GetAmount();Deliver(main,office);
    decimal minted=Currency.Requisition.GetAmount()-wallet+office.CollectedRequisition-7;
    Check(Revenue(main,"Requisition")==before+minted,"Auto-collection excludes pre-existing Requisition while recognizing newly minted rewards");
    office.CollectedRequisition=office.MaxRequisition;var source=new Stockpile();var incoming=Connect(source.Outputs[0],main.Inputs[0]);var blocked=Item();source.Outputs[0].BufferedItems.Add(blocked);
    decimal passed=main.History.Summary(60).Items.Values.Sum();incoming.Update(0);
    Check(source.Outputs[0].AvailableItem==blocked&&main.History.Summary(60).Items.Values.Sum()==passed&&main.IO.All(io=>!io.HasBuffer),"Full Requisition Office leaves the item at its producer without a Ledger buffer or counter increment");
    office.CollectedRequisition=0;Check(!main.Inputs[0].InputItem(Item("iron_helm")),"Requisition contract filters still reject the wrong item through Ledger");

    // The office waits for two native item contributions before issuing one
    // reward. Its pre-existing and passive contributions must keep their share.
    var delayed=Office(value*2);var ledger=To(delayed);Deliver(ledger,delayed);
    Check(Revenue(ledger,"Requisition")==0&&delayed.StoredValue==value,"Sub-threshold contract items do not invent a Requisition reward");
    var other=To(delayed);Deliver(other,delayed);
    Check(Revenue(ledger,"Requisition")==.5m&&Revenue(other,"Requisition")==.5m&&delayed.CollectedRequisition==1,"A real contract reward is apportioned across its contributing Ledger chains without double counting");
    var loaded=Office(value*2);AccessTools.Property(typeof(RequisitionOffice),"StoredValue").SetValue(loaded,value,null);var fresh=To(loaded);Deliver(fresh,loaded);
    Check(Revenue(fresh,"Requisition")==.5m,"Pre-existing contract progress is not credited to newly connected Ledger items");
    var passive=Office(value*2);NativeSet(passive,"_passiveGain",.5f);var measured=To(passive);var passiveItem=Item();Seed(passiveItem,100);measured.Inputs[0].InputItem(passiveItem);passive.Update(1);
    Check(passive.CollectedRequisition==1&&Revenue(measured,"Requisition")==.5m,"Passive contract income is excluded from item-attributed Requisition rewards");

    var routedOffice=Office(value);var stored=new Stockpile();var router=Place(new Collector());var routed=To(stored);
    var intoRouter=Connect(stored.Outputs[0],router.Inputs[0]);var intoOffice=Connect(router.Outputs[0],routedOffice.Inputs[0]);
    Check(routed.HasIncomeOutput,"Income destination detection follows storage and routing nodes");
    var routedItem=Item();Seed(routedItem,100);routed.Inputs[0].InputItem(routedItem);stored.Update(0);intoRouter.Update(0);router.Update(0);intoOffice.Update(0);routedOffice.Update(0);
    Check(Revenue(routed,"Requisition")==1,"Item receipts survive native Stockpile and Collector passage to a Requisition Office");
    var head=new Ledger();var tail=To(Office(value));Connect(head.Outputs[0],tail.Inputs[0]);var tailOffice=(RequisitionOffice)tail.Outputs[0].ConnectedPipe.ConnectedInput.Parent;Deliver(head,tailOffice);
    Check(Revenue(head,"Requisition")==1&&Revenue(tail,"Requisition")==1,"Chained Ledgers each recognize the same destination reward once");
    var untracked=Item();routedOffice.Inputs[0].BufferedItems.Add(untracked);decimal owned=Revenue(routed,"Requisition");routedOffice.Update(0);
    Check(Revenue(routed,"Requisition")==owned,"Items that bypass Ledger do not credit its income");

    var disenchanter=Place(new Disenchanter());var manaLedger=To(disenchanter);NativeSet(disenchanter,"_passiveGain",3);
    var enchanted=Item();Seed(enchanted,100);manaLedger.Inputs[0].InputItem(enchanted);disenchanter.Update(1);
    Check(Revenue(manaLedger,"Mana")==disenchanter.CollectedMana-3&&Revenue(manaLedger,"Mana")>0,"Disenchanter credits observed item Mana while excluding passive gain");
    decimal mana=Revenue(manaLedger,"Mana");disenchanter.DoManualCraft();disenchanter.Update(1);
    Check(Revenue(manaLedger,"Mana")==mana,"Manual collection and idle passive Mana do not duplicate item income");
    NativeSet(disenchanter,"_hasWorker",true);disenchanter.CollectedMana=9;var next=Item();Seed(next,100);manaLedger.Inputs[0].InputItem(next);disenchanter.Update(1);
    Check(Revenue(manaLedger,"Mana")==mana+next.ManaValue,"Disenchanter automatic collection counts only the new item reward");

    var restaurant=Place(new Restaurant());var mealLedger=To(restaurant);var meal=Item("mushroom_stew");Seed(meal,100);mealLedger.Inputs[0].InputItem(meal);restaurant.Update(0);
    var served=restaurant.RecentItems.Last();
    Check(Revenue(mealLedger,"Gold")==served.gold&&Revenue(mealLedger,"Crown")==served.crowns,"Restaurant credits its actual Gold and Crown outcomes");
    decimal dining=Revenue(mealLedger,"Gold");restaurant.DoManualCraft();Check(Revenue(mealLedger,"Gold")==dining,"Restaurant tray collection does not credit sales twice");

    GamePlayer.Current.AddTech("mercenary_conscripts");var guild=Place(new MercenaryGuild());var guildLedger=To(guild);var hire=Item("conscript");hire.SetQuality(3);Seed(hire,100);Check(guildLedger.Inputs[0].InputItem(hire),"Mercenary Guild native acceptance permits a qualified Ledger delivery");guild.Update(0);
    var hired=guild.RecentItems.Last();Check(Revenue(guildLedger,"Gold")==hired.gold&&Revenue(guildLedger,"Ducat")==hired.ducats,"Mercenary Guild credits native Gold and Ducats separately");

    var autoShop=Place(new Shop());autoShop.MaxGold=1000000;autoShop.CollectedGold=1234;NativeSet(autoShop,"_hasWorker",true);autoShop.Progress=1;
    var autoLedger=To(autoShop);var sold=Item();Seed(sold,100);autoLedger.Inputs[0].InputItem(sold);autoShop.Update(0);
    Check(Revenue(autoLedger,"Gold")==autoShop.RecentItems.Last().Value,"Automatic Shop till collection credits only the new sale and excludes its old balance");

    var rewardRecipe=new CraftingRecipe("iron_ingot"){Tag=RecipeTag.Forge,Reagents=new[]{new CraftingReagent("iron_ore",1)},Results=new[]{new CraftingResult("iron_ingot",2)},CraftingReward=new CurrencyCollection().Add(Currency.Faith,6)};
    var forge=Place(new Forge());forge.SetupRecipe(rewardRecipe);var ore=Item("iron_ore");LedgerObserver.SetTrace(ore,new CostTrace());forge.Inputs[0].BufferedItems.Add(ore);forge.Update(0);forge.Progress=1;forge.Update(0);
    var rewardLedger=To(new Stockpile());foreach(var ingot in forge.Outputs[0].BufferedItems){rewardLedger.History.Pass(ingot,0,new Stockpile());}
    Check(Revenue(rewardLedger,"Faith")==6&&forge.Outputs[0].BufferedItems.All(i=>LedgerObserver.Trace(i).Rewards["Faith"]==3),"Native crafting currency rewards follow upstream output provenance and split once across a batch");

    view.Refresh();view.OpenSettings();view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="ChainCostsTab").onClick.Invoke();
    Check(view.Settings.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Requisition Office")&&FinanceAmount(view.Settings,"Requisition","Revenue").StartsWith("+"),"In-game report lists the Requisition Office and its positive currency income");
    Capture(Path.Combine(directory,"ledger-requisition-report.png"));view.Settings.ClosePopup();view.Refresh();
    Check(NodeAmount(view,"Requisition","Net").StartsWith("+")&&NodeAmount(view,"Gold","Net").StartsWith("-"),"Compact main node shows contract currency gain beside upstream Gold loss");
    Capture(Path.Combine(directory,"ledger-requisition-node.png"));
   }
   finally
   {
    if(view.Settings!=null&&view.Settings.IsOpen)view.Settings.ClosePopup();
    foreach(var element in created)if(element.World)WorldFlowManager.Instance.RemoveElement(element);
    main.Outputs[0].ConnectedPipe=oldOutput;if(oldOutput!=null)oldOutput.ConnectedInput.ConnectedPipe=oldOutput;
    main.Inputs[0].ConnectedPipe=oldInput;if(oldInput!=null)oldInput.ConnectedOutput.ConnectedPipe=oldInput;
    LedgerHistory.Clock=oldClock;
   }
  }
 }
}
