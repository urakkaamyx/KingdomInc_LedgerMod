using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
 public sealed partial class LedgerPlugin
 {
  static decimal ObservedCost(Ledger ledger,string currency)
  {
   ledger.History.Summary(60).Costs.Money.TryGetValue(currency,out var value);return value;
  }
  void EnchanterModelChecks()
  {
   var enchanter=new Enchanter();enchanter.SetupRecipe(CraftingRecipe.Get("enchant_weapon_power"));
   var ledger=new Ledger();Connect(ledger.Outputs[0],enchanter.Inputs[0]);
   var item=Item();Seed(item,100);GamePlayer.Current.AddCurrency(Currency.Mana,1000000,false);
   Check(ledger.Inputs[0].InputItem(item),"Native weapon enchantment accepts an item through an upstream Ledger");
   Check(ObservedCost(ledger,"Mana")==0,"Connecting or delivering to Enchanter does not invent a Mana payment");
   long before=Currency.Mana.GetAmount();enchanter.Update(0);long spent=before-Currency.Mana.GetAmount();
   Check(spent>0&&ObservedCost(ledger,"Mana")==spent,"Downstream Enchanter reports its actual successful Mana payment to the Ledger that passed its item");
   enchanter.Update(0);Check(ObservedCost(ledger,"Mana")==spent,"Idle crafting updates do not charge downstream Mana twice");
   enchanter.Progress=1;enchanter.Update(0);
   Check(ReferenceEquals(enchanter.Outputs[0].AvailableItem,item)&&ObservedCost(ledger,"Mana")==spent,"Native enhancement returns its original item without charging completion a second time");
   var after=new Ledger();Connect(after.Outputs[0],new Stockpile().Inputs[0]);
   after.Inputs[0].InputItem(item);
   Check(ObservedCost(after,"Mana")==spent&&ObservedCost(after,"Gold")==100,"Enchanter upstream of a second Ledger retains its complete ordinary cost provenance");

   var waiting=new Enchanter();waiting.SetupRecipe(CraftingRecipe.Get("enchant_weapon_power"));var unpaid=new Ledger();Connect(unpaid.Outputs[0],waiting.Inputs[0]);
   var pending=Item();Seed(pending);unpaid.Inputs[0].InputItem(pending);long balance=Currency.Mana.GetAmount();
   Check(GamePlayer.Current.SpendCurrency(Currency.Mana,balance),"Diagnostic can temporarily exhaust Mana without making a production payment");waiting.Update(0);
   Check(waiting.Input==null&&waiting.Inputs[0].NextItem==pending&&ObservedCost(unpaid,"Mana")==0,"Insufficient Mana leaves the item queued and records no downstream expense");
   GamePlayer.Current.AddCurrency(Currency.Mana,balance,false);
   GamePlayer.Current.SetUpgradeSlotCount("Enchanter",1);waiting.SetUpgrade(0,new Assets.Source.Flow.Upgrades.Artisan());long upgradedCost=waiting.GetCraftingCost().GetCurrency().Single(c=>c.c==Currency.Mana).amt;before=Currency.Mana.GetAmount();waiting.Update(0);
   Check(waiting.GetUpgrade(0) is Assets.Source.Flow.Upgrades.Artisan&&ObservedCost(unpaid,"Mana")==upgradedCost&&ObservedCost(unpaid,"Mana")==before-Currency.Mana.GetAmount()&&waiting.Input!=null,"Later successful crafting records the native upgrade-adjusted Mana debit");

   var paired=new Ledger();paired.SetPairs(2);var mixed=new Enchanter();mixed.SetupRecipe(CraftingRecipe.Get("arcanium_ingot"));
   Connect(paired.Outputs[0],mixed.Inputs[0]);Connect(paired.Outputs[1],mixed.Inputs[1]);
   var iron=Item("iron_ingot");var flux=Item("mercurial_flux");LedgerObserver.SetTrace(iron,new CostTrace());LedgerObserver.SetTrace(flux,new CostTrace());
   paired.Inputs[0].InputItem(iron);mixed.Update(0);Check(ObservedCost(paired,"Mana")==0,"A multi-input Enchanter waits for every ingredient before charging the Ledger");
   paired.Inputs[1].InputItem(flux);before=Currency.Mana.GetAmount();mixed.Update(0);
   Check(ObservedCost(paired,"Mana")==before-Currency.Mana.GetAmount()&&ObservedCost(paired,"Mana")==20,"Two measured input lanes record one Enchanter payment for their shared batch");

   var head=new Ledger();var tail=new Ledger();var chained=new Enchanter();chained.SetupRecipe(CraftingRecipe.Get("enchant_weapon_power"));
   Connect(head.Outputs[0],tail.Inputs[0]);Connect(tail.Outputs[0],chained.Inputs[0]);var weapon=Item();LedgerObserver.SetTrace(weapon,new CostTrace());head.Inputs[0].InputItem(weapon);before=Currency.Mana.GetAmount();chained.Update(0);
   Check(ObservedCost(head,"Mana")==before-Currency.Mana.GetAmount()&&ObservedCost(tail,"Mana")==ObservedCost(head,"Mana"),"Each chained Ledger recognizes the downstream Enchanter payment once");

   var batchRecipe=new CraftingRecipe("iron_ingot"){Tag=RecipeTag.Enchanter,Reagents=new[]{new CraftingReagent("iron_ore",1)},Results=new[]{new CraftingResult("iron_ingot",2)},CraftingCost=new CurrencyCollection().Add(Currency.Mana,12)};
   var batchEnchant=new Enchanter();batchEnchant.SetupRecipe(batchRecipe);var batching=new Ledger();Connect(batching.Outputs[0],batchEnchant.Inputs[0]);var ore=Item("iron_ore");Seed(ore,100);batching.Inputs[0].InputItem(ore);batchEnchant.Update(0);batchEnchant.ProductivityProgress=1;batchEnchant.Update(0);batchEnchant.Progress=1;batchEnchant.Update(0);
   Check(batchEnchant.Outputs[0].BufferedItems.Count==4&&ObservedCost(batching,"Mana")==12,"Native productivity produces extra outputs without a repeated downstream Mana expense");
   Check(batchEnchant.Outputs[0].BufferedItems.Sum(i=>LedgerObserver.Trace(i).Money.TryGetValue("Mana",out var m)?m:0)==12,"Split Enchanter output provenance conserves the one paid batch");

   var continuous=new Enchanter();continuous.SetupRecipe(CraftingRecipe.Get("enchant_weapon_power"));var firstLedger=new Ledger();var secondLedger=new Ledger();
   Connect(firstLedger.Outputs[0],continuous.Inputs[0]);var firstWeapon=Item();LedgerObserver.SetTrace(firstWeapon,new CostTrace());firstLedger.Inputs[0].InputItem(firstWeapon);before=Currency.Mana.GetAmount();continuous.Update(0);long firstPayment=before-Currency.Mana.GetAmount();
   Connect(secondLedger.Outputs[0],continuous.Inputs[0]);var secondWeapon=Item();LedgerObserver.SetTrace(secondWeapon,new CostTrace());secondLedger.Inputs[0].InputItem(secondWeapon);
   Check(ObservedCost(secondLedger,"Mana")==0,"A queued next job has no attributed Mana cost before its payment");
   before=Currency.Mana.GetAmount();continuous.Progress=1;continuous.Update(0);long secondPayment=before-Currency.Mana.GetAmount();
   Check(firstPayment>0&&secondPayment>0&&ObservedCost(firstLedger,"Mana")==firstPayment&&ObservedCost(secondLedger,"Mana")==secondPayment,"Completing one job while starting the next attributes each payment only to its own measured items");
   continuous.Progress=1;continuous.Update(0);
   Check(ObservedCost(firstLedger,"Mana")==firstPayment&&ObservedCost(secondLedger,"Mana")==secondPayment,"Consecutive job completions never add another job's payment to an earlier receipt");

   double now=LedgerHistory.Clock()+10000;var oldClock=LedgerHistory.Clock;LedgerHistory.Clock=()=>now;
   try
   {
    var delayed=new Enchanter();delayed.SetupRecipe(CraftingRecipe.Get("enchant_weapon_power"));var late=new Ledger();Connect(late.Outputs[0],delayed.Inputs[0]);var lateItem=Item();LedgerObserver.SetTrace(lateItem,new CostTrace());late.Inputs[0].InputItem(lateItem);now+=61;before=Currency.Mana.GetAmount();delayed.Update(0);
    Check(late.History.Summary(60).Items.Count==0&&ObservedCost(late,"Mana")==before-Currency.Mana.GetAmount(),"Delayed downstream Mana spending uses its actual payment time after the original passage window expires");
   }
   finally{LedgerHistory.Clock=oldClock;}

  }

  void EnchanterWorldChecks(Ledger main,LedgerWorld view,string directory)
  {
   var clock=LedgerHistory.Clock;LedgerHistory.Clock=()=>clock()+1000000;
   var oldOutput=main.Outputs[0].ConnectedPipe;var oldInput=main.Inputs[0].ConnectedPipe;var created=new List<FlowElement>();
   T Place<T>(T element)where T:FlowElement
   {
    element.Position=new Vector2Int(1800+created.Count*450,700);Assets.Behaviour.Flow.WorldFlowManager.Instance.CreateNewElement(element);created.Add(element);return element;
   }
   try
   {
    GamePlayer.Current.AddCurrency(Currency.Mana,1000000,false);
    var transform=new Enchanter();transform.SetupRecipe(CraftingRecipe.Get("enchanted_cotton_thread"));Place(transform);
    var shop=Place(new Shop());shop.UpdateUpgrades();shop.MaxGold=1000000;
    var store=new Stockpile();var router=Place(new Collector());Connect(main.Outputs[0],store.Inputs[0]);var intoRouter=Connect(store.Outputs[0],router.Inputs[0]);var intoEnchant=Connect(router.Outputs[0],transform.Inputs[0]);var toShop=Connect(transform.Outputs[0],shop.Inputs[0]);
    var cotton=Item("cotton_cloth");Seed(cotton,100);Check(main.Inputs[0].InputItem(cotton),"Ledger can deliver the Enchanter's raw material through native storage");
    store.Update(0);intoRouter.Update(0);router.Update(0);intoEnchant.Update(0);long before=Currency.Mana.GetAmount();transform.Update(0);long spent=before-Currency.Mana.GetAmount();
    Check(spent==15&&ObservedCost(main,"Mana")==spent,"Downstream Enchanter payment reaches Ledger through Stockpile and Collector receipts");
    view.Refresh();view.OpenSettings();view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="ChainCostsTab").onClick.Invoke();
    Check(NodeAmount(view,"Mana","Net")=="-15"&&view.transform.Find("LedgerDisplay/MonetaryTotals/Mana/Net").GetComponent<TMP_Text>().color==LedgerUi.Red,"Main node displays the actual downstream Mana loss in red");
    Check(FinanceAmount(view.Settings,"Mana","Cost")=="-15"&&view.Settings.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Enchanter"),"Detailed report displays the downstream Enchanter stage and its Mana operating cost");
    Capture(Path.Combine(directory,"ledger-downstream-enchanter-report.png"));view.Settings.ClosePopup();Capture(Path.Combine(directory,"ledger-downstream-enchanter-node.png"));
    transform.Progress=1;transform.Update(0);var output=transform.Outputs[0].AvailableItem;
    Check(output!=null&&!ReferenceEquals(output,cotton),"Native material enchantment creates a new output item for receipt-transfer verification");
    toShop.Update(0);shop.Update(0);decimal sale=shop.RecentItems.Last().Value;
    Check(Revenue(main,"Gold")==sale&&ObservedCost(main,"Mana")==15&&main.History.Summary(60).SoldCosts["Mana"]==15,"Transformed Enchanter output preserves the original Ledger receipt through its actual shop sale");
    shop.DoCollect(false);Check(Revenue(main,"Gold")==sale&&ObservedCost(main,"Mana")==15,"Shop collection does not duplicate transformed-item income or Mana spending");
    var bypass=Item("cotton_cloth");transform.Inputs[0].BufferedItems.Add(bypass);transform.Update(0);
    Check(ObservedCost(main,"Mana")==15,"Other Enchanter jobs that bypass Ledger do not charge that Ledger");
    var annihilator=new Enchanter();annihilator.SetupRecipe(CraftingRecipe.Get("annihilate_gemstone"));Place(annihilator);Connect(main.Outputs[0],annihilator.Inputs[0]);
    var gem=Item("ancient_gem");LedgerObserver.SetTrace(gem,new CostTrace());Check(main.Inputs[0].InputItem(gem),"Native Enchanter accepts a measured gemstone for a currency-only recipe");annihilator.Update(0);before=Currency.Mana.GetAmount();annihilator.Progress=1;annihilator.Update(0);
    Check(Currency.Mana.GetAmount()>before&&Revenue(main,"Mana")==Currency.Mana.GetAmount()-before,"Currency-only Enchanter completion reports actual Mana income without requiring an output item");
   }
   finally
   {
    if(view.Settings!=null&&view.Settings.IsOpen)view.Settings.ClosePopup();
    foreach(var e in created)if(e.World)Assets.Behaviour.Flow.WorldFlowManager.Instance.RemoveElement(e);
    main.Outputs[0].ConnectedPipe=oldOutput;if(oldOutput!=null)oldOutput.ConnectedInput.ConnectedPipe=oldOutput;
    main.Inputs[0].ConnectedPipe=oldInput;if(oldInput!=null)oldInput.ConnectedOutput.ConnectedPipe=oldInput;
    LedgerHistory.Clock=clock;view.Refresh();
   }
  }
 }
}
