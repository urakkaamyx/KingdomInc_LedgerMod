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
        internal void Open(){if(IsOpen){ClosePopup();return;}Refresh();GameUI.Instance.TogglePopupMenu(this,true);openedFrame=Time.frameCount;restoreInput=GameInput.Enabled;GameInput.Disable();Canvas.ForceUpdateCanvases();Fit();}
    }
}
