using System.Linq;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorTest;

public class WeaponHelpersTests : BaseTestFixture
{
    [Test]
    [TestCase(true, true, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    public void TestIsWeaponAllocationAllowed(bool allowAfterFreezeTime, bool isFreezeTime, bool expected)
    {
        Configs.OverrideConfigDataForTests(new ConfigData() {AllowAllocationAfterFreezeTime = allowAfterFreezeTime});

        var canAllocate = WeaponHelpers.IsWeaponAllocationAllowed(isFreezeTime);

        Assert.That(canAllocate, Is.EqualTo(expected));
    }

    [Test]
    public void EnableAllWeaponsConfigAllowsCrossTeamOptions()
    {
        Configs.GetConfigData().EnableAllWeaponsForEveryone = true;

        var team = Utils.ParseTeam("CT");
        var weapons = WeaponHelpers.GetPossibleWeaponsForAllocationType(
            WeaponAllocationType.FullBuyPrimary, team);

        var ak47 = WeaponHelpers.FindValidWeaponsByName("ak47").First();
        var m4a1s = WeaponHelpers.FindValidWeaponsByName("m4a1s").First();

        Assert.That(weapons, Does.Contain(ak47));
        Assert.That(weapons, Does.Contain(m4a1s));
    }

    [Test]
    public void EnemyStuffPreferenceSwapsPrimaryWeapon()
    {
        var config = new ConfigData
        {
            EnableEnemyStuff = 1,
            ChanceForEnemyStuff = 100,
            AllowedWeaponSelectionTypes = new List<WeaponSelectionType>
            {
                WeaponSelectionType.PlayerChoice,
                WeaponSelectionType.Default
            },
        };

        Configs.OverrideConfigDataForTests(config);

        var userSetting = new UserSetting
        {
            EnemyStuffTeamPreference = EnemyStuffTeamPreference.CounterTerrorist,
        };
        userSetting.SetWeaponPreference(
            CsTeam.Terrorist,
            WeaponAllocationType.FullBuyPrimary,
            CsItem.Galil
        );
        userSetting.SetWeaponPreference(
            CsTeam.CounterTerrorist,
            WeaponAllocationType.FullBuyPrimary,
            CsItem.M4A1S
        );

        var selection = WeaponHelpers.GetWeaponsForRoundType(
            RoundType.FullBuy,
            CsTeam.CounterTerrorist,
            userSetting,
            givePreferred: false
        );

        var weapons = selection.Weapons.ToList();

        var primary = weapons.Last();
        var terroristPrimaries =
            WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.FullBuyPrimary, CsTeam.Terrorist);

        Assert.That(primary, Is.EqualTo(CsItem.Galil));
        Assert.That(terroristPrimaries, Does.Contain(primary));
        Assert.That(selection.EnemyStuffGranted, Is.True);
    }

    [Test]
    public void EnemyStuffPreferenceSwapsPistolRoundWeapon()
    {
        var config = new ConfigData
        {
            EnableEnemyStuff = 1,
            ChanceForEnemyStuff = 100,
            AllowedWeaponSelectionTypes = new List<WeaponSelectionType>
            {
                WeaponSelectionType.PlayerChoice,
                WeaponSelectionType.Default
            },
        };

        Configs.OverrideConfigDataForTests(config);

        var userSetting = new UserSetting
        {
            EnemyStuffTeamPreference = EnemyStuffTeamPreference.CounterTerrorist,
        };
        userSetting.SetWeaponPreference(
            CsTeam.Terrorist,
            WeaponAllocationType.PistolRound,
            CsItem.Tec9
        );
        userSetting.SetWeaponPreference(
            CsTeam.CounterTerrorist,
            WeaponAllocationType.PistolRound,
            CsItem.USPS
        );

        var selection = WeaponHelpers.GetWeaponsForRoundType(
            RoundType.Pistol,
            CsTeam.CounterTerrorist,
            userSetting,
            givePreferred: false
        );

        var weapons = selection.Weapons.ToList();

        Assert.That(weapons, Has.Count.EqualTo(1));
        Assert.That(weapons[0], Is.EqualTo(CsItem.Tec9));
        Assert.That(selection.EnemyStuffGranted, Is.True);
    }

    [Test]
    public void EnemyStuffPreferenceHonorsSelectedTeams()
    {
        var config = new ConfigData
        {
            EnableEnemyStuff = 1,
            ChanceForEnemyStuff = 100,
            AllowedWeaponSelectionTypes = new List<WeaponSelectionType>
            {
                WeaponSelectionType.PlayerChoice,
                WeaponSelectionType.Default
            },
        };

        Configs.OverrideConfigDataForTests(config);

        var userSetting = new UserSetting
        {
            EnemyStuffTeamPreference = EnemyStuffTeamPreference.Terrorist,
        };
        userSetting.SetWeaponPreference(
            CsTeam.Terrorist,
            WeaponAllocationType.FullBuyPrimary,
            CsItem.Galil
        );
        userSetting.SetWeaponPreference(
            CsTeam.CounterTerrorist,
            WeaponAllocationType.FullBuyPrimary,
            CsItem.M4A1S
        );

        var ctSelection = WeaponHelpers.GetWeaponsForRoundType(
            RoundType.FullBuy,
            CsTeam.CounterTerrorist,
            userSetting,
            givePreferred: false
        );

        Assert.That(ctSelection.EnemyStuffGranted, Is.False);

        var terroristSelection = WeaponHelpers.GetWeaponsForRoundType(
            RoundType.FullBuy,
            CsTeam.Terrorist,
            userSetting,
            givePreferred: false
        );

        Assert.That(terroristSelection.EnemyStuffGranted, Is.True);
        Assert.That(terroristSelection.Weapons.Last(), Is.EqualTo(CsItem.M4A1S));
    }

    [Test]
    public void EnemyStuffQuotaBlocksSwapWhenUnavailable()
    {
        var config = new ConfigData
        {
            EnableEnemyStuff = 1,
            ChanceForEnemyStuff = 100,
            AllowedWeaponSelectionTypes = new List<WeaponSelectionType>
            {
                WeaponSelectionType.PlayerChoice,
                WeaponSelectionType.Default
            },
        };

        Configs.OverrideConfigDataForTests(config);

        var userSetting = new UserSetting
        {
            EnemyStuffTeamPreference = EnemyStuffTeamPreference.CounterTerrorist,
        };
        userSetting.SetWeaponPreference(
            CsTeam.Terrorist,
            WeaponAllocationType.FullBuyPrimary,
            CsItem.Galil
        );

        var selection = WeaponHelpers.GetWeaponsForRoundType(
            RoundType.FullBuy,
            CsTeam.CounterTerrorist,
            userSetting,
            givePreferred: false,
            enemyStuffQuotaAvailable: false
        );

        var weapons = selection.Weapons.ToList();

        Assert.That(selection.EnemyStuffGranted, Is.False);
        Assert.That(weapons.Last(), Is.EqualTo(CsItem.M4A1S));
    }
}
