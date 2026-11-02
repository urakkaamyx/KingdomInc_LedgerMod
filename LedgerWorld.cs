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
  Ledger ledger;LedgerUi ui;TMP_Text period,pageText,throughput;RectTransform moneyStrip,body;
  readonly List<NodeCurrencyRow> moneyRows=new List<NodeCurrencyRow>();
  sealed class NodeCurrencyRow {internal RectTransform Root;internal TMP_Text Cost,Revenue,Net;internal Image Icon;internal TooltipSource Tip;}
  readonly TMP_Text[] names=new TMP_Text[6],amounts=new TMP_Text[6];readonly Image[] icons=new Image[6];
  readonly Image[] shades=new Image[6];
  Button previous,next;int page;float refresh;bool initialized,openWhenReady;
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
   gear.gameObject.AddComponent<TooltipSource>().SetTooltip("Ledger settings","Reporting window, input/output pairs, entire chain costs and shop profit.");
   period=ui.Text(body,"",32,35,270,24,13);period.color=LedgerUi.Muted;
   var quantity=ui.Text(body,"QTY",300,35,66,24,12);quantity.color=LedgerUi.Muted;quantity.alignment=TextAlignmentOptions.MidlineRight;
   ui.Rule(body,30,62,338);
   for(int i=0;i<6;i++)
   {
    if(i%2==0)shades[i]=ui.Fill(body,"ItemRow"+i,30,68+i*24,338,24,new Color(0,0,0,.12f));
    icons[i]=ui.Icon(body,null,36,70+i*24,20);icons[i].name="ItemIcon"+i;
    names[i]=ui.Text(body,"",68,68+i*24,214,24,17);
    amounts[i]=ui.Text(body,"",286,68+i*24,72,24,17);amounts[i].alignment=TextAlignmentOptions.MidlineRight;amounts[i].color=LedgerUi.Gold;
   }
   moneyStrip=LedgerUi.Rect(body,"MonetaryTotals",30,216,338,38);ui.Rule(moneyStrip,0,0,338);
   var labels=new[]{"OP. COSTS","REVENUE","NET"};
   for(int i=0;i<3;i++){var label=ui.Text(moneyStrip,labels[i],24+i*104,3,100,14,10);label.color=LedgerUi.Muted;}
   throughput=ui.Text(body,"",32,222,334,28,14);throughput.name="PassthroughTotal";throughput.color=LedgerUi.Muted;
   previous=ui.Button(body,"<",30,256,30,24,()=>{page--;Refresh();});next=ui.Button(body,">",338,256,30,24,()=>{page++;Refresh();});
   pageText=ui.Text(body,"",68,256,260,24,13);pageText.alignment=TextAlignmentOptions.Center;
   initialized=true;Refresh();return true;
  }
  void Update()
  {
   if(!TryInitialize())return;
   if(openWhenReady)OpenSettings();
   if((refresh-=Time.unscaledDeltaTime)<=0){refresh=.2f;Refresh();}
  }
  internal void Refresh()
  {
   if(!initialized)return;var summary=ledger.History.Summary(ledger.Period);var items=summary.Items.OrderBy(p=>LedgerHistory.ItemName(p.Key)).ToArray();
   int pages=Math.Max(1,(items.Length+5)/6);page=Math.Max(0,Math.Min(pages-1,page));
   string unit=ledger.Period==1?"second":ledger.Period==60?"minute":"hour";
   period.text="ITEMS PASSED / LAST "+unit.ToUpperInvariant();
   for(int i=0;i<6;i++)
   {
    int n=page*6+i;bool exists=n<items.Length;icons[i].enabled=exists;
    if(shades[i])shades[i].enabled=exists;
    names[i].text=exists?LedgerHistory.ItemName(items[n].Key):i==0&&items.Length==0?"No items passed yet":"";
    amounts[i].text=exists?LedgerHistory.Number(items[n].Value):"";
    if(exists)icons[i].sprite=ItemType.Get(items[n].Key)?.PreviewItem.Icon;
   }
   previous.interactable=page>0;next.interactable=page<pages-1;
   pageText.text=ledger.Inputs.Count+" in / "+ledger.Outputs.Count+" out · "+(page+1)+" / "+pages;
   var currencies=summary.Currencies;bool money=currencies.Length>0;moneyStrip.gameObject.SetActive(money);throughput.gameObject.SetActive(!money);
   throughput.text=LedgerHistory.Number(summary.Items.Values.Sum())+" items passed · "+summary.Items.Count+" types";
   int extra=Math.Max(0,currencies.Length-1)*22;int height=Ledger.Height+extra;
   moneyStrip.sizeDelta=new Vector2(338,38+extra);body.sizeDelta=new Vector2(Ledger.Width,height);
   if(ledger.Size.y!=height){ledger.Size=new Vector2Int(Ledger.Width,height);((RectTransform)transform).sizeDelta=ledger.Size;if(ledger.World.Collider)ledger.World.Collider.UpdateSize();}
   previous.GetComponent<RectTransform>().anchoredPosition=new Vector2(30,-256-extra);next.GetComponent<RectTransform>().anchoredPosition=new Vector2(338,-256-extra);pageText.rectTransform.anchoredPosition=new Vector2(68,-256-extra);
   for(int i=0;i<currencies.Length;i++)
   {
    if(moneyRows.Count<=i)
    {
     var row=new NodeCurrencyRow();row.Root=LedgerUi.Rect(moneyStrip,"CurrencyRow"+i,0,17+i*22,338,22);
     row.Icon=ui.Icon(row.Root,null,0,3,16);var values=new TMP_Text[3];
     for(int j=0;j<3;j++){values[j]=ui.Text(row.Root,"",24+j*104,0,100,22,13);values[j].enableAutoSizing=true;values[j].fontSizeMin=9;values[j].fontSizeMax=13;}
     row.Cost=values[0];row.Cost.name="Cost";row.Revenue=values[1];row.Revenue.name="Revenue";row.Net=values[2];row.Net.name="Net";
     var hover=ui.Fill(row.Root,"CurrencyHelp",0,0,338,22,Color.clear);hover.raycastTarget=true;row.Tip=hover.gameObject.AddComponent<TooltipSource>();moneyRows.Add(row);
    }
    string currency=currencies[i];var r=moneyRows[i];r.Root.name=currency;r.Root.gameObject.SetActive(true);
    if(Enum.TryParse(currency,out Currency type)){r.Icon.sprite=type.GetIcon();r.Icon.enabled=true;}
    summary.Costs.Money.TryGetValue(currency,out var cost);summary.Revenue.TryGetValue(currency,out var revenue);decimal net=revenue-cost;
    r.Cost.text=LedgerHistory.Amount(currency,cost);r.Cost.color=LedgerUi.Gold;r.Revenue.text=LedgerHistory.Amount(currency,revenue);
    r.Net.text=summary.ProfitComplete?LedgerHistory.Amount(currency,net,true):"Unknown";r.Net.color=summary.ProfitComplete?(net<0?LedgerUi.Red:net>0?LedgerUi.Green:LedgerUi.Muted):LedgerUi.Muted;
    r.Tip.SetTooltip(LedgerHistory.CurrencyName(currency),"Operating costs: "+LedgerHistory.Amount(currency,cost)+"\nRevenue: "+LedgerHistory.Amount(currency,revenue)+"\nNet: "+r.Net.text);
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
