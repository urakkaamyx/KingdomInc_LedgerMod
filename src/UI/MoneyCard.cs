using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KingdomLedger
{
    internal sealed class MoneyCard
    {
        internal void Count(string name, string amount, string description)
        {
            title.text = name.ToUpperInvariant();
            value.text = amount;
            detail.text = description;
        }

        readonly TMP_Text value, detail, title;
        internal MoneyCard(LedgerUi ui, Transform parent, string name, float x)
        {
            var card = ui.Box(parent, name + "Card", x, 0, 266, 104, "DefaultBorderDark");
            title = ui.Text(card.transform, name.ToUpperInvariant(), 16, 10, 234, 22, 14);
            title.color = LedgerUi.Muted;
            value = ui.Text(card.transform, "", 16, 34, 234, 38, 25);
            detail = ui.Text(card.transform, "", 16, 76, 234, 18, 13);
            detail.color = LedgerUi.Muted;
        }
    }
}
