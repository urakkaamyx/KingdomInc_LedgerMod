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
        internal const int ContentWidth=Ledger.Width-62, CurrencyWidth=(ContentWidth-12)/2, CurrencyStride=CurrencyWidth+12;
        Ledger ledger;LedgerUi ui;TMP_Text period,throughput,quantity;RectTransform moneyStrip,body;

        RectTransform timerFill;TooltipSource timerTip;int timerSecond=-1,timerPeriod;

        readonly List<NodeCurrencyRow> moneyRows=new List<NodeCurrencyRow>();




        readonly List<ItemRow> itemRows=new List<ItemRow>();
        float refresh;bool initialized,openWhenReady;

        internal LedgerPopup Settings{get;private set;}

        internal Ledger Element=>ledger;
        internal const int HeaderInset=20;

        void Start(){TryInitialize();}
    }
}
