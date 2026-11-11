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
        int Heading(int index,string title,string right="")

        {

         var row=Row(index,title,"","",right,null,LedgerUi.Gold);row.Background.color=new Color(.45f,.3f,.17f,.15f);row.Name.fontSize=14;return index+1;

        }
    }
}
