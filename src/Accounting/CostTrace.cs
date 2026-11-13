using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KingdomLedger
{
    internal sealed class CostTrace
    {
        internal CostTrace Copy(decimal scale = 1)
        {
            var result = new CostTrace();
            result.Merge(this, scale);
            return result;
        }

        internal bool Complete = true;
        internal readonly Dictionary<string, decimal> Money = new Dictionary<string, decimal>(), Rewards = new Dictionary<string, decimal>(), Resources = new Dictionary<string, decimal>(), Stages = new Dictionary<string, decimal>();
        internal readonly Dictionary<string, Dictionary<string, decimal>> StageMoney = new Dictionary<string, Dictionary<string, decimal>>();
        internal static void Add(Dictionary<string, decimal> d, string key, decimal amount)
        {
            d.TryGetValue(key, out var old);
            d[key] = old + amount;
        }

        internal void Merge(CostTrace t, decimal scale = 1)
        {
            if (t == null)
            {
                Complete = false;
                return;
            }

            Complete &= t.Complete;
            foreach (var p in t.Money)
                Add(Money, p.Key, p.Value * scale);
            foreach (var p in t.Rewards)
                Add(Rewards, p.Key, p.Value * scale);
            foreach (var p in t.Resources)
                Add(Resources, p.Key, p.Value * scale);
            foreach (var p in t.Stages)
                Add(Stages, p.Key, p.Value * scale);
            foreach (var stage in t.StageMoney)
            {
                if (!StageMoney.TryGetValue(stage.Key, out var values))
                    StageMoney[stage.Key] = values = new Dictionary<string, decimal>();
                foreach (var p in stage.Value)
                    Add(values, p.Key, p.Value * scale);
            }
        }

        internal void Stage(string name, Dictionary<string, decimal> spend, decimal quantity = 1)
        {
            Add(Stages, name, quantity);
            if (!StageMoney.TryGetValue(name, out var row))
                StageMoney[name] = row = new Dictionary<string, decimal>();
            foreach (var p in spend)
            {
                Add(Money, p.Key, p.Value);
                Add(row, p.Key, p.Value);
            }
        }
    }
}
