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
        internal string Destination(int lane)
        {
         Resolve(this,lane,null,out var receiver,out _,out var reason);
         if(receiver==null)return reason??"Disconnected";
         return (LedgerObserver.IsIncomeReceiver(receiver.Parent)?"Income: ":"Destination: ")+receiver.Parent.ElementType+(reason==null?"":" · "+reason);
        }
    }
}
