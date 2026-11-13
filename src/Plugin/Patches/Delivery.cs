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
    internal static class Delivery
    {
        internal static bool Prefix(WorkspacePort __instance, GameInventoryItem item, ref bool __result)
        {
            if (!(__instance.Parent?.Behaviour is Ledger ledger))
                return true;
            __result = ledger.Forward(__instance.ParentIndex, item);
            return false;
        }
    }
}
