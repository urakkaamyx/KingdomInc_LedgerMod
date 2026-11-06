using System;

using System.Linq;

using System.Collections.Generic;

using Assets.Source.Player;

using Assets.Behaviour.Flow;

using Assets.Behaviour.UI;

using Assets.Source.Items;

using Behaviour.UI.Tooltip;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



namespace KingdomLedger

{

 public sealed class LedgerWorld:MonoBehaviour

 {

  internal const int ContentWidth=Ledger.Width-62, CurrencyWidth=(ContentWidth-12)/2, CurrencyStride=CurrencyWidth+12;
  Ledger ledger;LedgerUi ui;TMP_Text period,throughput;RectTransform moneyStrip,body;

  RectTransform timerFill;TooltipSource timerTip;int timerSecond=-1,timerPeriod;

  readonly List<NodeCurrencyRow> moneyRows=new List<NodeCurrencyRow>();

  sealed class NodeCurrencyRow {internal RectTransform Root;internal TMP_Text Net;internal Image Icon;internal TooltipSource Tip;}

  sealed class ItemRow {internal RectTransform Root;internal TMP_Text Name,Amount;internal Image Icon,Shade;}
  readonly List<ItemRow> itemRows=new List<ItemRow>();
  float refresh;bool initialized,openWhenReady;

  internal LedgerPopup Settings{get;private set;}

  internal Ledger Element=>ledger;

  void Start(){TryInitialize();}

  bool TryInitialize()

  {

   if(initialized)return true;

   var world=GetComponent<WorldFlowElement>();var element=world?world.Element as Ledger:null;

   if(element==null)return false;

   var font=LedgerPlugin.Field<TMP_Text>(world,"_label")?.font;if(!font)return false;

   ledger=element;ui=new LedgerUi(font);

   body=LedgerUi.Rect(transform,"LedgerDisplay",0,0,Ledger.Width,Ledger.Height);

   var gear=ui.Button(body,"",Ledger.Width-42,0,30,30,OpenSettings);gear.name="LedgerGear";

   var cog=LedgerUi.Rect(gear.transform,"Cog",5,5,20,20).gameObject.AddComponent<LedgerGearGraphic>();cog.color=LedgerUi.Paper;cog.raycastTarget=false;

   gear.gameObject.AddComponent<TooltipSource>().SetTooltip("Ledger settings","Reporting window, input/output pairs, entire chain costs and currency profit.");

   period=ui.Text(body,"",32,35,Ledger.Width-130,24,13);period.color=LedgerUi.Muted;

   var quantity=ui.Text(body,"QTY",Ledger.Width-100,35,66,24,12);quantity.color=LedgerUi.Muted;quantity.alignment=TextAlignmentOptions.MidlineRight;

   var timer=ui.Fill(body,"WindowTimer",30,Ledger.TimerTop,ContentWidth,4,new Color(.12f,.085f,.10f,.9f));timer.raycastTarget=true;
   timerTip=timer.gameObject.AddComponent<TooltipSource>();timerFill=ui.Fill(timer.transform,"Fill",0,0,0,4,LedgerUi.Gold).rectTransform;

   EnsureItemRows(1);

   moneyStrip=LedgerUi.Rect(body,"MonetaryTotals",30,216,ContentWidth,38);ui.Rule(moneyStrip,0,0,ContentWidth);

   var label=ui.Text(moneyStrip,"PROFIT / LOSS",0,3,ContentWidth,14,10);label.color=LedgerUi.Muted;

   throughput=ui.Text(body,"",32,Ledger.ItemTop+Ledger.RowHeight,ContentWidth-4,24,14);throughput.name="PassthroughTotal";throughput.color=LedgerUi.Muted;

   initialized=true;LayoutNativeBackdrops(world);Refresh();return true;

  }

  // The generic native building prefab keeps its icon slot and progress frame after
  // their contents are hidden. Ledger has no building icon, so the slot is hidden, and
  // the progress frame is reused unchanged as the full-width header band.
  void LayoutNativeBackdrops(WorldFlowElement world)
  {
   var icon=LedgerPlugin.Field<Image>(world,"_icon");
   if(icon&&icon.transform.parent&&icon.transform.parent!=transform)icon.transform.parent.gameObject.SetActive(false);
   var progress=LedgerPlugin.Field<Image>(world,"_progress");
   if(progress&&progress.transform.parent is RectTransform band&&band!=transform)
   {
    band.name="LedgerHeaderBand";band.anchorMin=new Vector2(0,1);band.anchorMax=new Vector2(1,1);band.pivot=new Vector2(0,1);
    band.anchoredPosition=new Vector2(HeaderInset,-Ledger.HeaderTop);band.sizeDelta=new Vector2(-HeaderInset*2,Ledger.HeaderHeight);
   }
  }
  internal const int HeaderInset=20;

  void Update()

  {

   if(!TryInitialize())return;

   if(openWhenReady)OpenSettings();
   RefreshTimer();

   if((refresh-=Time.unscaledDeltaTime)<=0){refresh=.2f;Refresh();}

  }

  void EnsureItemRows(int count)
  {
   while(itemRows.Count<count)
   {
    int i=itemRows.Count;var row=new ItemRow();row.Root=LedgerUi.Rect(body,"ItemRow"+i,30,Ledger.ItemTop+i*Ledger.RowHeight,ContentWidth,24);
    if(i%2==0)row.Shade=ui.Fill(row.Root,"Shade",0,0,ContentWidth,24,new Color(0,0,0,.12f));
    row.Icon=ui.Icon(row.Root,null,6,2,20);row.Icon.name="ItemIcon"+i;
    row.Name=ui.Text(row.Root,"",38,0,ContentWidth-124,24,17);
    row.Amount=ui.Text(row.Root,"",ContentWidth-82,0,72,24,17);row.Amount.alignment=TextAlignmentOptions.MidlineRight;row.Amount.color=LedgerUi.Gold;
    itemRows.Add(row);
   }
  }

  internal void RefreshTimer()
  {
   if(!initialized)return;
   timerFill.sizeDelta=new Vector2(ContentWidth*ledger.PeriodProgress,4);
   int second=(int)ledger.PeriodElapsed;
   if(second==timerSecond&&timerPeriod==ledger.Period)return;
   timerSecond=second;timerPeriod=ledger.Period;
   timerTip.SetTooltip("Reporting timer",second+" / "+ledger.Period+" seconds\nRepeats for the selected window. Totals continue rolling; the bar does not clear them.");
  }

  internal void Refresh()

  {

   if(!initialized)return;RefreshTimer();var summary=ledger.History.Summary(ledger.Period);var items=summary.Items.OrderBy(p=>LedgerHistory.ItemName(p.Key)).ToArray();

   int visibleRows=Math.Max(1,items.Length);EnsureItemRows(visibleRows);int itemBottom=Ledger.ItemTop+visibleRows*Ledger.RowHeight;

   string unit=ledger.Period==1?"second":ledger.Period==60?"minute":"hour";

   period.text="ITEMS PASSED / LAST "+unit.ToUpperInvariant();

   for(int i=0;i<itemRows.Count;i++)
   {
    var row=itemRows[i];row.Root.gameObject.SetActive(i<visibleRows);bool exists=i<items.Length;row.Icon.enabled=exists;
    if(row.Shade)row.Shade.enabled=exists;
    row.Name.text=exists?LedgerHistory.ItemName(items[i].Key):i==0&&items.Length==0?"No items passed yet":"";
    row.Amount.text=exists?LedgerHistory.Number(items[i].Value):"";
    if(exists)row.Icon.sprite=ItemType.Get(items[i].Key)?.PreviewItem.Icon;
   }

   var currencies=summary.Currencies;bool money=currencies.Length>0;moneyStrip.gameObject.SetActive(money);throughput.gameObject.SetActive(!money);

   throughput.text=LedgerHistory.Number(summary.Items.Values.Sum())+" items passed · "+summary.Items.Count+" types";

   int extra=Math.Max(0,(currencies.Length+1)/2-1)*20;int height=Ledger.DisplayHeight(ledger.Inputs.Count,items.Length,currencies.Length);

   moneyStrip.anchoredPosition=new Vector2(30,-itemBottom-4);throughput.rectTransform.anchoredPosition=new Vector2(32,-itemBottom);
   moneyStrip.sizeDelta=new Vector2(ContentWidth,38+extra);body.sizeDelta=new Vector2(Ledger.Width,height);

   var size=new Vector2Int(Ledger.Width,height);
   if(ledger.Size!=size||((RectTransform)transform).sizeDelta!=(Vector2)size)
   {
    ledger.Size=size;((RectTransform)transform).sizeDelta=size;
    if(ledger.World.Collider)ledger.World.Collider.UpdateSize();
    foreach(var io in ledger.IO)if(io.ConnectedPipe?.World)io.ConnectedPipe.World.UpdateLinePositions();
   }

   for(int i=0;i<currencies.Length;i++)

   {

    if(moneyRows.Count<=i)

    {

     var row=new NodeCurrencyRow();row.Root=LedgerUi.Rect(moneyStrip,"CurrencyRow"+i,(i%2)*CurrencyStride,17+(i/2)*20,CurrencyWidth,20);

     row.Icon=ui.Icon(row.Root,null,0,3,14);row.Net=ui.Text(row.Root,"",20,0,CurrencyWidth-20,20,12);row.Net.name="Net";row.Net.enableAutoSizing=true;row.Net.fontSizeMin=9;row.Net.fontSizeMax=12;

     var hover=ui.Fill(row.Root,"CurrencyHelp",0,0,CurrencyWidth,20,Color.clear);hover.raycastTarget=true;row.Tip=hover.gameObject.AddComponent<TooltipSource>();moneyRows.Add(row);

    }

    string currency=currencies[i];var r=moneyRows[i];r.Root.name=currency;r.Root.gameObject.SetActive(true);

    if(Enum.TryParse(currency,out Currency type)){r.Icon.sprite=type.GetIcon();r.Icon.enabled=true;}

    summary.Costs.Money.TryGetValue(currency,out var cost);summary.Revenue.TryGetValue(currency,out var revenue);decimal net=revenue-cost;

    r.Net.text=(summary.ProfitComplete?"":"~")+LedgerHistory.Amount(currency,net,true);r.Net.color=net<0?LedgerUi.Red:net>0?LedgerUi.Green:LedgerUi.Muted;

    r.Tip.SetTooltip(LedgerHistory.CurrencyName(currency),"Operating costs: "+LedgerHistory.Amount(currency,-cost,true)+"\nRevenue: "+LedgerHistory.Amount(currency,revenue,true)+"\nProfit / loss: "+r.Net.text+(summary.ProfitComplete?"":"\n~ Estimated from known income and costs; upstream cost history is incomplete."));

   }

   for(int i=currencies.Length;i<moneyRows.Count;i++)moneyRows[i].Root.gameObject.SetActive(false);

  }



  public void OpenSettings()

  {

   // Placement calls HandleReopenBuyMenu immediately after SetElement, before

   // Unity runs Start. Build once here, or retain the request until bound.

   if(!GameUI.Instance||!TryInitialize()){openWhenReady=true;return;}

   openWhenReady=false;

   if(!Settings)

   {

    var toolbar=HarmonyLib.AccessTools.Field(typeof(GameUI),"_editHotbar").GetValue(GameUI.Instance)as RectTransform;

    var canvas=toolbar?toolbar.GetComponentInParent<Canvas>()?.rootCanvas:GetComponentInParent<Canvas>()?.rootCanvas;

    var root=new GameObject("LedgerSettings",typeof(RectTransform));root.SetActive(false);

    try

    {

     var menu=root.AddComponent<LedgerPopup>();menu.Initialize(this,ledger,ui.Font,canvas?canvas.transform:null);

     Settings=menu;

    }

    catch{Destroy(root);throw;}

   }

   Settings.Open();

  }

  void OnDisable(){if(Settings&&Settings.IsOpen&&GameUI.Instance)Settings.ClosePopup();}

  void OnDestroy(){if(Settings){if(Settings.IsOpen&&GameUI.Instance)Settings.ClosePopup();Destroy(Settings.gameObject);}}

 }



}

