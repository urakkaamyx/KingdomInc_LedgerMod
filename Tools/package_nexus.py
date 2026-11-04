"""Freeze explicitly listed Ledger player/source files after actual runtime verification."""
from pathlib import Path
import hashlib
import json
import tempfile
import zipfile
import xml.etree.ElementTree as ET

PROJECT = Path(__file__).resolve().parents[1]
GAME = PROJECT.parents[1]
OUT = PROJECT / 'Releases' / 'Nexus'
VERSION = ET.parse(PROJECT / 'LedgerMod.csproj').getroot().findtext('PropertyGroup/Version')

def sha(data):
    return hashlib.sha256(data).hexdigest()

def archive(path, entries):
    with zipfile.ZipFile(path, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as output:
        for name, data in sorted(entries.items()):
            assert not Path(name).is_absolute() and '..' not in Path(name).parts
            output.writestr(name, data)
    with zipfile.ZipFile(path) as check, tempfile.TemporaryDirectory(prefix='ledger-package-') as clean:
        assert check.testzip() is None and set(check.namelist()) == set(entries)
        check.extractall(clean)
        for name, data in entries.items():
            assert (Path(clean) / name).read_bytes() == data

def main():
    dll = PROJECT / 'bin/Release/netstandard2.1/LedgerMod.dll'
    installed = GAME / 'BepInEx/plugins/LedgerMod/LedgerMod.dll'
    checks = json.loads((PROJECT / 'Verification/runtime-tests.json').read_text())
    compatibility = json.loads((PROJECT / 'Verification/compatibility-tests.json').read_text())
    assert checks['passed'] and compatibility['passed']
    assert checks['version'] == compatibility['version'] == VERSION
    assert checks['saveWritesBlocked'] and not checks['savedKingdomLoaded']
    runtimes = sorted((PROJECT / 'Verification').glob('Runtime-*'), key=lambda p:p.stat().st_mtime, reverse=True)
    tested = next(p for p in runtimes if (p/'_Mods/LedgerMod/Verification/runtime-tests.json').exists())
    assert dll.read_bytes() == installed.read_bytes() == (tested/'BepInEx/plugins/LedgerMod/LedgerMod.dll').read_bytes()
    assert 'LEDGER_RUNTIME_PASS' in (tested/'diagnostic.log').read_text()
    OUT.mkdir(parents=True, exist_ok=True)
    published_path = OUT/'NEXUS_RELEASE.json'
    published = json.loads(published_path.read_text(encoding='utf-8')) if published_path.exists() else {}
    destination = (f"Existing Nexus page: {published.get('pageUrl')}; API mod {published.get('apiModId')}, file chain {published.get('fileChainId')}. "
                   f"Published version remains {published.get('version')}; this {VERSION} package is local only.") if published else 'No Nexus destination recorded.'
    entries = {'BepInEx/plugins/LedgerMod/LedgerMod.dll': dll.read_bytes(),
               'Ledger README.txt': (PROJECT/'README.md').read_bytes(),
               'Ledger CHANGELOG.txt': (PROJECT/'Nexus/CHANGELOG.txt').read_bytes()}
    player = OUT / f'KingdomInc-Ledger-{VERSION}.zip'
    archive(player, entries)
    sources = list(PROJECT.glob('*.cs')) + [PROJECT/n for n in ['LedgerMod.csproj', 'README.md', '.gitignore', 'install.ps1', 'verify-isolated.ps1']]
    sources += list((PROJECT/'Nexus').glob('*.txt')) + [Path(__file__).resolve()]
    source = OUT / f'KingdomInc-Ledger-{VERSION}-source.zip'
    archive(source, {'_Mods/LedgerMod/'+p.relative_to(PROJECT).as_posix():p.read_bytes() for p in sources})
    for name in ['NEXUS_DESCRIPTION.bbcode.txt','CHANGELOG.txt']:
        (OUT/name).write_bytes((PROJECT/'Nexus'/name).read_bytes())
    report = {'name':'Ledger','version':VERSION,'publication':'local only',
              'gameVersion':checks['gameVersion'],'unityVersion':checks['unityVersion'],
              'runtimeChecksPassed':len(checks['checks']),'companionChecksPassed':len(compatibility['checks']),
              'testedAtUtc':checks['testedAtUtc'],'dllSha256':sha(dll.read_bytes()),
              'gameAssemblySha256':sha((GAME/'KingdomInc_Data/Managed/Assembly-CSharp.dll').read_bytes()),
              'playerArchiveFiles':sorted(entries),'cleanExtraction':'passed',
              'archives':{p.name:{'bytes':p.stat().st_size,'sha256':sha(p.read_bytes())} for p in [player,source]}}
    (OUT/'RELEASE_VERIFICATION.json').write_text(json.dumps(report,indent=2)+'\n')
    (OUT/'SHA256.json').write_text(json.dumps({p.name:sha(p.read_bytes()) for p in [player,source]},indent=2)+'\n')
    (PROJECT/'Releases/MOD-WORKFLOW.md').write_text(f'''# Ledger release workflow

Version: {VERSION}. Author: Urakka. Plugin: `urakka.kingdominc.ledgermod` / `LedgerMod.dll`.
Source: `{PROJECT}`. Repository naming convention: `urakkaamyx/KingdomInc_LedgerMod`; no remote repository created.
Publication scope: local build, installation and release preparation. {destination} Vortex: untested.

## Acceptance and implementation

- Zero-storage direct transfers, refusal when blocked/full/filtered/disconnected, independent matched lanes, cycle rejection and adjacent-node cleanup.
- Default 1/1, maximum 5/5, settings saved/copied without copying history. Width 384 and ports spaced 64 units apart match the native grid; height follows port rows and visible content with no six-row reservation. Legacy native IO loading preserves lane connections while normalizing port positions.
- Main-node item names/icons/counts, gear report and rolling second/minute/hour buckets. A slim node timer animates and wraps for the selected interval without resetting accounting; changing the period restarts it. The node list expands for additional types; no input/output footer, page counter or paging arrows remain.
- Observed resource/Forest/crafting costs, paid-batch conservation, explicit unknown history, actual Item/Luxury Shop earnings and no till double-counting.
- Actual Requisition Office, Disenchanter, Restaurant and Mercenary Guild income, receipt tracking through native storage/routing, proportional contract rewards, passive/pre-existing-income exclusion and manual/automatic tray collection are verified. Upstream native crafting currency rewards retain batch conservation. Full Requisition Offices refuse admission.
- Downstream native Enchanter spending follows measured-item receipts through storage/routing; native upgrade debits, failed-payment exclusion, multi-input/chained Ledger deduplication, output transformation/sale receipt continuity and productivity conservation are verified. Currency-only Enchanter rewards are recognized at completion. Upstream costs retain their existing pass-time attribution.
- Session-local cost provenance; unsupported transformations do not claim complete profit. Remove custom nodes before uninstalling.
- Independent settings overlay stays visible with native HUD canvases hidden; actual mouse clicks and screen bounds are covered.
- Native bronze/burgundy UI, icon item rows and a complete per-currency revenue/cost/net table. Gold uses gold/silver/copper denominations; both node and popup fit every affected currency. The detailed report shows negative red expenses and positive paper revenue. The node shows only combined profit/loss in a smaller two-column grid, filled left to right then down; partial history uses an estimate marker. Net retains its gain/loss colors. Only item and chain tabs remain. In-game screenshots in Verification; preview screenshots use isolated showcase data.
- Paid chains and recorded currency activity enable operating costs, revenue and net profit/loss without requiring a shop. Net subtracts all passed-item operating costs; free passthrough chains focus on items with no shop guidance. Paid recipes, native Barracks/Outfitter losses, multiple currencies, missing-cost safeguards and period expiry are verified.
- Outfitter retains both input histories between intake/completion updates, with successive equipment, duplicate and timeout cases verified. Connected-chain traversal supplies upstream context; reports omit routing/storage and unobserved placeholder rows. Unsupported utilities retain known history.
- Native recipe spending in all nine currencies, exact denomination display, all visible node/table rows, hidden routing/placeholders, removal of Outputs, loaded coin sprites and screen bounds are verified.
- Native new-node placement opens settings before Unity Start without a null UI or half-created popup. Immediate configuration, exactly one gear/display after Start, correct model changes and pointer-driven gear reopening are covered.

## Actual checks

Build: `dotnet build _Mods/LedgerMod/LedgerMod.csproj -c Release --nologo` (passed).
Runtime: `& ./_Mods/LedgerMod/verify-isolated.ps1` (passed).
Companions: `& ./_Mods/LedgerMod/verify-isolated.ps1 -WithExistingPlugins` (passed with PartyNode, SharableBlueprints, KingdomFactoryApi).
Checks: {report['runtimeChecksPassed']} runtime / {report['companionChecksPassed']} companion; exact names in Verification reports.
Actual game: {checks['gameVersion']}; Unity: {checks['unityVersion']}; local loader BepInEx 5.4.23.6.
Verification timestamp UTC: {checks['testedAtUtc']}. Disposable kingdom; saves, latest-save loading, achievements and Steam initialization blocked.
No normal user game was terminated or loaded by diagnostics. Save compatibility checked through native node serialization/copy; accounting deliberately resets on load.

## Installation and package

Install: `& ./_Mods/LedgerMod/install.ps1 -StageForNextLaunch`; previous mapped DLL preserved outside plugin discovery, fresh DLL staged for restart.
Package: `python _Mods/LedgerMod/Tools/package_nexus.py`.
Built, tested and installed DLL bytes matched; ZIP entry inspection and clean extraction passed.
No game/loader assets, saves, logs, private configuration, or diagnostic runtimes packaged.
Player archive: `{player}` ({player.stat().st_size} bytes).
SHA-256: `{sha(player.read_bytes())}`.
Source archive: `{source}` ({source.stat().st_size} bytes).
SHA-256: `{sha(source.read_bytes())}`.
DLL SHA-256: `{report['dllSha256']}`.

## Listing and publication

Draft: Nexus/NEXUS_DESCRIPTION.bbcode.txt. Changelog: Nexus/CHANGELOG.txt.
Screenshots: Verification/ledger-node.png, ledger-chain-report.png, ledger-currencies-preview.png, ledger-all-currencies-node.png, ledger-five-pairs.png.
Native game UI assets are referenced at runtime and not bundled. The existing shared-template Ledger cover remains in Assets/Thumbnails. README contains install, update/removal, credits and limits.
{destination} No upload or listing mutation was performed while preparing this version. Published file/media states remain recorded in Nexus/NEXUS_RELEASE.json and Nexus/NEXUS_MEDIA.json.
''', encoding='utf-8')
    print(json.dumps(report,indent=2))

if __name__=='__main__':
    main()
