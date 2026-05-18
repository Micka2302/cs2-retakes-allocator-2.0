# CS2 Retakes Allocator

Allocator plugin that runs alongside B3none's [cs2-retakes](https://github.com/b3none/cs2-retakes). It picks round types, gives players the right loadouts, and handles sniper queues, enemy-weapon swaps, and Zeus preferences.

## What's new in 2.6
- Center-screen Kitsune loadout menu (`guns`, `!guns`, `/guns`) for primaries, pistols, sniper choice, enemy weapons, and Zeus.
- Sniper system reworked: separate AWP and SSG queues with per-queue access mode (disabled/everyone/VIP), per-team caps and minimum player gates, random sniper option, auto-snipers counted in the AWP queue.
- Enemy-weapon and Zeus preferences now have permissions, per-team limits, and menu controls.
- Config file is category-based (legacy keys auto-converted). Shotguns/SMGs can be added to full-buy pools; gun commands can be toggled.
- Optional bombsite HUD/chat announcements and signature auto-update switches under `Config`.

## Requirements
- CounterStrikeSharp server.
- B3none's cs2-retakes with `EnableFallbackAllocation` disabled.
- Release zip from this repo (includes sqlite runtimes and KitsuneMenu DLL).

## Installation
1. Stop the server.
2. Download the latest release from this repo.
3. Copy the zip contents into `game/csgo/addons/counterstrikesharp/plugins/RetakesAllocator/` (keep the `runtimes/` folder).
4. Start once to generate `config/config.json`, then edit it (see below).
5. Optional buy-menu support: in `game/csgo/cfg/cs2-retakes/retakes.cfg` set  
   `mp_buy_anywhere 1`, `mp_buytime 60000`, `mp_maxmoney 65535`, `mp_startmoney 65535`, `mp_afterroundmoney 65535`.

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
- **Loadout menu (Kitsune)**: type `guns`, `!guns`, `/guns`, or `!gun` (configured by `Config.InGameGunMenuCenterCommands`) to open a center-screen menu. It sets primary, secondary, pistol, sniper preference (AWP / SSG / Random / Disabled), enemy-weapon preference (Off / T / CT / Both), and Zeus toggle. Changes apply on the next round.
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

- **Config**: `ResetStateOnGameRestart`, `AllowAllocationAfterFreezeTime`, `UseOnTickFeatures`, round/bombsite announcements (`EnableRoundTypeAnnouncement`, center HUD options), menu triggers (`InGameGunMenuCenterCommands`), command toggle (`GunCommandsEnabled`), log level, chat prefix/name, signature controls (`EnableCanAcquireHook`, `AutoUpdateSignatures`, `CapabilityWeaponPaints`), migrations.
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
- `compile.ps1` (or `compile.cmd`) builds and copies the plugin; set `CopyPath` to push to a running server (Windows only).
- Run a local dedicated server with  
  `start cs2.exe -dedicated -insecure +game_type 0 +game_mode 0 +map de_dust2 +servercfgfile server.cfg`.
