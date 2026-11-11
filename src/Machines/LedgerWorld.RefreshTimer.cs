using System;

using System.Linq;

using System.Collections.Generic;

using Assets.Source.Player;

using Assets.Behaviour.Flow;

using Assets.Behaviour.UI;

using Assets.Source.Items;

using Behaviour.UI.Tooltip;

using TMPro;

using UnityEngine;

using UnityEngine.UI;

namespace KingdomLedger
{
    public sealed partial class LedgerWorld:MonoBehaviour
    {
        internal void RefreshTimer()
        {
         if(!initialized)return;
         timerFill.sizeDelta=new Vector2(ContentWidth*ledger.PeriodProgress,4);
         int second=(int)ledger.PeriodElapsed;
         if(second==timerSecond&&timerPeriod==ledger.Period)return;
         timerSecond=second;timerPeriod=ledger.Period;
         timerTip.SetTooltip("Reporting timer",second+" / "+ledger.Period+" seconds\nRepeats for the selected window. Totals continue rolling; the bar does not clear them.");
        }
    }
}
