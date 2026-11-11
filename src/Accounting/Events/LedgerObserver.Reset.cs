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
        internal static void Reset()
        {
         player=GamePlayer.Current;traces=new ConditionalWeakTable<InventoryItem,CostTrace>();batches=new ConditionalWeakTable<CraftingInput,Batch>();outfits=new ConditionalWeakTable<Outfitter,OutfitBatch>();
         queued=new ConditionalWeakTable<FlowElement,Queue<CostTrace>>();tickets=new ConditionalWeakTable<InventoryItem,Tickets>();current=null;ResetIncome();
        }
    }
}
