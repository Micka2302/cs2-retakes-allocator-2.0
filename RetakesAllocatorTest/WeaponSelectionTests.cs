using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using RetakesAllocatorCore.Managers;
using static RetakesAllocatorTest.TestConstants;

namespace RetakesAllocatorTest;

public class WeaponSelectionTests : BaseTestFixture
{
    [Test]
    public async Task SetWeaponPreferenceDirectly()
    {
        Assert.That(
            (await Queries.GetUserSettings(TestSteamId))
                ?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary),
            Is.EqualTo(null));

        await Queries.SetWeaponPreferenceForUserAsync(TestSteamId, CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary,
            CsItem.Galil);
        Assert.That(
            (await Queries.GetUserSettings(TestSteamId))
                ?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary),
            Is.EqualTo(CsItem.Galil));

        await Queries.SetWeaponPreferenceForUserAsync(TestSteamId, CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary,
            CsItem.AWP);
        Assert.That(
            (await Queries.GetUserSettings(TestSteamId))
                ?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary),
            Is.EqualTo(CsItem.AWP));

        await Queries.SetWeaponPreferenceForUserAsync(TestSteamId, CsTeam.Terrorist, WeaponAllocationType.PistolRound,
            CsItem.Deagle);
        Assert.That(
            (await Queries.GetUserSettings(TestSteamId))
                ?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.PistolRound),
            Is.EqualTo(CsItem.Deagle));

        Assert.That(
            (await Queries.GetUserSettings(TestSteamId))
                ?.GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.HalfBuyPrimary),
            Is.EqualTo(null));
        await Queries.SetWeaponPreferenceForUserAsync(TestSteamId, CsTeam.CounterTerrorist, WeaponAllocationType.HalfBuyPrimary,
            CsItem.MP9);
        Assert.That(
            (await Queries.GetUserSettings(TestSteamId))
                ?.GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.HalfBuyPrimary),
            Is.EqualTo(CsItem.MP9));
    }

    [Test]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "galil", CsItem.Galil, "Galil' is now", "Galil' is no longer")]
    [TestCase(RoundType.HalfBuy, CsTeam.Terrorist, "galil", null, "Galil' is now;;;at the next FullBuy",
        "Galil' is no longer")]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "krieg", CsItem.Krieg, "SG553' is now", "SG553' is no longer")]
    [TestCase(RoundType.HalfBuy, CsTeam.Terrorist, "mac10", CsItem.Mac10, "Mac10' is now", "Mac10' is no longer")]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "mac10", null, "Mac10' is now;;;at the next HalfBuy",
        "Mac10' is no longer")]
    [TestCase(RoundType.Pistol, CsTeam.CounterTerrorist, "deag", CsItem.Deagle, "Deagle' is now",
        "Deagle' is no longer")]
    [TestCase(RoundType.FullBuy, CsTeam.CounterTerrorist, "deag", CsItem.Deagle, "Deagle' is now",
        "Deagle' is no longer")]
    [TestCase(RoundType.HalfBuy, CsTeam.CounterTerrorist, "deag", CsItem.Deagle, "Deagle' is now",
        "Deagle' is no longer")]
    [TestCase(RoundType.FullBuy, CsTeam.CounterTerrorist, "galil", null, "Galil' is not valid", null)]
    [TestCase(RoundType.Pistol, CsTeam.CounterTerrorist, "tec9", null, "Tec9' is not valid", null)]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "poop", null, "not found", null)]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "galil,T", CsItem.Galil, "Galil' is now", null)]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "krieg,T", CsItem.Krieg, "SG553' is now", null)]
    [TestCase(RoundType.HalfBuy, CsTeam.Terrorist, "mac10,T", CsItem.Mac10, "Mac10' is now", null)]
    [TestCase(RoundType.HalfBuy, CsTeam.None, "mac10,T", null, "Mac10' is now", null)]
    [TestCase(RoundType.Pistol, CsTeam.CounterTerrorist, "deag,CT", CsItem.Deagle, "Deagle' is now", null)]
    [TestCase(RoundType.FullBuy, CsTeam.CounterTerrorist, "galil,CT", null, "Galil' is not valid", null)]
    [TestCase(RoundType.Pistol, CsTeam.CounterTerrorist, "tec9,CT", null, "Tec9' is not valid", null)]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "poop,T", null, "not found", null)]
    [TestCase(null, CsTeam.Terrorist, "ak", null, "AK47' is now", "AK47' is no longer")]
    [TestCase(RoundType.FullBuy, CsTeam.Spectator, "ak", null, "must join a team", "must join a team")]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "ak,F", null, "Invalid team", "Invalid team")]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "awp", null, "will now get a 'AWP", "no longer receive 'AWP")]
    [TestCase(RoundType.Pistol, CsTeam.CounterTerrorist, "awp", null, "will now get a 'AWP", "no longer receive 'AWP")]
    public async Task SetWeaponPreferenceCommandSingleArg(
        RoundType? roundType,
        CsTeam team,
        string strArgs,
        CsItem? expectedItem,
        string message,
        string? removeMessage
    )
    {
        var args = strArgs.Split(",");

        var result = await OnWeaponCommandHelper.HandleAsync(args, TestSteamId, roundType, team, false);

        var messages = message.Split(";;;");
        foreach (var m in messages)
        {
            Assert.That(result.Item1, Does.Contain(m));
        }

        var selectedItem = result.Item2;
        Assert.That(selectedItem, Is.EqualTo(expectedItem));

        var allocationType =
            selectedItem is not null
                ? WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(roundType, team, selectedItem.Value)
                : null;

        var setWeapon = allocationType is not null
            ? (await Queries.GetUserSettings(TestSteamId))?
                .GetWeaponPreference(team, allocationType.Value)
            : null;
        Assert.That(setWeapon, Is.EqualTo(expectedItem));

        if (removeMessage is not null)
        {
            result = await OnWeaponCommandHelper.HandleAsync(args, TestSteamId, roundType, team, true);
            Assert.That(result.Item1, Does.Contain(removeMessage));

            setWeapon = allocationType is not null
                ? (await Queries.GetUserSettings(TestSteamId))?.GetWeaponPreference(team, allocationType.Value)
                : null;
            Assert.That(setWeapon, Is.EqualTo(null));
        }
    }

    [Test]
    [TestCase("ak", CsItem.AK47, WeaponSelectionType.PlayerChoice, CsItem.AK47, "AK47' is now")]
    [TestCase("ak", CsItem.Galil, WeaponSelectionType.PlayerChoice, null, "not allowed")]
    [TestCase("ak", CsItem.AK47, WeaponSelectionType.Default, null, "cannot choose")]
    public async Task SetWeaponPreferencesConfig(
        string itemName,
        CsItem? allowedItem,
        WeaponSelectionType weaponSelectionType,
        CsItem? expectedItem,
        string message
    )
    {
        var team = CsTeam.Terrorist;
        Configs.GetConfigData().AllowedWeaponSelectionTypes = new List<WeaponSelectionType> {weaponSelectionType};
        Configs.GetConfigData().UsableWeapons = new List<CsItem> { };
        if (allowedItem is not null)
        {
            Configs.GetConfigData().UsableWeapons.Add(allowedItem.Value);
        }

        var args = new List<string> {itemName};
        var result = await OnWeaponCommandHelper.HandleAsync(args, TestSteamId, RoundType.FullBuy, team, false);

        Assert.That(result.Item1, Does.Contain(message));
        Assert.That(result.Item2, Is.EqualTo(expectedItem));

        var setWeapon = (await Queries.GetUserSettings(TestSteamId))
            ?.GetWeaponPreference(team, WeaponAllocationType.FullBuyPrimary);
        Assert.That(setWeapon, Is.EqualTo(expectedItem));
    }

    [Test]
    public async Task PreferredScoutCanBeSetAndRemoved()
    {
        var args = new List<string> {"weapon_ssg08"};
        var team = CsTeam.CounterTerrorist;

        await OnWeaponCommandHelper.HandleAsync(args, TestSteamId, RoundType.FullBuy, team, false);

        var userSettings = await Queries.GetUserSettings(TestSteamId);
        Assert.That(
            userSettings?.GetWeaponPreference(team, WeaponAllocationType.Preferred),
            Is.EqualTo(CsItem.Scout));
        Assert.That(
            userSettings?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Preferred),
            Is.EqualTo(CsItem.Scout));

        var allocationType =
            WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(RoundType.FullBuy, team, CsItem.Scout);
        Assert.That(allocationType, Is.EqualTo(WeaponAllocationType.Preferred));

        await OnWeaponCommandHelper.HandleAsync(args, TestSteamId, RoundType.FullBuy, team, true);

        userSettings = await Queries.GetUserSettings(TestSteamId);
        Assert.That(
            userSettings?.GetWeaponPreference(team, WeaponAllocationType.Preferred),
            Is.EqualTo(null));
        Assert.That(
            userSettings?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Preferred),
            Is.EqualTo(null));
    }

    [Test]
    [Retry(3)]
    public void RandomWeaponSelection()
    {
        Configs.OverrideConfigDataForTests(new ConfigData
        {
            RoundTypePercentages = new()
            {
                {RoundType.Pistol, 5},
                {RoundType.HalfBuy, 5},
                {RoundType.FullBuy, 90},
            },
            RoundTypeSelection = RoundTypeSelectionOption.Random,
        });
        var numPistol = 0;
        var numHalfBuy = 0;
        var numFullBuy = 0;
        for (var i = 0; i < 1000; i++)
        {
            var randomRoundType = RoundTypeManager.Instance.GetNextRoundType();
            switch (randomRoundType)
            {
                case RoundType.Pistol:
                    numPistol++;
                    break;
                case RoundType.HalfBuy:
                    numHalfBuy++;
                    break;
                case RoundType.FullBuy:
                    numFullBuy++;
                    break;
            }
        }

        // Ranges are very permissive to avoid flakes
        Assert.That(numPistol, Is.InRange(20, 80));
        Assert.That(numHalfBuy, Is.InRange(20, 80));
        Assert.That(numFullBuy, Is.InRange(850, 950));
    }

    [Test]
    public async Task SsgChanceDisablesDistributionWhenZero()
    {
        var (allocations, roundType) = await RunSsgRoundAsync(new ConfigData
        {
            ChanceForAwpWeapon = 0,
            ChanceForSsgWeapon = 0,
        });

        Assert.That(roundType, Is.EqualTo(RoundType.FullBuy));
        Assert.That(allocations.ContainsKey(1), Is.True);
        Assert.That(allocations.ContainsKey(2), Is.True);
        Assert.That(allocations[1], Does.Not.Contain(CsItem.Scout));
        Assert.That(allocations[2], Does.Not.Contain(CsItem.Scout));
    }

    [Test]
    public async Task SsgChanceEnablesDistributionWhenConfigured()
    {
        var (allocations, _) = await RunSsgRoundAsync(new ConfigData
        {
            ChanceForAwpWeapon = 0,
            ChanceForSsgWeapon = 100,
        });

        Assert.That(allocations[1], Does.Contain(CsItem.Scout));
        Assert.That(allocations[2], Does.Contain(CsItem.Scout));
    }

    [Test]
    public async Task SsgRespectsPerTeamLimits()
    {
        var (allocations, _) = await RunSsgRoundAsync(new ConfigData
        {
            ChanceForAwpWeapon = 0,
            ChanceForSsgWeapon = 100,
            MaxSsgWeaponsPerTeam = new()
            {
                {CsTeam.Terrorist, 0},
                {CsTeam.CounterTerrorist, 0},
            },
        });

        Assert.That(allocations[1], Does.Not.Contain(CsItem.Scout));
        Assert.That(allocations[2], Does.Not.Contain(CsItem.Scout));
    }

    [Test]
    public async Task RandomSniperPreferenceRespectsTeamLimits()
    {
        var config = new ConfigData
        {
            ChanceForAwpWeapon = 100,
            ChanceForSsgWeapon = 100,
            MaxAwpWeaponsPerTeam = new()
            {
                {CsTeam.Terrorist, 1},
                {CsTeam.CounterTerrorist, 1},
            },
            MaxSsgWeaponsPerTeam = new()
            {
                {CsTeam.Terrorist, 1},
                {CsTeam.CounterTerrorist, 1},
            },
        };

        Configs.OverrideConfigDataForTests(config);
        RoundTypeManager.Instance.Initialize();

        var players = new List<int> {1, 2, 3, 4};

        foreach (var player in players)
        {
            var team = player <= 2 ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
            await Queries.SetWeaponPreferenceForUserAsync(
                (ulong)player,
                team,
                WeaponAllocationType.Preferred,
                WeaponHelpers.RandomSniperPreference
            );
        }

        try
        {
            for (var i = 0; i < 25; i++)
            {
                RoundTypeManager.Instance.SetNextRoundTypeOverride(RoundType.FullBuy);

                var allocations = new Dictionary<int, List<CsItem>>();
                OnRoundPostStartHelper.Handle(
                    players,
                    p => (ulong)p,
                    p => p <= 2 ? CsTeam.Terrorist : CsTeam.CounterTerrorist,
                    _ => { },
                    (player, items, _) => { allocations[player] = new(items); },
                    _ => true,
                    _ => true,
                    _ => false,
                    out var roundType
                );

                Assert.That(roundType, Is.EqualTo(RoundType.FullBuy));

                int CountTeamItems(Func<int, bool> isTeamPlayer, Func<CsItem, bool> predicate) =>
                    allocations
                        .Where(kvp => isTeamPlayer(kvp.Key))
                        .SelectMany(kvp => kvp.Value)
                        .Count(predicate);

                var tAwpCount = CountTeamItems(
                    player => player <= 2,
                    item => item is CsItem.AWP or CsItem.AutoSniperT or CsItem.AutoSniperCT
                );
                var tScoutCount = CountTeamItems(player => player <= 2, item => item == CsItem.Scout);

                var ctAwpCount = CountTeamItems(
                    player => player > 2,
                    item => item is CsItem.AWP or CsItem.AutoSniperT or CsItem.AutoSniperCT
                );
                var ctScoutCount = CountTeamItems(player => player > 2, item => item == CsItem.Scout);

                Assert.LessOrEqual(tAwpCount, config.MaxAwpWeaponsPerTeam[CsTeam.Terrorist]);
                Assert.LessOrEqual(tScoutCount, config.MaxSsgWeaponsPerTeam[CsTeam.Terrorist]);
                Assert.LessOrEqual(ctAwpCount, config.MaxAwpWeaponsPerTeam[CsTeam.CounterTerrorist]);
                Assert.LessOrEqual(ctScoutCount, config.MaxSsgWeaponsPerTeam[CsTeam.CounterTerrorist]);
            }
        }
        finally
        {
            RoundTypeManager.Instance.SetNextRoundTypeOverride(null);
        }
    }

    private async Task<(Dictionary<int, List<CsItem>> allocations, RoundType roundType)> RunSsgRoundAsync(
        ConfigData configData
    )
    {
        Configs.OverrideConfigDataForTests(configData);
        RoundTypeManager.Instance.Initialize();
        RoundTypeManager.Instance.SetNextRoundTypeOverride(RoundType.FullBuy);

        var players = new List<int> {1, 2};
        await Queries.SetWeaponPreferenceForUserAsync(1, CsTeam.Terrorist, WeaponAllocationType.Preferred,
            CsItem.Scout);
        await Queries.SetWeaponPreferenceForUserAsync(2, CsTeam.CounterTerrorist, WeaponAllocationType.Preferred,
            CsItem.Scout);

        var allocations = new Dictionary<int, List<CsItem>>();
        try
        {
            OnRoundPostStartHelper.Handle(
                players,
                p => (ulong)p,
                p => p == 1 ? CsTeam.Terrorist : CsTeam.CounterTerrorist,
                _ => { },
                (player, items, _) => { allocations[player] = new List<CsItem>(items); },
                _ => true,
                _ => true,
                _ => false,
                out var roundType
            );

            return (allocations, roundType);
        }
        finally
        {
            RoundTypeManager.Instance.SetNextRoundTypeOverride(null);
        }
    }
    [Test]
    public async Task EnableAllWeaponsConfigAllowsCrossTeamWeapons()
    {
        Configs.GetConfigData().EnableAllWeaponsForEveryone = true;

        var team = CsTeam.CounterTerrorist;

        var result = await OnWeaponCommandHelper.HandleAsync(new[] {"galil"}, TestSteamId, RoundType.FullBuy, team, false);
        Assert.That(result.Item2, Is.EqualTo(CsItem.Galil));
        Assert.That(result.Item1, Does.Not.Contain("not valid"));
        var preference = (await Queries.GetUserSettings(TestSteamId))
            ?.GetWeaponPreference(team, WeaponAllocationType.FullBuyPrimary);
        Assert.That(preference, Is.EqualTo(CsItem.Galil));

        result = await OnWeaponCommandHelper.HandleAsync(new[] {"tec9"}, TestSteamId, RoundType.Pistol, team, false);
        Assert.That(result.Item2, Is.EqualTo(CsItem.Tec9));
        Assert.That(result.Item1, Does.Not.Contain("not valid"));
        preference = (await Queries.GetUserSettings(TestSteamId))
            ?.GetWeaponPreference(team, WeaponAllocationType.PistolRound);
        Assert.That(preference, Is.EqualTo(CsItem.Tec9));
    }

}


