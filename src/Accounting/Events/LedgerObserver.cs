using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KingdomLedger
{
    internal static class LedgerObserver
    {
        static void CaptureIncome(global::KingdomLedger.Scope scope)
        {
            if (!IsIncomeReceiver(scope.Element))
                return;
            scope.IncomeBefore = IncomeBalances(scope.Element);
            if (scope.Element.TypeId == "RequisitionOffice")
            {
                scope.StoredValue = scope.Element.Production.StoredValue;
                scope.PassiveTimer = scope.Element.Production.PassiveTimer;
            }

            if (scope.Element.TypeId == "Disenchanter")
                scope.PassiveTimer = scope.Element.Production.PassiveTimer;
        }

        static void Consumed(WorkspacePort __instance, GameInventoryItem __result)
        {
            if (current != null && __instance.IsInput && __instance.Parent == current.Element && __result != null)
                current.Consumed.Add(__result);
        }

        static void Credit(global::KingdomLedger.Tickets source, IDictionary<string, decimal> income, bool first = true)
        {
            if (source == null || !income.Values.Any(v => v > 0))
                return;
            foreach (var entry in source.Values)
                if (entry.ledger.TryGetTarget(out var ledger))
                    ledger.Sale(entry.trace, income, first);
        }

        static void StartEnchanting(global::KingdomLedger.Scope scope, global::KingdomLedger.Batch batch)
        {
            global::KingdomLedger.Tickets sources = new global::KingdomLedger.Tickets();
            // A recipe may consume several inputs from the same Ledger or from chained
            // Ledgers. Record its actual payment once per distinct report, never per input.
            foreach (var item in scope.Consumed.Distinct())
            {
                if (!tickets.TryGetValue(item, out global::KingdomLedger.Tickets receipt))
                    continue;
                foreach (var entry in receipt.Values)
                {
                    if (!entry.ledger.TryGetTarget(out var ledger))
                        continue;
                    var existing = sources.Values.Find(v => v.ledger.TryGetTarget(out var h) && ReferenceEquals(h, ledger));
                    if (existing.trace != null)
                        existing.trace.Merge(entry.trace);
                    else
                        sources.Values.Add((new WeakReference<LedgerHistory>(ledger), entry.trace.Copy()));
                }

                tickets.Remove(item);
            }

            if (sources.Values.Count == 0)
                return;
            batch.Receipts = sources;
            foreach (var entry in sources.Values)
            {
                entry.trace.Stage(Stage(scope.Element), scope.Spent, 0);
                if (entry.ledger.TryGetTarget(out var ledger))
                    ledger.Endpoint(Stage(scope.Element), scope.Spent, 0);
            }
        }

        static void Earned(GameCurrency __0, long __1, bool __2)
        {
            if (current != null && __2 && __1 > 0)
                CostTrace.Add(current.Earned, __0.ToString(), __1);
        }

        static void Endpoint(global::KingdomLedger.Tickets source, WorkspaceObject destination, IDictionary<string, decimal> spend)
        {
            if (source == null)
                return;
            foreach (var entry in source.Values)
                if (entry.ledger.TryGetTarget(out var ledger))
                    ledger.Endpoint(Stage(destination), spend);
        }

        static object Enter(GameExecutionCall call)
        {
            try
            {
                if (FoundationApi.Kingdom == null || !(call.Subject is WorkspaceObject element))
                    return null;
                string method = call.Operation;
                if (method != "Update" && method != "DoManualCraft" && method != "QueueCraft")
                    return null;
                if (!ReferenceEquals(player, FoundationApi.Kingdom))
                    Reset();
                if (Routers.Contains(element.TypeId) || (current?.Element == element && method != "QueueCraft"))
                    return null;
                global::KingdomLedger.Scope state = new global::KingdomLedger.Scope
                {
                    Parent = current,
                    Element = element,
                    Method = method,
                    Outputs = new HashSet<GameInventoryItem>(element.Outputs.SelectMany(output => output.BufferedItems)),
                    Input = element.Production.Input,
                    Human = element.Production.CurrentHuman,
                    Equipment = element.Production.CurrentEquipment,
                    Queued = QueueCount(element),
                    Delta = method == "Update" && call.ArgumentCount > 0 ? call.Argument<float>(0) : 0
                };
                CaptureIncome(state);
                current = state;
                return state;
            }
            catch (Exception error)
            {
                LedgerPlugin.Warn("Observer start: " + error.Message);
                return null;
            }
        }

        static Exception Exit(Exception __exception, global::KingdomLedger.Scope __state)
        {
            if (__state == null)
                return __exception;
            try
            {
                if (__exception == null)
                    Finish(__state);
            }
            catch (Exception e)
            {
                LedgerPlugin.Warn("Observer finish: " + e.Message);
            }
            finally
            {
                current = __state.Parent;
            }

            return __exception;
        }

        static void Finish(global::KingdomLedger.Scope s)
        {
            var e = s.Element;
            if ((e.Production.IsResource || e.TypeId == "Forest") && s.Method == "QueueCraft")
            {
                if (QueueCount(e) > s.Queued)
                {
                    var trace = new CostTrace();
                    trace.Stage(Stage(e), s.Spent, 0);
                    queued.GetOrCreateValue(e).Enqueue(trace);
                    if (s.Parent?.Element == e)
                        s.Parent.Queued++;
                }

                return;
            }

            var outputs = e.Outputs.SelectMany(o => o.BufferedItems).Where(i => !s.Outputs.Contains(i)).Distinct().ToArray();
            if (FinishIncome(s))
                return;
            if (e.TypeId == "Outfitter")
            {
                // WorkspaceObject consumes both inputs at the START of a job, then emits the
                // same human on a later update. Keep that job's provenance between frames.
                if (e.Production.CurrentHuman != null && !ReferenceEquals(e.Production.CurrentHuman, s.Human))
                {
                    var trace = Inputs(s.Consumed);
                    if (!s.Consumed.Contains(e.Production.CurrentHuman) || !s.Consumed.Contains(e.Production.CurrentEquipment))
                        trace.Complete = false;
                    trace.Stage(Stage(e), s.Spent, 0);
                    outfits.Remove(e);
                    outfits.Add(e, new global::KingdomLedger.OutfitBatch { Human = e.Production.CurrentHuman, Cost = trace });
                }

                foreach (var output in outputs)
                {
                    CostTrace trace;
                    if (outfits.TryGetValue(e, out global::KingdomLedger.OutfitBatch job) && ReferenceEquals(output, job.Human))
                        trace = job.Cost.Copy();
                    else if (ReferenceEquals(output, s.Human))
                        trace = Inputs(new[] { s.Human, s.Equipment }.Where(i => i != null));
                    else if (s.Consumed.Contains(output))
                        trace = Inputs(s.Consumed);
                    else
                    {
                        trace = Trace(output).Copy();
                        trace.Complete = false;
                    }

                    trace.Stage(Stage(e), s.Spent, 1);
                    SetTrace(output, trace);
                }

                if (e.Production.CurrentHuman == null)
                    outfits.Remove(e);
                return;
            }

            if (e.Production.IsRecipe)
            {
                if (outputs.Length > 0 || (e.TypeId == "Enchanter" && s.Input != null && s.Earned.Count > 0))
                {
                    CostTrace trace;
                    global::KingdomLedger.Batch batch = null;
                    bool observed = s.Input != null && batches.TryGetValue(s.Input, out batch), first = observed && !batch.Charged;
                    if (observed)
                    {
                        trace = batch.Charged ? new CostTrace() : batch.Cost.Copy();
                        batch.Charged = true;
                    }
                    else
                        trace = Inputs(s.Input?.AllItems ?? Enumerable.Empty<GameInventoryItem>());
                    if (s.Input == null || !batches.TryGetValue(s.Input, out _))
                        trace.Complete = false;
                    foreach (var reward in s.Earned)
                        CostTrace.Add(trace.Rewards, reward.Key, reward.Value);
                    trace.Stage(Stage(e), new Dictionary<string, decimal>(), outputs.Length);
                    foreach (var output in outputs)
                        SetTrace(output, trace.Copy(1m / outputs.Length));
                    if (e.TypeId == "Enchanter" && observed)
                        FinishEnchanting(s, batch, outputs, first);
                }

                if (e.Production.Input != null && !ReferenceEquals(e.Production.Input, s.Input))
                {
                    var trace = Inputs(s.Consumed);
                    trace.Stage(Stage(e), s.Spent, 0);
                    global::KingdomLedger.Batch next = new global::KingdomLedger.Batch
                    {
                        Cost = trace
                    };
                    if (e.TypeId == "Enchanter")
                        StartEnchanting(s, next);
                    batches.Remove(e.Production.Input);
                    batches.Add(e.Production.Input, next);
                }

                return;
            }

            if ((e.Production.IsResource || e.TypeId == "Forest") && outputs.Length > 0)
            {
                var trace = new CostTrace();
                bool paidCycle = QueueCount(e) < s.Queued;
                if (paidCycle)
                {
                    if (queued.TryGetValue(e, out var q) && q.Count > 0)
                        trace.Merge(q.Dequeue());
                    else
                        trace.Complete = false;
                }

                // Productivity outputs have no additional ingredient/operating spend.
                // The paid output bears the paid resource ticket; no cost is duplicated.
                trace.Stage(Stage(e), new Dictionary<string, decimal>(), outputs.Length);
                foreach (var output in outputs)
                {
                    var unit = trace.Copy(1m / outputs.Length);
                    CostTrace.Add(unit.Resources, output.Type.Identifier, 1);
                    SetTrace(output, unit);
                }

                return;
            }

            if (outputs.Length > 0)
            {
                var trace = Inputs(s.Consumed);
                trace.Complete = false;
                trace.Stage(Stage(e), s.Spent, outputs.Length);
                foreach (var output in outputs)
                {
                    // A delayed utility may return an existing object with valuable upstream
                    // history. Preserve it even when this utility's extra costs are unknown.
                    if (traces.TryGetValue(output, out var known))
                    {
                        var retained = known.Copy();
                        retained.Complete = false;
                        retained.Stage(Stage(e), s.Spent.ToDictionary(p => p.Key, p => p.Value / outputs.Length), 1);
                        SetTrace(output, retained);
                    }
                    else
                        SetTrace(output, trace.Copy(1m / outputs.Length));
                }
            }
        }

        static void FinishEnchanting(global::KingdomLedger.Scope scope, global::KingdomLedger.Batch batch, GameInventoryItem[] outputs, bool first)
        {
            if (batch.Receipts == null)
                return;
            // Update can finish this job and pay for its next job in the same call.
            // scope.Spent belongs to that next job; only StartEnchanting attributes it.
            var completionSpend = new Dictionary<string, decimal>();
            foreach (var entry in batch.Receipts.Values)
            {
                if (!entry.ledger.TryGetTarget(out var ledger))
                    continue;
                ledger.Endpoint(Stage(scope.Element), completionSpend, Math.Max(1, outputs.Length));
                if (scope.Earned.Count > 0)
                    ledger.Sale(entry.trace, scope.Earned, false);
            }

            foreach (var output in outputs)
            {
                global::KingdomLedger.Tickets source = new global::KingdomLedger.Tickets();
                foreach (var entry in batch.Receipts.Values)
                {
                    // Native enchantments return their original equipment; material recipes
                    // create new objects. Carry receipts in both cases, splitting the paid
                    // basis across a batch and assigning no duplicate cost to productivity.
                    var trace = first ? entry.trace.Copy(1m / outputs.Length) : new CostTrace();
                    trace.Stage(Stage(scope.Element), completionSpend, 1);
                    source.Values.Add((entry.ledger, trace));
                }

                tickets.Remove(output);
                tickets.Add(output, source);
            }
        }

        static bool FinishIncome(global::KingdomLedger.Scope scope)
        {
            var element = scope.Element;
            if (!IsIncomeReceiver(element))
                return false;
            // Earned currency plus the change in its native collection tray is the
            // new reward. Flushing an existing tray contributes zero, even in Update.
            var income = new Dictionary<string, decimal>(scope.Earned);
            foreach (var value in IncomeBalances(element))
                CostTrace.Add(income, value.Key, value.Value);
            foreach (var value in scope.IncomeBefore)
                CostTrace.Add(income, value.Key, -value.Value);
            if (element.TypeId == "RequisitionOffice")
            {
                FinishRequisition(scope, element, income);
                return true;
            }

            if (scope.Method == "Update" && element.TypeId == "Disenchanter" && scope.PassiveTimer + scope.Delta >= 1f)
                CostTrace.Add(income, "Mana", -(int)element.Production.PassiveGain);
            if (scope.Consumed.Count == 1)
            {
                var item = scope.Consumed[0];
                tickets.TryGetValue(item, out global::KingdomLedger.Tickets source);
                Endpoint(source, element, scope.Spent);
                Credit(source, income.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value));
                tickets.Remove(item);
            }

            return true;
        }

        static void FinishRequisition(global::KingdomLedger.Scope scope, WorkspaceObject office, Dictionary<string, decimal> income)
        {
            if (scope.Method != "Update")
                return;
            int unit = office.Production.ValuePerRequisition;
            if (unit <= 0)
                return;
            global::KingdomLedger.RequisitionPool pool = requisitions.GetOrCreateValue(office);
            // A loaded residual, or value changed by another mod, has no attributable
            // history. Preserve it as unowned rather than giving it to the next item.
            if (pool.Value != scope.StoredValue)
            {
                pool.Shares.Clear();
                pool.Value = 0;
                pool.Add(scope.StoredValue);
            }

            income.TryGetValue("Requisition", out var minted);
            decimal added = office.Production.StoredValue - scope.StoredValue + minted * unit;
            decimal passive = scope.PassiveTimer + scope.Delta >= 1f && scope.IncomeBefore["Requisition"] < office.Production.MaximumRequisition ? Mathf.RoundToInt(unit * office.Production.PassiveGain) : 0;
            passive = Math.Min(Math.Max(0, added), passive);
            pool.Add(passive);
            if (scope.Consumed.Count == 1)
            {
                var item = scope.Consumed[0];
                tickets.TryGetValue(item, out global::KingdomLedger.Tickets source);
                Endpoint(source, office, scope.Spent);
                pool.Add(Math.Max(0, added - passive), source);
                tickets.Remove(item);
            }
            else
                pool.Add(Math.Max(0, added - passive));
            decimal remaining = Math.Max(0, minted) * unit;
            while (remaining > 0 && pool.Shares.Count > 0)
            {
                global::KingdomLedger.RequisitionShare share = pool.Shares.Peek();
                decimal amount = Math.Min(remaining, share.Value);
                Credit(share.Source, new Dictionary<string, decimal> { { "Requisition", amount / unit } }, !share.Credited);
                share.Credited = true;
                remaining -= amount;
                share.Value -= amount;
                pool.Value -= amount;
                if (share.Value == 0)
                    pool.Shares.Dequeue();
            }
        }

        static Dictionary<string, decimal> IncomeBalances(WorkspaceObject e)
        {
            var values = new Dictionary<string, decimal>();
            if (e.TypeId == "Shop")
                values["Gold"] = e.Production.CollectedCurrency(GameCurrency.Gold);
            if (e.TypeId == "RequisitionOffice")
                values["Requisition"] = e.Production.CollectedCurrency(GameCurrency.Requisition);
            if (e.TypeId == "Disenchanter")
                values["Mana"] = e.Production.CollectedCurrency(GameCurrency.Mana);
            if (e.TypeId == "Restaurant")
            {
                values["Gold"] = e.Production.CollectedCurrency(GameCurrency.Gold);
                values["Crown"] = e.Production.CollectedCurrency(GameCurrency.Crown);
            }

            return values;
        }

        static CostTrace Inputs(IEnumerable<GameInventoryItem> items)
        {
            var result = new CostTrace();
            foreach (var item in items)
                result.Merge(Trace(item));
            return result;
        }

        internal static void Install(List<IDisposable> hooks)
        {
            hooks.Add(FoundationApi.ExecutionHooks.Subscribe(LedgerPlugin.Id, GameExecutionPoints.BuildingActivity, Enter, (call, state) => Exit(call.Exception, state as global::KingdomLedger.Scope)));
            hooks.Add(FoundationApi.GameHooks.Subscribe(LedgerPlugin.Id, GameHookPoints.PortConsumed, after: call => Consumed((WorkspacePort)call.Subject, call.Result<GameInventoryItem>())));
            hooks.Add(FoundationApi.GameHooks.Subscribe(LedgerPlugin.Id, GameHookPoints.CurrencySpent, after: call => Spent(call.Argument<GameCurrency>(0), call.Argument<long>(1), call.Result<bool>())));
            hooks.Add(FoundationApi.GameHooks.Subscribe(LedgerPlugin.Id, GameHookPoints.CurrencyAdded, after: call => Earned(call.Argument<GameCurrency>(0), call.Argument<long>(1), call.Argument<bool>(2))));
        }

        internal static bool IsIncomeReceiver(WorkspaceObject e) => e != null && new[]
        {
            "Shop",
            "LuxuryShop",
            "RequisitionOffice",
            "Disenchanter",
            "Restaurant",
            "MercenaryGuild"
        }.Contains(e.TypeId);
        // Requisition contracts can consume several items before minting a reward.
        // Retain only their outstanding value and weak ledger receipts, never items.
        static ConditionalWeakTable<WorkspaceObject, global::KingdomLedger.RequisitionPool> requisitions = new ConditionalWeakTable<WorkspaceObject, global::KingdomLedger.RequisitionPool>();
        static void ResetIncome() => requisitions = new ConditionalWeakTable<WorkspaceObject, global::KingdomLedger.RequisitionPool>();
        static ConditionalWeakTable<GameInventoryItem, CostTrace> traces = new ConditionalWeakTable<GameInventoryItem, CostTrace>();
        static ConditionalWeakTable<GameCraftingInput, global::KingdomLedger.Batch> batches = new ConditionalWeakTable<GameCraftingInput, global::KingdomLedger.Batch>();
        static ConditionalWeakTable<WorkspaceObject, global::KingdomLedger.OutfitBatch> outfits = new ConditionalWeakTable<WorkspaceObject, global::KingdomLedger.OutfitBatch>();
        static ConditionalWeakTable<WorkspaceObject, Queue<CostTrace>> queued = new ConditionalWeakTable<WorkspaceObject, Queue<CostTrace>>();
        static ConditionalWeakTable<GameInventoryItem, global::KingdomLedger.Tickets> tickets = new ConditionalWeakTable<GameInventoryItem, global::KingdomLedger.Tickets>();
        [ThreadStatic]
        static global::KingdomLedger.Scope current;
        static GameKingdom player;
        static readonly HashSet<string> Routers = new HashSet<string>
        {
            "Collector",
            "Distributor",
            "Manifold",
            "Filter",
            "TrackSwitch",
            "Stockpile",
            "Inspector",
            "SingleButton",
            "Timer",
            "Accountant",
            "Appraiser",
            "Portal"
        };
        internal static bool IsRoutingStage(string stage) => Routers.Contains(stage.Split(new[] { " / " }, StringSplitOptions.None)[0]) || stage == Ledger.TypeId;
        [Obsolete("Compatibility helper last used with game build 1.0.14; remove in the next Ledger release.")]
        static T Native<T>(object owner, string name)
        {
            var state = ((WorkspaceObject)owner).Production;
            object value = name == "_passiveTimer" ? (object)state.PassiveTimer : name == "_valuePerReq" ? state.ValuePerRequisition : name == "_passiveGain" ? state.PassiveGain : throw new ArgumentException("Unknown compatibility field.", nameof(name));
            return (T)Convert.ChangeType(value, typeof(T));
        }

        [Obsolete("Compatibility helper last used with game build 1.0.14; remove in the next Ledger release.")]
        static MethodInfo Patch(string name) => typeof(LedgerObserver).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        static int QueueCount(WorkspaceObject e) => e.Production.QueuedCount;
        internal static void Reset()
        {
            player = FoundationApi.Kingdom;
            traces = new ConditionalWeakTable<GameInventoryItem, CostTrace>();
            batches = new ConditionalWeakTable<GameCraftingInput, global::KingdomLedger.Batch>();
            outfits = new ConditionalWeakTable<WorkspaceObject, global::KingdomLedger.OutfitBatch>();
            queued = new ConditionalWeakTable<WorkspaceObject, Queue<CostTrace>>();
            tickets = new ConditionalWeakTable<GameInventoryItem, global::KingdomLedger.Tickets>();
            current = null;
            ResetIncome();
        }

        internal static void SetTrace(GameInventoryItem item, CostTrace trace)
        {
            traces.Remove(item);
            traces.Add(item, trace);
        }

        static void Spent(GameCurrency __0, long __1, bool __result)
        {
            if (current != null && __result && __1 > 0)
                CostTrace.Add(current.Spent, __0.ToString(), __1);
        }

        internal static string Stage(WorkspaceObject element)
        {
            var state = element.Production;
            if (state.HasRecipe)
                return element.TypeId + " / " + FoundationApi.GameInfo.Translate(state.RecipeDisplayName);
            if (state.IsResource && state.Resource != null)
                return element.TypeId + " / " + FoundationApi.GameInfo.Translate(state.Resource.PreviewDisplayName);
            if (element.TypeId == "Forest" && state.SelectedForestResource != null)
                return "Forest / " + FoundationApi.GameInfo.Translate(state.SelectedForestResource.PreviewDisplayName);
            if (IsIncomeReceiver(element))
                return element.TypeId + " / " + FoundationApi.GameInfo.Translate(element.Label);
            return element.TypeId;
        }

        internal static void Ticket(GameInventoryItem item, LedgerHistory ledger, CostTrace trace)
        {
            global::KingdomLedger.Tickets t = tickets.GetOrCreateValue(item);
            // Chained Ledgers are each credited once when the actual destination sells.
            if (!t.Values.Any(v => v.ledger.TryGetTarget(out var h) && ReferenceEquals(h, ledger)))
                t.Values.Add((new WeakReference<LedgerHistory>(ledger), trace.Copy()));
        }

        internal static CostTrace Trace(GameInventoryItem item) => traces.TryGetValue(item, out var t) ? t : new CostTrace
        {
            Complete = false
        };
    }
}
