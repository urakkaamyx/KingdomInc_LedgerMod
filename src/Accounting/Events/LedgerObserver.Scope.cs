using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;

namespace KingdomLedger
{
    internal static partial class LedgerObserver
    {
        internal sealed partial class Scope
        {
            internal Scope Parent; internal FlowElement Element;internal string Method;
            internal HashSet<InventoryItem> Outputs;
            internal CraftingInput Input;
            internal InventoryItem Human,Equipment;
            internal int Queued;
            internal Dictionary<string,decimal> IncomeBefore;internal int StoredValue;internal float Delta,PassiveTimer;
            internal readonly List<InventoryItem> Consumed=new List<InventoryItem>();
            internal readonly Dictionary<string,decimal> Spent=new Dictionary<string,decimal>(),Earned=new Dictionary<string,decimal>();
        }
    }
}
