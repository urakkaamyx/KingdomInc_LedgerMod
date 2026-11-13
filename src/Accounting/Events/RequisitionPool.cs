using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KingdomLedger
{
    internal sealed class RequisitionPool
    {
        internal readonly Queue<global::KingdomLedger.RequisitionShare> Shares = new Queue<global::KingdomLedger.RequisitionShare>();
        internal decimal Value;
        internal void Add(decimal amount, global::KingdomLedger.Tickets source = null)
        {
            if (amount <= 0)
                return;
            Shares.Enqueue(new global::KingdomLedger.RequisitionShare { Value = amount, Source = source });
            Value += amount;
        }
    }
}
