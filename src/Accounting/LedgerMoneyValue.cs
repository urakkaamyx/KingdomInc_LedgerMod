using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using TMPro;
using UnityEngine;

namespace KingdomLedger
{
    internal sealed class LedgerMoneyValue
    {
        internal readonly RectTransform Root;
        readonly TMP_Text number, sign;
        readonly TMP_Text[] coins = new TMP_Text[3];
        readonly GameObject coinGroup;
        static Sprite silver, copper;
        internal LedgerMoneyValue(LedgerUi ui, Transform parent, string name, float x, float y, float width, float height)
        {
            Root = LedgerUi.Rect(parent, name, x, y, width, height);
            number = ui.Text(Root, "", 0, 0, width, height, 18);
            number.name = "Amount";
            number.alignment = TextAlignmentOptions.MidlineRight;
            number.enableAutoSizing = true;
            number.fontSizeMin = 12;
            number.fontSizeMax = 18;
            var group = LedgerUi.Rect(Root, "Denominations", 0, 0, width, height);
            coinGroup = group.gameObject;
            sign = ui.Text(group, "", 0, 0, 18, height, 16);
            sign.name = "Sign";
            if (!silver)
                silver = FoundationApi.UiAccess.CurrencyIcon("Gold", 1);
            if (!copper)
                copper = FoundationApi.UiAccess.CurrencyIcon("Gold", 2);
            var sprites = new[]
            {
                GameCurrency.Gold.GetIcon(),
                silver,
                copper
            };
            var labels = new[]
            {
                "Gold",
                "Silver",
                "Copper"
            };
            float cell = (width - 18) / 3;
            for (int i = 0; i < 3; i++)
            {
                coins[i] = ui.Text(group, "", 18 + i * cell, 0, cell - 20, height, 18);
                coins[i].name = labels[i];
                coins[i].alignment = TextAlignmentOptions.MidlineRight;
                coins[i].enableAutoSizing = true;
                coins[i].fontSizeMin = 11;
                coins[i].fontSizeMax = 18;
                ui.Icon(group, sprites[i], 18 + (i + 1) * cell - 17, (height - 15) / 2, 15).name = labels[i] + "Coin";
            }
        }

        internal void Show(string currency, decimal amount, Color color, bool signed = false, bool unknown = false)
        {
            bool gold = currency == "Gold" && !unknown;
            coinGroup.SetActive(gold);
            number.gameObject.SetActive(!gold);
            number.text = unknown ? "Unknown" : LedgerHistory.Amount(currency, amount, signed);
            number.color = color;
            if (!gold)
                return;
            var value = Math.Abs(amount);
            sign.text = amount < 0 ? "-" : signed && amount > 0 ? "+" : "";
            sign.color = color;
            var values = new[]
            {
                Math.Floor(value / 10000m),
                Math.Floor(value % 10000m / 100m),
                value % 100m
            };
            for (int i = 0; i < 3; i++)
            {
                coins[i].text = LedgerHistory.Number(values[i]);
                coins[i].color = color;
            }
        }
    }
}
