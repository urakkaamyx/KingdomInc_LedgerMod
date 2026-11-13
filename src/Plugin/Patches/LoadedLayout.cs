using KingdomFoundation.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KingdomFoundation.Api;

namespace KingdomLedger
{
    internal static class LoadedLayout
    {
        internal static void Postfix(WorkspaceObject __result)
        {
            if (__result?.Behaviour is Ledger ledger)
                ledger.SetPairs(ledger.Inputs.Count);
        }
    }
}
