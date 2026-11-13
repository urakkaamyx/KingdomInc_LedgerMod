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
    internal sealed class ItemRow
    {
        internal RectTransform Root;
        internal TMP_Text Name, Amount;
        internal Image Icon, Shade;
    }
}
