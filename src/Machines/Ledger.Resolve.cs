using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using LightJson;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomLedger
{
    public sealed partial class Ledger : WorkspaceNode
    {
        internal static bool Resolve(Ledger start,int lane,InventoryItem item,out FlowInput receiver,out List<(Ledger ledger,int lane)> path,out string reason)
        {
         receiver=null;path=new List<(Ledger,int)>();var seen=new HashSet<Ledger>();
         var ledger=start;
         while(ledger!=null)
         {
          if(!seen.Add(ledger)||seen.Count>128){reason="Ledger loop blocked";return false;}
          if(lane<0||lane>=ledger.Outputs.Count){reason="Invalid output";return false;}
          path.Add((ledger,lane));
          var output=ledger.Outputs[lane];var pipe=output.ConnectedPipe;
          if(pipe==null||pipe.ConnectedOutput!=output||pipe.ConnectedInput==null||pipe.ConnectedInput.ConnectedPipe!=pipe)
           {reason="Output "+(lane+1)+": disconnected";return false;}
          receiver=pipe.ConnectedInput;
          if(receiver.Parent is Ledger next){ledger=next;lane=receiver.ParentIndex;continue;}
          if(!receiver.CanBuffer){reason="Output "+(lane+1)+": full";return false;}
          if(receiver.Parent is Stockpile store && store.Contents.Count+store.Inputs.Sum(i=>i.BufferedItems.Count)>=store.Capacity)
           {reason="Stockpile full";return false;}
          if(receiver.Parent is Shop shop && shop.CollectedGold>=shop.MaxGold)
           {reason="Shop till full";return false;}
          if(receiver.Parent is RequisitionOffice office&&office.CollectedRequisition>=office.MaxRequisition)
           {reason="Requisition Office full";return false;}
          if(item!=null&&!receiver.CanAcceptItem(item)){reason="Item rejected by destination";return false;}
          reason=null;return true;
         }
         reason="No destination";return false;
        }
    }
}
