using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;

namespace KingdomLedger
{
    internal sealed partial class LedgerChain
    {
        internal static bool HasIncomeDestination(Ledger ledger)
        {
         var pending=new Stack<FlowElement>();var seen=new HashSet<FlowElement>();pending.Push(ledger);
         while(pending.Count>0)
         {
          var element=pending.Pop();if(!seen.Add(element))continue;
          if(LedgerObserver.IsIncomeReceiver(element))return true;
          foreach(var output in element.Outputs)
          {
           var pipe=output.ConnectedPipe;
           if(pipe!=null&&pipe.ConnectedOutput==output&&pipe.ConnectedInput!=null&&pipe.ConnectedInput.ConnectedPipe==pipe)pending.Push(pipe.ConnectedInput.Parent);
          }
         }
         return false;
        }
    }
}
