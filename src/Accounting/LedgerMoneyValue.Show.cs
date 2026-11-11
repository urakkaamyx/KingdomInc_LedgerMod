using System;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Player;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace KingdomLedger
{
    internal sealed partial class LedgerMoneyValue
    {
        internal void Show(string currency,decimal amount,Color color,bool signed=false,bool unknown=false)
        {
         bool gold=currency=="Gold"&&!unknown;coinGroup.SetActive(gold);number.gameObject.SetActive(!gold);
         number.text=unknown?"Unknown":LedgerHistory.Amount(currency,amount,signed);number.color=color;
         if(!gold)return;
         var value=Math.Abs(amount);sign.text=amount<0?"-":signed&&amount>0?"+":"";sign.color=color;
         var values=new[]{Math.Floor(value/10000m),Math.Floor(value%10000m/100m),value%100m};
         for(int i=0;i<3;i++){coins[i].text=LedgerHistory.Number(values[i]);coins[i].color=color;}
        }
    }
}
