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
    internal static class WorkspaceView
    {
        internal static void Postfix(Component __instance, WorkspaceObject element)
        {
            if (!(element.Behaviour is Ledger) || __instance.GetComponent<LedgerWorld>())
                return;
            foreach (var graphic in new[]
            {
                GameBuildingGraphic.Icon,
                GameBuildingGraphic.Progress,
                GameBuildingGraphic.Productivity,
                GameBuildingGraphic.OutputFull,
                GameBuildingGraphic.CraftButton,
                GameBuildingGraphic.UpgradeSlots
            }

            )
            {
                var component = FoundationApi.WorkspaceVisuals.BuildingGraphic(__instance, graphic);
                if (component)
                    component.gameObject.SetActive(false);
            }

            __instance.gameObject.AddComponent<LedgerWorld>();
        }
    }
}
