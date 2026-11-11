using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using LightJson;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomLedger
{
    public sealed partial class Ledger : WorkspaceNode
    {
        public Ledger():base(TypeId)
        {
         // Preserve legacy construction currencies; Foundation metadata supplies other defaults.
         CostCurrency=CurrencyCollection.GoldMedal;
         SetPairs(1);
        }
    }
}
