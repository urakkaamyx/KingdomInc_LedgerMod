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
        internal static string Money(IEnumerable<KeyValuePair<string,decimal>> values)
        {
         var text=values.Where(p=>p.Value!=0).OrderBy(p=>p.Key).Select(p=>p.Key=="Gold"?Gold(p.Value):Number(p.Value)+" "+CurrencyName(p.Key));
         return string.Join(" · ",text.DefaultIfEmpty("0g 0s 0c"));
        }
    }
}
