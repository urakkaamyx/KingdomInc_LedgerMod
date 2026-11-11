using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;

namespace KingdomLedger
{
    internal sealed partial class LedgerChain
    {
        internal readonly List<Entry> Stages=new List<Entry>();
        internal readonly HashSet<string> Resources=new HashSet<string>();
        internal readonly HashSet<FlowElement> Elements=new HashSet<FlowElement>();
        internal bool HasOperatingCosts;
        static bool Paid(CurrencyCollection cost)=>cost!=null&&cost.GetCurrency().Any(p=>p.amt>0);
    }
}
