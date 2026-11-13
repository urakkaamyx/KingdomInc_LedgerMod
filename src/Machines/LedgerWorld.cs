using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    public sealed class LedgerWorld : MonoBehaviour
    {
        internal const int ContentWidth = Ledger.Width - 62, CurrencyWidth = (ContentWidth - 12) / 2, CurrencyStride = CurrencyWidth + 12;
        Ledger ledger;
        LedgerUi ui;
        TMP_Text period, throughput, quantity;
        RectTransform moneyStrip, body;
        RectTransform timerFill;
        FoundationTooltipSource timerTip;
        int timerSecond = -1, timerPeriod;
        readonly List<global::KingdomLedger.NodeCurrencyRow> moneyRows = new List<global::KingdomLedger.NodeCurrencyRow>();
        readonly List<global::KingdomLedger.ItemRow> itemRows = new List<global::KingdomLedger.ItemRow>();
        float refresh;
        bool initialized, openWhenReady;
        internal LedgerPopup Settings { get; private set; }
        internal Ledger Element => ledger;

        internal const int HeaderInset = 20;
        void Start()
        {
            TryInitialize();
        }

        void EnsureItemRows(int count)
        {
            while (itemRows.Count < count)
            {
                int i = itemRows.Count;
                global::KingdomLedger.ItemRow row = new global::KingdomLedger.ItemRow();
                row.Root = LedgerUi.Rect(body, "ItemRow" + i, 30, Ledger.ItemTop + i * Ledger.RowHeight, ContentWidth, 24);
                if (i % 2 == 0)
                    row.Shade = ui.Fill(row.Root, "Shade", 0, 0, ContentWidth, 24, new Color(0, 0, 0, .12f));
                row.Icon = ui.Icon(row.Root, null, 6, 2, 20);
                row.Icon.name = "ItemIcon" + i;
                row.Name = ui.Text(row.Root, "", 38, 0, ContentWidth - 124, 24, 17);
                row.Amount = ui.Text(row.Root, "", ContentWidth - 82, 0, 72, 24, 17);
                row.Amount.alignment = TextAlignmentOptions.MidlineRight;
                row.Amount.color = LedgerUi.Gold;
                itemRows.Add(row);
            }
        }

        // The generic native building prefab keeps its icon slot and progress frame after
        // their contents are hidden. Ledger has no building icon, so the slot is hidden, and
        // the progress frame is reused unchanged as the full-width header band.
        void LayoutNativeBackdrops(Component world)
        {
            var icon = FoundationApi.WorkspaceVisuals.Icon(world);
            if (icon && icon.transform.parent && icon.transform.parent != transform)
                icon.transform.parent.gameObject.SetActive(false);
            var progress = FoundationApi.WorkspaceVisuals.BuildingGraphic(world, GameBuildingGraphic.Progress) as Image;
            if (progress && progress.transform.parent is RectTransform band && band != transform)
            {
                band.name = "LedgerHeaderBand";
                band.anchorMin = new Vector2(0, 1);
                band.anchorMax = new Vector2(1, 1);
                band.pivot = new Vector2(0, 1);
                band.anchoredPosition = new Vector2(HeaderInset, -Ledger.HeaderTop);
                band.sizeDelta = new Vector2(-HeaderInset * 2, Ledger.HeaderHeight);
            }
        }

        void OnDestroy()
        {
            if (Settings)
            {
                if (Settings.IsOpen && FoundationApi.UiAccess.Root)
                    Settings.ClosePopup();
                Destroy(Settings.gameObject);
            }
        }

        void OnDisable()
        {
            if (Settings && Settings.IsOpen && FoundationApi.UiAccess.Root)
                Settings.ClosePopup();
        }

        public void OpenSettings()
        {
            // Placement calls HandleReopenBuyMenu immediately after SetElement, before
            // Unity runs Start. Build once here, or retain the request until bound.
            if (!FoundationApi.UiAccess.Root || !TryInitialize())
            {
                openWhenReady = true;
                return;
            }

            openWhenReady = false;
            if (!Settings)
            {
                var toolbar = FoundationApi.UiAccess.EditToolbar;
                var canvas = toolbar ? toolbar.GetComponentInParent<Canvas>()?.rootCanvas : GetComponentInParent<Canvas>()?.rootCanvas;
                var root = new GameObject("LedgerSettings", typeof(RectTransform));
                root.SetActive(false);
                try
                {
                    var menu = root.AddComponent<LedgerPopup>();
                    menu.Initialize(this, ledger, ui.Font, canvas ? canvas.transform : null);
                    Settings = menu;
                }
                catch
                {
                    Destroy(root);
                    throw;
                }
            }

            Settings.Open();
        }

        internal void Refresh()
        {
            if (!initialized)
                return;
            RefreshTimer();
            var summary = ledger.History.Summary(ledger.Period);
            var items = summary.Items.OrderBy(p => LedgerHistory.ItemName(p.Key)).ToArray();
            int visibleRows = Math.Max(1, items.Length);
            EnsureItemRows(visibleRows);
            int itemBottom = Ledger.ItemTop + visibleRows * Ledger.RowHeight;
            string unit = ledger.Period == 1 ? "second" : ledger.Period == 60 ? "minute" : "hour";
            period.text = "ITEMS PASSED / LAST " + unit.ToUpperInvariant();
            var currencies = summary.Currencies;
            bool money = currencies.Length > 0;
            int extra = Math.Max(0, (currencies.Length + 1) / 2 - 1) * 20;
            int height = Ledger.DisplayHeight(ledger.Inputs.Count, items.Length, currencies.Length);
            bool showContent = items.Length > 0 || height >= 128;
            if (quantity)
                quantity.gameObject.SetActive(showContent);
            for (int i = 0; i < itemRows.Count; i++)
            {
                global::KingdomLedger.ItemRow row = itemRows[i];
                bool showRow = items.Length > 0 ? i < visibleRows : (showContent && i == 0);
                row.Root.gameObject.SetActive(showRow);
                bool exists = i < items.Length;
                row.Icon.enabled = exists;
                if (row.Shade)
                    row.Shade.enabled = exists;
                row.Name.text = exists ? LedgerHistory.ItemName(items[i].Key) : showRow ? "No items passed yet" : "";
                row.Amount.text = exists ? LedgerHistory.Number(items[i].Value) : "";
                if (exists)
                    row.Icon.sprite = FoundationApi.ItemAccess.Find(items[i].Key)?.PreviewItem.Icon;
            }

            moneyStrip.gameObject.SetActive(money);
            throughput.gameObject.SetActive(!money && showContent);
            throughput.text = LedgerHistory.Number(summary.Items.Values.Sum()) + " items passed Â· " + summary.Items.Count + " types";
            moneyStrip.anchoredPosition = new Vector2(30, -itemBottom - 4);
            throughput.rectTransform.anchoredPosition = new Vector2(32, -itemBottom);
            moneyStrip.sizeDelta = new Vector2(ContentWidth, 38 + extra);
            body.sizeDelta = new Vector2(Ledger.Width, height);
            var size = new Vector2Int(Ledger.Width, height);
            if (ledger.Size != size || ((RectTransform)transform).sizeDelta != (Vector2)size)
            {
                ledger.Size = size;
                ((RectTransform)transform).sizeDelta = size;
                FoundationApi.WorkspaceVisuals.UpdateCollider(ledger.World);
                foreach (var io in ledger.IO)
                    if (io.ConnectedPipe?.World)
                        FoundationApi.WorkspaceVisuals.RefreshPipe(io.ConnectedPipe.World);
            }

            for (int i = 0; i < currencies.Length; i++)
            {
                if (moneyRows.Count <= i)
                {
                    global::KingdomLedger.NodeCurrencyRow row = new global::KingdomLedger.NodeCurrencyRow();
                    row.Root = LedgerUi.Rect(moneyStrip, "CurrencyRow" + i, (i % 2) * CurrencyStride, 17 + (i / 2) * 20, CurrencyWidth, 20);
                    row.Icon = ui.Icon(row.Root, null, 0, 3, 14);
                    row.Net = ui.Text(row.Root, "", 20, 0, CurrencyWidth - 20, 20, 12);
                    row.Net.name = "Net";
                    row.Net.enableAutoSizing = true;
                    row.Net.fontSizeMin = 9;
                    row.Net.fontSizeMax = 12;
                    var hover = ui.Fill(row.Root, "CurrencyHelp", 0, 0, CurrencyWidth, 20, Color.clear);
                    hover.raycastTarget = true;
                    row.Tip = hover.gameObject.AddComponent<FoundationTooltipSource>();
                    moneyRows.Add(row);
                }

                string currency = currencies[i];
                global::KingdomLedger.NodeCurrencyRow r = moneyRows[i];
                r.Root.name = currency;
                r.Root.gameObject.SetActive(true);
                if (Enum.TryParse(currency, out GameCurrency type))
                {
                    r.Icon.sprite = type.GetIcon();
                    r.Icon.enabled = true;
                }

                summary.Costs.Money.TryGetValue(currency, out var cost);
                summary.Revenue.TryGetValue(currency, out var revenue);
                decimal net = revenue - cost;
                r.Net.text = (summary.ProfitComplete ? "" : "~") + LedgerHistory.Amount(currency, net, true);
                r.Net.color = net < 0 ? LedgerUi.Red : net > 0 ? LedgerUi.Green : LedgerUi.Muted;
                r.Tip.SetTooltip(LedgerHistory.CurrencyName(currency), "Operating costs: " + LedgerHistory.Amount(currency, -cost, true) + "\nRevenue: " + LedgerHistory.Amount(currency, revenue, true) + "\nProfit / loss: " + r.Net.text + (summary.ProfitComplete ? "" : "\n~ Estimated from known income and costs; upstream cost history is incomplete."));
            }

            for (int i = currencies.Length; i < moneyRows.Count; i++)
                moneyRows[i].Root.gameObject.SetActive(false);
        }

        internal void RefreshTimer()
        {
            if (!initialized)
                return;
            timerFill.sizeDelta = new Vector2(ContentWidth * ledger.PeriodProgress, 4);
            int second = (int)ledger.PeriodElapsed;
            if (second == timerSecond && timerPeriod == ledger.Period)
                return;
            timerSecond = second;
            timerPeriod = ledger.Period;
            timerTip.SetTooltip("Reporting timer", second + " / " + ledger.Period + " seconds\nRepeats for the selected window. Totals continue rolling; the bar does not clear them.");
        }

        bool TryInitialize()
        {
            if (initialized)
                return true;
            var world = FoundationApi.WorkspaceAccess.FindBuildingView(gameObject);
            var element = world ? FoundationApi.WorkspaceAccess.FromView(world)?.Behaviour as Ledger : null;
            if (element == null)
                return false;
            var font = FoundationApi.WorkspaceVisuals.LabelText(world)?.font;
            if (!font)
                return false;
            ledger = element;
            ui = new LedgerUi(font);
            body = LedgerUi.Rect(transform, "LedgerDisplay", 0, 0, Ledger.Width, Ledger.Height);
            var gear = ui.Button(body, "", Ledger.Width - 42, 0, 30, 30, OpenSettings);
            gear.name = "LedgerGear";
            var cog = LedgerUi.Rect(gear.transform, "Cog", 5, 5, 20, 20).gameObject.AddComponent<LedgerGearGraphic>();
            cog.color = LedgerUi.Paper;
            cog.raycastTarget = false;
            gear.gameObject.AddComponent<FoundationTooltipSource>().SetTooltip("Ledger settings", "Reporting window, input/output pairs, entire chain costs and currency profit.");
            period = ui.Text(body, "", 32, 35, Ledger.Width - 130, 24, 13);
            period.color = LedgerUi.Muted;
            quantity = ui.Text(body, "QTY", Ledger.Width - 100, 35, 66, 24, 12);
            quantity.color = LedgerUi.Muted;
            quantity.alignment = TextAlignmentOptions.MidlineRight;
            var timer = ui.Fill(body, "WindowTimer", 30, Ledger.TimerTop, ContentWidth, 4, new Color(.12f, .085f, .10f, .9f));
            timer.raycastTarget = true;
            timerTip = timer.gameObject.AddComponent<FoundationTooltipSource>();
            timerFill = ui.Fill(timer.transform, "Fill", 0, 0, 0, 4, LedgerUi.Gold).rectTransform;
            EnsureItemRows(1);
            moneyStrip = LedgerUi.Rect(body, "MonetaryTotals", 30, 216, ContentWidth, 38);
            ui.Rule(moneyStrip, 0, 0, ContentWidth);
            var label = ui.Text(moneyStrip, "PROFIT / LOSS", 0, 3, ContentWidth, 14, 10);
            label.color = LedgerUi.Muted;
            throughput = ui.Text(body, "", 32, Ledger.ItemTop + Ledger.RowHeight, ContentWidth - 4, 24, 14);
            throughput.name = "PassthroughTotal";
            throughput.color = LedgerUi.Muted;
            initialized = true;
            LayoutNativeBackdrops(world);
            Refresh();
            return true;
        }

        void Update()
        {
            if (!TryInitialize())
                return;
            if (openWhenReady)
                OpenSettings();
            RefreshTimer();
            if ((refresh -= Time.unscaledDeltaTime) <= 0)
            {
                refresh = .2f;
                Refresh();
            }
        }
    }
}
