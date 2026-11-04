using System;

using System.Collections.Generic;

using System.Linq;

using Assets.Behaviour.Flow;

using Assets.Behaviour.UI;

using Assets.Behaviour.UI.Shared;

using Assets.Source.Items;

using Assets.Source.Player;

using Assets.Source.Util;

using Behaviour.UI.Tooltip;

using TMPro;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.InputSystem;

using UnityEngine.UI;

using GameInput=Assets.Behaviour.Input.PlayerInput;



namespace KingdomLedger

{

 public sealed class LedgerPopup:UIPopupMenu

 {

  const float Width=880,Height=732,ReportWidth=798,ReportHeight=258;

  Ledger ledger;LedgerWorld owner;LedgerUi ui;RectTransform panel,content,details,financePanel,counters;

  TMP_Text financeEmpty,unitsColumn;float reportY,currentHeight=Height;

  readonly List<FinanceRow> financeRows=new List<FinanceRow>();

  TMP_Text count,subtitle,activity,status,notice,firstColumn,lastColumn;

  GameObject historyHelp;

  Button minus,plus;Button[] periods,tabs;

  readonly List<ReportRow> rows=new List<ReportRow>();

  MoneyCard earned,spent,profit;

  Canvas referenceCanvas;CanvasScaler scaler;ScrollRect scroll;

  bool restoreInput;int openedFrame,tab=1;float refresh;

  internal bool IsOpen=>gameObject.activeSelf;



  // Reuse visible rows while the live ledger updates. Scrolling and pointer

  // targets stay stable; a busy chain does not rebuild a UI tree every frame.

  sealed class ReportRow

  {

   internal RectTransform Root;internal Image Icon,Background;internal TMP_Text Name,Detail,Amount,Cost;internal TooltipSource Tooltip;

   internal ReportRow(LedgerUi ui,Transform parent,int index)

   {

    Root=LedgerUi.Rect(parent,"ReportRow"+index,0,index*44,ReportWidth,44);

    Background=ui.Fill(Root,"RowShade",0,0,ReportWidth,43,new Color(0,0,0,index%2==0?.15f:.04f));

    // This surface owns an optional tooltip; the rest of the row remains a scroll target.

    Background.raycastTarget=true;Tooltip=Background.gameObject.AddComponent<TooltipSource>();

    Icon=ui.Icon(Root,null,12,7,30);

    Name=ui.Text(Root,"",52,2,412,25,18);

    Detail=ui.Text(Root,"",52,25,412,17,12);Detail.color=LedgerUi.Muted;

    Amount=ui.Text(Root,"",466,0,100,44,18);Amount.alignment=TextAlignmentOptions.MidlineRight;

    Cost=ui.Text(Root,"",580,0,202,44,17);Cost.alignment=TextAlignmentOptions.MidlineRight;Cost.color=LedgerUi.Gold;

   }

  }

  sealed class MoneyCard

  {

   readonly TMP_Text value,detail,title;

   internal MoneyCard(LedgerUi ui,Transform parent,string name,float x)

   {

    var card=ui.Box(parent,name+"Card",x,0,266,104,"DefaultBorderDark");

    title=ui.Text(card.transform,name.ToUpperInvariant(),16,10,234,22,14);title.color=LedgerUi.Muted;

    value=ui.Text(card.transform,"",16,34,234,38,25);detail=ui.Text(card.transform,"",16,76,234,18,13);detail.color=LedgerUi.Muted;

   }

   internal void Count(string name,string amount,string description){title.text=name.ToUpperInvariant();value.text=amount;detail.text=description;}

  }

  sealed class FinanceRow

  {

   internal RectTransform Root;internal TMP_Text Name;internal Image Icon;internal LedgerMoneyValue Revenue,Cost,Net;internal TooltipSource Tip;

   internal FinanceRow(LedgerUi ui,Transform parent,int index)

   {

    Root=LedgerUi.Rect(parent,"CurrencyRow"+index,0,36+index*36,824,36);

    var shade=ui.Fill(Root,"Shade",8,0,808,35,new Color(0,0,0,index%2==0?.13f:.03f));shade.raycastTarget=true;Tip=shade.gameObject.AddComponent<TooltipSource>();

    Icon=ui.Icon(Root,null,14,8,20);Name=ui.Text(Root,"",42,0,136,36,16);

    Revenue=new LedgerMoneyValue(ui,Root,"Revenue",184,0,196,36);Cost=new LedgerMoneyValue(ui,Root,"Cost",389,0,196,36);Net=new LedgerMoneyValue(ui,Root,"Net",594,0,214,36);

   }

  }

  void RefreshFinance(LedgerSummary summary,bool money)

  {

   var currencies=summary.Currencies;financePanel.gameObject.SetActive(money);counters.gameObject.SetActive(!money);

   float h=money?Math.Max(104,44+currencies.Length*36):104;financePanel.sizeDelta=new Vector2(824,h);

   currentHeight=Height+h-104;panel.sizeDelta=new Vector2(Width,currentHeight);details.anchoredPosition=new Vector2(0,-270-(h-104));

   financeEmpty.gameObject.SetActive(currencies.Length==0);

   for(int i=0;i<currencies.Length;i++)

   {

    while(financeRows.Count<=i)financeRows.Add(new FinanceRow(ui,financePanel,financeRows.Count));

    string currency=currencies[i];var row=financeRows[i];row.Root.name=currency;row.Root.gameObject.SetActive(true);

    row.Name.text=LedgerHistory.CurrencyName(currency);if(Enum.TryParse(currency,out Currency type)){row.Icon.sprite=type.GetIcon();row.Icon.enabled=true;}

    summary.Revenue.TryGetValue(currency,out var revenue);summary.Costs.Money.TryGetValue(currency,out var cost);decimal net=revenue-cost;

    row.Revenue.Show(currency,revenue,revenue<0?LedgerUi.Red:LedgerUi.Paper,true);row.Cost.Show(currency,-cost,cost>0?LedgerUi.Red:LedgerUi.Gold,true);

    row.Net.Show(currency,net,summary.ProfitComplete?(net<0?LedgerUi.Red:net>0?LedgerUi.Green:LedgerUi.Muted):LedgerUi.Muted,true,!summary.ProfitComplete);

    row.Tip.SetTooltip(LedgerHistory.CurrencyName(currency),"Revenue: "+LedgerHistory.Amount(currency,revenue,true)+"\nOperating costs: "+LedgerHistory.Amount(currency,-cost,true)+"\nNet: "+(summary.ProfitComplete?LedgerHistory.Amount(currency,net,true):"Unknown: incomplete upstream history"));

    SetLayer(row.Root,gameObject.layer);

   }

   for(int i=currencies.Length;i<financeRows.Count;i++)financeRows[i].Root.gameObject.SetActive(false);

  }

  internal void Initialize(LedgerWorld world,Ledger element,TMP_FontAsset font,Transform parent)

  {

   owner=world;ledger=element;tab=ledger.ShowFinancials(ledger.History.Summary(ledger.Period))?1:0;ui=new LedgerUi(font);referenceCanvas=parent?parent.GetComponentInParent<Canvas>()?.rootCanvas:null;

   // The flow chart remains usable with the HUD hidden. Keep this a true root overlay.

   transform.SetParent(null,false);transform.localScale=Vector3.one;var root=(RectTransform)transform;

   var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;

   if(referenceCanvas)canvas.sortingLayerID=referenceCanvas.sortingLayerID;

   scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=referenceCanvas?Mathf.Max(.1f,referenceCanvas.scaleFactor):1;

   gameObject.AddComponent<GraphicRaycaster>();

   var backdrop=LedgerUi.Rect(root,"DismissBackdrop",0,0,0,0);backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;

   backdrop.gameObject.AddComponent<Image>().color=new Color(.025f,.018f,.026f,.78f);backdrop.gameObject.AddComponent<Button>().onClick.AddListener(ClosePopup);

   panel=ui.Box(root,"LedgerPanel",0,0,Width,Height,"Tooltip").rectTransform;

   panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;

   ui.Icon(panel,ui.Sprite("Icons_149"),28,25,38);

   ui.Text(panel,"Production ledger",80,19,650,40,28);

   subtitle=ui.Text(panel,"",81,57,650,22,14);subtitle.color=LedgerUi.Muted;

   ui.Button(panel,"X",820,26,32,32,ClosePopup);ui.Rule(panel,28,86,824);



   var windowLabel=ui.Text(panel,"WINDOW",28,98,130,30,13);windowLabel.color=LedgerUi.Muted;

   periods=new Button[3];var seconds=new[]{1,60,3600};var labels=new[]{"Second","Minute","Hour"};

   for(int i=0;i<3;i++){int s=seconds[i];periods[i]=ui.Button(panel,labels[i],166+96*i,96,90,32,()=>{ledger.SetPeriod(s);Refresh();owner.Refresh();});periods[i].name="Period"+s;}

   var pairs=ui.Text(panel,"PAIRS",560,98,70,30,13);pairs.color=LedgerUi.Muted;

   minus=ui.Button(panel,"−",637,96,32,32,()=>Change(-1));

   count=ui.Text(panel,"",677,96,100,32,18);count.alignment=TextAlignmentOptions.Center;

   plus=ui.Button(panel,"+",785,96,32,32,()=>Change(1));plus.name="AddLedgerPair";minus.name="RemoveLedgerPair";

   notice=ui.Text(panel,"",28,132,824,22,13);notice.color=LedgerUi.Gold;

   counters=LedgerUi.Rect(panel,"ItemCounters",0,166,Width,104);

   earned=new MoneyCard(ui,counters,"Items passed",28);spent=new MoneyCard(ui,counters,"Item types",307);profit=new MoneyCard(ui,counters,"Connected pairs",586);

   financePanel=ui.Box(panel,"CurrencyTotals",28,166,824,104,"DefaultBorderDark").rectTransform;

   var headers=new[]{"CURRENCY","REVENUE","OPERATING COSTS","NET CHANGE"};var positions=new[]{14,184,389,594};

   for(int i=0;i<4;i++){var label=ui.Text(financePanel,headers[i],positions[i],6,i==0?160:210,24,13);label.color=LedgerUi.Muted;}

   financeEmpty=ui.Text(financePanel,"No currency changes recorded in this window",16,38,792,44,16);financeEmpty.color=LedgerUi.Muted;

   details=LedgerUi.Rect(panel,"LedgerDetails",0,270,Width,Height-270);

   activity=ui.Text(details,"",30,12,514,24,16);

   status=ui.Text(details,"",550,12,300,24,14);status.alignment=TextAlignmentOptions.MidlineRight;

   var help=ui.Fill(details,"HistoryHelp",550,12,300,24,Color.clear);help.raycastTarget=true;

   historyHelp=help.gameObject;

   help.gameObject.AddComponent<TooltipSource>().SetTooltip("Session history","Older items and unsupported production stages may have missing costs. Affected items keep the net total incomplete. History resets when a kingdom is loaded.");

   tabs=new Button[2];var tabNames=new[]{"Items passed","Chain costs"};

   for(int i=0;i<2;i++){int selected=i;tabs[i]=ui.Button(details,tabNames[i],28+417*i,50,407,34,()=>{tab=selected;scroll.verticalNormalizedPosition=1;Refresh();});}

   tabs[0].name="ItemsPassedTab";tabs[1].name="ChainCostsTab";

   firstColumn=ui.Text(details,"",42,96,430,24,13);firstColumn.color=LedgerUi.Muted;

   var units=ui.Text(details,"UNITS",494,96,100,24,13);unitsColumn=units;units.name="UnitsColumn";units.color=LedgerUi.Muted;units.alignment=TextAlignmentOptions.MidlineRight;

   lastColumn=ui.Text(details,"",608,96,202,24,13);lastColumn.color=LedgerUi.Muted;lastColumn.alignment=TextAlignmentOptions.MidlineRight;

   ui.Rule(details,28,125,824);

   var viewport=LedgerUi.Rect(details,"LedgerReportViewport",28,130,ReportWidth,ReportHeight);viewport.gameObject.AddComponent<RectMask2D>();

   viewport.gameObject.AddComponent<Image>().color=Color.clear;

   content=LedgerUi.Rect(viewport,"LedgerReportContent",0,0,ReportWidth,ReportHeight);

   scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=40;

   var track=ui.Fill(details,"ScrollTrack",838,130,8,ReportHeight,new Color(0,0,0,.22f));track.raycastTarget=true;

   var handle=ui.Fill(track.transform,"ScrollHandle",0,0,8,ReportHeight,LedgerUi.Muted);handle.rectTransform.anchorMin=Vector2.zero;handle.rectTransform.anchorMax=Vector2.one;handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.offsetMin=handle.rectTransform.offsetMax=Vector2.zero;handle.raycastTarget=true;

   var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=scrollbar;

   scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;

   ui.Rule(details,28,404,824);



   var info=ui.Fill(details,"StorageHelp",28,412,540,32,Color.clear);info.raycastTarget=true;

   info.gameObject.AddComponent<TooltipSource>().SetTooltip("Items keep moving","Each input delivers directly to its matching output. When the destination is full, filtered or disconnected, the item stays at its producer. Ledger never stores it.");

   var escape=ui.Text(details,"ESC to close",674,416,176,24,13);escape.color=LedgerUi.Muted;escape.alignment=TextAlignmentOptions.MidlineRight;

   SetLayer(transform,LayerMask.NameToLayer("UI"));

  }

  static void SetLayer(Transform root,int layer){if(layer<0)return;root.gameObject.layer=layer;foreach(Transform child in root)SetLayer(child,layer);}

  void Fit()

  {

   if(referenceCanvas)scaler.scaleFactor=Mathf.Max(.1f,referenceCanvas.scaleFactor);

   var bounds=((RectTransform)transform).rect;

   panel.localScale=Vector3.one*Mathf.Max(.1f,Mathf.Min(1,(bounds.width-32)/Width,(bounds.height-32)/currentHeight));

  }

  void Change(int delta){notice.text=ledger.SetPairs(ledger.Inputs.Count+delta)?"":"Disconnect the last pair before removing it.";Refresh();owner.Refresh();}

  internal void Open(){if(IsOpen){ClosePopup();return;}Refresh();GameUI.Instance.TogglePopupMenu(this,true);openedFrame=Time.frameCount;restoreInput=GameInput.Enabled;GameInput.Disable();Canvas.ForceUpdateCanvases();Fit();}

  internal static Dictionary<string,decimal> Net(LedgerSummary s){var net=new Dictionary<string,decimal>(s.Revenue);foreach(var p in s.Costs.Money)CostTrace.Add(net,p.Key,-p.Value);return net;}

  internal static Color NetColor(LedgerSummary s)

  {

   var values=Net(s).Values;return values.Any(v=>v<0)?values.Any(v=>v>0)?LedgerUi.Gold:LedgerUi.Red:values.Any(v=>v>0)?LedgerUi.Green:LedgerUi.Muted;

  }

  ReportRow Row(int index,string name,string detail,string amount,string cost,Sprite icon=null,Color? color=null,string help=null,float height=44)

  {

   while(rows.Count<=index)rows.Add(new ReportRow(ui,content,rows.Count));

   var row=rows[index];row.Root.anchoredPosition=new Vector2(0,-reportY);row.Root.sizeDelta=new Vector2(ReportWidth,height);reportY+=height;row.Background.rectTransform.sizeDelta=new Vector2(ReportWidth,height-1);row.Cost.rectTransform.sizeDelta=new Vector2(202,height-8);row.Cost.rectTransform.anchoredPosition=new Vector2(580,-4);row.Cost.alignment=TextAlignmentOptions.TopRight;row.Cost.fontSize=16;row.Root.gameObject.SetActive(true);row.Name.text=name;row.Detail.text=detail;

   row.Name.rectTransform.anchoredPosition=new Vector2(icon?52:16,detail.Length>0?-2:-9);

   row.Detail.rectTransform.anchoredPosition=new Vector2(icon?52:16,-25);

   row.Name.color=color??LedgerUi.Paper;row.Detail.color=LedgerUi.Muted;row.Amount.text=amount;row.Cost.text=cost;row.Cost.color=LedgerUi.Red;

   row.Icon.sprite=icon;row.Icon.enabled=icon;

   row.Tooltip.SetTooltip(name,help??(string.IsNullOrEmpty(detail)?name:detail));

   SetLayer(row.Root,gameObject.layer);return row;

  }

  int Heading(int index,string title,string right="")

  {

   var row=Row(index,title,"","",right,null,LedgerUi.Gold);row.Background.color=new Color(.45f,.3f,.17f,.15f);row.Name.fontSize=14;return index+1;

  }

  int ChainStage(int index,string key,LedgerSummary summary,bool money,LedgerChain.Entry connected=null)

  {

   if(LedgerObserver.IsRoutingStage(key))return index;

   summary.Costs.Stages.TryGetValue(key,out var quantity);summary.Costs.StageMoney.TryGetValue(key,out var spend);

   var values=(spend??new Dictionary<string,decimal>()).Where(p=>p.Value!=0).OrderBy(p=>p.Key).ToArray();

   if(quantity<=0&&values.Length==0)return index;

   var pieces=key.Split(new[]{" / "},StringSplitOptions.None);string detail=pieces.Length>1?pieces[0]:"Production stage";

   if(connected!=null&&connected.Buildings>1)detail+=" · "+connected.Buildings+" buildings";

   string cost=money?string.Join("\n",values.Select(p=>LedgerHistory.Money(new[]{new KeyValuePair<string,decimal>(p.Key,-p.Value)}))):"";

   Row(index,pieces.Length>1?pieces[1]:pieces[0],detail,quantity>0?LedgerHistory.Number(quantity):"",cost,connected?.Element.Icon,null,key+(values.Length>0?"\n"+LedgerHistory.Money(values):""),Math.Max(44,values.Length*20+8));

   return index+1;

  }

  internal void Refresh()

  {

   var s=ledger.History.Summary(ledger.Period);count.text=ledger.Inputs.Count+(ledger.Inputs.Count==1?" pair":" pairs");

   minus.interactable=ledger.Inputs.Count>1;plus.interactable=ledger.Inputs.Count<5;

   var seconds=new[]{1,60,3600};for(int i=0;i<3;i++)ui.Select(periods[i],ledger.Period==seconds[i]);

   for(int i=0;i<2;i++)ui.Select(tabs[i],tab==i);

   string unit=ledger.Period==1?"second":ledger.Period==60?"minute":"hour";

   bool money=ledger.ShowFinancials(s);

   subtitle.text="Last "+unit;

   RefreshFinance(s,money);

   if(!money)

   {

    earned.Count("Items passed",LedgerHistory.Number(s.Items.Values.Sum()),"Last "+unit);

    spent.Count("Item types",s.Items.Count.ToString(),"Distinct items passed");

    profit.Count("Connected pairs",ledger.Outputs.Count(o=>o.ConnectedPipe!=null)+" / "+ledger.Outputs.Count,"Matched inputs / outputs");

   }

   activity.text=LedgerHistory.Number(s.Items.Values.Sum())+" items passed  ·  "+s.Items.Count+" types";

   historyHelp.SetActive(money);

   status.text=!money?"":s.UnknownPassed>0?"Partial cost history  ?":s.Items.Count==0?"Waiting for production":"Cost history complete";

   status.color=s.UnknownPassed>0?LedgerUi.Gold:LedgerUi.Muted;

   tabs[1].GetComponentInChildren<TMP_Text>().text=money?"Chain costs":"Chain items";

   firstColumn.text=tab==0?"ITEM":"PRODUCTION CHAIN";

   unitsColumn.text="UNITS";

   lastColumn.text=tab==0?"":money?"OBSERVED SPEND":"";

   // Reset presentation when pooled rows change between tabs.

   for(int i=0;i<rows.Count;i++){rows[i].Name.fontSize=18;rows[i].Background.color=new Color(0,0,0,i%2==0?.15f:.04f);}

   int n=0;reportY=0;

   if(tab==0)

   {

    foreach(var p in s.Items.OrderBy(p=>LedgerHistory.ItemName(p.Key)))Row(n++,LedgerHistory.ItemName(p.Key),"","× "+LedgerHistory.Number(p.Value),"",ItemType.Get(p.Key)?.PreviewItem.Icon);

    if(n==0)Row(n++,"No items passed yet","Items appear here as they move through Ledger.","","",ui.Sprite("Icons_149"),LedgerUi.Muted);

   }

   else if(tab==1)

   {

    var chain=LedgerChain.Read(ledger);var listed=new HashSet<string>();

    n=Heading(n,"PRODUCTION STAGES");

    foreach(var entry in chain.Stages){n=ChainStage(n,entry.Stage,s,money,entry);listed.Add(entry.Stage);}

    foreach(var key in s.Costs.Stages.Keys.Where(k=>!listed.Contains(k)).OrderBy(k=>k))n=ChainStage(n,key,s,money);

    var resources=s.Costs.Resources.Where(p=>p.Value>0).OrderBy(p=>LedgerHistory.ItemName(p.Key)).ToArray();

    if(resources.Length>0)n=Heading(n,"RAW RESOURCES");

    foreach(var resource in resources)Row(n++,LedgerHistory.ItemName(resource.Key),"",LedgerHistory.Number(resource.Value),"",ItemType.Get(resource.Key)?.PreviewItem.Icon);

    if(n==1)Row(n++,"No production observations yet","Recorded stages and resources will appear here.","","",null,LedgerUi.Muted);



   }

   for(int i=n;i<rows.Count;i++)rows[i].Root.gameObject.SetActive(false);

   content.sizeDelta=new Vector2(ReportWidth,Math.Max(ReportHeight,reportY));

  }

  void Update()

  {

   if(!owner||!owner.isActiveAndEnabled||!WorldFlowManager.Visible){ClosePopup();return;}

   if(Time.frameCount!=openedFrame&&((Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)||(Mouse.current!=null&&Mouse.current.rightButton.wasPressedThisFrame))){ClosePopup();return;}

   Fit();if((refresh-=Time.unscaledDeltaTime)<=0){refresh=.25f;Refresh();}

  }

  public override void PopupClosed(){if(EventSystem.current&&EventSystem.current.currentSelectedGameObject&&EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))EventSystem.current.SetSelectedGameObject(null);base.PopupClosed();}

  void OnDisable(){base.OnPointerExit(null);if(restoreInput&&GameUI.Instance&&!GameUI.MenuVisible)GameInput.Enable();restoreInput=false;}

 }

}

