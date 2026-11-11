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
        internal static string Amount(string currency,decimal amount,bool signed=false)=>currency=="Gold"?(signed&&amount>0?"+":"")+Gold(amount):(signed&&amount>0?"+":"")+Number(amount);
    }
}
