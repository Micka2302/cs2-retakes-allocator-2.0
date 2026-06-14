# Changelog

## Unreleased

### Added
- Added SharpModMenu/CSSUniversalMenuAPI support for the in-game loadout menu.
- Added `EnableBuyMenu` config option to allow or block `buy`, `autobuy`, `rebuy`, and ammo buy commands.
- Added `css_enemy` command to toggle enemy weapon preferences with `disable`, `t`, `ct`, or `both`.
- Added localized command, vote, menu, center announcement, and weapon preference messages.
- Added HE grenade alias handling so `HE` and `HEGrenade` are treated as the same utility.

### Changed
- Replaced the embedded KitsuneMenu integration with SharpModMenu.
- Updated release packaging to include SharpModMenu, CSSUniversalMenuAPI, and SharpModMenu config files.
- Reworked the loadout menu lifecycle so menus are tracked, closed on disconnect/spawn, and cleaned up on unload.
- Improved weapon preference feedback with shorter colorized messages and readable round/team names.
- Updated bombsite announcements to avoid stale center messages and use translated chat output.
- Updated README installation and configuration notes for SharpModMenu and `EnableBuyMenu`.

### Fixed
- Fixed HE grenade matching in utility allocation and weapon acquire checks.
- Fixed utility acquire handling so unallocated utility is blocked consistently.
- Fixed vote, reload, and config command responses to use translations.
- Fixed weapon preference messages displaying enum values instead of player-facing weapon names.

### Removed
- Removed bundled KitsuneMenu source files and the KitsuneMenu shared DLL from the release package.

### Tests
- Updated config and weapon selection tests for the new messages.
- Added coverage for HE/HEGrenade utility alias behavior.
