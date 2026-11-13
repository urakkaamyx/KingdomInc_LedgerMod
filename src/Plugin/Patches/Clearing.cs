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
    internal static class Clearing
    {
        internal static void Prefix(WorkspaceObject __instance) => global::KingdomLedger.LedgerPlugin.PrepareClear(__instance);
    }
}
