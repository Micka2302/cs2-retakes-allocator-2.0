using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using RetakesAllocatorCore.Managers;
using System;

namespace RetakesAllocatorCore;

public class OnRoundPostStartHelper
{
    private enum SniperLimitType
    {
        Awp,
        Ssg,
    }

    private static Dictionary<CsTeam, Dictionary<SniperLimitType, int>> CreateSniperCountMap()
    {
        return new Dictionary<CsTeam, Dictionary<SniperLimitType, int>>
        {
            {
                CsTeam.Terrorist, new Dictionary<SniperLimitType, int>
                {
                    {SniperLimitType.Awp, 0},
                    {SniperLimitType.Ssg, 0},
                }
            },
            {
                CsTeam.CounterTerrorist, new Dictionary<SniperLimitType, int>
                {
                    {SniperLimitType.Awp, 0},
                    {SniperLimitType.Ssg, 0},
                }
            },
        };
    }

    private static SniperLimitType? GetSniperLimitType(CsItem weapon)
    {
        if (WeaponHelpers.IsAwpOrAutoSniperWeapon(weapon))
        {
            return SniperLimitType.Awp;
        }

        if (WeaponHelpers.IsSsgWeapon(weapon))
        {
            return SniperLimitType.Ssg;
        }

        return null;
    }

    private static int GetMaxSnipersForTeam(ConfigData config, CsTeam team, SniperLimitType sniperLimitType)
    {
        var limits = sniperLimitType == SniperLimitType.Awp
            ? config.MaxAwpWeaponsPerTeam
            : config.MaxSsgWeaponsPerTeam;

        return limits.TryGetValue(team, out var limit) ? limit : 1;
    }

    private static bool IsSniperAllowedForRound(ConfigData config, SniperLimitType sniperLimitType, RoundType roundType)
    {
        return sniperLimitType == SniperLimitType.Awp
            ? config.IsAwpAllowedForRoundType(roundType)
            : config.IsSsgAllowedForRoundType(roundType);
    }

    private static Dictionary<CsTeam, Dictionary<SniperLimitType, int>> CountSniperReservations<T>(
        IDictionary<T, CsItem> tPreferredWeapons,
        IDictionary<T, CsItem> ctPreferredWeapons
    ) where T : notnull
    {
        var reservations = CreateSniperCountMap();

        void Count(CsTeam team, IEnumerable<CsItem> weapons)
        {
            foreach (var weapon in weapons)
            {
                var sniperType = GetSniperLimitType(weapon);
                if (sniperType is not null)
                {
                    reservations[team][sniperType.Value]++;
                }
            }
        }

        Count(CsTeam.Terrorist, tPreferredWeapons.Values);
        Count(CsTeam.CounterTerrorist, ctPreferredWeapons.Values);

        return reservations;
    }

    private static ICollection<CsItem> ApplySniperTeamLimits(
        ICollection<CsItem> weapons,
        CsTeam team,
        RoundType roundType,
        CsItem? preferredOverride,
        ConfigData config,
        IDictionary<CsTeam, Dictionary<SniperLimitType, int>> grantedSnipersPerTeam,
        IDictionary<CsTeam, Dictionary<SniperLimitType, int>> remainingPreferredReservations
    )
    {
        if (team is not CsTeam.Terrorist and not CsTeam.CounterTerrorist)
        {
            return weapons;
        }

        var filteredWeapons = new List<CsItem>();
        var currentPreferredType = preferredOverride.HasValue
            ? GetSniperLimitType(preferredOverride.Value)
            : null;
        var consumedCurrentPreferredReservation = false;

        foreach (var weapon in weapons)
        {
            var sniperType = GetSniperLimitType(weapon);
            if (sniperType is null)
            {
                filteredWeapons.Add(weapon);
                continue;
            }

            var isCurrentPreferred =
                !consumedCurrentPreferredReservation &&
                currentPreferredType == sniperType &&
                preferredOverride == weapon;
            var sniperAllowedForRound = IsSniperAllowedForRound(config, sniperType.Value, roundType);
            var reservedAfterThisPlayer = Math.Max(
                0,
                remainingPreferredReservations[team][sniperType.Value] - (isCurrentPreferred ? 1 : 0)
            );
            var maxForTeam = GetMaxSnipersForTeam(config, team, sniperType.Value);
            var allowedBeforeFutureReservations = maxForTeam - reservedAfterThisPlayer;
            var grantedForTeam = grantedSnipersPerTeam[team][sniperType.Value];

            if (sniperAllowedForRound && maxForTeam > 0 && grantedForTeam < allowedBeforeFutureReservations)
            {
                filteredWeapons.Add(weapon);
                grantedSnipersPerTeam[team][sniperType.Value] = grantedForTeam + 1;
            }
            else if (WeaponHelpers.GetNonSniperPrimaryFallback(roundType, team) is { } fallback)
            {
                filteredWeapons.Add(fallback);
            }

            if (isCurrentPreferred)
            {
                consumedCurrentPreferredReservation = true;
            }
        }

        if (currentPreferredType is not null && remainingPreferredReservations[team][currentPreferredType.Value] > 0)
        {
            remainingPreferredReservations[team][currentPreferredType.Value]--;
        }

        return filteredWeapons;
    }

    public static void Handle<T>(
        ICollection<T> allPlayers,
        Func<T?, ulong> getSteamId,
        Func<T, CsTeam> getTeam,
        Action<T> giveDefuseKit,
        Action<T, ICollection<CsItem>, string?> allocateItemsForPlayer,
        Func<T, bool> hasAwpPermission,
        Func<T, bool> hasSsgPermission,
        Func<T, bool> hasEnemyStuffPermission,
        out RoundType currentRoundType
    ) where T : notnull
    {
        var roundType = RoundTypeManager.Instance.GetNextRoundType();
        currentRoundType = roundType;

        var tPlayers = new List<T>();
        var ctPlayers = new List<T>();
        var playerIds = new List<ulong>();
        foreach (var player in allPlayers)
        {
            var steamId = getSteamId(player);
            if (steamId != 0)
            {
                playerIds.Add(steamId);
            }

            var playerTeam = getTeam(player);
            if (playerTeam == CsTeam.Terrorist)
            {
                tPlayers.Add(player);
            }
            else if (playerTeam == CsTeam.CounterTerrorist)
            {
                ctPlayers.Add(player);
            }
        }

        Log.Debug($"#T Players: {string.Join(",", tPlayers.Select(getSteamId))}");
        Log.Debug($"#CT Players: {string.Join(",", ctPlayers.Select(getSteamId))}");

        var userSettingsByPlayerId = Queries.GetUsersSettings(playerIds);

        var defusingPlayer = Utils.Choice(ctPlayers);

        HashSet<T> FilterPreferredPlayers(IEnumerable<T> ps, Func<CsItem, bool> preferenceFilter) =>
            ps.Where(p =>
                    userSettingsByPlayerId.TryGetValue(getSteamId(p), out var userSetting) &&
                    userSetting.GetWeaponPreference(getTeam(p), WeaponAllocationType.Preferred) is { } preferredWeapon &&
                    preferenceFilter(preferredWeapon))
                .ToHashSet();

        var tPreferredWeapons = new Dictionary<T, CsItem>();
        var ctPreferredWeapons = new Dictionary<T, CsItem>();

        void AssignPreferredWeapons(
            Dictionary<T, CsItem> preferredWeapons,
            IEnumerable<T> eligiblePlayers,
            Func<IEnumerable<T>, Func<T, bool>, CsTeam, IList<T>> selectPlayers,
            Func<T, bool> hasPermission,
            CsTeam team,
            Func<CsItem> randomWeaponFactory
        )
        {
            var selectedPlayers = selectPlayers(eligiblePlayers, hasPermission, team);
            foreach (var selectedPlayer in selectedPlayers)
            {
                if (preferredWeapons.ContainsKey(selectedPlayer))
                {
                    continue;
                }

                var steamId = getSteamId(selectedPlayer);
                if (!userSettingsByPlayerId.TryGetValue(steamId, out var userSetting))
                {
                    continue;
                }

                var preference =
                    userSetting.GetWeaponPreference(team, WeaponAllocationType.Preferred);
                if (preference is null)
                {
                    continue;
                }

                var weapon = preference.Value;
                if (WeaponHelpers.IsRandomSniperPreference(weapon))
                {
                    weapon = randomWeaponFactory();
                }
                else if (!WeaponHelpers.IsUsableWeapon(weapon))
                {
                    continue;
                }

                preferredWeapons[selectedPlayer] = weapon;
            }
        }

        var config = Configs.GetConfigData();
        var random = new Random();
        var enemyStuffGrantedPerTeam = new Dictionary<CsTeam, int>
        {
            {CsTeam.Terrorist, 0},
            {CsTeam.CounterTerrorist, 0},
        };
        var zeusGrantedPerTeam = new Dictionary<CsTeam, int>
        {
            {CsTeam.Terrorist, 0},
            {CsTeam.CounterTerrorist, 0},
        };

        if (config.IsAwpAllowedForRoundType(roundType))
        {
            if (random.NextDouble() * 100 <= config.ChanceForAwpWeapon)
            {
                var tAwpEligible = FilterPreferredPlayers(tPlayers, WeaponHelpers.IsAwpOrAutoSniperPreference);
                var ctAwpEligible = FilterPreferredPlayers(ctPlayers, WeaponHelpers.IsAwpOrAutoSniperPreference);

                AssignPreferredWeapons(
                    tPreferredWeapons,
                    tAwpEligible,
                    WeaponHelpers.SelectPreferredPlayers,
                    hasAwpPermission,
                    CsTeam.Terrorist,
                    () => CsItem.AWP
                );
                AssignPreferredWeapons(
                    ctPreferredWeapons,
                    ctAwpEligible,
                    WeaponHelpers.SelectPreferredPlayers,
                    hasAwpPermission,
                    CsTeam.CounterTerrorist,
                    () => CsItem.AWP
                );
            }
        }

        if (config.IsSsgAllowedForRoundType(roundType) &&
            random.NextDouble() * 100 <= config.ChanceForSsgWeapon)
        {
            var tSsgEligible = FilterPreferredPlayers(tPlayers, WeaponHelpers.IsSsgPreference)
                .Where(player => !tPreferredWeapons.ContainsKey(player));
            var ctSsgEligible = FilterPreferredPlayers(ctPlayers, WeaponHelpers.IsSsgPreference)
                .Where(player => !ctPreferredWeapons.ContainsKey(player));

            AssignPreferredWeapons(
                tPreferredWeapons,
                tSsgEligible,
                WeaponHelpers.SelectPreferredSsgPlayers,
                hasSsgPermission,
                CsTeam.Terrorist,
                () => CsItem.Scout
            );
            AssignPreferredWeapons(
                ctPreferredWeapons,
                ctSsgEligible,
                WeaponHelpers.SelectPreferredSsgPlayers,
                hasSsgPermission,
                CsTeam.CounterTerrorist,
                () => CsItem.Scout
            );
        }

        var grantedSnipersPerTeam = CreateSniperCountMap();
        var remainingPreferredSniperReservations = CountSniperReservations(tPreferredWeapons, ctPreferredWeapons);

        var nadesByPlayer = new Dictionary<T, ICollection<CsItem>>();
        NadeHelpers.AllocateNadesToPlayers(
            NadeHelpers.GetUtilForTeam(
                RoundTypeManager.Instance.Map,
                roundType,
                CsTeam.Terrorist,
                tPlayers.Count
            ),
            tPlayers,
            nadesByPlayer
        );
        NadeHelpers.AllocateNadesToPlayers(
            NadeHelpers.GetUtilForTeam(
                RoundTypeManager.Instance.Map,
                roundType,
                CsTeam.CounterTerrorist,
                ctPlayers.Count
            ),
            ctPlayers,
            nadesByPlayer
        );

        foreach (var player in allPlayers)
        {
            var team = getTeam(player);
            var playerSteamId = getSteamId(player);
            userSettingsByPlayerId.TryGetValue(playerSteamId, out var userSetting);
            var items = new List<CsItem>
            {
                RoundTypeHelpers.GetArmorForRoundType(roundType),
                team == CsTeam.Terrorist ? CsItem.DefaultKnifeT : CsItem.DefaultKnifeCT,
            };

            CsItem? preferredOverride = team switch
            {
                CsTeam.Terrorist => tPreferredWeapons.TryGetValue(player, out var weapon)
                    ? weapon
                    : (CsItem?)null,
                CsTeam.CounterTerrorist => ctPreferredWeapons.TryGetValue(player, out var weapon)
                    ? weapon
                    : (CsItem?)null,
                _ => null,
            };
            var givePreferred = preferredOverride.HasValue;

            var enemyStuffQuotaAvailable =
                hasEnemyStuffPermission(player) &&
                userSetting is not null &&
                userSetting.IsEnemyStuffEnabledForTeam(team) &&
                team is CsTeam.Terrorist or CsTeam.CounterTerrorist &&
                (config.GetMaxEnemyStuffForTeam(team) < 0 ||
                 enemyStuffGrantedPerTeam[team] < config.GetMaxEnemyStuffForTeam(team));

            var weaponSelection = WeaponHelpers.GetWeaponsForRoundType(
                roundType,
                team,
                userSetting,
                givePreferred,
                enemyStuffQuotaAvailable,
                preferredOverride
            );
            var weapons = ApplySniperTeamLimits(
                weaponSelection.Weapons,
                team,
                roundType,
                preferredOverride,
                config,
                grantedSnipersPerTeam,
                remainingPreferredSniperReservations
            );
            items.AddRange(weapons);

            if (weaponSelection.EnemyStuffGranted && team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            {
                enemyStuffGrantedPerTeam[team]++;
            }

            if (nadesByPlayer.TryGetValue(player, out var playerNades))
            {
                items.AddRange(playerNades);
            }

            if (team == CsTeam.CounterTerrorist)
            {
                // On non-pistol rounds, everyone gets defuse kit and util
                if (roundType != RoundType.Pistol)
                {
                    giveDefuseKit(player);
                }
                else if (getSteamId(defusingPlayer) == getSteamId(player))
                {
                    // On pistol rounds, only one person gets a defuse kit
                    giveDefuseKit(player);
                }
            }

            if (config.IsZeusEnabled() && userSetting?.ZeusEnabled == true &&
                team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            {
                var maxZeusForTeam = config.MaxZeusPerTeam.TryGetValue(team, out var limit)
                    ? limit
                    : 0;
                if (maxZeusForTeam > 0 &&
                    zeusGrantedPerTeam.TryGetValue(team, out var currentCount) &&
                    currentCount < maxZeusForTeam &&
                    random.NextDouble() * 100 <= config.ChanceForZeusWeapon)
                {
                    items.Add(CsItem.Zeus);
                    zeusGrantedPerTeam[team] = currentCount + 1;
                }
            }

            allocateItemsForPlayer(player, items, team == CsTeam.Terrorist ? "slot5" : "slot1");
        }
    }
}
