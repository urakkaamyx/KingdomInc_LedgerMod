using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace KingdomLedger
{
    internal sealed class Tickets
    {
        internal readonly List<(WeakReference<LedgerHistory> ledger, CostTrace trace)> Values = new List<(WeakReference<LedgerHistory>, CostTrace)>();
    }
}
