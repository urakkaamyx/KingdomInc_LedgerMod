using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomLedger
{
    internal sealed partial class LedgerHistory
    {
        // One aggregate per active second; no item references or unbounded event log.





        readonly Queue<LedgerBucket> buckets=new Queue<LedgerBucket>();
        LedgerBucket latest;
        internal static Func<double> Clock=()=>UnityEngine.Time.unscaledTimeAsDouble;
        LedgerBucket Current()
        {
         long second=(long)Math.Floor(Clock());
         while(buckets.Count>0&&buckets.Peek().Second<second-3600)buckets.Dequeue();
         if(buckets.Count==0||latest.Second!=second){latest=new LedgerBucket{Second=second};buckets.Enqueue(latest);}
         return latest;
        }
    }
}
