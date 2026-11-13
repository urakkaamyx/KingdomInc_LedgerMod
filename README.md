# Ledger

**Current release: 0.5.2 — for Kingdom Inc game version 1.0.14.**

[GitHub releases](https://github.com/KingdomIncModding/KingdomInc_LedgerMod/releases) · [Nexus Mods](https://www.nexusmods.com/kingdominc/mods/7) · Version 0.5.2

Zero-storage production ledger for Kingdom Inc. See what your production chains produce, what they consume, and whether they earn their keep.

Place Ledger between the last producer and its destination to track passing items, upstream resources, operating costs, and profit or loss. Item counts and a compact currency report stay visible on the node, with a detailed breakdown available from the gear button.

## Features

- **Live Item Tracking:** Item names, native icons, and quantities stay visible on the node. The list dynamically expands as additional item types pass through.
- **Profit and Loss by Currency:** A compact two-column display shows one combined net value for each affected currency. Gains are green and losses are red. Unused currencies remain hidden.
- **All Game Currencies Supported:** Gold, Silver, and Copper use native coin denominations. Requisition, Medals, Rings, Mana, Crowns, Ducats, Ingenuity, and Faith retain their own units and icons.
- **Rolling Reporting Intervals:** Choose between Second, Minute, or Hour intervals from the gear menu. A slim gold progress bar visually tracks the active interval cycle.
- **Upstream Production Breakdown:** Inspect observed production stages, raw resources, and operating spend, including recruitment and equipment branches through Outfitter.
- **Downstream Enchanter Mana Costs:** Actual Mana payments for measured items reach Ledger even after native storage and routing. Receipts survive enchantments and transformations.
- **Actual Destination Income:** Track rewards from Item Shops, Luxury Shops, Requisition Offices, Disenchanters, Restaurants, and Mercenary Guilds.
- **One to Five Matched Lanes:** Start with 1 input and 1 output, then configure up to 5 matching pairs through the gear menu. Each lane operates independently.
- **Zero Item Storage:** Ledger forwards items directly without holding inventory. If downstream receivers are full or disconnected, items hold at the producer.
- **Native Game Aesthetics:** Bronze frames, burgundy panels, the native pixel font, and original icons keep Ledger seamless with Kingdom Inc's UI.

## How the Accounting Works

For each affected currency:
**Profit / Loss = Recorded Income - Observed Operating Costs**

- Currencies are tracked separately without artificial exchange rates.
- Upstream production costs and crafting currency rewards follow the physical items passing through Ledger.
- Destination income is credited when supported buildings earn it.
- Requisition Offices record contract contributions proportionally when rewards are issued.

## Getting Started

1. Research **Accountant** in the base tech tree.
2. Open **Tech Tree > Mods** and research **Ledger** for free (registered via Foundation).
3. Place Ledger from the **Utility** build menu between producers and their destination.
4. Connect inputs and matching outputs.
5. Click the gear icon to customize lane count (1–5) and reporting window (Second, Minute, or Hour).

## Installation

1. Install [BepInEx 5 (Windows x64 Mono)](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6).
2. Install [Foundation](https://www.nexusmods.com/kingdominc/mods/18) (0.14.0 or newer).
3. Extract the release ZIP into your Kingdom Inc game folder.
4. Confirm the file is located at `BepInEx/plugins/LedgerMod/LedgerMod.dll`.
5. Launch the game and research Ledger in Tech Tree > Mods!

## Compatibility

| Class.Method | Hook Type | Impact |
|---|---|---|
| FlowIO.CanAcceptItem | Foundation hook (prefix) | Enforces zero-storage admission rules based on downstream receiver readiness. |
| FlowElement.ClearIO | Foundation hook (prefix) | Prepares zero-storage buffers before clearing I/O to avoid item destruction. |
| FlowInput.InputItem | Foundation hook (prefix) | Directly forwards items through paired lanes without local storage. |
| FlowElement.FromJson | Foundation hook (postfix) | Restores configured input/output port pair count from kingdom save data. |
| WorldFlowIO.SetIO | Foundation hook (postfix) | Applies custom port indicator visuals and colors to Ledger I/O ports. |
| WorldFlowElement.SetElement | Foundation hook (postfix) | Attaches the Ledger node UI view, currency summary counters, and gear controls. |
| FlowIO.ConsumeItem | Foundation hook (postfix) | Observes upstream item consumption events across production chains. |
| GamePlayer.SpendCurrency | Foundation hook (postfix) | Tracks currency spending events for raw material and production cost attribution. |
| GamePlayer.AddCurrency | Foundation hook (postfix) | Tracks currency income events credited from connected production and sales. |
| FlowElement.Update / DoManualCraft / QueueCraft | Foundation execution observer | Dynamically scopes active production cycles to attribute item costs and batch yields. |

## Removal

Remove all Ledger nodes from your kingdom and reconnect production lines before uninstalling. Then delete `BepInEx/plugins/LedgerMod/`.

## Credits and Source

Created by **Urakka** (rockyamyx). [Source code on GitHub](https://github.com/KingdomIncModding/KingdomInc_LedgerMod). Game artwork and assets belong to their respective owners.
