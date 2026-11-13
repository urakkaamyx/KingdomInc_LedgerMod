using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace KingdomLedger
{
    internal sealed class Scope
    {
        internal global::KingdomLedger.Scope Parent;
        internal WorkspaceObject Element;
        internal string Method;
        internal HashSet<GameInventoryItem> Outputs;
        internal GameCraftingInput Input;
        internal GameInventoryItem Human, Equipment;
        internal int Queued;
        internal Dictionary<string, decimal> IncomeBefore;
        internal int StoredValue;
        internal float Delta, PassiveTimer;
        internal readonly List<GameInventoryItem> Consumed = new List<GameInventoryItem>();
        internal readonly Dictionary<string, decimal> Spent = new Dictionary<string, decimal>(), Earned = new Dictionary<string, decimal>();
    }
}
