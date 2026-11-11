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
        public void SetPeriod(int seconds) { int selected=seconds==1||seconds==3600?seconds:60;if(selected!=Period){Period=selected;periodStarted=LedgerHistory.Clock();} }
    }
}
