using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using HarmonyLib;
using UnityEngine;

namespace KingdomLedger
{
    internal static partial class LedgerObserver
    {
        static Dictionary<string,decimal> IncomeBalances(FlowElement e)
        {
         var values=new Dictionary<string,decimal>();
         if(e is Shop shop)values["Gold"]=shop.CollectedGold;
         if(e is RequisitionOffice office)values["Requisition"]=office.CollectedRequisition;
         if(e is Disenchanter disenchanter)values["Mana"]=disenchanter.CollectedMana;
         if(e is Restaurant restaurant){values["Gold"]=restaurant.CollectedGold;values["Crown"]=restaurant.CollectedCrowns;}
         return values;
        }
    }
}
