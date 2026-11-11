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
        public bool SetPairs(int count)
        {
         if(count<1||count>5)return false;
         for(int i=count;i<Inputs.Count;i++)
          if(Inputs[i].ConnectedPipe!=null||Outputs[i].ConnectedPipe!=null)return false;
         while(Inputs.Count<count)
         {
          int i=Inputs.Count;
          Inputs.Add(new FlowInput(this,i){BufferSize=0,Filter=item=>true,Position=-10-Grid*i});
          Outputs.Add(new FlowOutput(this,i){BufferSize=0,Position=10+Grid*i});
         }
         if(Inputs.Count>count){Inputs.RemoveRange(count,Inputs.Count-count); Outputs.RemoveRange(count,Outputs.Count-count);}
         // Native ports begin 10 units into row 0, one grid row apart per lane.
         // Rewrite existing positions too, including ports loaded from older saves.
         for(int i=0;i<count;i++){Inputs[i].Position=-10-Grid*i;Outputs[i].Position=10+Grid*i;}
         Size=new Vector2Int(Width,DisplayHeight(count,0,0));
         if(World){World.UpdateIO();World.GetComponent<LedgerWorld>()?.Refresh();}
         return true;
        }
    }
}
