using System;

using System.Linq;

using System.Collections.Generic;

using Assets.Source.Player;

using Assets.Behaviour.Flow;

using Assets.Behaviour.UI;

using Assets.Source.Items;

using Behaviour.UI.Tooltip;

using TMPro;

using UnityEngine;

using UnityEngine.UI;

namespace KingdomLedger
{
    public sealed partial class LedgerWorld:MonoBehaviour
    {
        // The generic native building prefab keeps its icon slot and progress frame after
        // their contents are hidden. Ledger has no building icon, so the slot is hidden, and
        // the progress frame is reused unchanged as the full-width header band.
        void LayoutNativeBackdrops(WorldFlowElement world)
        {
         var icon=LedgerPlugin.Field<Image>(world,"_icon");
         if(icon&&icon.transform.parent&&icon.transform.parent!=transform)icon.transform.parent.gameObject.SetActive(false);
         var progress=LedgerPlugin.Field<Image>(world,"_progress");
         if(progress&&progress.transform.parent is RectTransform band&&band!=transform)
         {
          band.name="LedgerHeaderBand";band.anchorMin=new Vector2(0,1);band.anchorMax=new Vector2(1,1);band.pivot=new Vector2(0,1);
          band.anchoredPosition=new Vector2(HeaderInset,-Ledger.HeaderTop);band.sizeDelta=new Vector2(-HeaderInset*2,Ledger.HeaderHeight);
         }
        }
    }
}
