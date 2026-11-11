using System;

using System.Collections.Generic;

using System.Linq;

using Assets.Behaviour.Flow;

using Assets.Behaviour.UI;

using Assets.Behaviour.UI.Shared;

using Assets.Source.Items;

using Assets.Source.Player;

using Assets.Source.Util;

using Behaviour.UI.Tooltip;

using TMPro;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.InputSystem;

using UnityEngine.UI;

using GameInput=Assets.Behaviour.Input.PlayerInput;

namespace KingdomLedger
{
    public sealed partial class LedgerPopup:UIPopupMenu
    {
        int ChainStage(int index,string key,LedgerSummary summary,bool money,LedgerChain.Entry connected=null)

        {

         if(LedgerObserver.IsRoutingStage(key))return index;

         summary.Costs.Stages.TryGetValue(key,out var quantity);summary.Costs.StageMoney.TryGetValue(key,out var spend);

         var values=(spend??new Dictionary<string,decimal>()).Where(p=>p.Value!=0).OrderBy(p=>p.Key).ToArray();

         if(quantity<=0&&values.Length==0)return index;

         var pieces=key.Split(new[]{" / "},StringSplitOptions.None);string detail=pieces.Length>1?pieces[0]:"Production stage";

         if(connected!=null&&connected.Buildings>1)detail+=" · "+connected.Buildings+" buildings";

         string cost=money?string.Join("\n",values.Select(p=>LedgerHistory.Money(new[]{new KeyValuePair<string,decimal>(p.Key,-p.Value)}))):"";

         Row(index,pieces.Length>1?pieces[1]:pieces[0],detail,quantity>0?LedgerHistory.Number(quantity):"",cost,connected?.Element.Icon,null,key+(values.Length>0?"\n"+LedgerHistory.Money(values):""),Math.Max(44,values.Length*20+8));

         return index+1;

        }
    }
}
