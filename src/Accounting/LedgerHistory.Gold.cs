using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomLedger
{
    internal sealed partial class LedgerHistory
    {
        internal static string Gold(decimal amount)
        {
         var value=Math.Abs(amount);decimal gold=Math.Floor(value/10000m),silver=Math.Floor(value%10000m/100m),copper=value%100m;
         return (amount<0?"-":"")+Number(gold)+"g "+Number(silver)+"s "+Number(copper)+"c";
        }
    }
}
