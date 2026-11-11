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
        void Update()

        {

         if(!TryInitialize())return;

         if(openWhenReady)OpenSettings();
         RefreshTimer();

         if((refresh-=Time.unscaledDeltaTime)<=0){refresh=.2f;Refresh();}

        }
    }
}
