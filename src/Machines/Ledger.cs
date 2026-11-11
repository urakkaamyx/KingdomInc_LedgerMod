using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using LightJson;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomLedger
{
    public sealed partial class Ledger : WorkspaceNode
    {
        public const string TypeId="Ledger", TechId="ledger_mod";
        public const int Grid=64, Width=6*Grid, Height=3*Grid;
        // Header band: full-width native backdrop behind the column labels, timer directly below,
        // then a small gap before the item rows. Paired ports begin on the first grid row (row 0)
        // to align with adjacent nodes.
        internal const int HeaderTop=30, HeaderHeight=30, TimerTop=61, ItemTop=72, RowHeight=24;
        internal const string NodeDescription="A zero-storage production ledger. Items enter only when their matching output can deliver immediately. Gear: reporting period, paired ports and full chain costs. Actual shop sales and currency-converting destinations determine income.";
        public int Period { get; private set; }=60;
        double periodStarted=LedgerHistory.Clock();
        internal double PeriodElapsed=>Math.Max(0,LedgerHistory.Clock()-periodStarted)%Period;
        internal float PeriodProgress=>(float)(PeriodElapsed/Period);
        internal readonly LedgerHistory History=new LedgerHistory();
        internal bool HasIncomeOutput=>LedgerChain.HasIncomeDestination(this);
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
        internal static int DisplayHeight(int pairs,int items,int currencies)
        {
         int requiredHeight=Math.Max(Height,pairs*Grid);
         if(items==0&&currencies==0)return requiredHeight;
         int itemBottom=ItemTop+Math.Max(1,items)*RowHeight;
         int contentBottom=currencies>0?itemBottom+4+38+Math.Max(0,(currencies+1)/2-1)*20+6:itemBottom+24+8;
         return Math.Max(requiredHeight,((contentBottom+Grid-1)/Grid)*Grid);
        }
    }
}
