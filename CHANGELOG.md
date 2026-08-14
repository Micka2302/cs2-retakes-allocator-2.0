# Changelog

## Unreleased

### Added
- Added Absynthium_Menu support for the in-game loadout menu, including SVG button rendering.
- Added a complete dependency, fresh-install, upgrade, verification, and troubleshooting guide for the bundled Absynthium_Menu stack.
- Added `EnableBuyMenu` config option to allow or block `buy`, `autobuy`, `rebuy`, and ammo buy commands.
- Added `css_enemy` command to toggle enemy weapon preferences with `disable`, `t`, `ct`, or `both`.
- Added localized command, vote, menu, center announcement, and weapon preference messages.
- Added HE grenade alias handling so `HE` and `HEGrenade` are treated as the same utility.

### Changed
- Replaced the SharpModMenu/CSSUniversalMenuAPI integration with Absynthium_Menu.
- Updated release packaging to include Absynthium_Menu, PlayerSettings, AnyBaseLib, their runtime dependencies, and an SVG-enabled AZERTY example config.
- Updated the plugin runtime requirement to CounterStrikeSharp 1.0.371 and .NET 10.
- Updated all solution projects and MigrationRunner to target .NET 10.
- Replaced the allocator's bundled SQLite native library with the operating-system SQLite provider (`libsqlite3.so.0` on Linux and `winsqlite3.dll` on Windows).
- Replaced `Microsoft.EntityFrameworkCore.Sqlite` with `Microsoft.EntityFrameworkCore.Sqlite.Core` and explicit SQLitePCLRaw system providers.
- Aligned the Absynthium_Menu SVG resource path with the Workshop VPK (`s2r://panorama/images/menu_buttons/*.vsvg`) and the client addon `3782333430`.
- Reworked the loadout menu lifecycle so menus are tracked, closed on disconnect/spawn, and cleaned up on unload.
- Improved weapon preference feedback with shorter colorized messages and readable round/team names.
- Updated bombsite announcements to avoid stale center messages and use translated chat output.
- Updated README installation and configuration notes for Absynthium_Menu, Workshop SVG images, and `EnableBuyMenu`.

### Fixed
- Fixed `DllNotFoundException` on Linux servers running glibc older than 2.33.
- Fixed the `NU1903` warning caused by the vulnerable `SQLitePCLRaw.lib.e_sqlite3` 2.1.7 dependency in RetakesAllocator.
- Fixed HE grenade matching in utility allocation and weapon acquire checks.
- Fixed utility acquire handling so unallocated utility is blocked consistently.
- Fixed vote, reload, and config command responses to use translations.
- Fixed weapon preference messages displaying enum values instead of player-facing weapon names.

### Removed
- Removed SharpModMenu and CSSUniversalMenuAPI from the release package.

### Tests
- Updated config and weapon selection tests for the new messages.
- Added coverage for HE/HEGrenade utility alias behavior.
