using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System.Collections.Generic;
using System.Linq;

namespace KingdomLedger
{
    internal sealed class LedgerChain
    {
        internal readonly List<global::KingdomLedger.LedgerChainEntry> Stages = new List<global::KingdomLedger.LedgerChainEntry>();
        internal readonly HashSet<string> Resources = new HashSet<string>();
        internal readonly HashSet<WorkspaceObject> Elements = new HashSet<WorkspaceObject>();
        internal bool HasOperatingCosts;
        static bool Paid(GameMoney cost) => cost != null && cost.Entries.Any(p => p.Amount > 0);
        internal static bool HasIncomeDestination(Ledger ledger)
        {
            var pending = new Stack<WorkspaceObject>();
            var seen = new HashSet<WorkspaceObject>();
            pending.Push(ledger);
            while (pending.Count > 0)
            {
                var element = pending.Pop();
                if (!seen.Add(element))
                    continue;
                if (LedgerObserver.IsIncomeReceiver(element))
                    return true;
                foreach (var output in element.Outputs)
                {
                    var pipe = output.ConnectedPipe;
                    if (pipe != null && pipe.ConnectedOutput == output && pipe.ConnectedInput != null && pipe.ConnectedInput.ConnectedPipe == pipe)
                        pending.Push(pipe.ConnectedInput.Parent);
                }
            }

            return false;
        }

        internal static LedgerChain Read(Ledger ledger)
        {
            var result = new LedgerChain();
            var grouped = new Dictionary<string, global::KingdomLedger.LedgerChainEntry>();
            var stack = new Stack<(WorkspaceObject element, bool expanded)>();
            stack.Push((ledger, false));
            // Iterative postorder handles long chains, split branches, shared sources,
            // and cycles without recursion or counting a connected building twice.
            while (stack.Count > 0)
            {
                var step = stack.Pop();
                var element = step.element;
                if (!step.expanded)
                {
                    if (!result.Elements.Add(element))
                        continue;
                    stack.Push((element, true));
                    for (int i = element.Inputs.Count - 1; i >= 0; i--)
                    {
                        var input = element.Inputs[i];
                        var pipe = input.ConnectedPipe;
                        if (pipe != null && pipe.ConnectedInput == input && pipe.ConnectedOutput != null && pipe.ConnectedOutput.ConnectedPipe == pipe)
                            stack.Push((pipe.ConnectedOutput.Parent, false));
                    }

                    continue;
                }

                if (ReferenceEquals(element, ledger.Model))
                    continue;
                if (Paid(element.Production.ResourceCost) || Paid(element.Production.CraftingCost))
                    result.HasOperatingCosts = true;
                string stage = LedgerObserver.Stage(element);
                if (!grouped.TryGetValue(stage, out global::KingdomLedger.LedgerChainEntry entry))
                {
                    entry = new global::KingdomLedger.LedgerChainEntry
                    {
                        Stage = stage,
                        Element = element
                    };
                    grouped.Add(stage, entry);
                    result.Stages.Add(entry);
                }

                entry.Buildings++;
                if (element.Production.IsResource && element.Production.Resource != null)
                    result.Resources.Add(element.Production.Resource.Identifier);
                if (element.TypeId == "Forest" && element.Production.SelectedForestResource != null)
                    result.Resources.Add(element.Production.SelectedForestResource.Identifier);
            }

            return result;
        }
    }
}
