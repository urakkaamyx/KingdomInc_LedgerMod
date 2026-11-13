using KingdomFoundation.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomLedger
{
    public sealed class Ledger : FrameworkWorkspaceNode
    {
        protected override void CopySettingsTo(FrameworkWorkspaceNode copy)
        {
            var l = (Ledger)copy;
            l.SetPairs(Inputs.Count);
            l.SetPeriod(Period);
        }

        public const string TypeId = "Ledger", TechId = "ledger_mod";
        public const int Grid = 64, Width = 6 * Grid, Height = 3 * Grid;
        // Header band: full-width native backdrop behind the column labels, timer directly below,
        // then a small gap before the item rows. Paired ports begin on the first grid row (row 0)
        // to align with adjacent nodes.
        internal const int HeaderTop = 30, HeaderHeight = 30, TimerTop = 61, ItemTop = 72, RowHeight = 24;
        internal const string NodeDescription = "A zero-storage production ledger. Items enter only when their matching output can deliver immediately. Gear: reporting period, paired ports and full chain costs. Actual shop sales and currency-converting destinations determine income.";
        public int Period { get; private set; } = 60;

        double periodStarted = LedgerHistory.Clock();
        internal double PeriodElapsed => Math.Max(0, LedgerHistory.Clock() - periodStarted) % Period;
        internal float PeriodProgress => (float)(PeriodElapsed / Period);

        internal readonly LedgerHistory History = new LedgerHistory();
        internal bool HasIncomeOutput => LedgerChain.HasIncomeDestination(this);

        internal bool HasShopOutput
        {
            get
            {
                for (int i = 0; i < Outputs.Count; i++)
                {
                    Resolve(this, i, null, out var receiver, out _, out _);
                    // Full shops still count as connected; admission and display mode are separate.
                    if (receiver != null && IsShop(receiver.Parent))
                        return true;
                }

                return false;
            }
        }

        internal static int DisplayHeight(int pairs, int items, int currencies)
        {
            int requiredHeight = Math.Max(Height, pairs * Grid);
            if (items == 0 && currencies == 0)
                return requiredHeight;
            int itemBottom = ItemTop + Math.Max(1, items) * RowHeight;
            int contentBottom = currencies > 0 ? itemBottom + 4 + 38 + Math.Max(0, (currencies + 1) / 2 - 1) * 20 + 6 : itemBottom + 24 + 8;
            return Math.Max(requiredHeight, ((contentBottom + Grid - 1) / Grid) * Grid);
        }

        protected override void DataFromJson(JsonObject data, GameWorkspace parent)
        {
            SetPairs(data["LedgerPairs"].IsInteger ? Math.Max(1, Math.Min(5, data["LedgerPairs"].AsInteger)) : 1);
            SetPeriod(data["LedgerPeriod"].AsInteger);
        }

        protected override void DataToJson(JsonObject data)
        {
            data["LedgerPairs"] = Inputs.Count;
            data["LedgerPeriod"] = Period;
        }

        internal string Destination(int lane)
        {
            Resolve(this, lane, null, out var receiver, out _, out var reason);
            if (receiver == null)
                return reason ?? "Disconnected";
            return (LedgerObserver.IsIncomeReceiver(receiver.Parent) ? "Income: " : "Destination: ") + receiver.Parent.TypeId + (reason == null ? "" : " · " + reason);
        }

        internal bool Forward(int lane, GameInventoryItem item)
        {
            if (item == null || !Resolve(this, lane, item, out var receiver, out var path, out _))
                return false;
            if (!receiver.InputItem(item))
                return false;
            // The native incoming pipe consumes the parent's item only after true is returned.
            // Ledger never adds the item to any input/output buffer.
            try
            {
                foreach (var entry in path)
                    entry.ledger.History.Pass(item, entry.lane, receiver.Parent);
            }
            catch (Exception e)
            {
                LedgerPlugin.Warn("Transfer completed; reporting failed: " + e.Message);
            }

            return true;
        }

        public override bool HandleReopenBuyMenu()
        {
            if (World)
                World.GetComponent<LedgerWorld>()?.OpenSettings();
            return false;
        }

        internal static bool IsShop(WorkspaceObject e) => e != null && (e.TypeId == "Shop" || e.TypeId == "LuxuryShop");
        public Ledger() : base(TypeId)
        {
            // Preserve legacy construction currencies; Foundation metadata supplies other defaults.
            CostCurrencies = new[]
            {
                GameCurrency.Gold,
                GameCurrency.Medal
            };
            SetPairs(1);
        }

        internal static bool Resolve(Ledger start, int lane, GameInventoryItem item, out WorkspacePort receiver, out List<(Ledger ledger, int lane)> path, out string reason)
        {
            receiver = null;
            path = new List<(Ledger, int)>();
            var seen = new HashSet<Ledger>();
            var ledger = start;
            while (ledger != null)
            {
                if (!seen.Add(ledger) || seen.Count > 128)
                {
                    reason = "Ledger loop blocked";
                    return false;
                }

                if (lane < 0 || lane >= ledger.Outputs.Count)
                {
                    reason = "Invalid output";
                    return false;
                }

                path.Add((ledger, lane));
                var output = ledger.Outputs[lane];
                var pipe = output.ConnectedPipe;
                if (pipe == null || pipe.ConnectedOutput != output || pipe.ConnectedInput == null || pipe.ConnectedInput.ConnectedPipe != pipe)
                {
                    reason = "Output " + (lane + 1) + ": disconnected";
                    return false;
                }

                receiver = pipe.ConnectedInput;
                if (receiver.Parent?.Behaviour is Ledger next)
                {
                    ledger = next;
                    lane = receiver.ParentIndex;
                    continue;
                }

                if (!receiver.CanBuffer)
                {
                    reason = "Output " + (lane + 1) + ": full";
                    return false;
                }

                if (receiver.Parent.Production.IsStockpile && receiver.Parent.Production.StoredItemCount + receiver.Parent.Inputs.Sum(i => i.BufferedItems.Count) >= receiver.Parent.Production.StorageCapacity)
                {
                    reason = "Stockpile full";
                    return false;
                }

                if (receiver.Parent.TypeId == "Shop" && receiver.Parent.Production.CollectedCurrency(GameCurrency.Gold) >= receiver.Parent.Production.MaximumGold)
                {
                    reason = "Shop till full";
                    return false;
                }

                if (receiver.Parent.TypeId == "RequisitionOffice" && receiver.Parent.Production.CollectedCurrency(GameCurrency.Requisition) >= receiver.Parent.Production.MaximumRequisition)
                {
                    reason = "Requisition Office full";
                    return false;
                }

                if (item != null && !receiver.CanAcceptItem(item))
                {
                    reason = "Item rejected by destination";
                    return false;
                }

                reason = null;
                return true;
            }

            reason = "No destination";
            return false;
        }

        public bool SetPairs(int count)
        {
            if (count < 1 || count > 5)
                return false;
            for (int i = count; i < Inputs.Count; i++)
                if (Inputs[i].ConnectedPipe != null || Outputs[i].ConnectedPipe != null)
                    return false;
            while (Inputs.Count < count)
            {
                int i = Inputs.Count;
                AddInput(0, -10 - Grid * i, item => true);
                AddOutput(0, 10 + Grid * i);
            }

            if (Inputs.Count > count)
            {
                TrimInputs(count);
                TrimOutputs(count);
            }

            // Native ports begin 10 units into row 0, one grid row apart per lane.
            // Rewrite existing positions too, including ports loaded from older saves.
            for (int i = 0; i < count; i++)
            {
                Inputs[i].Position = -10 - Grid * i;
                Outputs[i].Position = 10 + Grid * i;
            }

            Size = new Vector2Int(Width, DisplayHeight(count, 0, 0));
            if (World)
            {
                FoundationApi.WorkspaceVisuals.RefreshPorts(World);
                World.GetComponent<LedgerWorld>()?.Refresh();
            }

            return true;
        }

        public void SetPeriod(int seconds)
        {
            int selected = seconds == 1 || seconds == 3600 ? seconds : 60;
            if (selected != Period)
            {
                Period = selected;
                periodStarted = LedgerHistory.Clock();
            }
        }

        internal bool ShowFinancials(LedgerSummary summary) => HasIncomeOutput || summary.HasMoneyActivity || LedgerChain.Read(this).HasOperatingCosts;
        public override void Update(float delta)
        {
        }
    }
}
