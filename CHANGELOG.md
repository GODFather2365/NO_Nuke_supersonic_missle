# Changelog

## [1.2.0] - 2026-10-05
### Added
- Nuclear warhead X-77N (500 kt): BlastYieldKg 700 kg → 500_000_000 kg TNT-equivalent, info.nuclear=true, ammo per mount via NuclearAmmo=1.
- WarewindShockwaveFx: scales cloned Shockwave rings (radius/lifetime/height) for the 500 kt blast; templates stay untouched.
- Tuning constants: NukeYieldThresholdKg=200, ShockwaveScaleMult=6, ShockwaveMaxRadiusCapM=3000, ShockwaveMaxLifetimeS=8, NuclearAmmo=1.
### Changed
- Names: "X-77N Warewind (500Kt)" (WeaponInfoName/MountDisplayName/UnitName), ShortName "X-77N", BogeyName "Warewind Nuke"; nuclear description in encyclopedia/mount tooltip; HUD "HE:" → "NUKE:".
- TBM FX slots now receive runtime clones (" (X77N)") instead of shared BallisticMissile1 template objects.

### Changed

- **Blueprinter 2.0.1:** bootstrap waits `PatchRunner.ApplyAllOps` instead of removed `PatchingComplete`; build references `Blueprinter_2.0.1.dll`.

> [!IMPORTANT]
> Requires **[Blueprinter 2.0.1+](https://github.com/nikkorap/NOBlueprinter-Releases)**. Remove legacy `BepInEx/plugins/Blueprinter.dll` (1.8.x) if both are installed.

## [1.1.2] - 2026-08-27

### Fixed

- Aircraft RCS no longer increases when Warewind is loaded in internal bays (`hardpoint.bayDoors`); external pylons still add mount RCS as before

## [1.1.1] - 2026-08-25

### Fixed

- Kill feed / HQ identity: stamp Warewind definition on shared AAM2 shell before Instantiation; re-apply on `UnitRegistry.RegisterUnit`
- Spawn prefab stamp gated around `Spawner.SpawnMissile` so pending Fire cannot leak AAM-36 visuals

### Changed

- Survivability: copy armor / hitpoints from vanilla Piledriver TBM (`BallisticMissile1`) instead of hardcoded body values
- EW jam: sticky single radar lock, independent IR flare path, **5 s cooldown** after lock drop
- Threat scan: prefer sticky radar lock; IR scanned separately so flares are not blocked by a closer SAM

## [1.0.1] - 2026-08-22

### Fixed

- Intermittent post-launch AAM-36 identity/visual leak on shared vanilla AAM2 shell (multi-pylon / lost Pending spawn token)
- Shared `WeaponInfo` + `sortWeapons` on mount spawn; rescue `Claim` when AAM2 shell spawns within Fire window
- Force Warewind `weaponPrefab` on Fire; bootstrap resolves AAM2 only (no generic AAM fallback)

## [1.0.0] - 2026-08-19

First public release.

### Added

- X-77 Warewind standalone BepInEx plugin (`com.mursisru.x77warewind`)
- Two-stage hypersonic flight profile: Drop → Align → Loft → Cruise → Dive
- Stage-1 TWR punch (10× for 5 s), optical guidance, 700 kg HE, 2800 kg launch mass
- Custom `WarewindVisual` bundle (`X77Warewind.nobp`) with Blender 1:1 materials
- Motor exhaust FX (TBM booster) and trail particles on engine sockets
- Combat HUD weapon preview (`PreviewWarewind.png`)
- Add-only Darkreach / Alkyon HE Piledriver slots; Alkyon bay fit with dorsal sink
- Flares (15 km gate), EW jam, survivability overrides, HUD range tune

### Notes

- Requires **BepInEx 5** and **Blueprinter**
- Json keys unchanged: `missilepack_x77_warewind` / `MissilePack_X77_Warewind_single`

## [1.1.0] - 2026-08-17

### Changed

- Split out of MissilePack into a standalone X-77 Warewind plugin (`com.mursisru.x77warewind`)
- Plugin folder `BepInEx/plugins/X-77-Warewind/`, bundle `X77Warewind.nobp`

Json keys are unchanged (`missilepack_x77_warewind`).

### Added (from MissilePack 1.1.0)

- Two-stage hypersonic (Optical HUD, 700 kg HE, mass 2800 kg)
- Shared vanilla AAM2 `unitPrefab` spawn contract; stamp `WarewindVisual` after Spawn
- Fire-and-forget: Drop → Loft → 50 km cruise → Dive
- Dual motors, stage-1 mesh sep, flares / EW, DockingPort eject
