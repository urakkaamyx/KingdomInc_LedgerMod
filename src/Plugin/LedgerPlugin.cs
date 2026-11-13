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
    [BepInPlugin(Id, "Ledger", Version)]
    [BepInDependency("urakka.kingdominc.foundation", "0.14.0")]
    public sealed class LedgerPlugin : BaseUnityPlugin
    {
        void Awake()
        {
            instance = this;
            try
            {
                context = FoundationApi.ForMod(Id);
                RegisterFoundation();
                RegisterHooks();
                LedgerObserver.Install(hooks);
            }
            catch
            {
                context?.Dispose();
                ReleaseHooks();
                throw;
            }
        }

        public const string Id = "urakka.kingdominc.ledgermod", Version = "0.5.2";
        FoundationModContext context;
        readonly List<IDisposable> hooks = new List<IDisposable>();
        static LedgerPlugin instance;
        ITechNode research;
        IWorkspaceNodeType workspace;
        internal static ITechNode ResearchRegistration => instance?.research;
        internal static IWorkspaceNodeType WorkspaceRegistration => instance?.workspace;

        internal static void Warn(string message) => instance?.Logger.LogWarning(message);
        void ReleaseHooks()
        {
            foreach (var hook in hooks)
                hook.Dispose();
            hooks.Clear();
        }

        void RegisterHooks()
        {
            hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.BuildingBound, after: call => global::KingdomLedger.WorkspaceView.Postfix(call.View, call.Argument<WorkspaceObject>(0))));
            hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.PortBound, after: call => global::KingdomLedger.PortColors.Postfix(call.View, call.Argument<WorkspacePort>(0))));
            hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.BuildingDeserialize, after: call => global::KingdomLedger.LoadedLayout.Postfix(call.Result<WorkspaceObject>())));
            hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.BuildingClearPorts, before: call => global::KingdomLedger.Clearing.Prefix((WorkspaceObject)call.Subject)));
            hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.PortAcceptance, before: call =>
            {
                bool result = false;
                if (!global::KingdomLedger.Acceptance.Prefix((WorkspacePort)call.Subject, call.Argument<GameInventoryItem>(0), ref result))
                {
                    call.SetResult(result);
                    call.Cancel();
                }
            }));
            hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.PortDelivery, before: call =>
            {
                bool result = false;
                if (!global::KingdomLedger.Delivery.Prefix((WorkspacePort)call.Subject, call.Argument<GameInventoryItem>(0), ref result))
                {
                    call.SetResult(result);
                    call.Cancel();
                }
            }));
        }

        // Bind the ledger display to the native workspace node view.
        [Obsolete("Compatibility helper last used with game build 1.0.14; remove in the next Ledger release.")]
        internal static T Field<T>(Component world, string name)
            where T : class => FoundationApi.WorkspaceVisuals.BuildingGraphic(world, (GameBuildingGraphic)Enum.Parse(typeof(GameBuildingGraphic), name.TrimStart('_'), true)) as T;
        void OnDestroy()
        {
            context?.Dispose();
            ReleaseHooks();
            if (instance == this)
                instance = null;
        }

        // Native deserialization restores IO positions after DataFromJson. Normalize legacy
        // 40-unit spacing only after that step, keeping saved pipe/lane identities intact.
        // ClearIO normally writes directly into adjacent buffers, bypassing InputItem.
        // Keep native deletion/recipe-change behavior from placing items inside Ledger.
        internal static void PrepareClear(WorkspaceObject element)
        {
            foreach (var input in element.Inputs)
            {
                var origin = input.ConnectedPipe?.ConnectedOutput;
                if (!(origin?.Parent?.Behaviour is Ledger))
                    continue;
                var seen = new HashSet<Ledger>();
                while (origin?.Parent?.Behaviour is Ledger previous)
                {
                    if (!seen.Add(previous))
                    {
                        origin = null;
                        break;
                    }

                    origin = previous.Inputs[origin.ParentIndex].ConnectedPipe?.ConnectedOutput;
                }

                // Return queued arrivals to the real producer, never a Ledger output.
                if (origin != null)
                    for (int i = input.BufferedItems.Count - 1; i >= 0; i--)
                        origin.PrependBuffer(input.BufferedItems[i]);
                input.ClearBuffer();
            }

            foreach (var output in element.Outputs)
            {
                var target = output.ConnectedPipe?.ConnectedInput;
                if (!(target?.Parent?.Behaviour is Ledger))
                    continue;
                while (output.HasBuffer && target.CanAcceptItem(output.AvailableItem) && target.InputItem(output.AvailableItem))
                    output.ConsumeItem();
            }
        }

        void RegisterFoundation()
        {
            var accountant = FoundationApi.ResearchAccess.Find("accountant") ?? throw new InvalidOperationException("Ledger requires native Accountant research.");
            // Keep legacy IDs so existing unlocked research, node types and pipe lanes load unchanged.
            research = context.Register(new TechNodeDefinition(Ledger.TechId, "Ledger", Id, "Ledger", Vector2.zero, "Track items, full production costs and actual currency profit. Zero storage; configure 1â€“5 input/output pairs and second/minute/hour windows with the gear.", research: TechNodeOptions.Create(costTier: accountant.CostTier, costMultiplier: .5, requirements: new[] { accountant.Identifier }, iconName: "Icons_149")));
            // Preserve the native Accountant currency and price used by existing saves.
            var native = FoundationApi.ResearchAccess.Find(research.Id);
            native.SetCostCurrencies(accountant.CostCurrencies);
            native.ResetCost();
            workspace = context.Register(new FrameworkWorkspaceNodeDefinition(Ledger.TypeId, "Ledger", () => new Ledger(), Ledger.TechId, size: new Vector2Int(Ledger.Width, Ledger.Height), description: Ledger.NodeDescription, iconName: "Icons_149", costMultiplier: .25));
        }

        void Start()
        {
            Logger.LogInfo("Ledger ready through Foundation: native Mods research, registered workspace and zero-storage accounting.");
        }
    }
}
