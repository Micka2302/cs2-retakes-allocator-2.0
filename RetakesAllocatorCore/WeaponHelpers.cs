using System;
using System.Collections;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorCore;

public enum WeaponAllocationType
{
    FullBuyPrimary,
    HalfBuyPrimary,
    Secondary,
    PistolRound,

    // eg. AWP is a preferred gun - you cant always get it even if its your preference
    // Right now its only snipers, but if we make this configurable, we need to change:
    // - CoercePreferredTeam
    // - "your turn" wording in the weapon command handler
    Preferred,
}

public enum ItemSlotType
{
    Primary,
    Secondary,
    Util
}

public readonly struct WeaponSelectionResult
{
    public WeaponSelectionResult(ICollection<CsItem> weapons, bool enemyStuffGranted)
    {
        Weapons = weapons;
        EnemyStuffGranted = enemyStuffGranted;
    }

    public ICollection<CsItem> Weapons { get; }
    public bool EnemyStuffGranted { get; }
}

public static class WeaponHelpers
{
    private static readonly ICollection<CsItem> _sharedPistols = new HashSet<CsItem>
    {
        CsItem.Deagle,
        CsItem.P250,
        CsItem.CZ,
        CsItem.Dualies,
        CsItem.R8,
    };

    private static readonly ICollection<CsItem> _tPistols = new HashSet<CsItem>
    {
        CsItem.Glock,
        CsItem.Tec9,
    };


    private static readonly ICollection<CsItem> _ctPistols = new HashSet<CsItem>
    {
        CsItem.USPS,
        CsItem.P2000,
        CsItem.FiveSeven,
    };

    private static readonly ICollection<CsItem> _pistolsForT =
        _sharedPistols.Concat(_tPistols).ToHashSet();

    private static readonly ICollection<CsItem> _pistolsForCt =
        _sharedPistols.Concat(_ctPistols).ToHashSet();

    private static readonly ICollection<CsItem> _sharedMidRange = new HashSet<CsItem>
    {
        // SMG
        CsItem.P90,
        CsItem.UMP45,
        CsItem.MP7,
        CsItem.Bizon,
        CsItem.MP5,

        // Shotgun
        CsItem.XM1014,
        CsItem.Nova,

    };

    private static readonly ICollection<CsItem> _tMidRange = new HashSet<CsItem>
    {
        CsItem.Mac10,
        CsItem.SawedOff,
    };


    private static readonly ICollection<CsItem> _ctMidRange = new HashSet<CsItem>
    {
        CsItem.MP9,
        CsItem.MAG7,
    };

    private static readonly ICollection<CsItem> _midRangeForCt = _sharedMidRange.Concat(_ctMidRange).ToHashSet();
    private static readonly ICollection<CsItem> _midRangeForT = _sharedMidRange.Concat(_tMidRange).ToHashSet();

    private static readonly int _maxSmgItemValue = (int)CsItem.UMP;

    private static readonly ICollection<CsItem> _smgsForT =
        _sharedMidRange.Concat(_tMidRange).Where(i => (int)i <= _maxSmgItemValue).ToHashSet();

    private static readonly ICollection<CsItem> _smgsForCt =
        _sharedMidRange.Concat(_ctMidRange).Where(i => (int)i <= _maxSmgItemValue).ToHashSet();

    private static readonly ICollection<CsItem> _shotgunsShared = new HashSet<CsItem>
    {
        CsItem.XM1014,
        CsItem.Nova,
    };

    private static readonly ICollection<CsItem> _shotgunsForT = _shotgunsShared
        .Concat(new[] {CsItem.SawedOff})
        .ToHashSet();

    private static readonly ICollection<CsItem> _shotgunsForCt = _shotgunsShared
        .Concat(new[] {CsItem.MAG7})
        .ToHashSet();

    private static readonly ICollection<CsItem> _tRifles = new HashSet<CsItem>
    {
        CsItem.AK47,
        CsItem.Galil,
        CsItem.Krieg,
    };

    private static readonly ICollection<CsItem> _ctRifles = new HashSet<CsItem>
    {
        CsItem.M4A1S,
        CsItem.M4A4,
        CsItem.Famas,
        CsItem.AUG,
    };

    private static readonly ICollection<CsItem> _sharedPreferred = new HashSet<CsItem>
    {
        CsItem.AWP,
        CsItem.Scout,
    };

    private static readonly ICollection<CsItem> _tPreferred = new HashSet<CsItem>
    {
        CsItem.AutoSniperT,
    };

    private static readonly ICollection<CsItem> _ctPreferred = new HashSet<CsItem>
    {
        CsItem.AutoSniperCT,
    };

    private static readonly ICollection<CsItem> _preferredForT = _sharedPreferred.Concat(_tPreferred).ToHashSet();
    private static readonly ICollection<CsItem> _preferredForCt = _sharedPreferred.Concat(_ctPreferred).ToHashSet();

    private static readonly ICollection<CsItem> _allPreferred =
        _preferredForT.Concat(_preferredForCt).ToHashSet();

    private static readonly ICollection<CsItem> _awpAndAutoPreferred = new HashSet<CsItem>
    {
        CsItem.AWP,
        CsItem.AutoSniperT,
        CsItem.AutoSniperCT,
    };

    private static readonly ICollection<CsItem> _ssgPreferredWeapons = new HashSet<CsItem>
    {
        CsItem.Scout,
    };

    private const int RandomSniperPreferenceValue = int.MinValue + 1024;

    public static readonly CsItem RandomSniperPreference = (CsItem)RandomSniperPreferenceValue;

    private static readonly IReadOnlyList<CsItem> _randomSniperPool = new List<CsItem>
    {
        CsItem.AWP,
        CsItem.Scout,
    };

    private static readonly ICollection<CsItem> _heavys = new HashSet<CsItem>
    {
        CsItem.M249,
        CsItem.Negev,
    };

    private static readonly ICollection<CsItem> _fullBuyPrimaryForT =
        _tRifles.Concat(_heavys).ToHashSet();

    private static readonly ICollection<CsItem> _fullBuyPrimaryForCt =
        _ctRifles.Concat(_heavys).ToHashSet();

    private static readonly ICollection<CsItem> _allWeapons = Enum.GetValues<CsItem>()
        .Where(item => (int)item >= 200 && (int)item < 500)
        .ToHashSet();

    private static readonly ICollection<CsItem> _allFullBuy =
        _allPreferred.Concat(_heavys).Concat(_tRifles).Concat(_ctRifles).ToHashSet();

    private static readonly ICollection<CsItem> _allHalfBuy =
        _midRangeForT.Concat(_midRangeForCt).ToHashSet();

    private static readonly ICollection<CsItem> _allPistols =
        _pistolsForT.Concat(_pistolsForCt).ToHashSet();

    private static readonly ICollection<CsItem> _allPrimary = _allPreferred
        .Concat(_allFullBuy)
        .Concat(_allHalfBuy)
        .ToHashSet();

    private static readonly ICollection<CsItem> _allSecondary = _allPistols.ToHashSet();

    private static readonly ICollection<CsItem> _allUtil = new HashSet<CsItem>
    {
        CsItem.Flashbang,
        CsItem.HE,
        CsItem.Molotov,
        CsItem.Incendiary,
        CsItem.Smoke,
        CsItem.Decoy,
    };

    private static readonly Dictionary<RoundType, ICollection<WeaponAllocationType>>
        _validAllocationTypesForRound = new()
        {
            {RoundType.Pistol, new HashSet<WeaponAllocationType> {WeaponAllocationType.PistolRound}},
            {
                RoundType.HalfBuy,
                new HashSet<WeaponAllocationType> {WeaponAllocationType.Secondary, WeaponAllocationType.HalfBuyPrimary}
            },
            {
                RoundType.FullBuy,
                new HashSet<WeaponAllocationType>
                {
                    WeaponAllocationType.Secondary, WeaponAllocationType.FullBuyPrimary, WeaponAllocationType.Preferred
                }
            },
        };

    private static readonly Dictionary<
        CsTeam,
        Dictionary<WeaponAllocationType, ICollection<CsItem>>
    > _validWeaponsByTeamAndAllocationType = new()
    {
        {
            CsTeam.Terrorist, new()
            {
                {WeaponAllocationType.PistolRound, _pistolsForT},
                {WeaponAllocationType.Secondary, _pistolsForT},
                {WeaponAllocationType.HalfBuyPrimary, _midRangeForT},
                {WeaponAllocationType.FullBuyPrimary, _fullBuyPrimaryForT},
                {WeaponAllocationType.Preferred, _preferredForT},
            }
        },
        {
            CsTeam.CounterTerrorist, new()
            {
                {WeaponAllocationType.PistolRound, _pistolsForCt},
                {WeaponAllocationType.Secondary, _pistolsForCt},
                {WeaponAllocationType.HalfBuyPrimary, _midRangeForCt},
                {WeaponAllocationType.FullBuyPrimary, _fullBuyPrimaryForCt},
                {WeaponAllocationType.Preferred, _preferredForCt},
            }
        }
    };

    private static readonly Dictionary<
        CsTeam,
        Dictionary<WeaponAllocationType, CsItem>
    > _defaultWeaponsByTeamAndAllocationType = new()
    {
        {
            CsTeam.Terrorist, new()
            {
                {WeaponAllocationType.FullBuyPrimary, CsItem.AK47},
                {WeaponAllocationType.HalfBuyPrimary, CsItem.Mac10},
                {WeaponAllocationType.Secondary, CsItem.Deagle},
                {WeaponAllocationType.PistolRound, CsItem.Glock},
            }
        },
        {
            CsTeam.CounterTerrorist, new()
            {
                {WeaponAllocationType.FullBuyPrimary, CsItem.M4A1S},
                {WeaponAllocationType.HalfBuyPrimary, CsItem.MP9},
                {WeaponAllocationType.Secondary, CsItem.Deagle},
                {WeaponAllocationType.PistolRound, CsItem.USPS},
            }
        }
    };

    private static ICollection<CsItem> GetAvailableWeaponsForTeamAndAllocationType(WeaponAllocationType allocationType, CsTeam team)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return new List<CsItem>();
        }

        var availableWeapons = new HashSet<CsItem>(_validWeaponsByTeamAndAllocationType[team][allocationType]);

        if (allocationType == WeaponAllocationType.FullBuyPrimary && Configs.IsLoaded())
        {
            var config = Configs.GetConfigData();
            if (config.EnableWeaponShotguns)
            {
                availableWeapons.UnionWith(GetShotgunsForTeam(team));
            }

            if (config.EnableWeaponPms)
            {
                availableWeapons.UnionWith(GetSmgsForTeam(team));
            }
        }

        var allowAllWeapons = Configs.IsLoaded() && Configs.GetConfigData().EnableAllWeaponsForEveryone;
        if (!allowAllWeapons || allocationType == WeaponAllocationType.Preferred)
        {
            return availableWeapons;
        }

        var otherTeam = team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;

        if (_validWeaponsByTeamAndAllocationType.TryGetValue(otherTeam, out var otherAllocations) &&
            otherAllocations.TryGetValue(allocationType, out var otherWeapons))
        {
            availableWeapons.UnionWith(otherWeapons);
        }

        if (allocationType == WeaponAllocationType.FullBuyPrimary && Configs.IsLoaded())
        {
            var config = Configs.GetConfigData();
            if (config.EnableWeaponShotguns)
            {
                availableWeapons.UnionWith(GetShotgunsForTeam(otherTeam));
            }

            if (config.EnableWeaponPms)
            {
                availableWeapons.UnionWith(GetSmgsForTeam(otherTeam));
            }
        }

        return availableWeapons;
    }

    private static readonly Dictionary<string, CsItem> _weaponNameSearchOverrides = new()
    {
        {"m4a1", CsItem.M4A1S},
        {"m4a1-s", CsItem.M4A1S},
    };

    private static readonly Dictionary<CsItem, string> _weaponNameOverrides = new()
    {
        {CsItem.M4A4, "M4A4"},
    };

    public static List<WeaponAllocationType> WeaponAllocationTypes =>
        Enum.GetValues<WeaponAllocationType>().ToList();

    public static Dictionary<
        CsTeam,
        Dictionary<WeaponAllocationType, CsItem>
    > DefaultWeaponsByTeamAndAllocationType => new(_defaultWeaponsByTeamAndAllocationType);

    public static List<CsItem> AllWeapons => _allWeapons.ToList();

    public static bool IsWeapon(CsItem item) => _allWeapons.Contains(item);

    public static string GetName(this CsItem item) =>
        _weaponNameOverrides.TryGetValue(item, out var overrideName)
            ? overrideName
            : item.ToString();

    public static ItemSlotType? GetSlotTypeForItem(CsItem? item)
    {
        if (item is null)
        {
            return null;
        }

        if (_allSecondary.Contains(item.Value))
        {
            return ItemSlotType.Secondary;
        }

        if (_allPrimary.Contains(item.Value))
        {
            return ItemSlotType.Primary;
        }

        if (_allUtil.Contains(item.Value))
        {
            return ItemSlotType.Util;
        }

        return null;
    }

    public static string GetSlotNameForSlotType(ItemSlotType? slotType)
    {
        return slotType switch
        {
            ItemSlotType.Primary => "slot1",
            ItemSlotType.Secondary => "slot2",
            ItemSlotType.Util => "slot4",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static ICollection<CsItem> GetPossibleWeaponsForAllocationType(WeaponAllocationType allocationType,
        CsTeam team)
    {
        var availableWeapons = GetAvailableWeaponsForTeamAndAllocationType(allocationType, team);
        return availableWeapons.Where(IsUsableWeapon).ToList();
    }
    public static bool IsAllocationTypeValidForRound(WeaponAllocationType? allocationType, RoundType? roundType)
    {
        if (allocationType is null || roundType is null)
        {
            return false;
        }

        return _validAllocationTypesForRound[roundType.Value].Contains(allocationType.Value);
    }

    public static bool IsPreferred(CsTeam team, CsItem weapon)
    {
        return team switch
        {
            CsTeam.Terrorist => _preferredForT.Contains(weapon),
            CsTeam.CounterTerrorist => _preferredForCt.Contains(weapon),
            _ => false,
        };
    }

    public static bool IsRandomSniperPreference(CsItem? weapon)
    {
        return weapon.HasValue && weapon.Value == RandomSniperPreference;
    }

    public static CsItem ChooseRandomSniperWeapon()
    {
        return _randomSniperPool[Random.Shared.Next(_randomSniperPool.Count)];
    }

    public static bool IsAwpOrAutoSniperPreference(CsItem weapon)
    {
        return _awpAndAutoPreferred.Contains(weapon) || IsRandomSniperPreference(weapon);
    }

    public static bool IsSsgPreference(CsItem weapon)
    {
        return _ssgPreferredWeapons.Contains(weapon) || IsRandomSniperPreference(weapon);
    }

    private static ICollection<CsItem> GetShotgunsForTeam(CsTeam team)
    {
        return team switch
        {
            CsTeam.Terrorist => _shotgunsForT,
            CsTeam.CounterTerrorist => _shotgunsForCt,
            _ => Array.Empty<CsItem>(),
        };
    }

    private static ICollection<CsItem> GetSmgsForTeam(CsTeam team)
    {
        return team switch
        {
            CsTeam.Terrorist => _smgsForT,
            CsTeam.CounterTerrorist => _smgsForCt,
            _ => Array.Empty<CsItem>(),
        };
    }

    public static IList<T> SelectPreferredPlayers<T>(IEnumerable<T> players, Func<T, bool> hasPermission, CsTeam team)
    {
        var config = Configs.GetConfigData();
        var awpMode = config.GetAwpMode();
        return SelectPreferredPlayersCore(
            players,
            hasPermission,
            team,
            new SniperSelectionSettings
            {
                Enabled = awpMode != AccessMode.Disabled,
                AllowEveryone = awpMode == AccessMode.Everyone,
                VipOnly = awpMode == AccessMode.VipOnly,
                ExtraVipChances = 0,
            },
            config.MaxAwpWeaponsPerTeam,
            config.MinPlayersPerTeamForAwpWeapon
        );
    }

    public static IList<T> SelectPreferredSsgPlayers<T>(IEnumerable<T> players, Func<T, bool> hasPermission, CsTeam team)
    {
        var config = Configs.GetConfigData();
        var ssgMode = config.GetSsgMode();
        return SelectPreferredPlayersCore(
            players,
            hasPermission,
            team,
            new SniperSelectionSettings
            {
                Enabled = ssgMode != AccessMode.Disabled,
                AllowEveryone = ssgMode == AccessMode.Everyone,
                VipOnly = ssgMode == AccessMode.VipOnly,
                ExtraVipChances = 0,
            },
            config.MaxSsgWeaponsPerTeam,
            config.MinPlayersPerTeamForSsgWeapon
        );
    }

    private static IList<T> SelectPreferredPlayersCore<T>(
        IEnumerable<T> players,
        Func<T, bool> hasPermission,
        CsTeam team,
        SniperSelectionSettings settings,
        IDictionary<CsTeam, int> maxWeaponsPerTeam,
        IDictionary<CsTeam, int> minPlayersPerTeam
    )
    {
        if (!settings.Enabled)
        {
            return new List<T>();
        }

        var playersList = players.ToList();

        if (settings.AllowEveryone)
        {
            return new List<T>(playersList);
        }

        if (minPlayersPerTeam.TryGetValue(team, out var minTeamPlayers))
        {
            if (playersList.Count < minTeamPlayers)
            {
                return new List<T>();
            }
        }

        if (!maxWeaponsPerTeam.TryGetValue(team, out var maxPerTeam))
        {
            maxPerTeam = 1;
        }

        if (maxPerTeam == 0)
        {
            return new List<T>();
        }

        var choicePlayers = new List<T>();
        foreach (var p in playersList)
        {
        if (settings.VipOnly && !hasPermission(p))
        {
            continue;
        }

        choicePlayers.Add(p);

        if (!settings.VipOnly && settings.ExtraVipChances > 0 && hasPermission(p))
        {
            for (var i = 0; i < settings.ExtraVipChances; i++)
            {
                choicePlayers.Add(p);
            }
            }
        }

        if (choicePlayers.Count == 0)
        {
            return new List<T>();
        }

        Utils.Shuffle(choicePlayers);
        return new HashSet<T>(choicePlayers).Take(maxPerTeam).ToList();
    }

    public static bool IsUsableWeapon(CsItem weapon)
    {
        return Configs.GetConfigData().UsableWeapons.Contains(weapon);
    }

    public static CsItem? CoercePreferredTeam(CsItem? item, CsTeam team)
    {
        if (item == null)
        {
            return null;
        }

        if (IsRandomSniperPreference(item))
        {
            return RandomSniperPreference;
        }

        if (!_allPreferred.Contains(item.Value))
        {
            return null;
        }

        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return null;
        }

        return item switch
        {
            CsItem.AWP => item,
            CsItem.Scout => item,
            CsItem.AutoSniperT => team == CsTeam.Terrorist ? CsItem.AutoSniperT : CsItem.AutoSniperCT,
            CsItem.AutoSniperCT => team == CsTeam.Terrorist ? CsItem.AutoSniperT : CsItem.AutoSniperCT,
            _ => null,
        };
    }

    public static ICollection<RoundType> GetRoundTypesForWeapon(CsItem weapon)
    {
        if (_allPistols.Contains(weapon))
        {
            return new HashSet<RoundType> {RoundType.Pistol, RoundType.HalfBuy, RoundType.FullBuy};
        }

        if (_allHalfBuy.Contains(weapon))
        {
            return new HashSet<RoundType> {RoundType.HalfBuy};
        }

        if (_allFullBuy.Contains(weapon))
        {
            return new HashSet<RoundType> {RoundType.FullBuy};
        }

        return new HashSet<RoundType>();
    }

    private record SniperSelectionSettings
    {
        public bool Enabled { get; init; }
        public bool AllowEveryone { get; init; }
        public bool VipOnly { get; init; }
        public int ExtraVipChances { get; init; }
    }

    public static ICollection<CsItem> FindValidWeaponsByName(string needle)
    {
        return FindItemsByName(needle)
            .Where(item => _allWeapons.Contains(item))
            .ToList();
    }

    public static WeaponAllocationType? GetWeaponAllocationTypeForWeaponAndRound(RoundType? roundType, CsTeam team,
        CsItem weapon)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return null;
        }

        // First populate all allocation types that could match
        // For a pistol this could be multiple allocation types, for any other weapon type only one can match
        var potentialAllocationTypes = new HashSet<WeaponAllocationType>();
        foreach (var allocationType in _validWeaponsByTeamAndAllocationType[team].Keys)
        {
            var items = GetAvailableWeaponsForTeamAndAllocationType(allocationType, team);
            if (items.Contains(weapon))
            {
                potentialAllocationTypes.Add(allocationType);
            }
        }

        // If theres only 1 to choose from, return that, or return null if there are none
        if (potentialAllocationTypes.Count == 1)
        {
            return potentialAllocationTypes.First();
        }

        if (potentialAllocationTypes.Count == 0)
        {
            return null;
        }

        // For a pistol, the set will be {PistolRound, Secondary}
        // We need to find which of those matches the current round type
        foreach (var allocationType in potentialAllocationTypes)
        {
            if (roundType is null || IsAllocationTypeValidForRound(allocationType, roundType))
            {
                return allocationType;
            }
        }

        return null;
    }

    /**
     * This function should only be used when you have an item that you want to find out what *replacement*
     * allocation type it belongs to. Eg. if you have a Preferred, it should be replaced with a PrimaryFullBuy
     */
    public static WeaponAllocationType? GetReplacementWeaponAllocationTypeForWeapon(RoundType? roundType)
    {
        return roundType switch
        {
            RoundType.Pistol => WeaponAllocationType.PistolRound,
            RoundType.HalfBuy => WeaponAllocationType.HalfBuyPrimary,
            RoundType.FullBuy => WeaponAllocationType.FullBuyPrimary,
            _ => null,
        };
    }

    public static WeaponSelectionResult GetWeaponsForRoundType(
        RoundType roundType,
        CsTeam team,
        UserSetting? userSetting,
        bool givePreferred,
        bool enemyStuffQuotaAvailable = true,
        CsItem? preferredOverride = null
    )
    {
        WeaponAllocationType? primaryWeaponAllocation =
            givePreferred
                ? WeaponAllocationType.Preferred
                : roundType switch
                {
                    RoundType.HalfBuy => WeaponAllocationType.HalfBuyPrimary,
                    RoundType.FullBuy => WeaponAllocationType.FullBuyPrimary,
                    _ => null,
                };

        var secondaryWeaponAllocation = roundType switch
        {
            RoundType.Pistol => WeaponAllocationType.PistolRound,
            _ => WeaponAllocationType.Secondary,
        };

        var weapons = new List<CsItem>();
        var enemyStuffGranted = false;
        var secondary = GetWeaponForAllocationType(
            secondaryWeaponAllocation,
            team,
            userSetting,
            allowEnemySwap: true,
            enemyStuffQuotaAvailable,
            ref enemyStuffGranted
        );
        if (secondary is not null)
        {
            weapons.Add(secondary.Value);
        }

        if (primaryWeaponAllocation is null)
        {
            return new WeaponSelectionResult(weapons, enemyStuffGranted);
        }

        CsItem? primary;
        if (primaryWeaponAllocation == WeaponAllocationType.Preferred && preferredOverride.HasValue)
        {
            primary = preferredOverride.Value;
        }
        else
        {
            primary = GetWeaponForAllocationType(
                primaryWeaponAllocation.Value,
                team,
                userSetting,
                allowEnemySwap: true,
                enemyStuffQuotaAvailable,
                ref enemyStuffGranted
            );
        }

        if (primary is not null)
        {
            weapons.Add(primary.Value);
        }

        return new WeaponSelectionResult(weapons, enemyStuffGranted);
    }

    private static ICollection<CsItem> FindItemsByName(string needle)
    {
        needle = needle.ToLower();
        if (_weaponNameSearchOverrides.TryGetValue(needle, out var nameOverride))
        {
            return new List<CsItem> {nameOverride};
        }

        var needles = new HashSet<string> {needle};
        const string weaponPrefix = "weapon_";
        if (needle.StartsWith(weaponPrefix))
        {
            needles.Add(needle[weaponPrefix.Length..]);
        }

        return Enum.GetNames<CsItem>()
            .Where(name =>
            {
                var lowered = name.ToLower();
                return needles.Any(n => lowered.Contains(n));
            })
            .Select(Enum.Parse<CsItem>)
            .ToList();
    }

    private static CsItem? GetDefaultWeaponForAllocationType(WeaponAllocationType allocationType, CsTeam team)
    {
        if (team is CsTeam.None or CsTeam.Spectator)
        {
            return null;
        }

        if (allocationType == WeaponAllocationType.Preferred)
        {
            return null;
        }

        CsItem? defaultWeapon = null;

        var configDefaultWeapons = Configs.GetConfigData().DefaultWeapons;
        if (configDefaultWeapons.TryGetValue(team, out var teamDefaults))
        {
            if (teamDefaults.TryGetValue(allocationType, out var configuredDefaultWeapon))
            {
                defaultWeapon = configuredDefaultWeapon;
            }
        }

        defaultWeapon ??= _defaultWeaponsByTeamAndAllocationType[team][allocationType];

        return IsUsableWeapon(defaultWeapon.Value) ? defaultWeapon : null;
    }

    private static CsItem GetRandomWeaponForAllocationType(WeaponAllocationType allocationType, CsTeam team)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return CsItem.Deagle;
        }

        var availableWeapons = GetAvailableWeaponsForTeamAndAllocationType(allocationType, team)
            .Where(IsUsableWeapon)
            .ToList();

        if (availableWeapons.Count == 0)
        {
            return CsItem.Deagle;
        }

        return Utils.Choice(availableWeapons);
    }

    private static CsItem? GetWeaponForAllocationType(
        WeaponAllocationType allocationType,
        CsTeam team,
        UserSetting? userSetting,
        bool allowEnemySwap,
        bool enemyStuffQuotaAvailable,
        ref bool enemyStuffGranted
    )
    {
        CsItem? weapon = null;

        if (Configs.GetConfigData().CanPlayersSelectWeapons() && userSetting is not null)
        {
            var weaponPreference = userSetting.GetWeaponPreference(team, allocationType);
            if (IsRandomSniperPreference(weaponPreference))
            {
                weapon = ChooseRandomSniperWeapon();
            }
            else if (weaponPreference is not null && IsUsableWeapon(weaponPreference.Value))
            {
                weapon = weaponPreference;
            }
        }

        if (weapon is null && Configs.GetConfigData().CanAssignRandomWeapons())
        {
            weapon = GetRandomWeaponForAllocationType(allocationType, team);
        }

        if (weapon is null && Configs.GetConfigData().CanAssignDefaultWeapons())
        {
            weapon = GetDefaultWeaponForAllocationType(allocationType, team);
        }

        if (allowEnemySwap && weapon is not null)
        {
            weapon = MaybeSwapForEnemyStuff(
                allocationType,
                team,
                userSetting,
                weapon.Value,
                enemyStuffQuotaAvailable,
                ref enemyStuffGranted
            );
        }

        return weapon;
    }

    public static bool IsWeaponAllocationAllowed(bool isFreezePeriod)
    {
        return Configs.GetConfigData().AllowAllocationAfterFreezeTime || isFreezePeriod;
    }

    private static CsItem? MaybeSwapForEnemyStuff(
        WeaponAllocationType allocationType,
        CsTeam team,
        UserSetting? userSetting,
        CsItem weapon,
        bool enemyStuffQuotaAvailable,
        ref bool enemyStuffGranted
    )
    {
        var config = Configs.GetConfigData();
        if (config.GetEnemyStuffMode() == AccessMode.Disabled || config.ChanceForEnemyStuff <= 0)
        {
            return weapon;
        }

        if (!enemyStuffQuotaAvailable)
        {
            return weapon;
        }

        if (userSetting is null || !userSetting.IsEnemyStuffEnabledForTeam(team))
        {
            return weapon;
        }

        if (team is not CsTeam.Terrorist and not CsTeam.CounterTerrorist)
        {
            return weapon;
        }

        if (Random.Shared.NextDouble() * 100 >= config.ChanceForEnemyStuff)
        {
            return weapon;
        }

        var enemyTeam = team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
        var enemyWeapon = GetWeaponForAllocationType(
                allocationType,
                enemyTeam,
                userSetting,
                allowEnemySwap: false,
                enemyStuffQuotaAvailable: true,
                ref enemyStuffGranted
            )
            ?? GetRandomWeaponForAllocationType(allocationType, enemyTeam);

        if (!enemyWeapon.Equals(weapon))
        {
            enemyStuffGranted = true;
        }

        return enemyWeapon;
    }
}
