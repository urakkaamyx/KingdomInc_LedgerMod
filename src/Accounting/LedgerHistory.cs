using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KingdomLedger
{
    internal sealed class LedgerHistory
    {
        internal static string Amount(string currency, decimal amount, bool signed = false) => currency == "Gold" ? (signed && amount > 0 ? "+" : "") + Gold(amount) : (signed && amount > 0 ? "+" : "") + Number(amount);
        // One aggregate per active second; no item references or unbounded event log.
        readonly Queue<global::KingdomLedger.LedgerBucket> buckets = new Queue<global::KingdomLedger.LedgerBucket>();
        global::KingdomLedger.LedgerBucket latest;
        internal static Func<double> Clock = () => UnityEngine.Time.unscaledTimeAsDouble;
        global::KingdomLedger.LedgerBucket Current()
        {
            long second = (long)Math.Floor(Clock());
            while (buckets.Count > 0 && buckets.Peek().Second < second - 3600)
                buckets.Dequeue();
            if (buckets.Count == 0 || latest.Second != second)
            {
                latest = new global::KingdomLedger.LedgerBucket
                {
                    Second = second
                };
                buckets.Enqueue(latest);
            }

            return latest;
        }

        internal static string CurrencyName(string currency) => Enum.TryParse(currency, out GameCurrency value) ? FoundationApi.GameInfo.Translate(value.GetDisplayName()) : currency;
        internal void Endpoint(string stage, IDictionary<string, decimal> spend, decimal quantity = 1)
        {
            Current().Value.Costs.Stage(stage, new Dictionary<string, decimal>(spend), quantity);
        }

        internal static string Gold(decimal amount)
        {
            var value = Math.Abs(amount);
            decimal gold = Math.Floor(value / 10000m), silver = Math.Floor(value % 10000m / 100m), copper = value % 100m;
            return (amount < 0 ? "-" : "") + Number(gold) + "g " + Number(silver) + "s " + Number(copper) + "c";
        }

        internal static string ItemName(string id) => FoundationApi.GameInfo.Translate(FoundationApi.ItemAccess.Find(id)?.PreviewDisplayName ?? id);
        internal static string Money(IEnumerable<KeyValuePair<string, decimal>> values)
        {
            var text = values.Where(p => p.Value != 0).OrderBy(p => p.Key).Select(p => p.Key == "Gold" ? Gold(p.Value) : Number(p.Value) + " " + CurrencyName(p.Key));
            return string.Join(" Â· ", text.DefaultIfEmpty("0g 0s 0c"));
        }

        internal static string Net(Dictionary<string, decimal> revenue, Dictionary<string, decimal> cost)
        {
            var net = new Dictionary<string, decimal>(revenue);
            foreach (var p in cost)
                CostTrace.Add(net, p.Key, -p.Value);
            return Money(net);
        }

        internal static string Number(decimal n) => n.ToString("#,0.##", CultureInfo.InvariantCulture);
        internal void Pass(GameInventoryItem item, int lane, WorkspaceObject destination)
        {
            var value = Current().Value;
            CostTrace.Add(value.Items, item.Type.Identifier, 1);
            var trace = LedgerObserver.Trace(item);
            value.Costs.Merge(trace);
            if (!trace.Complete)
                value.UnknownPassed++;
            foreach (var reward in trace.Rewards)
                CostTrace.Add(value.Revenue, reward.Key, reward.Value);
            LedgerObserver.Ticket(item, this, trace);
        }

        internal void Sale(CostTrace trace, IDictionary<string, decimal> revenue, bool first = true)
        {
            var value = Current().Value;
            if (first)
                value.Sold++;
            value.SaleSeen = true;
            if (!trace.Complete)
                value.UnknownSold++;
            if (first)
                foreach (var p in trace.Money)
                    CostTrace.Add(value.SoldCosts, p.Key, p.Value);
            foreach (var p in revenue)
                CostTrace.Add(value.Revenue, p.Key, p.Value);
        }

        internal LedgerSummary Summary(int seconds)
        {
            Current();
            long first = (long)Math.Floor(Clock()) - seconds + 1;
            var result = new LedgerSummary();
            foreach (global::KingdomLedger.LedgerBucket bucket in buckets.Where(b => b.Second >= first))
            {
                var v = bucket.Value;
                foreach (var p in v.Items)
                    CostTrace.Add(result.Items, p.Key, p.Value);
                foreach (var p in v.Revenue)
                    CostTrace.Add(result.Revenue, p.Key, p.Value);
                foreach (var p in v.SoldCosts)
                    CostTrace.Add(result.SoldCosts, p.Key, p.Value);
                result.Costs.Merge(v.Costs);
                result.Sold += v.Sold;
                result.UnknownPassed += v.UnknownPassed;
                result.UnknownSold += v.UnknownSold;
                result.SaleSeen |= v.SaleSeen;
            }

            return result;
        }
    }
}
