# Ledger 0.5.1

A BepInEx 5 mod for Kingdom Inc. Uses **Foundation 0.5.0+** to add Ledger research under **Tech Tree → Mods**, requiring Accountant, and a Ledger utility in the native build menu.

## Foundation integration

Install Foundation separately at `BepInEx/plugins/Foundation/Foundation.dll` before launching Ledger 0.5.1. Ledger uses Foundation's public research/workspace registries and native generic node prefab; its zero-storage accounting and custom report remain Ledger-owned. The old `ledger_mod` research ID and `Ledger` building type are retained, so existing unlocks, nodes, pair counts, periods and pipe lanes load without replacement. Newly researched Ledger is on the Mods tab; Accountant stays on Base Game. This migration adds no feature gates or extra research purchases.

[Foundation integration review](FOUNDATION_INTEGRATION.txt) lists framework gaps and the adapters retained in Ledger. The highest priorities are supported custom workspace view binding, stateless IO hooks, shared tech-tree layout/group ownership and post-load/geometry hooks. Foundation source was not changed.

## Use

Place Ledger between the last producer and its destination. It starts with **one input and one matching output**. The gear adds pairs up to **five inputs / five outputs** and selects a rolling **second, minute, or hour** report. Disconnect the final pair before removing it.

Ledger has **zero item storage**. An incoming item goes straight into the matching receiver's input if there is room and the receiver accepts it. Otherwise the original item stays at its producer. A full stockpile, shop till or Requisition Office also blocks admission. Each lane works independently; it is not a merger or distributor. Chained Ledgers work, while loops refuse admission. Clearing adjacent nodes routes around Ledger storage as well.

A slim gold timer bar below the node header fills left to right and repeats for the selected second, minute or hour. It restarts when the reporting window changes and on a new session. The bar uses the same real-time clock as the rolling report; wrapping it does not clear the totals. Hover it for elapsed seconds.

The node is six native grid cells wide (384 units), with matching input/output rows spaced 64 units apart like Collector, Distributor and Manifold. Height follows the number of port rows with a two-cell minimum, expanding by whole grid cells only when the actual item list or currency totals need more room. Empty item rows no longer reserve space. Existing saves adopt the corrected dimensions and spacing when loaded, preserving lane connections.

The main node lists every item type that passed during the selected window, with its count and native icon. The list expands for additional item types, with no paging arrows, page counter or input/output-count footer. The list stays visible when the report is closed. Input ports are green; outputs are blue.

The gear report shows intermediate production stages, their observed operating spend, and the raw resources used. Values follow actual item production through native resource buildings and crafting recipes, including upgrade-adjusted currency payments. Distinct ingredients are added once, and batch outputs share costs. Productivity never charges the same batch twice: the first yield carries its batch costs; subsequent productivity yields have zero additional cost. This conserves total costs, although individual yield margins can differ.

The chain tab follows every connected upstream input, including equipment and recruitment branches. It displays only recorded production quantities or currency spend. Routing/storage stages such as Manifold, Distributor, Collector and Stockpile are hidden, as are unobserved stages and resources; no unknown dash rows are added. Useful production stages with zero spend keep their item quantities and leave the spend column blank. Matching building/product rows are grouped and identify the number of connected buildings. Shared upstream sources are visited once, and cyclic wiring cannot loop the report. Previously observed production stays in the rolling report if the chain is rewired.

Outfitter preserves both the human's and equipment's full observed production history across the separate intake and completion updates. Successive Outfitters add only their newly consumed equipment; duplicate-equipment and timeout passthrough preserve the existing history. Unsupported delayed utilities retain known upstream history while keeping their additional costs incomplete.

The settings window uses its own screen overlay, so hiding the native HUD does not hide Ledger settings. The overlay follows the game's UI scale, fits the screen, and keeps native Escape/backdrop dismissal.

Newly placed Ledgers can open settings immediately from the game's placement callback, before Unity runs the node's first Start callback. Initialization runs once, early requests wait for their model/UI if necessary, and a failed window construction cannot leave a half-created settings object behind. Closing the initial window and clicking the new node's gear reopens the same configuration.

The interface uses the game's native bronze frames, burgundy panels, pixel font, item icons and buttons. A currency table shows every affected currency in its own row, with separate revenue, operating costs and net change. Operating costs display as negative red amounts, while positive revenue displays with a plus sign in its existing paper color. Gold uses native gold, silver and copper icons: one gold is 100 silver or 10,000 copper. The main node shows one combined profit/loss amount per affected currency, in a compact two-column grid filled left to right then down. Its smaller amounts use g/s/c notation for Gold. With incomplete upstream history, the node shows known revenue minus known costs with a ~ estimate marker and an explanatory tooltip; the detailed report still withholds a complete net figure. Requisition, Medals, Rings, Mana, Crowns, Ducats, Ingenuity and Faith keep their own units and icons; they are never converted to gold. The window grows to fit all affected currency rows; the node grows only for additional grid rows. Detailed report net totals remain explicitly marked Unknown when cost history is incomplete. Only Items passed and Chain costs/Chain items tabs remain; the Outputs tab is removed. Hover totals and history notices for explanations. Green and red values distinguish gains and losses by currency.

Paid chains display their operating loss even without a shop destination: for example, 2 gold of observed costs with no revenue produces -2 gold. Configured native resource, Forest and crafting costs enable this view as soon as the upstream chain is connected, while monetary totals still use actual observations. Disconnecting a shop preserves currency activity until it ages out of the reporting window. A chain with no monetary costs, revenue or supported income destination instead displays item counts, item types, connected pairs, production quantities and raw resources. No shop reminders are shown. A full shop remains a shop destination; chained Ledgers are resolved to their final receiver.

A downstream Enchanter reports its actual currency payment back to every Ledger whose measured items it consumes. This works through native storage/routing nodes, preserves native upgrade-adjusted spending, and never records a failed or merely configured payment. Each job is charged once per Ledger even when it consumes multiple measured inputs. Its receipts survive equipment enchantments, material transformations and output batches, so later supported destination income still reaches the same Ledger. Productivity adds no duplicate batch cost; jobs that bypass Ledger cannot charge it. Currency-only Enchanter recipes also report their actual earned currency. Other downstream production types are not added to this receipt-transfer support by this change.

Output detection is automatic. Item Shop, Luxury Shop, Requisition Office, Disenchanter, Restaurant and Mercenary Guild rewards are tracked in their actual currencies. Receipts follow the original item through routing and storage nodes, so destinations need not be directly beside Ledger. Income is recognized when the native building earns it, including additions to its collection tray; manual or automatic collection never credits it again. Consumed destination items also add that building to the observed chain report.

Requisition Office receipts retain unfinished contract contributions until the game issues a reward. Shared Ledger contributions divide that actual reward proportionally; existing contract progress and passive generation keep their own uncredited share. Fractional amounts represent attribution of a real reward across its contributors. Disenchanter passive Mana is excluded as well. Items that never passed a Ledger cannot credit that Ledger's income. Actual currency rewards earned during upstream crafting travel with the output provenance and are split across batch items, like operating costs.

**Profit/loss = confirmed revenue recorded in the reporting window minus the observed operating costs of all items passed during that window.** Unsold items contribute their recorded costs, and a chain with expenses but no sales shows a loss. Upstream costs are attributed when items pass Ledger; downstream Enchanter payments and destination income use the time the observed payment or reward occurs. This is not an independent log of every producer payment's original timestamp. Currencies remain separate; 10,000 internal Gold units display as one gold. Resource quantities are not assigned an invented gold price. Building purchases and research are not operating costs and are excluded.

## History and limits

- Port count and window settings survive saves, copies, and native blueprint discovery. Copies start with empty accounting history.
- Rolling counters and item provenance are session-local and reset on load. Nothing is written into native inventory-item serialization.
- Items already buffered when a session starts have unknown upstream costs. The report marks incomplete costs and withholds a complete net profit/loss figure while affected passed or sold items remain in the reporting window.
- Native `ResourceElement`, Forest harvesting, `RecipeElement` and Outfitter chains are tracked, together with the six income destinations listed above. Unsupported special production, training, combat, portals that reconstruct items, and other mods' transformations may have incomplete provenance. Unknown is never treated as free; unobserved rows are omitted and the summary still marks incomplete history.
- Ledger applies normal destination input capacity plus full stockpile, shop till and Requisition Office checks. It does not change destination processing speed, shop acceptance, source deletion rules, or inventory capacity.
- Party-Up's whole-batch release requires storage capacity at its receiver, so Ledger's zero-capacity input is not a compatible whole-party receiver. Ordinary production pipes are supported.
- Reports use rolling one-second aggregate buckets of real elapsed session time. Pausing does not generate items; old observations age out. Changing windows does not multiply a short sample into an hourly forecast.

## Install / update / remove

Requires Kingdom Inc's Unity Mono build, **BepInEx 5**, and **Foundation 0.5.0 or later**, installed separately. Built and runtime-tested on **Kingdom Inc Release v1.0.12** against the local game's assemblies and BepInEx **5.4.23.6** (reported engine version 6000.3.27f2). No Factory API, dashboard, or Party-Up dependency. Foundation is a required framework dependency. Exact runtime game/Unity versions are recorded in the verification report.

Extract the player archive into the game directory so `LedgerMod.dll` is at `BepInEx/plugins/LedgerMod/LedgerMod.dll`. Restart the game after installation. Manual installation is supported; Vortex deployment has not been tested.

For local development, run `./_Mods/LedgerMod/install.ps1`. Existing Ledger DLLs are backed up first. By default updates require the game to be closed. With `-StageForNextLaunch`, the running game's mapped DLL is preserved intact in the project's backup folder and a fresh DLL is staged for the next launch; the current game continues using its previous version until restart.

Before uninstalling, remove Ledger nodes from every kingdom you intend to keep, reconnect their production chains, save, then close the game and remove the LedgerMod plugin directory. Do not load a save containing custom Ledger nodes without the mod. Ledger holds no inventory to recover. The mod does not patch base-game files or alter other installed plugin DLLs.

## Build and verify

Install Foundation 0.5.0+ first. The project references the installed Foundation DLL with `Private=false`; it is not distributed in Ledger archives.

```
dotnet build _Mods/LedgerMod/LedgerMod.csproj -c Release
& ./_Mods/LedgerMod/verify-isolated.ps1
& ./_Mods/LedgerMod/verify-isolated.ps1 -WithExistingPlugins
python _Mods/LedgerMod/Tools/package_nexus.py
```

The isolated diagnostic loads a disposable kingdom with save writes, latest-save loads, Steam initialization and achievements disabled. It never loads or terminates the user's normal game. Results and screenshots are in `Verification/`. Both verification modes load the installed Foundation DLL. The companion run also loads the other installed plugins, with a private Factory API port. It verifies Ledger behavior with those plugins present; it does not claim every feature of those mods has been retested.

The package tool checks matching built/installed/tested DLL bytes, passing runtime reports, source and archive paths, clean extraction, and SHA-256. Packages contain the mod DLL and instructions; source packages exclude game/loader assemblies, saves, logs, local configuration, and diagnostic runtimes.

Native UI frames, fonts, icons and gear style are reused at runtime. No game assets are bundled. UI construction and isolated capture helpers were adapted from the user's PartyNode project. Plugin ID: `urakka.kingdominc.ledgermod`; assembly: `LedgerMod.dll`; source convention: `urakkaamyx/KingdomInc_LedgerMod` 

Source repository: https://github.com/urakkaamyx/KingdomInc_LedgerMod
