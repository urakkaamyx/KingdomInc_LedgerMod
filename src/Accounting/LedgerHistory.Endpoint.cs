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
        internal void Endpoint(string stage,IDictionary<string,decimal> spend,decimal quantity=1)
        {
         Current().Value.Costs.Stage(stage,new Dictionary<string,decimal>(spend),quantity);
        }
    }
}
