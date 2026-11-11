using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed partial class LedgerUi
    {
        internal Sprite Sprite(string name) => sprites.FirstOrDefault(s => s.name == name);
    }
}
