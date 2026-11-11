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
        internal partial class LedgerBucket
        {
            internal long Second;
            internal readonly LedgerSummary Value=new LedgerSummary();
        }
    }
}
