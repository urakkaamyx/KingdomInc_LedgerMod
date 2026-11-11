using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Behaviour.Flow;
using Assets.Behaviour.Flow.Elements;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;
using Behaviour.UI.Tooltip;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KingdomFoundation.Api;

namespace KingdomLedger
{
     [BepInPlugin(Id,"Ledger",Version)]
     [BepInDependency("urakka.kingdominc.foundation", "0.11.0")]
    public sealed partial class LedgerPlugin:BaseUnityPlugin
    {
        public const string Id="urakka.kingdominc.ledgermod",Version="0.5.1";
        FoundationModContext context;
        Harmony harmony;
        static LedgerPlugin instance;
        ITechNode research;
        IWorkspaceNodeType workspace;
        internal static ITechNode ResearchRegistration=>instance?.research;
        internal static IWorkspaceNodeType WorkspaceRegistration=>instance?.workspace;

        internal static void Warn(string message)=>instance?.Logger.LogWarning(message);
    }
}
