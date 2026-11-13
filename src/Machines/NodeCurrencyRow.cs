using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed class NodeCurrencyRow
    {
        internal RectTransform Root;
        internal TMP_Text Net;
        internal Image Icon;
        internal FoundationTooltipSource Tip;
    }
}
