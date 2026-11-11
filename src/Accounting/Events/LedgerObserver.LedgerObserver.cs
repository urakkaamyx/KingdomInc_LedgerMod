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
        static ConditionalWeakTable<InventoryItem,CostTrace> traces=new ConditionalWeakTable<InventoryItem,CostTrace>();
        static ConditionalWeakTable<CraftingInput,Batch> batches=new ConditionalWeakTable<CraftingInput,Batch>();
        static ConditionalWeakTable<Outfitter,OutfitBatch> outfits=new ConditionalWeakTable<Outfitter,OutfitBatch>();
        static ConditionalWeakTable<FlowElement,Queue<CostTrace>> queued=new ConditionalWeakTable<FlowElement,Queue<CostTrace>>();
        static ConditionalWeakTable<InventoryItem,Tickets> tickets=new ConditionalWeakTable<InventoryItem,Tickets>();
        [ThreadStatic] static Scope current;
        static GamePlayer player;
        static readonly HashSet<string> Routers=new HashSet<string>{"Collector","Distributor","Manifold","Filter","TrackSwitch","Stockpile","Inspector","SingleButton","Timer","Accountant","Appraiser","Portal"};
        internal static bool IsRoutingStage(string stage)=>Routers.Contains(stage.Split(new[]{" / "},StringSplitOptions.None)[0])||stage==Ledger.TypeId;
    }
}
