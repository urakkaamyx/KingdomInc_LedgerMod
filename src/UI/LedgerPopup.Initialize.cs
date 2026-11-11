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
    public sealed partial class LedgerPopup:UIPopupMenu
    {
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
    }
}
