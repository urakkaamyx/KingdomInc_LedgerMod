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
        internal static Color NetColor(LedgerSummary s)

        {

         var values=Net(s).Values;return values.Any(v=>v<0)?values.Any(v=>v>0)?LedgerUi.Gold:LedgerUi.Red:values.Any(v=>v>0)?LedgerUi.Green:LedgerUi.Muted;

        }
    }
}
