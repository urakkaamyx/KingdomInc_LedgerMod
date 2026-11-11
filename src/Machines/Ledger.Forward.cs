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
        internal bool Forward(int lane,InventoryItem item)
        {
         if(item==null||!Resolve(this,lane,item,out var receiver,out var path,out _))return false;
         if(!receiver.InputItem(item))return false;
         // The native incoming pipe consumes the parent's item only after true is returned.
         // Ledger never adds the item to any input/output buffer.
         try{foreach(var entry in path)entry.ledger.History.Pass(item,entry.lane,receiver.Parent);}
         catch(Exception e){LedgerPlugin.Warn("Transfer completed; reporting failed: "+e.Message);}
         return true;
        }
    }
}
