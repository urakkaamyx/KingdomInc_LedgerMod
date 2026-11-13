using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KingdomLedger
{
    internal class LedgerBucket
    {
        internal long Second;
        internal readonly LedgerSummary Value = new LedgerSummary();
    }
}
