using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KingdomLedger
{
    internal sealed class RequisitionShare
    {
        internal decimal Value;
        internal global::KingdomLedger.Tickets Source;
        internal bool Credited;
    }
}
