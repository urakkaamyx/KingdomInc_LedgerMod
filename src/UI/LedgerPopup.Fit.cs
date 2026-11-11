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
        void Fit()

        {

         if(referenceCanvas)scaler.scaleFactor=Mathf.Max(.1f,referenceCanvas.scaleFactor);

         var bounds=((RectTransform)transform).rect;

         panel.localScale=Vector3.one*Mathf.Max(.1f,Mathf.Min(1,(bounds.width-32)/Width,(bounds.height-32)/currentHeight));

        }
    }
}
