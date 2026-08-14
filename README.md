# CS2 Retakes Allocator

Allocator plugin that runs alongside B3none's [cs2-retakes](https://github.com/b3none/cs2-retakes). It picks round types, gives players the right loadouts, and handles sniper queues, enemy-weapon swaps, and Zeus preferences.

## What's new in 2.6
- Absynthium_Menu loadout menu (`guns`, `!guns`, `/guns`) for primaries, pistols, sniper choice, enemy weapons, and Zeus.
- Sniper system reworked: separate AWP and SSG queues with per-queue access mode (disabled/everyone/VIP), per-team caps and minimum player gates, random sniper option, auto-snipers counted in the AWP queue.
- Enemy-weapon and Zeus preferences now have permissions, per-team limits, and menu controls.
- Config file is category-based (legacy keys auto-converted). Shotguns/SMGs can be added to full-buy pools; gun commands can be toggled.
- Optional bombsite HUD/chat announcements and signature auto-update switches under `Config`.
- Native SQLite now uses the operating-system library, avoiding `GLIBC_2.33` errors on older Linux servers.

## Requirements

### Install separately

- [Metamod:Source](https://www.sourcemm.net/downloads.php?branch=master) and [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) 1.0.371 or newer. The plugin targets .NET 10; use a CounterStrikeSharp package that includes a compatible runtime, or install the .NET 10 runtime separately.
- B3none's [cs2-retakes](https://github.com/b3none/cs2-retakes), with `EnableFallbackAllocation` disabled so both plugins do not allocate weapons simultaneously.
- Linux with the default SQLite configuration: the system package that provides `libsqlite3.so.0` (`libsqlite3-0` on Debian/Ubuntu or `sqlite-libs` on RHEL-compatible distributions). Windows uses the system `winsqlite3.dll` and needs no additional SQLite package.
- For SVG button images: [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager) and the [Absynthium client Workshop addon](https://steamcommunity.com/sharedfiles/filedetails/?id=3782333430). The menu can run without the Workshop addon, but its button images will not be displayed.
- Optional: a MySQL/MariaDB server when `Database.DatabaseProvider` is set to `MySql`. SQLite is the default and does not require a separate database server.

### Included in `RetakesAllocator.zip`

Do not download these components separately; the release archive installs compatible copies together:

- `RetakesAllocator` and its managed database dependencies.
- `Absynthium_MenuCore` and `Absynthium_MenuApi`.
- PlayerSettings 0.9.4 and `PlayerSettingsApi`, used to persist each player's selected menu type.
- AnyBaseLib 0.9.4 and the runtime files required by PlayerSettings.
- An `Absynthium_MenuCore.example.json` configuration enabling Workshop SVG images with an AZERTY layout.

If these dependencies are already installed, replace them with the copies from the same release ZIP to avoid API/version mismatches.

## Installation

### Fresh installation

1. Stop the server.
2. Install or update Metamod:Source, CounterStrikeSharp 1.0.371+, and B3none's cs2-retakes.
3. Download `RetakesAllocator.zip` from the latest GitHub release.
4. Extract the ZIP directly into `game/csgo/addons/counterstrikesharp/`. Keep its directory structure intact. After extraction, the important directories are:

   ```text
   configs/plugins/Absynthium_MenuCore/
   plugins/Absynthium_MenuCore/
   plugins/PlayerSettings/
   plugins/RetakesAllocator/
   shared/Absynthium_MenuApi/
   shared/AnyBaseLib/
   shared/PlayerSettingsApi/
   ```

5. On a Debian/Ubuntu Linux server using SQLite, install the system library:

   ```bash
   sudo apt-get update
   sudo apt-get install libsqlite3-0
   ```

   On RHEL-compatible distributions, install `sqlite-libs` instead. This step is not needed on Windows or when the allocator is configured exclusively for MySQL.

6. Enable the included menu's Workshop button images:

   - Install MultiAddonManager and verify that `meta list` shows it as loaded.
   - Add the Workshop ID to `game/csgo/cfg/multiaddonmanager/multiaddonmanager.cfg`:

     ```cfg
     mm_client_extra_addons "3782333430"
     ```

   - If `mm_client_extra_addons` already contains IDs, append `3782333430` with a comma instead of adding a second setting.
   - Copy `Absynthium_MenuCore.example.json` to `Absynthium_MenuCore.json` in `game/csgo/addons/counterstrikesharp/configs/plugins/Absynthium_MenuCore/`. The example enables Workshop images and defaults to `KeyboardLayout: "AZERTY"`; change it to `QWERTY` when appropriate.

7. In `game/csgo/cfg/cs2-retakes/retakes.cfg`, set `EnableFallbackAllocation` to `false` so cs2-retakes does not compete with RetakesAllocator.
8. Start the server. RetakesAllocator creates `game/csgo/addons/counterstrikesharp/configs/plugins/RetakesAllocator/config.json`; stop the server, edit it, and start the server again.
9. Verify the installation:

   - Run `css_plugins list` in the server console and confirm that RetakesAllocator, Absynthium_Menu Core, and PlayerSettings are loaded.
   - Run `!menus` in game to choose `ButtonMenu`.
   - Run `!testmenu` as a player with `@css/root` to verify the Workshop images.
   - Run `!guns` to open the allocator loadout menu.

10. Optional buy-menu support: in `game/csgo/cfg/cs2-retakes/retakes.cfg` set
   `mp_buy_anywhere 1`, `mp_buytime 60000`, `mp_maxmoney 65535`, `mp_startmoney 65535`, `mp_afterroundmoney 65535`.

### Updating an existing installation

1. Stop the server and back up the RetakesAllocator and Absynthium_Menu configuration files, plus the SQLite database if used.
2. Replace the existing `plugins/RetakesAllocator`, `plugins/Absynthium_MenuCore`, `plugins/PlayerSettings`, `shared/Absynthium_MenuApi`, `shared/AnyBaseLib`, and `shared/PlayerSettingsApi` directories with those from the new ZIP. Do not delete the directories under `configs/plugins/`.
3. When upgrading from the former SharpModMenu integration, remove its old files only if no other installed plugin still uses them.
4. Recheck the MultiAddonManager Workshop ID and the Absynthium_Menu settings, then restart the server.

### Menu troubleshooting

- `Absynthium_Menu Core was not found`: reinstall every bundled `plugins/` and `shared/` dependency from the same ZIP, then perform a full server restart rather than a hot reload.
- Menu text appears but SVG buttons do not: check `UseWorkshopButtonImages`, `ButtonImagePath`, `KeyboardLayout`, MultiAddonManager, and Workshop ID `3782333430`; reconnect clients after the addon has downloaded.
- `libsqlite3.so.0` cannot be loaded on Linux: install the distribution's SQLite runtime package. RetakesAllocator no longer loads its former `libe_sqlite3.so`, which required `GLIBC_2.33` on the affected servers.
- Menu choice is not remembered: verify that PlayerSettings and AnyBaseLib are loaded from the bundled `plugins/` and `shared/` directories.

## How allocation works
### Round types
- **Pistol**: pistols only, kevlar, no helmet; one CT gets a defuse kit.
- **HalfBuy**: SMGs/shotguns, kevlar+helmet, one nade plus 50% chance of a second; all CTs get kits.
- **FullBuy**: rifles/snipers/heavies (SMGs/shotguns can be added), kevlar+helmet, one nade plus 50% chance of a second; all CTs get kits.
Round order can be `Random` (weighted), `RandomFixedCounts`, or `ManualOrdering`.

### Weapon allocation order
1. Player preference (saved per SteamID, applied on the next allocation; random sniper preference resolved here).
2. Random pick if `AllowedWeaponSelectionTypes` includes `Random`.
3. Default weapon per team/allocation type.
`EnableAllWeaponsForEveryone` lets teams use each other's primaries. `EnableWeaponShotguns` and `EnableWeaponPms` expand full-buy pools with shotguns and SMGs. Preferences never swap weapons mid-round.

### Player controls
- **Loadout menu (Absynthium_Menu)**: type `guns`, `!guns`, `/guns`, or `!gun` (configured by `Config.InGameGunMenuCenterCommands`) to open the SVG button menu. It sets primary, secondary, pistol, sniper preference (AWP / SSG / Random / Disabled), enemy-weapon preference (Off / T / CT / Both), and Zeus toggle. Changes apply on the next round.
- **Quick commands** (disable with `GunCommandsEnabled`):
  - `!gun <weapon> [T|CT]` / `!removegun <weapon> [T|CT]`
  - `!awp`, `!ssg`, `!zeus`
  - `!nextround` (vote), `!setnextround <P|H|F>` (admin)
  - `!reload_allocator_config`, `!print_config <section>`

### Sniper system (AWP / SSG)
- Round availability is configurable per queue with `EnableRoundType`: `1` = HalfBuy, `2` = FullBuy, `3` = both.
- Two queues:
  - **AWP/Auto-sniper queue**: AWP, G3SG1/SCAR-20, or Random preferences.
  - **SSG queue**: SSG or Random preferences (players already given an AWP/auto are skipped).
- Access mode per queue: `EnableAwp` / `EnableSsg` = `0` (off), `1` (everyone), `2` (requires `AwpPermission` / `SsgPermission`).
- Roll per queue: `ChanceForAwpWeapon` and `ChanceForSsgWeapon` (0-100).
- Gates: `MinPlayersPerTeamForAwpWeapon` / `...SsgWeapon`; caps: `MaxAwpWeaponsPerTeam` / `MaxSsgWeaponsPerTeam`.
- Selection order: AWP queue rolls first, then SSG fills remaining sniper requests. The Random preference resolves to the queue that selects the player. Legacy sniper keys are converted automatically.

### Enemy weapons
- Players opt in per team or both via the loadout menu.
- Server controls: `EnableEnemyStuff` (0/1/2 with permission), `EnemyStuffPermission`, `ChanceForEnemyStuff`, `MaxEnemyStuffPerTeam` (-1 for unlimited). The loadout is swapped for an enemy-team equivalent when the roll succeeds and the team quota allows it.

### Zeus
- `EnableZeus` (0 disables, >0 enables), `ChanceForZeusWeapon`, `MaxZeusPerTeam` per side.
- Toggle via the loadout menu or `!zeus`. Zeus rolls after primary/secondary allocation.

### Nades
- `Nades.MaxNades` caps per nade type, per team, optionally per map (GLOBAL fallback).
- `Nades.MaxTeamNades` caps total nades per team per round type (`One`, `Two`, ... `Ten`, or per-player averages). Map-specific overrides share the same shape.
- Incendiary/Molotov keys are normalized automatically.

## Configuration
Config lives in `addons/counterstrikesharp/configs/plugins/RetakesAllocator/config.json` (created on first run). The file is category-based; omitted fields keep defaults.

- **Config**: `ResetStateOnGameRestart`, `AllowAllocationAfterFreezeTime`, `UseOnTickFeatures`, round/bombsite announcements (`EnableRoundTypeAnnouncement`, center HUD options), menu triggers (`InGameGunMenuCenterCommands`), command toggles (`GunCommandsEnabled`, `EnableBuyMenu`), log level, chat prefix/name, signature controls (`EnableCanAcquireHook`, `AutoUpdateSignatures`, `CapabilityWeaponPaints`), migrations.
- **RoundTypes**: `RoundTypeSelection`, `RoundTypePercentages`, `RoundTypeRandomFixedCounts`, `RoundTypeManualOrdering`.
- **Weapons**: `UsableWeapons`, `AllowedWeaponSelectionTypes`, `DefaultWeapons`, `EnableAllWeaponsForEveryone`, `EnableWeaponShotguns`, `EnableWeaponPms`.
- **AWP / SSG**: access mode, round availability, permissions, chances, per-team minimums and caps.
- **EnemyStuff**: access mode, permission, chance, per-team limits.
- **Zeus**: enable flag, chance, per-team caps.
- **Nades**: `MaxNades`, `MaxTeamNades`.
- **Database**: provider (`Sqlite`/`MySql`), connection string, migration toggle.

Minimal categorized example:
```json
{
  "Config": {
    "EnableRoundTypeAnnouncement": true,
    "ChatMessagePluginName": "Retakes",
    "InGameGunMenuCenterCommands": "guns,!guns,/guns"
  },
  "RoundTypes": { "RoundTypeSelection": "Random" },
  "Weapons": { "EnableAllWeaponsForEveryone": false },
  "AWP": { "EnableAwp": 2, "ChanceForAwpWeapon": 100 },
  "SSG": { "EnableSsg": 2, "ChanceForSsgWeapon": 100 }
}
```

Full configuration example (current Absynthium server):
```json
{
  "Config": {
    "ResetStateOnGameRestart": true,
    "AllowAllocationAfterFreezeTime": true,
    "UseOnTickFeatures": true,
    "CapabilityWeaponPaints": true,
    "GunCommandsEnabled": true,
    "EnableRoundTypeAnnouncement": true,
    "EnableRoundTypeAnnouncementCenter": false,
    "EnableBombSiteAnnouncementCenter": false,
    "BombSiteAnnouncementCenterToCTOnly": false,
    "DisableDefaultBombPlantedCenterMessage": false,
    "ForceCloseBombSiteAnnouncementCenterOnPlant": true,
    "BombSiteAnnouncementCenterDelay": 1,
    "BombSiteAnnouncementCenterShowTimer": 5,
    "EnableBombSiteAnnouncementChat": false,
    "EnableNextRoundTypeVoting": false,
    "EnableBuyMenu": 1,
    "EnableCanAcquireHook": true,
    "LogLevel": "Information",
    "ChatMessagePluginName": "Absynthium - Retakes",
    "ChatMessagePluginPrefix": "[GREEN][Absynthium - Retakes][WHITE] ",
    "InGameGunMenuCenterCommands": "guns,!guns,/guns,gun,!gun,!gun",
    "AutoUpdateSignatures": true
  },
  "RoundTypes": {
    "RoundTypeSelection": "ManualOrdering",
    "RoundTypePercentages": {
      "Pistol": 15,
      "HalfBuy": 25,
      "FullBuy": 60
    },
    "RoundTypeRandomFixedCounts": {
      "Pistol": 5,
      "HalfBuy": 10,
      "FullBuy": 15
    },
    "RoundTypeManualOrdering": [
      {
        "Type": "Pistol",
        "Count": 5
      },
      {
        "Type": "HalfBuy",
        "Count": 0
      },
      {
        "Type": "FullBuy",
        "Count": 200
      }
    ]
  },
  "Weapons": {
    "UsableWeapons": [
      "Deagle",
      "Glock",
      "USPS",
      "HKP2000",
      "Elite",
      "Tec9",
      "P250",
      "CZ",
      "FiveSeven",
      "Revolver",
      "Mac10",
      "MP9",
      "MP7",
      "P90",
      "MP5SD",
      "Bizon",
      "UMP45",
      "XM1014",
      "Nova",
      "MAG7",
      "SawedOff",
      "AK47",
      "M4A1S",
      "M4A1",
      "GalilAR",
      "Famas",
      "SG556",
      "AWP",
      "AUG",
      "SSG08"
    ],
    "AllowedWeaponSelectionTypes": [
      "PlayerChoice",
      "Default"
    ],
    "DefaultWeapons": {
      "Terrorist": {
        "FullBuyPrimary": "AK47",
        "HalfBuyPrimary": "Mac10",
        "Secondary": "Deagle",
        "PistolRound": "Glock"
      },
      "CounterTerrorist": {
        "FullBuyPrimary": "M4A1S",
        "HalfBuyPrimary": "MP9",
        "Secondary": "Deagle",
        "PistolRound": "USPS"
      }
    },
    "EnableAllWeaponsForEveryone": false,
    "EnableWeaponShotguns": true,
    "EnableWeaponPms": true
  },
  "Nades": {
    "MaxNades": {
      "de_dust2": {
        "Terrorist": {
          "Flashbang": 1,
          "Smoke": 0,
          "Molotov": 1,
          "HighExplosive": 1
        },
        "CounterTerrorist": {
          "Flashbang": 1,
          "Smoke": 1,
          "Molotov": 1,
          "HighExplosive": 2
        }
      },
      "de_mirage": {
        "Terrorist": {
          "Flashbang": 1,
          "Smoke": 0,
          "Molotov": 1,
          "HighExplosive": 1
        },
        "CounterTerrorist": {
          "Flashbang": 1,
          "Smoke": 1,
          "Molotov": 1,
          "HighExplosive": 2
        }
      },
      "de_inferno": {
        "Terrorist": {
          "Flashbang": 1,
          "Smoke": 0,
          "Molotov": 0,
          "HighExplosive": 1
        },
        "CounterTerrorist": {
          "Flashbang": 1,
          "Smoke": 1,
          "Molotov": 1,
          "HighExplosive": 2
        }
      },
      "de_overpass": {
        "Terrorist": {
          "Flashbang": 1,
          "Smoke": 0,
          "Molotov": 1,
          "HighExplosive": 1
        },
        "CounterTerrorist": {
          "Flashbang": 1,
          "Smoke": 1,
          "Molotov": 1,
          "HighExplosive": 1
        }
      },
      "de_nuke": {
        "Terrorist": {
          "Flashbang": 1,
          "Smoke": 0,
          "Molotov": 1,
          "HighExplosive": 1
        },
        "CounterTerrorist": {
          "Flashbang": 1,
          "Smoke": 1,
          "Molotov": 1,
          "HighExplosive": 2
        }
      },
      "de_vertigo": {
        "Terrorist": {
          "Flashbang": 1,
          "Smoke": 0,
          "Molotov": 1,
          "HighExplosive": 2
        },
        "CounterTerrorist": {
          "Flashbang": 1,
          "Smoke": 1,
          "Molotov": 1,
          "HighExplosive": 2
        }
      },
      "de_ancient": {
        "Terrorist": {
          "Flashbang": 1,
          "Smoke": 0,
          "Molotov": 1,
          "HighExplosive": 1
        },
        "CounterTerrorist": {
          "Flashbang": 1,
          "Smoke": 1,
          "Molotov": 1,
          "HighExplosive": 1
        }
      },
      "de_ancient_night": {
        "Terrorist": {
          "Flashbang": 1,
          "Smoke": 0,
          "Molotov": 1,
          "HighExplosive": 1
        },
        "CounterTerrorist": {
          "Flashbang": 1,
          "Smoke": 1,
          "Molotov": 1,
          "HighExplosive": 1
        }
      },
      "de_anubis": {
        "Terrorist": {
          "Flashbang": 1,
          "Smoke": 0,
          "Molotov": 1,
          "HighExplosive": 1
        },
        "CounterTerrorist": {
          "Flashbang": 1,
          "Smoke": 1,
          "Molotov": 1,
          "HighExplosive": 1
        }
      },
      "de_train": {
        "Terrorist": {
          "Flashbang": 1,
          "Smoke": 0,
          "Molotov": 1,
          "HighExplosive": 1
        },
        "CounterTerrorist": {
          "Flashbang": 1,
          "Smoke": 1,
          "Molotov": 1,
          "HighExplosive": 1
        }
      }
    },
    "MaxTeamNades": {
      "GLOBAL": {
        "Terrorist": {
          "Pistol": "AverageOnePerPlayer",
          "HalfBuy": "AverageOnePerPlayer",
          "FullBuy": "AverageOnePerPlayer"
        },
        "CounterTerrorist": {
          "Pistol": "AverageOnePerPlayer",
          "HalfBuy": "AverageOnePerPlayer",
          "FullBuy": "AverageOnePerPlayer"
        }
      }
    }
  },
  "AWP": {
    "EnableAwp": 2,
    "EnableRoundType": 2,
    "AwpPermission": "@css/vip",
    "ChanceForAwpWeapon": 50,
    "MaxAwpWeaponsPerTeam": {
      "Terrorist": 1,
      "CounterTerrorist": 1
    },
    "MinPlayersPerTeamForAwpWeapon": {
      "Terrorist": 1,
      "CounterTerrorist": 1
    }
  },
  "SSG": {
    "EnableSsg": 2,
    "EnableRoundType": 2,
    "SsgPermission": "@css/vip",
    "ChanceForSsgWeapon": 50,
    "MaxSsgWeaponsPerTeam": {
      "Terrorist": 1,
      "CounterTerrorist": 1
    },
    "MinPlayersPerTeamForSsgWeapon": {
      "Terrorist": 1,
      "CounterTerrorist": 1
    }
  },
  "EnemyStuff": {
    "EnableEnemyStuff": 2,
    "EnemyStuffPermission": "@abs/premium",
    "ChanceForEnemyStuff": 20,
    "MaxEnemyStuffPerTeam": {
      "Terrorist": 1,
      "CounterTerrorist": 1
    }
  },
  "Zeus": {
    "EnableZeus": 2,
    "ChanceForZeusWeapon": 50,
    "MaxZeusPerTeam": {
      "Terrorist": 2,
      "CounterTerrorist": 2
    }
  },
  "Database": {
    "DatabaseProvider": "MySql",
    "DatabaseConnectionString": "Server=127.0.0.1;Port=3306;Database=xxxx;Uid=absynthium;Pwd=xxxxx",
    "MigrateOnStartup": true
  }
}
```

## Game data / signatures
The plugin relies on custom signatures for `GetCSWeaponDataFromKey`, `CCSPlayer_ItemServices_CanAcquire`, and `GiveNamedItem2`.
- `AutoUpdateSignatures: true` downloads updated gamedata on startup (recommended).
- If disabled, place `RetakesAllocator_gamedata.json` in `RetakesAllocator/gamedata/` yourself.
- `CapabilityWeaponPaints` and `EnableCanAcquireHook` depend on the custom gamedata.

## Build / dev
- Install the .NET 10 SDK and keep the `Absynthium_Menu` repository next to this repository.
- `compile.ps1` (or `compile.cmd`) rebuilds Absynthium_Menu, builds the plugin, and creates a complete upload-ready ZIP. Set `ABSYNTHIUM_MENU_ROOT` if the menu repository is elsewhere. PlayerSettings 0.9.4 and AnyBaseLib 0.9.4 are downloaded from their official releases and verified by SHA-256.
- Set `CopyPath` to push a Debug build to a running server (Windows only).
- Run a local dedicated server with  
  `start cs2.exe -dedicated -insecure +game_type 0 +game_mode 0 +map de_dust2 +servercfgfile server.cfg`.
