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
        internal static string CurrencyName(string currency)=>Enum.TryParse(currency,out Currency value)?Assets.Source.Util.Translation.Translate(value.GetDisplayName()):currency;
    }
}
