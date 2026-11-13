using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace KingdomLedger
{
    internal sealed class Batch
    {
        internal CostTrace Cost;
        internal bool Charged;
        internal global::KingdomLedger.Tickets Receipts;
    }
}
