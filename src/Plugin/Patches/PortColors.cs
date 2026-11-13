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
    internal static class PortColors
    {
        internal static void Postfix(Component __instance, WorkspacePort io)
        {
            if (!(io.Parent?.Behaviour is Ledger))
                return;
            var image = FoundationApi.WorkspaceVisuals.PortGraphic(__instance, GamePortGraphic.Sprite) as Image;
            image.sprite = FoundationApi.WorkspaceVisuals.PortSprite(__instance, io.IsInput, 2);
        }
    }
}
