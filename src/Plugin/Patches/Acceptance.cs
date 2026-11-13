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
    internal static class Acceptance
    {
        internal static bool Prefix(WorkspacePort __instance, GameInventoryItem item, ref bool __result)
        {
            if (!(__instance.IsInput) || !(__instance.Parent?.Behaviour is Ledger ledger))
                return true;
            __result = item != null && Ledger.Resolve(ledger, __instance.ParentIndex, item, out _, out _, out _);
            return false;
        }
    }
}
