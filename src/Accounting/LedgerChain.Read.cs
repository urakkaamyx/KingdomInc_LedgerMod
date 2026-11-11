using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;

namespace KingdomLedger
{
    internal sealed partial class LedgerChain
    {
        internal static LedgerChain Read(Ledger ledger)
        {
         var result=new LedgerChain();var grouped=new Dictionary<string,Entry>();
         var stack=new Stack<(FlowElement element,bool expanded)>();stack.Push((ledger,false));
         // Iterative postorder handles long chains, split branches, shared sources,
         // and cycles without recursion or counting a connected building twice.
         while(stack.Count>0)
         {
          var step=stack.Pop();var element=step.element;
          if(!step.expanded)
          {
           if(!result.Elements.Add(element))continue;
           stack.Push((element,true));
           for(int i=element.Inputs.Count-1;i>=0;i--)
           {
            var input=element.Inputs[i];var pipe=input.ConnectedPipe;
            if(pipe!=null&&pipe.ConnectedInput==input&&pipe.ConnectedOutput!=null&&pipe.ConnectedOutput.ConnectedPipe==pipe)
             stack.Push((pipe.ConnectedOutput.Parent,false));
           }
           continue;
          }
          if(ReferenceEquals(element,ledger))continue;
          if(element is ResourceElement paidResource&&Paid(paidResource.GetResourceCost())||element is RecipeElement recipe&&Paid(recipe.GetCraftingCost())||element is Forest paidForest&&Paid(paidForest.GetResourceCost()))result.HasOperatingCosts=true;
          string stage=LedgerObserver.Stage(element);
          if(!grouped.TryGetValue(stage,out var entry))
          {
           entry=new Entry{Stage=stage,Element=element};grouped.Add(stage,entry);result.Stages.Add(entry);
          }
          entry.Buildings++;
          if(element is ResourceElement resource&&resource.Resource!=null)result.Resources.Add(resource.Resource.Identifier);
          if(element is Forest forest&&forest.SelectedResource!=null)result.Resources.Add(forest.SelectedResource.Identifier);
         }
         return result;
        }
    }
}
