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
        internal bool ShowFinancials(LedgerSummary summary)=>HasIncomeOutput||summary.HasMoneyActivity||LedgerChain.Read(this).HasOperatingCosts;
    }
}
