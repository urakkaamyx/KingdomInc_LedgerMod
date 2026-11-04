using System;

using System.Collections;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using Assets.Behaviour.Flow;

using Assets.Behaviour.UI;

using Assets.Behaviour.UI.TechTree;

using Assets.Source.Flow;

using Assets.Source.Flow.Elements;

using Assets.Source.Items;

using Assets.Source.Player;

using Assets.Source.Util;

using BepInEx;

using HarmonyLib;

using LightJson;

using TMPro;

using UnityEngine;

using UnityEngine.SceneManagement;

using UnityEngine.UI;

using UnityEngine.EventSystems;

using UnityEngine.InputSystem;

using UnityEngine.InputSystem.LowLevel;



namespace KingdomLedger

{

 public sealed partial class LedgerPlugin

 {

  readonly List<string> checks=new List<string>();

  CostTrace allCurrencyTrace;

  void Check(bool passed,string name){if(!passed)throw new InvalidOperationException(name);checks.Add(name);}

  static FlowPipe Connect(FlowOutput output,FlowInput input){var pipe=new FlowPipe();pipe.Connect(output,input);return pipe;}

  static InventoryItem Item(string id="iron_sword")=>new InventoryItem(id);

  static void Seed(InventoryItem item,decimal gold=10000m)

  {

   var trace=new CostTrace();CostTrace.Add(trace.Resources,"iron_ore",3);trace.Stage("Blacksmith / "+LedgerHistory.ItemName(item.Type.Identifier),new Dictionary<string,decimal>{{"Gold",gold}});LedgerObserver.SetTrace(item,trace);

  }

  void ModelChecks()

  {

   var fixture=new GamePlayer();GamePlayer.Current=fixture;fixture.Flows.Add(new FlowDiagram{Label="Ledger tests"});

   LedgerObserver.Reset();

   Check(TechNode.Get(Ledger.TechId)?.Previous.Identifier=="accountant","Ledger research follows Accountant");

   fixture.AddTech("accountant");fixture.AddTech(Ledger.TechId);

   Check(new Ledger().IsPurchaseable(fixture),"Research unlocks construction");

   Check(FlowElement.Create(Ledger.TypeId)is Ledger&&FlowElement.Create("Forge")is Forge,"Native and custom factories resolve");

   var ledger=new Ledger();var parent=new Collector();var receiver=new Stockpile();var input=Connect(parent.Outputs[0],ledger.Inputs[0]);var output=Connect(ledger.Outputs[0],receiver.Inputs[0]);

   var item=Item();Seed(item);parent.Outputs[0].BufferedItems.Add(item);

   Check(ledger.Inputs.Count==1&&ledger.Outputs.Count==1,"Default 1 in / 1 out");

   Check(ledger.IO.All(io=>io.BufferSize==0),"Every Ledger port has zero storage capacity");
   LayoutModelChecks();

   receiver.Inputs[0].BufferedItems.Add(Item());input.Update(0);

   Check(parent.Outputs[0].AvailableItem==item&&!ledger.IO.Any(io=>io.HasBuffer),"Full downstream queue leaves original item at parent");

   Check(ledger.History.Summary(60).Items.Count==0,"Blocked transfers do not increment counters");

   receiver.Inputs[0].BufferedItems.Clear();receiver.Contents.AddRange(Enumerable.Range(0,receiver.Capacity).Select(_=>Item()));input.Update(0);

   Check(parent.Outputs[0].AvailableItem==item,"Full internal stockpile refuses delivery before input queue is used");

   receiver.Contents.Clear();receiver.Inputs[0].Filter=i=>false;input.Update(0);Check(parent.Outputs[0].AvailableItem==item,"Destination filter refuses delivery without moving item");

   receiver.Inputs[0].Filter=i=>true;input.Update(0);

   Check(!parent.Outputs[0].HasBuffer&&ReferenceEquals(receiver.Inputs[0].NextItem,item),"Successful pipe update forwards the original object directly");

   Check(ledger.IO.All(io=>!io.HasBuffer),"Successful transfer never leaves a Ledger buffer occupied");

   output.Update(0);input.Update(0);Check(ledger.History.Summary(60).Items["iron_sword"]==1,"Idle outgoing pipes do not double count");

   Check(ledger.History.Summary(60).Costs.Resources["iron_ore"]==3,"Passed item carries full upstream raw resource count");

   Check(!ledger.History.Summary(60).SaleSeen,"Storage destination is not counted as a sale");

   Check(!ledger.HasShopOutput&&ledger.ShowFinancials(ledger.History.Summary(60)),"Paid items retain financial reporting through a Stockpile destination");

   Check(ledger.SetPairs(5)&&ledger.Inputs.Count==5&&ledger.Outputs.Count==5&&!ledger.SetPairs(6),"Paired ports expand to five and reject six");

   var p2=new Collector();var r2=new Stockpile();var pipe2=Connect(p2.Outputs[0],ledger.Inputs[1]);Connect(ledger.Outputs[1],r2.Inputs[0]);

   var second=Item("iron_helm");p2.Outputs[0].BufferedItems.Add(second);parent.Outputs[0].BufferedItems.Add(Item());pipe2.Update(0);input.Update(0);

   Check(r2.Inputs[0].NextItem==second&&parent.Outputs[0].HasBuffer,"One blocked lane does not block a different free lane");

   var last=new Stockpile();Connect(ledger.Outputs[4],last.Inputs[0]);Check(!ledger.SetPairs(4),"Connected last pair cannot be removed");

   last.Inputs[0].ConnectedPipe=null;ledger.Outputs[4].ConnectedPipe=null;Check(ledger.SetPairs(4),"Disconnected final pair can be removed");

   ledger.SetPeriod(3600);var copy=(Ledger)ledger.CreateCopy();Check(copy.Period==3600&&copy.Inputs.Count==4&&copy.History.Summary(3600).Items.Count==0,"Copies preserve settings without copying accounting history");

   var loaded=(Ledger)FlowElement.FromJson(ledger.ToJson(),fixture.Flows[0]);Check(loaded.Period==3600&&loaded.Inputs.Count==4&&loaded.IO.All(io=>!io.HasBuffer),"Native save serialization preserves settings and zero storage");

   var l1=new Ledger();var l2=new Ledger();Connect(l1.Outputs[0],l2.Inputs[0]);Connect(l2.Outputs[0],l1.Inputs[0]);

   Check(!l1.Inputs[0].CanAcceptItem(Item()),"Cyclic Ledger wiring rejects without recursion overflow");

   var isolated=new Ledger();Check(!isolated.Inputs[0].InputItem(Item()),"Disconnected direct admission is rejected");

   var source=new Collector();var sink=new Stockpile();Connect(l2.Outputs[0],sink.Inputs[0]);var chain=Connect(source.Outputs[0],l1.Inputs[0]);var chained=Item();Seed(chained);source.Outputs[0].BufferedItems.Add(chained);chain.Update(0);

   var sellingLedger=new Ledger();var sellingChain=new Ledger();var sellingShop=new Shop();Connect(sellingLedger.Outputs[0],sellingChain.Inputs[0]);Connect(sellingChain.Outputs[0],sellingShop.Inputs[0]);

   Check(sellingLedger.HasShopOutput&&sellingChain.HasShopOutput,"Shop display mode follows chained Ledger outputs");

   Check(sink.Inputs[0].NextItem==chained&&l1.History.Summary(60).Items["iron_sword"]==1&&l2.History.Summary(60).Items["iron_sword"]==1,"Chained Ledgers forward atomically and each count once");

   Check(l1.IO.Concat(l2.IO).All(io=>!io.HasBuffer),"Chained Ledgers retain no items");

   Check(ledger.History.Summary(60).UnknownPassed==1,"Unobserved pre-existing items report incomplete cost history");

   double time=100;var oldClock=LedgerHistory.Clock;LedgerHistory.Clock=()=>time;

   try{var history=new LedgerHistory();var counted=Item();Seed(counted);history.Pass(counted,0,sink);time=101.01;Check(history.Summary(1).Items.Count==0&&history.Summary(60).Items.Count==1,"Second and minute windows expire independently");time=3701;Check(history.Summary(3600).Items.Count==0,"Hour window prunes old aggregates");}

   finally{LedgerHistory.Clock=oldClock;}

   var a=new CostTrace();a.Stage("A",new Dictionary<string,decimal>{{"Gold",10000}});CostTrace.Add(a.Resources,"iron_ore",1);var combined=new CostTrace();combined.Merge(a);combined.Merge(a);

   Check(combined.Money["Gold"]==20000&&combined.Resources["iron_ore"]==2,"Distinct consumed ingredients are added once each");

   Check(LedgerHistory.Money(new Dictionary<string,decimal>{{"Gold",10000},{"Mana",2}}).Contains("1g 0s 0c"),"Gold units are formatted using the game's 10,000 internal units per gold");

   Check(LedgerHistory.Net(new Dictionary<string,decimal>{{"Gold",20000}},new Dictionary<string,decimal>{{"Gold",30000}})=="-1g 0s 0c","Loss calculation preserves negative values");

   Check(LedgerHistory.Gold(66205)=="6g 62s 5c"&&LedgerHistory.Gold(-105)=="-0g 1s 5c","Gold formatting preserves gold, silver, copper and negative values");

   var paidBalance=new LedgerSummary();CostTrace.Add(paidBalance.Costs.Money,"Gold",20000);CostTrace.Add(paidBalance.Costs.Money,"Requisition",3);

   Check(paidBalance.Profit.Contains("-2g 0s 0c")&&paidBalance.Profit.Contains("-3 Requisition"),"No-sale costs produce losses with each currency kept separate");

   paidBalance.UnknownPassed=1;Check(!paidBalance.ProfitComplete&&paidBalance.Profit.Contains("unavailable"),"Unknown unsold costs prevent a misleading complete profit or loss");

   var freeLedger=new Ledger();var freeRouter=new Collector();Connect(freeRouter.Outputs[0],freeLedger.Inputs[0]);

   Check(!freeLedger.ShowFinancials(freeLedger.History.Summary(60)),"Building purchase prices do not turn a free passthrough chain into an operating expense");

   // Actual resource / recipe methods, with all effects silenced only in diagnostics.

   fixture.AddCurrency(Currency.Gold,10000000,false);

   var mine=new Mine();mine.SetupResource((ResourceItemType)ItemType.Get("iron_ore"));mine.DoManualCraft();mine.Progress=1;mine.Update(0);

   var ore=mine.Outputs[0].AvailableItem;Check(ore!=null&&LedgerObserver.Trace(ore).Complete,"Actual resource production receives observed provenance");

   var forge=new Forge();forge.SetupRecipe(CraftingRecipe.Get("iron_ingot"));forge.Inputs[0].BufferedItems.Add(ore);forge.Update(0);forge.Progress=1;forge.Update(0);

   var ingot=forge.Outputs[0].AvailableItem;Check(ingot!=null&&LedgerObserver.Trace(ingot).Complete&&LedgerObserver.Trace(ingot).Resources["iron_ore"]==1,"Actual recipe inherits resource provenance across craft start/completion");

   Check(LedgerObserver.Trace(ingot).Stages.Keys.Any(k=>k.StartsWith("Forge")),"Crafted item includes intermediate production stage");

   var oreType=(ResourceItemType)ItemType.Get("iron_ore");var previousCost=oreType.ResourceCost;

   InventoryItem paidOre=null;

   try

   {

    oreType.ResourceCost=new CurrencyCollection().Add(Currency.Gold,20000);

    var paidMine=new Mine();paidMine.SetupResource(oreType);paidMine.DoManualCraft();paidMine.Progress=1;paidMine.Update(0);paidOre=paidMine.Outputs[0].AvailableItem;

    Check(LedgerObserver.Trace(paidOre).Money["Gold"]==20000,"Paid resource queue attaches the actual successful currency debit");

   }finally{oreType.ResourceCost=previousCost;}

   var testRecipe=new CraftingRecipe("ledger_diagnostic_recipe"){Tag=RecipeTag.Forge,Reagents=new[]{new CraftingReagent("iron_ore",1)},Results=new[]{new CraftingResult("iron_ingot",2)},CraftingCost=new CurrencyCollection().Add(Currency.Gold,30000)};

   var paidForge=new Forge();paidForge.SetupRecipe(testRecipe);paidForge.Inputs[0].BufferedItems.Add(paidOre);paidForge.Update(0);paidForge.ProductivityProgress=1;paidForge.Update(0);

   Check(paidForge.Outputs[0].BufferedItems.Count==2&&paidForge.Outputs[0].BufferedItems.All(i=>LedgerObserver.Trace(i).Money["Gold"]==25000),"Batch outputs split inherited and crafting costs without doubling them");

   paidForge.Progress=1;paidForge.Update(0);var total=new CostTrace();foreach(var result in paidForge.Outputs[0].BufferedItems)total.Merge(LedgerObserver.Trace(result));

   Check(paidForge.Outputs[0].BufferedItems.Count==4&&total.Money["Gold"]==50000&&total.Resources["iron_ore"]==1,"Productivity output conserves batch costs and consumed resources");

   var paidRecipeLedger=new Ledger();Connect(paidForge.Outputs[0],paidRecipeLedger.Inputs[0]);

   Check(paidRecipeLedger.ShowFinancials(paidRecipeLedger.History.Summary(60)),"Connected paid crafting recipe enables the financial view before an item passes");

   var allCost=new CurrencyCollection();int currencyIndex=0;

   foreach(Currency currency in Enum.GetValues(typeof(Currency))){fixture.AddCurrency(currency,1000000,false);allCost.Add(currency,currency==Currency.Gold?66205:++currencyIndex+1);}

   var mixedRecipe=new CraftingRecipe("iron_ingot"){Tag=RecipeTag.Forge,Reagents=new[]{new CraftingReagent("iron_ore",1)},Results=new[]{new CraftingResult("iron_ingot",1)},CraftingCost=allCost};

   var mixedForge=new Forge();mixedForge.SetupRecipe(mixedRecipe);var mixedOre=Item("iron_ore");LedgerObserver.SetTrace(mixedOre,new CostTrace());mixedForge.Inputs[0].BufferedItems.Add(mixedOre);mixedForge.Update(0);mixedForge.Progress=1;mixedForge.Update(0);

   allCurrencyTrace=LedgerObserver.Trace(mixedForge.Outputs[0].AvailableItem).Copy();

   Check(allCurrencyTrace.Complete&&allCurrencyTrace.Money.Count==9&&allCost.GetCurrency().All(p=>allCurrencyTrace.Money[p.c.ToString()]==p.amt),"Native recipe spending records all nine currencies, not just Gold");

   var cleaningSource=new Collector();var cleaningLedger=new Ledger();var cleaningSink=new Stockpile();Connect(cleaningSource.Outputs[0],cleaningLedger.Inputs[0]);Connect(cleaningLedger.Outputs[0],cleaningSink.Inputs[0]);

   var cleanupItem=Item();cleaningSource.Outputs[0].BufferedItems.Add(cleanupItem);PrepareClear(cleaningSource);

   Check(cleaningSink.Inputs[0].NextItem==cleanupItem&&cleaningLedger.IO.All(io=>!io.HasBuffer),"Source clearing forwards directly instead of populating Ledger buffers");

   PrepareClear(cleaningSink);Check(cleaningSource.Outputs[0].AvailableItem==cleanupItem&&cleaningLedger.IO.All(io=>!io.HasBuffer),"Destination clearing returns queued items to the original producer through Ledger");

   fixture.ForestSettings=new ForestSettings();var forest=new Forest();forest.SetupResource((Assets.Source.Items.Type.ForestItemType)ItemType.Get("wood_log"));forest.DoManualCraft();forest.Progress=1;forest.Update(0);

   var log=forest.Outputs[0].AvailableItem;Check(log!=null&&LedgerObserver.Trace(log).Complete&&LedgerObserver.Trace(log).Resources["wood_log"]==1,"Native Forest harvesting carries its raw resources and observed harvest costs");

   EnchanterModelChecks();

   OutfitterChecks();

  }

  sealed class OutfitFixture

  {

   internal Ledger Ledger=new Ledger();internal Stockpile Sink=new Stockpile();

   internal Mine Mine=new Mine();internal Forge Forge=new Forge();internal Blacksmith Smith=new Blacksmith();internal Barracks Barracks=new Barracks();internal Outfitter Outfitter=new Outfitter();

   internal FlowPipe OrePipe,IngotPipe,EquipmentPipe,HumanPipe,OutputPipe;

   internal OutfitFixture()

   {

    Mine.SetupResource((ResourceItemType)ItemType.Get("iron_ore"));Forge.SetupRecipe(CraftingRecipe.Get("iron_ingot"));Smith.SetupRecipe(CraftingRecipe.Get("iron_helm"));Barracks.SetupResource((ResourceItemType)ItemType.Get("conscript"));Outfitter.Start();

    OrePipe=Connect(Mine.Outputs[0],Forge.Inputs[0]);IngotPipe=Connect(Forge.Outputs[0],Smith.Inputs[0]);

    EquipmentPipe=Connect(Smith.Outputs[0],Outfitter.Inputs[1]);HumanPipe=Connect(Barracks.Outputs[0],Outfitter.Inputs[0]);

    OutputPipe=Connect(Outfitter.Outputs[0],Ledger.Inputs[0]);Connect(Ledger.Outputs[0],Sink.Inputs[0]);

   }

   internal InventoryItem Produce()

   {

    GamePlayer.Current.AddCurrency(Currency.Requisition,100,false);

    for(int i=0;i<2;i++)

    {

     Mine.DoManualCraft();Mine.Progress=1;Mine.Update(0);OrePipe.Update(0);

     Forge.Update(0);Forge.Progress=1;Forge.Update(0);IngotPipe.Update(0);

    }

    Smith.Update(0);Smith.Progress=1;Smith.Update(0);EquipmentPipe.Update(0);

    Barracks.DoManualCraft();Barracks.Progress=1;Barracks.Update(0);HumanPipe.Update(0);

    Outfitter.Update(0);Outfitter.Queued=true;Outfitter.Progress=1;Outfitter.Update(0);OutputPipe.Update(0);

    return Sink.Inputs[0].NextItem;

   }

  }

  void OutfitterChecks()

  {

   var chain=new OutfitFixture();var topology=LedgerChain.Read(chain.Ledger);

   Check(topology.Elements.Count==6&&topology.Stages.Count==5&&topology.Resources.SetEquals(new[]{"iron_ore","conscript"}),"Connected chain includes both equipment and human branches before any item passes");

   var human=chain.Produce();Check(human!=null,"Native Mine to Forge to Blacksmith plus Barracks to Outfitter chain delivers through Ledger");

   var trace=LedgerObserver.Trace(human);var summary=chain.Ledger.History.Summary(60);

   Check(trace.Complete&&summary.UnknownPassed==0,"Outfitter retains complete provenance across separate intake and completion updates");

   Check(trace.Resources["iron_ore"]==2&&trace.Resources["conscript"]==1,"Equipped human retains upstream ore and recruitment quantities exactly once");

   Check(trace.Stages.Count==5&&trace.Stages["Outfitter"]==1&&trace.Stages.Keys.Any(k=>k.StartsWith("Mine /"))&&trace.Stages.Keys.Any(k=>k.StartsWith("Forge /"))&&trace.Stages.Keys.Any(k=>k.StartsWith("Blacksmith /"))&&trace.Stages.Keys.Any(k=>k.StartsWith("Barracks /")),"Ledger receives all five actual production stages through Outfitter");

   Check(trace.Money["Requisition"]==2,"Barracks recruitment payment survives Outfitter completion");

   var sword=Item();Seed(sword);var next=new Outfitter();next.Start();next.Inputs[0].BufferedItems.Add(human);next.Inputs[1].BufferedItems.Add(sword);next.Update(0);next.Queued=true;next.Progress=1;next.Update(0);

   var equipped=next.Outputs[0].AvailableItem;var combined=LedgerObserver.Trace(equipped);

   Check(ReferenceEquals(equipped,human)&&combined.Complete&&combined.Resources["iron_ore"]==5&&combined.Stages["Outfitter"]==2&&combined.Money["Requisition"]==2,"Successive Outfitters merge new equipment without doubling earlier human or gear costs");

   var skipped=new Outfitter();skipped.Start();skipped.Inputs[0].BufferedItems.Add(human);skipped.Inputs[1].BufferedItems.Add(Item("iron_helm"));skipped.Update(0);

   Check(skipped.Inputs[1].HasBuffer&&ReferenceEquals(skipped.Outputs[0].AvailableItem,human)&&LedgerObserver.Trace(human).Resources["iron_ore"]==5,"Duplicate equipment passthrough preserves history and does not charge unequipped gear");

   var timed=new Outfitter{Timeout=1};timed.Start();timed.Inputs[0].BufferedItems.Add(human);timed.Update(2);

   Check(ReferenceEquals(timed.Outputs[0].AvailableItem,human)&&LedgerObserver.Trace(human).Complete&&LedgerObserver.Trace(human).Resources["iron_ore"]==5,"Outfitter timeout passthrough preserves upstream history");

   var loaded=new Outfitter{CurrentHuman=Item("conscript"),CurrentEquip=Item("iron_helm"),Queued=true,Progress=1};loaded.Update(0);

   Check(!LedgerObserver.Trace(loaded.Outputs[0].AvailableItem).Complete,"Loaded Outfitter work without observed inputs remains explicitly incomplete");

   var training=new TrainingGrounds();training.Inputs[0].BufferedItems.Add(human);training.Update(0);training.Contents.First(s=>s!=null).TicksRemaining=0;training.Update(0);

   var trained=training.Outputs[0].AvailableItem;

   Check(trained!=null&&LedgerObserver.Trace(trained).Resources["iron_ore"]==5&&!LedgerObserver.Trace(trained).Complete,"Unsupported delayed utilities preserve known upstream resources without claiming complete extra costs");

   var loopA=new Ledger();var loopB=new Ledger();Connect(loopA.Outputs[0],loopB.Inputs[0]);Connect(loopB.Outputs[0],loopA.Inputs[0]);

   Check(LedgerChain.Read(loopA).Elements.Count==2,"Connected-chain traversal safely terminates on cyclic wiring");

  }

  IEnumerator RuntimeCheck()

  {

   yield return new WaitForSecondsRealtime(5);string directory=Path.Combine(Paths.GameRootPath,"_Mods","LedgerMod","Verification");Directory.CreateDirectory(directory);string error=null;

   try

   {

    if(GameUI.Instance||GamePlayer.Current!=null)throw new InvalidOperationException("Diagnostic must start without a loaded kingdom");

    ModelChecks();var fixture=GamePlayer.CreateNewPlayer();fixture.Flows[0].Elements.Clear();fixture.Flows[0].Pipes.Clear();fixture.Flows[0].Label="Ledger verification";fixture.AddTech("accountant");fixture.AddTech(Ledger.TechId);fixture.AddTech("shop");

    var ledger=new Ledger{Position=new Vector2Int(-200,142)};fixture.Flows[0].Elements.Add(ledger);

    var shop=new Shop{Position=new Vector2Int(300,142)};shop.UpdateUpgrades();fixture.Flows[0].Elements.Add(shop);

    var pipe=Connect(ledger.Outputs[0],shop.Inputs[0]);fixture.Flows[0].Pipes.Add(pipe);

    fixture.Flows[0].Elements.Add(new LuxuryShop{Position=new Vector2Int(900,142)});

    SceneManager.LoadScene("Game");

   }catch(Exception e){error=e.ToString();}

   if(error==null)

   {

    yield return new WaitForSecondsRealtime(4);

    try

    {

     var ledger=GamePlayer.Current.Flows[0].Elements.OfType<Ledger>().Single();var view=ledger.World.GetComponent<LedgerWorld>();

     var gear=ledger.World.GetComponentsInChildren<Button>().Single(b=>b.name=="LedgerGear");

     Canvas.ForceUpdateCanvases();var canvas=gear.GetComponentInParent<Canvas>();

     var screen=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,gear.transform.TransformPoint(((RectTransform)gear.transform).rect.center));

     var pointer=new PointerEventData(EventSystem.current){position=screen,button=PointerEventData.InputButton.Left};

     var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);

     Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==gear.gameObject,"Gear is the first pointer target before opening");

     ExecuteEvents.Execute(gear.gameObject,pointer,ExecuteEvents.pointerClickHandler);

     var popup=view.Settings;var popupCanvas=popup.GetComponent<Canvas>();var rootCanvas=popupCanvas.rootCanvas;

     GameUI.Instance.ToggleUI(false);

     Check(rootCanvas.enabled,"Popup remains on an enabled canvas when native HUD canvases are hidden");

     Logger.LogInfo("LEDGER_POPUP_RENDER: popup="+popup.IsOpen+" owner="+view.isActiveAndEnabled+" flow="+WorldFlowManager.Visible+" layer="+popup.gameObject.layer+" canvas="+popupCanvas.renderMode+" root="+rootCanvas.name+" camera="+rootCanvas.worldCamera?.name+" mask="+rootCanvas.worldCamera?.cullingMask);

    }catch(Exception e){error=e.ToString();}

    yield return new WaitForSecondsRealtime(.5f);

    if(error==null)try

    {

     var view=GamePlayer.Current.Flows[0].Elements.OfType<Ledger>().Single().World.GetComponent<LedgerWorld>();

     Check(view.Settings!=null&&view.Settings.IsOpen,"Gear report remains open after subsequent game frames");

     var panel=(RectTransform)view.Settings.transform.Find("LedgerPanel");var corners=new Vector3[4];panel.GetWorldCorners(corners);

     Check(corners.All(c=>c.x>=0&&c.x<=Screen.width&&c.y>=0&&c.y<=Screen.height),"Settings panel is fully inside the actual screen bounds");

     var popupCanvas=view.Settings.GetComponent<Canvas>();

     Check(popupCanvas.isRootCanvas&&popupCanvas.enabled&&popupCanvas.renderMode==RenderMode.ScreenSpaceOverlay,"Settings renders on its own enabled screen overlay");

     var hour=view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="Period3600");

     var hourScreen=RectTransformUtility.WorldToScreenPoint(null,hour.transform.TransformPoint(((RectTransform)hour.transform).rect.center));

     var pointer=new PointerEventData(EventSystem.current){position=hourScreen,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);

     Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==hour.gameObject,"Settings controls are the topmost visible pointer targets with HUD hidden");

     ExecuteEvents.Execute(hour.gameObject,pointer,ExecuteEvents.pointerClickHandler);

     Check(view.Element.Period==3600,"Pointer click changes the reporting period with native HUD hidden");view.Element.SetPeriod(60);

     var close=view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="XButton");

     ExecuteEvents.Execute(close.gameObject,pointer,ExecuteEvents.pointerClickHandler);

     Check(!view.Settings.IsOpen,"Settings close button dismisses the independent overlay with HUD hidden");

     GameUI.Instance.ToggleUI(true);

    }catch(Exception e){error=e.ToString();}

   }

   if(error==null)

   {

    Mouse diagnosticMouse=null;Keyboard diagnosticKeyboard=null;Vector2 gearScreen=Vector2.zero;

    try

    {

     var view=GamePlayer.Current.Flows[0].Elements.OfType<Ledger>().Single().World.GetComponent<LedgerWorld>();

     Check(view.Settings==null||!view.Settings.IsOpen,"Report is closed before real mouse input test");

     var gear=view.GetComponentsInChildren<Button>().Single(b=>b.name=="LedgerGear");var canvas=gear.GetComponentInParent<Canvas>();

     gearScreen=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,gear.transform.TransformPoint(((RectTransform)gear.transform).rect.center));

     InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;

     GameUI.Instance.ToggleUI(false);

     diagnosticMouse=InputSystem.AddDevice<Mouse>("LedgerDiagnosticMouse");

     InputSystem.QueueStateEvent(diagnosticMouse,new MouseState{position=gearScreen});

    }catch(Exception e){error=e.ToString();}

    yield return new WaitForSecondsRealtime(.3f);

    if(error==null)InputSystem.QueueStateEvent(diagnosticMouse,new MouseState{position=gearScreen,buttons=1});

    yield return new WaitForSecondsRealtime(.1f);

    if(error==null)InputSystem.QueueStateEvent(diagnosticMouse,new MouseState{position=gearScreen});

    yield return new WaitForSecondsRealtime(.3f);

    if(error==null)try

    {

     var view=GamePlayer.Current.Flows[0].Elements.OfType<Ledger>().Single().World.GetComponent<LedgerWorld>();

     Check(view.Settings!=null&&view.Settings.IsOpen,"Real input-module mouse press and release opens Ledger gear report with HUD hidden");

     diagnosticKeyboard=InputSystem.AddDevice<Keyboard>("LedgerDiagnosticKeyboard");

     InputSystem.QueueStateEvent(diagnosticKeyboard,new KeyboardState(Key.Escape));

    }catch(Exception e){error=e.ToString();}

    yield return new WaitForSecondsRealtime(.1f);

    if(diagnosticKeyboard!=null)InputSystem.QueueStateEvent(diagnosticKeyboard,new KeyboardState());

    yield return new WaitForSecondsRealtime(.1f);

    if(error==null)try

    {

     var view=GamePlayer.Current.Flows[0].Elements.OfType<Ledger>().Single().World.GetComponent<LedgerWorld>();

     Check(!view.Settings.IsOpen,"Real Escape key closes settings with native HUD hidden");

    }catch(Exception e){error=e.ToString();}

    GameUI.Instance.ToggleUI(true);

    if(diagnosticKeyboard!=null)InputSystem.RemoveDevice(diagnosticKeyboard);

    if(diagnosticMouse!=null)InputSystem.RemoveDevice(diagnosticMouse);

   }

   if(error==null)

   {

    try

    {

     var ledger=GamePlayer.Current.Flows[0].Elements.OfType<Ledger>().Single();var shop=GamePlayer.Current.Flows[0].Elements.OfType<Shop>().Single();var view=ledger.World.GetComponent<LedgerWorld>();

     Check(view!=null,"Ledger prefab renders in the actual game scene");Check(ledger.World.GetComponentsInChildren<WorldFlowIO>().Length==2,"Default node renders two physical ports");

     Check(FlowElement.GetBuildableTypes().Count(e=>e is Ledger)==1,"Build catalog includes exactly one Ledger");

     var item=Item();Seed(item,10000);Check(ledger.Inputs[0].InputItem(item),"Item is directly admitted into connected native shop");

     Check(!ledger.History.Summary(60).SaleSeen,"Shop connection alone does not count an unsold item as revenue");

     shop.Update(0);var s=ledger.History.Summary(60);

     Check(s.Sold==1&&s.Revenue["Gold"]==shop.RecentItems.Last().Value,"Actual shop sale credits its observed price");

     Check(s.SoldCosts["Gold"]==10000&&s.UnknownSold==0,"Profit uses the sold item's complete cost basis");

     decimal revenue=s.Revenue["Gold"];shop.DoCollect(false);Check(ledger.History.Summary(60).Revenue["Gold"]==revenue,"Till collection is not counted as a second sale");

     var luxury=GamePlayer.Current.Flows[0].Elements.OfType<LuxuryShop>().Single();shop.Inputs[0].ConnectedPipe=null;Connect(ledger.Outputs[0],luxury.Inputs[0]);

     var luxuryItem=Item();luxuryItem.SetQuality(1.5f);Seed(luxuryItem,20000);Check(ledger.Inputs[0].InputItem(luxuryItem),"Luxury Shop is automatically recognized as a compatible sale destination");

     luxury.Update(0);var luxurySale=luxury.RecentItems.Last();var afterLuxury=ledger.History.Summary(60);

     Check(afterLuxury.Sold==2&&afterLuxury.Revenue["Gold"]==revenue+luxurySale.gold,"Luxury Shop records actual quality-adjusted earnings");

     afterLuxury.Revenue.TryGetValue("Crown",out var crowns);Check(crowns==luxurySale.crowns,"Additional Luxury Shop currency stays separate from gold");

     luxury.Inputs[0].ConnectedPipe=null;Connect(ledger.Outputs[0],shop.Inputs[0]);

     var unknown=Item("iron_helm");Check(ledger.Inputs[0].InputItem(unknown),"Different item types pass through the same lane");shop.Update(0);

     Check(ledger.History.Summary(60).UnknownSold==1&&ledger.History.Summary(60).Profit.Contains("unavailable"),"Unknown sold-item costs suppress misleading profit");

     var item2=Item();Seed(item2);ledger.Inputs[0].InputItem(item2);shop.CollectedGold=shop.MaxGold;Check(!ledger.Inputs[0].InputItem(Item()),"Full shop till refuses further delivery");Check(ledger.HasShopOutput,"Full shop retains the monetary display mode");shop.Inputs[0].BufferedItems.Clear();shop.CollectedGold=0;

     view.Refresh();Check(ledger.World.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains(LedgerHistory.ItemName("iron_sword"))),"Main node lists actual item names outside gear popup");

     WorldFlowCamera.Instance.SetZoomLevel(3);Capture(Path.Combine(directory,"ledger-node.png"));

     var gear=ledger.World.GetComponentsInChildren<Button>().Single(b=>b.name=="LedgerGear");

     Canvas.ForceUpdateCanvases();var gearCanvas=gear.GetComponentInParent<Canvas>();

     var screen=RectTransformUtility.WorldToScreenPoint(gearCanvas.worldCamera,gear.transform.TransformPoint(((RectTransform)gear.transform).rect.center));

     var pointer=new PointerEventData(EventSystem.current){position=screen,button=PointerEventData.InputButton.Left};

     var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);

     Logger.LogInfo("LEDGER_GEAR_RAYCAST: "+screen+" layer="+gear.gameObject.layer+" canvas="+gearCanvas.name+" enabled="+gear.enabled+" interactable="+gear.IsInteractable()+" hits="+string.Join(",",hits.Select(h=>h.gameObject.name+"/"+h.module.GetType().Name)));

     Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==gear.gameObject,"Gear is reachable by the actual screen pointer raycast");

     ExecuteEvents.Execute(gear.gameObject,pointer,ExecuteEvents.pointerClickHandler);

     Check(view.Settings.IsOpen,"Gear opens configuration report");

     view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="Period3600").onClick.Invoke();Check(ledger.Period==3600,"Gear changes reporting window to hour");

     view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="Period60").onClick.Invoke();

     for(int i=0;i<4;i++)view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="AddLedgerPair").onClick.Invoke();

     Check(ledger.Inputs.Count==5&&ledger.World.GetComponentsInChildren<WorldFlowIO>().Length==10,"Gear expansion rebuilds all ten physical ports");

     Check(!view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="AddLedgerPair").interactable,"Gear disables additions at five pairs");

     Capture(Path.Combine(directory,"ledger-chain-report.png"));

     view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="ItemsPassedTab").onClick.Invoke();

     Check(view.Settings.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==LedgerHistory.ItemName("iron_sword")),"Item tab displays native item names in rows");

     Capture(Path.Combine(directory,"ledger-items.png"));

     Check(!view.Settings.GetComponentsInChildren<Button>().Any(b=>b.name=="OutputsTab"),"Outputs tab is removed from Ledger settings");

     GameUI.Instance.ProcessEscape();Check(!view.Settings.IsOpen,"Escape closes report");

     Check(ledger.World.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains(LedgerHistory.ItemName("iron_sword"))),"Closing report preserves item list on main node");

     Capture(Path.Combine(directory,"ledger-five-pairs.png"));

     CaptureStylePreview(ledger,view,shop,directory);
     IncomeChecks(ledger,view,directory);
     TimerChecks(ledger,view,directory);
     LayoutWorldChecks(ledger,view,directory);
     EnchanterWorldChecks(ledger,view,directory);

    }catch(Exception e){error=e.ToString();}

   }

   if(error==null){yield return StartCoroutine(PlacementCheck(directory));error=placementError;}

   var result=new JsonObject{{"passed",error==null},{"savedKingdomLoaded",false},{"saveWritesBlocked",true},{"version",Version},{"gameVersion",Application.version},{"unityVersion",Application.unityVersion},{"testedAtUtc",DateTime.UtcNow.ToString("O")}};

   var names=new JsonArray();foreach(var name in checks)names.Add(name);result["checks"]=names;if(error!=null)result["error"]=error;

   File.WriteAllText(Path.Combine(directory,"runtime-tests.json"),result.ToString(true));if(error==null)Logger.LogInfo("LEDGER_RUNTIME_PASS: "+checks.Count+" checks");else Logger.LogError(error);

   yield return new WaitForSecondsRealtime(1);Application.Quit();

  }

  string placementError;

  IEnumerator PlacementCheck(string directory)

  {

   Ledger placed=null;

   try

   {

    // This is the same immediate reopen callback used by WorldFlowGhost after

    // native placement, before the new MonoBehaviour's first Start callback.

    placed=new Ledger{Position=new Vector2Int(-650,-300)};WorldFlowManager.Instance.CreateNewElement(placed);

    Check(placed.World!=null,"Native placement creates a fresh Ledger world node");

    placed.HandleReopenBuyMenu();

    Check(placed.World.GetComponent<LedgerWorld>().Settings?.IsOpen==true,"Placement callback opens settings immediately after native binding, before Start");

   }

   catch(Exception e){placementError=e.ToString();}

   yield return null;

   yield return new WaitForSecondsRealtime(.2f);

   if(placementError==null)try

   {

    var view=placed.World.GetComponent<LedgerWorld>();

    Check(view.Settings!=null&&view.Settings.IsOpen,"Fresh placement opens configuration before the node's first Start callback");

    Check(view.GetComponentsInChildren<Button>().Count(b=>b.name=="LedgerGear")==1&&view.transform.Cast<Transform>().Count(t=>t.name=="LedgerDisplay")==1,"Start after placement does not duplicate the gear or display");

    view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="Period3600").onClick.Invoke();

    view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="AddLedgerPair").onClick.Invoke();

    Check(placed.Period==3600&&placed.Inputs.Count==2,"New-node settings change the newly placed Ledger");

    Check(GamePlayer.Current.Flows[0].Elements.OfType<Ledger>().First(l=>l!=placed).Period==60,"New-node settings do not edit the existing Ledger");

    Capture(Path.Combine(directory,"ledger-new-placement.png"));

    view.Settings.ClosePopup();

    var gear=view.GetComponentsInChildren<Button>().Single(b=>b.name=="LedgerGear");Canvas.ForceUpdateCanvases();

    var canvas=gear.GetComponentInParent<Canvas>();var screen=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,gear.transform.TransformPoint(((RectTransform)gear.transform).rect.center));

    var pointer=new PointerEventData(EventSystem.current){position=screen,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);

    Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==gear.gameObject,"Freshly placed node's gear is the actual pointer target after closing placement settings");

    ExecuteEvents.Execute(gear.gameObject,pointer,ExecuteEvents.pointerClickHandler);

    Check(view.Settings.IsOpen,"New-node gear reopens configuration after automatic placement settings close");

    view.Settings.ClosePopup();

   }

   catch(Exception e){placementError=e.ToString();}

   if(placed!=null&&placed.World)WorldFlowManager.Instance.RemoveElement(placed);

  }

  void CaptureStylePreview(Ledger ledger,LedgerWorld view,Shop shop,string directory)

  {

   // UI showcase data lives only in the disposable, save-blocked diagnostic.

   // Advance its reporting window so accounting edge-case fixtures stay separate.

   var clock=LedgerHistory.Clock;

   try

   {

    LedgerHistory.Clock=()=>clock()+120;ledger.SetPeriod(60);

    foreach(var id in new[]{"iron_sword","iron_helm"})for(int i=0;i<(id=="iron_sword"?12:8);i++)

    {

     var item=Item(id);var trace=new CostTrace();CostTrace.Add(trace.Resources,"iron_ore",3);

     trace.Stage("Mine / "+LedgerHistory.ItemName("iron_ore"),new Dictionary<string,decimal>{{"Gold",100}},3);

     trace.Stage("Forge / "+LedgerHistory.ItemName("iron_ingot"),new Dictionary<string,decimal>{{"Gold",200}},3);

     trace.Stage("Blacksmith / "+LedgerHistory.ItemName(id),new Dictionary<string,decimal>{{"Gold",id=="iron_sword"?400:200}});

     LedgerObserver.SetTrace(item,trace);ledger.History.Pass(item,0,shop);

     ledger.History.Sale(trace,new Dictionary<string,decimal>{{"Gold",id=="iron_sword"?2400:1800}});

    }

    view.Refresh();view.OpenSettings();

    view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="ItemsPassedTab").onClick.Invoke();

    Capture(Path.Combine(directory,"ledger-styled-preview.png"));

    view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="ChainCostsTab").onClick.Invoke();

    var scroll=view.Settings.GetComponentInChildren<ScrollRect>();

    Check(scroll.content.rect.height>scroll.viewport.rect.height,"Long chain reports extend the scrollable table");

    scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();

    Check(scroll.content.anchoredPosition.y>0,"Chain cost table can scroll to upstream resource rows");

    Capture(Path.Combine(directory,"ledger-styled-chain.png"));

    view.Settings.ClosePopup();view.Refresh();Capture(Path.Combine(directory,"ledger-styled-node.png"));

    CheckDisplayModes(ledger,view,shop,directory);

    CaptureConnectedChain(ledger,view,directory,clock);

    CaptureCurrencies(ledger,view,directory,clock);

   }

   finally{LedgerHistory.Clock=clock;}

  }

  void CaptureConnectedChain(Ledger ledger,LedgerWorld view,string directory,Func<double> clock)

  {

   LedgerHistory.Clock=()=>clock()+480;

   var chain=new OutfitFixture();var oldInput=ledger.Inputs[0].ConnectedPipe;var oldOutput=ledger.Outputs[0].ConnectedPipe;

   chain.OutputPipe.Connect(chain.Outfitter.Outputs[0],ledger.Inputs[0]);Connect(ledger.Outputs[0],chain.Sink.Inputs[0]);

   try

   {

    view.Refresh();view.OpenSettings();view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="ChainCostsTab").onClick.Invoke();

    var labels=view.Settings.GetComponentsInChildren<TMP_Text>();

    Check(!labels.Any(t=>t.text==LedgerHistory.ItemName("iron_ore"))&&!labels.Any(t=>t.text=="Outfitter"),"Unobserved connected stages and resource placeholders are hidden");

    Check(!labels.Any(t=>t.text.Contains("—"))&&ledger.ShowFinancials(ledger.History.Summary(60)),"Configured recruitment costs remain recognized without displaying unknown placeholder rows");

    Capture(Path.Combine(directory,"ledger-connected-chain.png"));

    var human=chain.Produce();view.Refresh();view.Settings.Refresh();

    var observed=ledger.History.Summary(60);

    Check(human!=null&&observed.Costs.Stages.Count==5&&observed.Costs.Resources["iron_ore"]==2&&observed.UnknownPassed==0,"Actual Outfitter chain populates the in-game Ledger report with all stages and resources");

    Check(!ledger.HasShopOutput&&observed.Profit=="-2 Requisition"&&FinanceAmount(view.Settings,"Requisition","Net")=="-2","Native Barracks operating spend displays a loss through Outfitter without a shop destination");

    var scroll=view.Settings.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=1;Canvas.ForceUpdateCanvases();

    Capture(Path.Combine(directory,"ledger-full-chain.png"));

    scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();Capture(Path.Combine(directory,"ledger-full-chain-resources.png"));

    view.Settings.ClosePopup();

   }

   finally

   {

    ledger.Inputs[0].ConnectedPipe=oldInput;ledger.Outputs[0].ConnectedPipe=oldOutput;

    if(oldOutput!=null)oldOutput.ConnectedInput.ConnectedPipe=oldOutput;

   }

  }

  static string NodeAmount(LedgerWorld view,string currency,string column)=>view.transform.Find("LedgerDisplay/MonetaryTotals/"+currency+"/"+column).GetComponent<TMP_Text>().text;

  void CaptureCurrencies(Ledger ledger,LedgerWorld view,string directory,Func<double> clock)

  {

   LedgerHistory.Clock=()=>clock()+600;

   var oldInput=ledger.Inputs[0].ConnectedPipe;var storage=new Stockpile();var unseen=new Mine();unseen.SetupResource((ResourceItemType)ItemType.Get("sapphire"));

   Connect(unseen.Outputs[0],storage.Inputs[0]);Connect(storage.Outputs[0],ledger.Inputs[0]);

   try

   {

    var item=Item("iron_ingot");var trace=allCurrencyTrace.Copy();CostTrace.Add(trace.Resources,"iron_ore",1);

    foreach(var route in new[]{"Collector","Distributor","Manifold","Stockpile"})trace.Stage(route,new Dictionary<string,decimal>(),1);

    LedgerObserver.SetTrace(item,trace);ledger.History.Pass(item,0,storage);ledger.History.Sale(trace,new Dictionary<string,decimal>{{"Gold",80000},{"Crown",10}});

    view.Refresh();view.OpenSettings();view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="ChainCostsTab").onClick.Invoke();

    var currencyTable=view.Settings.transform.Find("LedgerPanel/CurrencyTotals");

    Check(Enum.GetNames(typeof(Currency)).All(k=>currencyTable.Find(k)?.gameObject.activeSelf==true),"Every affected currency has a visible revenue, expense and net row");

    Check(FinanceAmount(view.Settings,"Gold","Cost")=="-6g 62s 5c"&&FinanceAmount(view.Settings,"Gold","Net")=="+1g 37s 95c","Gold summary renders exact coin denominations and carries correctly between them");

    Check(Enum.GetNames(typeof(Currency)).All(k=>NodeAmount(view,k,"Net")==LedgerHistory.Amount(k,(k=="Gold"?80000m:k=="Crown"?10m:0m)-trace.Money[k],true)),"Main node lists every affected currency without truncating the list to one currency");

    Check(ledger.Size.y>=Ledger.DisplayHeight(ledger.Inputs.Count,1,9),"Main node fits all currency rows on the native grid");

    var affected=ledger.History.Summary(60).Currencies;var strip=view.transform.Find("LedgerDisplay/MonetaryTotals");
    Check(affected.Select((k,i)=>((RectTransform)strip.Find(k)).anchoredPosition==new Vector2((i%2)*LedgerWorld.CurrencyStride,-17-(i/2)*20)).All(v=>v),"Profit grid fills left to right in two columns before starting the next row");
    Check(ledger.Size.y==Ledger.DisplayHeight(ledger.Inputs.Count,1,9)&&affected.All(k=>((RectTransform)strip.Find(k)).sizeDelta==new Vector2(LedgerWorld.CurrencyWidth,20)),"Nine affected currencies occupy five compact grid rows");
    Check(affected.All(k=>strip.Find(k+"/Cost")==null&&strip.Find(k+"/Revenue")==null&&strip.Find(k+"/Net").GetComponent<TMP_Text>().fontSizeMax==12),"Node shows only one smaller combined profit value per currency");


    Check(Enum.GetNames(typeof(Currency)).All(k=>currencyTable.Find(k+"/Cost").GetComponentsInChildren<TMP_Text>().All(t=>t.color==LedgerUi.Red)),"Every affected currency displays operating costs in red");
    Check(view.transform.Find("LedgerDisplay/MonetaryTotals/Gold/Net").GetComponent<TMP_Text>().color==LedgerUi.Green&&view.transform.Find("LedgerDisplay/MonetaryTotals/Mana/Net").GetComponent<TMP_Text>().color==LedgerUi.Red,"Node profit grid preserves green gains and red losses");
    Check(FinanceAmount(view.Settings,"Gold","Revenue")=="+8g 0s 0c"&&FinanceAmount(view.Settings,"Crown","Revenue")=="+10","Revenue shows a positive sign for Gold denominations and other currencies");
    Check(currencyTable.Find("Gold/Revenue").GetComponentsInChildren<TMP_Text>().All(t=>t.color==LedgerUi.Paper),"Revenue retains its original paper color");
    Check(currencyTable.Find("Gold/Net").GetComponentsInChildren<TMP_Text>().All(t=>t.color==LedgerUi.Green)&&currencyTable.Find("Mana/Net").GetComponentsInChildren<TMP_Text>().All(t=>t.color==LedgerUi.Red),"Net gain and loss colors remain unchanged");


    var text=view.Settings.GetComponentsInChildren<TMP_Text>();

    Check(!text.Any(t=>new[]{"Collector","Distributor","Manifold","Stockpile"}.Contains(t.text)),"Routing and storage stages stay hidden even when their observed quantities are present");

    Check(!text.Any(t=>t.text.Contains("—")||t.text.Contains("--"))&&!text.Any(t=>t.text==LedgerHistory.ItemName("sapphire")),"Unknown connected stages and resources do not create placeholder rows");

    Check(!view.Settings.GetComponentsInChildren<Button>().Any(b=>b.name=="OutputsTab"),"The simplified interface contains only item and chain tabs");

    var gold=currencyTable.Find("Gold/Cost/Denominations");

    Check(gold.Find("GoldCoin").GetComponent<Image>().sprite&&gold.Find("SilverCoin").GetComponent<Image>().sprite&&gold.Find("CopperCoin").GetComponent<Image>().sprite,"All three denomination icons reuse loaded native coin sprites");

    Canvas.ForceUpdateCanvases();var corners=new Vector3[4];((RectTransform)view.Settings.transform.Find("LedgerPanel")).GetWorldCorners(corners);

    Check(corners.All(c=>c.x>=0&&c.x<=Screen.width&&c.y>=0&&c.y<=Screen.height),"The all-currency ledger remains fully inside screen bounds");

    Capture(Path.Combine(directory,"ledger-all-currencies.png"));
    view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="ItemsPassedTab").onClick.Invoke();
    Capture(Path.Combine(directory,"ledger-currencies-preview.png"));view.Settings.ClosePopup();Capture(Path.Combine(directory,"ledger-all-currencies-node.png"));

    var moreItems=new[]{"iron_sword","iron_helm","wood_log","iron_ore","sapphire","conscript"};
    foreach(var id in moreItems){var extra=Item(id);LedgerObserver.SetTrace(extra,new CostTrace());ledger.History.Pass(extra,0,storage);}
    view.Refresh();var display=view.transform.Find("LedgerDisplay");
    Check(moreItems.Concat(new[]{"iron_ingot"}).All(id=>display.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==LedgerHistory.ItemName(id))),"Removing paging controls keeps every passed item type visible, including a seventh type");
    Check(display.GetComponentsInChildren<Button>().All(b=>b.name=="LedgerGear")&&!display.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains(" in / ")),"Main node has no input/output footer, page counter or paging arrows");
    Check(ledger.Size.y==Ledger.DisplayHeight(ledger.Inputs.Count,7,9)&&((RectTransform)display.Find("MonetaryTotals")).anchoredPosition.y==-240,"Item list expands above the profit grid while the node ends just below its last currency row");
    Capture(Path.Combine(directory,"ledger-node-no-footer.png"));


   }

   finally{ledger.Inputs[0].ConnectedPipe=oldInput;}

  }

  static string FinanceAmount(LedgerPopup popup,string currency,string column)

  {

   var root=popup.transform.Find("LedgerPanel/CurrencyTotals/"+currency+"/"+column);var number=root.Find("Amount");

   if(number.gameObject.activeSelf)return number.GetComponent<TMP_Text>().text;

   var parts=root.Find("Denominations");return parts.Find("Sign").GetComponent<TMP_Text>().text+parts.Find("Gold").GetComponent<TMP_Text>().text+"g "+parts.Find("Silver").GetComponent<TMP_Text>().text+"s "+parts.Find("Copper").GetComponent<TMP_Text>().text+"c";

  }

  void CheckDisplayModes(Ledger ledger,LedgerWorld view,Shop shop,string directory)

  {

   var unsold=Item();Seed(unsold,20000);ledger.History.Pass(unsold,0,shop);view.Refresh();

   Check(NodeAmount(view,"Gold","Net")=="+1g 8s 0c","Main node combines revenue and all passed-item operating costs into one profit value");

   view.OpenSettings();

   Check(FinanceAmount(view.Settings,"Gold","Cost")=="-3g 24s 0c","Operating cost total includes every passed item's observed spend");

   Check(FinanceAmount(view.Settings,"Gold","Net")=="+1g 8s 0c","Profit deducts all operating costs, including unsold passed items");

   Check(Enum.GetNames(typeof(Currency)).Where(k=>view.Settings.transform.Find("LedgerPanel/CurrencyTotals/"+k)?.gameObject.activeSelf==true).SequenceEqual(new[]{"Gold"}),"Report hides every currency without recorded revenue or operating costs");
   Check(Enum.GetNames(typeof(Currency)).Where(k=>view.transform.Find("LedgerDisplay/MonetaryTotals/"+k)?.gameObject.activeSelf==true).SequenceEqual(new[]{"Gold"}),"Main node hides currencies unaffected in the selected window");
   var filtered=new LedgerSummary();filtered.Costs.Money["Gold"]=100;filtered.Revenue["Gold"]=100;filtered.Costs.Money["Mana"]=0;filtered.Revenue["Crown"]=0;
   Check(filtered.Currencies.SequenceEqual(new[]{"Gold"}),"Currency filtering keeps offsetting real activity with zero net and excludes zero-valued entries");


   Capture(Path.Combine(directory,"ledger-operating-costs.png"));

   var pipe=ledger.Outputs[0].ConnectedPipe;ledger.Outputs[0].ConnectedPipe=null;shop.Inputs[0].ConnectedPipe=null;

   try

   {

    view.Refresh();view.Settings.Refresh();

    Check(!ledger.HasShopOutput&&view.transform.Find("LedgerDisplay/MonetaryTotals").gameObject.activeSelf,"Disconnecting a shop keeps financial reporting when the window contains monetary activity");

    Check(FinanceAmount(view.Settings,"Gold","Net")=="+1g 8s 0c","Rewiring does not erase recorded revenue, expenses or net profit");

   }

   finally{ledger.Outputs[0].ConnectedPipe=pipe;shop.Inputs[0].ConnectedPipe=pipe;}

   view.Refresh();view.Settings.Refresh();

   Check(view.transform.Find("LedgerDisplay/MonetaryTotals").gameObject.activeSelf&&FinanceAmount(view.Settings,"Gold","Cost")=="-3g 24s 0c","Reconnecting a shop restores monetary totals without losing history");

   var clock=LedgerHistory.Clock;var sink=new Stockpile();shop.Inputs[0].ConnectedPipe=null;Connect(ledger.Outputs[0],sink.Inputs[0]);

   try

   {

    LedgerHistory.Clock=()=>clock()+120;

    var paid=Item();Seed(paid,20000);Check(ledger.Inputs[0].InputItem(paid),"Paid item passes directly through Ledger into storage");

    view.Refresh();view.Settings.Refresh();

    Check(ledger.History.Summary(60).Profit=="-2g 0s 0c"&&NodeAmount(view,"Gold","Net")=="-2g 0s 0c","Main node shows operating loss with no shop or sales");

    Check(FinanceAmount(view.Settings,"Gold","Net")=="-2g 0s 0c"&&view.Settings.transform.Find("LedgerPanel/CurrencyTotals/Gold/Net/Denominations/Sign").GetComponent<TMP_Text>().color==LedgerUi.Red,"Configuration displays a negative cost-only result in red");

    Capture(Path.Combine(directory,"ledger-cost-only-report.png"));

    view.Settings.ClosePopup();Capture(Path.Combine(directory,"ledger-cost-only-node.png"));view.OpenSettings();

    LedgerHistory.Clock=()=>clock()+240;sink.Inputs[0].BufferedItems.Clear();

    var free=Item();LedgerObserver.SetTrace(free,new CostTrace());Check(ledger.Inputs[0].InputItem(free),"Free item still passes into storage after prior monetary observations expire");

    view.Refresh();view.Settings.Refresh();

    Func<TMP_Text,bool> moneyText=t=>new[]{"gold","revenue","profit","cost","shop"}.Any(word=>t.text.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0);

    Check(!view.transform.Find("LedgerDisplay/MonetaryTotals").gameObject.activeSelf&&!view.GetComponentsInChildren<TMP_Text>().Any(moneyText),"Free passthrough node shows items without money or shop reminders");

    Check(!view.Settings.GetComponentsInChildren<TMP_Text>().Any(moneyText),"Free passthrough report hides currency totals and cost warnings");

    view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="ItemsPassedTab").onClick.Invoke();

    Check(view.Settings.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==LedgerHistory.ItemName("iron_sword")),"Free passthrough report preserves item rows");

    Capture(Path.Combine(directory,"ledger-passthrough-report.png"));

    view.Settings.ClosePopup();Capture(Path.Combine(directory,"ledger-passthrough-node.png"));view.OpenSettings();

    LedgerHistory.Clock=()=>clock()+280;sink.Inputs[0].BufferedItems.Clear();
    var partial=Item();var partialTrace=new CostTrace{Complete=false};partialTrace.Money["Gold"]=20000;LedgerObserver.SetTrace(partial,partialTrace);ledger.Inputs[0].InputItem(partial);
    view.Refresh();view.Settings.Refresh();
    Check(NodeAmount(view,"Gold","Net")=="~-2g 0s 0c"&&view.transform.Find("LedgerDisplay/MonetaryTotals/Gold/Net").GetComponent<TMP_Text>().color==LedgerUi.Red,"Incomplete history shows known currency loss with an estimate marker instead of Unknown on the node");
    Check(FinanceAmount(view.Settings,"Gold","Net")=="Unknown","Detailed report still distinguishes incomplete costs from a complete net result");
    view.Settings.ClosePopup();Capture(Path.Combine(directory,"ledger-partial-profit-node.png"));view.OpenSettings();


   }

   finally{ledger.Outputs[0].ConnectedPipe=pipe;shop.Inputs[0].ConnectedPipe=pipe;}

   view.Settings.ClosePopup();

  }

          static void Capture(string path)

        {

            // Hidden Windows players can suspend presentation and never resume

            // WaitForEndOfFrame. Render cameras explicitly to an offscreen target.

            Canvas.ForceUpdateCanvases();

            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);

            var previous = RenderTexture.active;

            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

            // Render only this popup as a temporary overlay root. Moving all UI

            // roots would also move native camera/mask objects out of their layers.

            var popup = UnityEngine.Object.FindObjectsByType<LedgerPopup>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsOpen);

            var popupRoot = popup ? (RectTransform)popup.transform : null;

            var parent = popupRoot ? popupRoot.parent : null;

            var rootCanvas = popup ? popup.GetComponent<Canvas>().rootCanvas : null;

            var popupCanvas = popup ? popup.GetComponent<Canvas>() : null;

            var oldMode = popupCanvas ? popupCanvas.renderMode : RenderMode.ScreenSpaceOverlay;

            var oldCamera = popupCanvas ? popupCanvas.worldCamera : null;

            float oldDistance = popupCanvas ? popupCanvas.planeDistance : 0;

            float oldScaleFactor = popupCanvas ? popupCanvas.scaleFactor : 1;

            var oldAnchorMin = popupRoot ? popupRoot.anchorMin : Vector2.zero;

            var oldAnchorMax = popupRoot ? popupRoot.anchorMax : Vector2.one;

            var oldPivot = popupRoot ? popupRoot.pivot : Vector2.zero;

            var oldSize = popupRoot ? popupRoot.sizeDelta : Vector2.zero;

            var oldPosition = popupRoot ? popupRoot.anchoredPosition : Vector2.zero;

            var oldScale = popupRoot ? popupRoot.localScale : Vector3.one;

            var layers = popup ? popup.GetComponentsInChildren<Transform>(true)

                .Select(t => (transform: t, layer: t.gameObject.layer)).ToArray() : new (Transform transform, int layer)[0];

            var overlayCameraObject = new GameObject("LedgerDiagnosticOverlayCapture");

            var overlayCamera = overlayCameraObject.AddComponent<Camera>();

            overlayCamera.clearFlags = CameraClearFlags.Depth; overlayCamera.orthographic = true;

            overlayCamera.orthographicSize = Screen.height / 2f; overlayCamera.depth = 10000;

            overlayCamera.cullingMask = 1 << 31; overlayCamera.targetTexture = target;

            try

            {

                foreach (var camera in Camera.allCameras.Where(c => c != overlayCamera).OrderBy(c => c.depth))

                {

                    var oldTarget = camera.targetTexture;

                    try { camera.targetTexture = target; camera.Render(); }

                    finally { camera.targetTexture = oldTarget; }

                }

                if (popup)

                {

                    foreach (var entry in layers) entry.transform.gameObject.layer = 31;

                    float scale = rootCanvas.scaleFactor;

                    popupRoot.SetParent(null, false);

                    popupCanvas.renderMode = RenderMode.ScreenSpaceCamera; popupCanvas.worldCamera = overlayCamera;

                    popupCanvas.planeDistance = 10; popupCanvas.scaleFactor = scale;

                    Canvas.ForceUpdateCanvases(); overlayCamera.Render();

                }

                RenderTexture.active = target;

                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); texture.Apply();

                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));

            }

            finally

            {

                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);

                if (popup)

                {

                    popupCanvas.renderMode = oldMode; popupCanvas.worldCamera = oldCamera;

                    popupCanvas.planeDistance = oldDistance; popupCanvas.scaleFactor = oldScaleFactor;

                    popupRoot.SetParent(parent, false); popupRoot.anchorMin = oldAnchorMin; popupRoot.anchorMax = oldAnchorMax;

                    popupRoot.pivot = oldPivot; popupRoot.sizeDelta = oldSize; popupRoot.anchoredPosition = oldPosition; popupRoot.localScale = oldScale;

                }

                foreach (var entry in layers) if (entry.transform) entry.transform.gameObject.layer = entry.layer;

                UnityEngine.Object.DestroyImmediate(overlayCameraObject);

                UnityEngine.Object.Destroy(texture);

            }

        }





  [HarmonyPatch(typeof(SaveGame),nameof(SaveGame.Store))]static class NoSave{static bool Prefix()=>!Diagnostic;}

  [HarmonyPatch(typeof(SaveGame),nameof(SaveGame.StoreAutosaveState))]static class NoAutosave{static bool Prefix()=>!Diagnostic;}

  [HarmonyPatch(typeof(SaveGame),nameof(SaveGame.LoadLatestSave))]static class NoLoad{static bool Prefix(ref bool __result){if(!Diagnostic)return true;__result=false;return false;}}

  [HarmonyPatch(typeof(SteamAchievement),nameof(SteamAchievement.Trigger))]static class NoAchievement{static bool Prefix()=>!Diagnostic;}

  [HarmonyPatch(typeof(SteamManager),nameof(SteamManager.Initialized),MethodType.Getter)]static class Offline{static bool Prefix(ref bool __result){if(!Diagnostic)return true;__result=false;return false;}}

  [HarmonyPatch(typeof(FlowElement),nameof(FlowElement.GetCraftEffectsVolume))]static class SilentEffects{static bool Prefix(ref float __result){if(!Diagnostic)return true;__result=0;return false;}}

 }

}

