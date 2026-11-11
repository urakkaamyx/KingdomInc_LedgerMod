using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomLedger
{
    internal sealed partial class CostTrace
    {
        internal bool Complete=true;
        internal readonly Dictionary<string,decimal> Money=new Dictionary<string,decimal>(), Rewards=new Dictionary<string,decimal>(), Resources=new Dictionary<string,decimal>(), Stages=new Dictionary<string,decimal>();
        internal readonly Dictionary<string,Dictionary<string,decimal>> StageMoney=new Dictionary<string,Dictionary<string,decimal>>();
        internal static void Add(Dictionary<string,decimal> d,string key,decimal amount){d.TryGetValue(key,out var old);d[key]=old+amount;}
    }
}
