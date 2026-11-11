using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed partial class LedgerUi
    {
        internal static readonly Color Paper = new Color(1, .94f, .79f);
        internal static readonly Color Muted = new Color(.72f, .67f, .57f);
        internal static readonly Color Gold = new Color(.94f, .73f, .38f);
        internal static readonly Color Green = new Color(.62f, .85f, .57f);
        internal static readonly Color Red = new Color(.96f, .52f, .44f);
        internal static readonly Color Blue = new Color(.57f, .75f, .91f);
        internal readonly TMP_FontAsset Font;
        readonly Sprite[] sprites;
        internal LedgerUi(TMP_FontAsset font) { Font = font; sprites = Resources.FindObjectsOfTypeAll<Sprite>(); }
    }
}
