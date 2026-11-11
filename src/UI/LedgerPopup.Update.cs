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
        void Update()

        {

         if(!owner||!owner.isActiveAndEnabled||!WorldFlowManager.Visible){ClosePopup();return;}

         if(Time.frameCount!=openedFrame&&((Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)||(Mouse.current!=null&&Mouse.current.rightButton.wasPressedThisFrame))){ClosePopup();return;}

         Fit();if((refresh-=Time.unscaledDeltaTime)<=0){refresh=.25f;Refresh();}

        }
    }
}
