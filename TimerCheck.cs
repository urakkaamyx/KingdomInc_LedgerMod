using System;
using System.IO;
using System.Linq;
using Assets.Source.Flow.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
 public sealed partial class LedgerPlugin
 {
  void TimerChecks(Ledger ledger,LedgerWorld view,string directory)
  {
   var clock=LedgerHistory.Clock;int oldPeriod=ledger.Period;double now=clock()+2000;LedgerHistory.Clock=()=>now;
   var track=(RectTransform)view.transform.Find("LedgerDisplay/WindowTimer");var fill=(RectTransform)track.Find("Fill");
   try
   {
    foreach(int period in new[]{1,60,3600})
    {
     ledger.SetPeriod(period==1?60:1);ledger.SetPeriod(period);view.RefreshTimer();
     Check(fill.sizeDelta.x==0,"Timer starts empty when selecting the "+period+"-second window");
     now+=period*.5;view.RefreshTimer();
     Check(Math.Abs(fill.sizeDelta.x-LedgerWorld.ContentWidth*.5)<.01,"Node timer fills halfway through the "+period+"-second interval");
     now+=period*.75;view.RefreshTimer();
     Check(Math.Abs(fill.sizeDelta.x-LedgerWorld.ContentWidth*.25)<.01,"Node timer wraps correctly at the "+period+"-second boundary");
    }
    Check(track.anchoredPosition==new Vector2(30,-61)&&track.sizeDelta==new Vector2(LedgerWorld.ContentWidth,4)&&!fill.GetComponent<Image>().raycastTarget,"Slim timer fits below the existing header without adding a footer or blocking its hover target");
    ledger.SetPeriod(60);now+=54;var item=Item();Seed(item,100);ledger.History.Pass(item,0,new Stockpile());
    now+=12;view.Refresh();
    Check(ledger.History.Summary(60).Items["iron_sword"]==1&&ledger.History.Summary(60).Costs.Money["Gold"]==100,"Timer wrapping preserves items and costs that remain inside the rolling window");
    view.OpenSettings();view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="Period3600").onClick.Invoke();
    Check(fill.sizeDelta.x==0&&ledger.Period==3600,"Actual gear selection restarts the visible node timer immediately");
    view.Settings.GetComponentsInChildren<Button>().Single(b=>b.name=="Period60").onClick.Invoke();view.Settings.ClosePopup();now+=30;view.Refresh();
    ledger.SetPeriod(60);view.RefreshTimer();
    Check(Math.Abs(fill.sizeDelta.x-LedgerWorld.ContentWidth*.5)<.01,"Reselecting the same period retains elapsed timer progress");
    Capture(Path.Combine(directory,"ledger-timer-node.png"));
   }
   finally
   {
    if(view.Settings!=null&&view.Settings.IsOpen)view.Settings.ClosePopup();
    LedgerHistory.Clock=clock;ledger.SetPeriod(oldPeriod==1?60:1);ledger.SetPeriod(oldPeriod);view.Refresh();
   }
  }
 }
}
