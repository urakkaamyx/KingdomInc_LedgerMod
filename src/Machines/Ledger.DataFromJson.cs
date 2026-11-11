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
        protected override void DataFromJson(JsonObject data,FlowDiagram parent){SetPairs(data["LedgerPairs"].IsInteger?Math.Max(1,Math.Min(5,data["LedgerPairs"].AsInteger)):1);SetPeriod(data["LedgerPeriod"].AsInteger);}
    }
}
