using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using System.Text.Json;

namespace RetakesAllocatorTest;

public class ConfigTests : BaseTestFixture
{
    [Test]
    public void TestDefaultWeaponsValidation()
    {
        var usableWeapons = WeaponHelpers.AllWeapons;
        usableWeapons.Remove(CsItem.Glock);
        var warnings = Configs.OverrideConfigDataForTests(
            new ConfigData()
            {
                UsableWeapons = usableWeapons,
            }
        ).Validate();
        Assert.That(warnings[0],
            Is.EqualTo(
                "Glock18 in the DefaultWeapons.Terrorist.PistolRound " +
                "config is not in the UsableWeapons list."));

        var defaults =
            new Dictionary<CsTeam, Dictionary<WeaponAllocationType, CsItem>>(Configs.GetConfigData().DefaultWeapons);
        defaults[CsTeam.Terrorist] = new Dictionary<WeaponAllocationType, CsItem>(defaults[CsTeam.Terrorist]);
        defaults[CsTeam.Terrorist].Remove(WeaponAllocationType.FullBuyPrimary);
        warnings = Configs.OverrideConfigDataForTests(
            new ConfigData()
            {
                DefaultWeapons = defaults
            }
        ).Validate();
        Assert.That(warnings[0], Is.EqualTo("Missing FullBuyPrimary in DefaultWeapons.Terrorist config."));

        defaults.Remove(CsTeam.CounterTerrorist);
        warnings = Configs.OverrideConfigDataForTests(
            new ConfigData()
            {
                DefaultWeapons = defaults
            }
        ).Validate();
        Assert.That(warnings[0], Is.EqualTo("Missing FullBuyPrimary in DefaultWeapons.Terrorist config."));
        Assert.That(warnings[1], Is.EqualTo("Missing CounterTerrorist in DefaultWeapons config."));

        defaults[CsTeam.Terrorist][WeaponAllocationType.FullBuyPrimary] = CsItem.Kevlar;
        var error = Assert.Catch(() =>
        {
            Configs.OverrideConfigDataForTests(
                new ConfigData()
                {
                    DefaultWeapons = defaults
                }
            );
        });
        Assert.That(error?.Message,
            Is.EqualTo("Kevlar is not a valid weapon in config DefaultWeapons.Terrorist.FullBuyPrimary."));

        defaults =
            new Dictionary<CsTeam, Dictionary<WeaponAllocationType, CsItem>>(Configs.GetConfigData().DefaultWeapons);
        defaults[CsTeam.Terrorist][WeaponAllocationType.Preferred] = CsItem.AWP;
        error = Assert.Catch(() =>
        {
            Configs.OverrideConfigDataForTests(
                new ConfigData()
                {
                    DefaultWeapons = defaults
                }
            );
        });
        Assert.That(error?.Message, Is.EqualTo(
            "Preferred is not a valid default weapon allocation type for config DefaultWeapons.Terrorist."
        ));
    }

    [Test]
    public void CategorizedConfigLayoutRoundTripPreservesSettings()
    {
        var config = new ConfigData
        {
            ResetStateOnGameRestart = false,
            AllowAllocationAfterFreezeTime = false,
            UseOnTickFeatures = false,
            CapabilityWeaponPaints = false,
            EnableRoundTypeAnnouncement = false,
            EnableRoundTypeAnnouncementCenter = true,
            EnableBombSiteAnnouncementCenter = true,
            BombSiteAnnouncementCenterToCTOnly = true,
            DisableDefaultBombPlantedCenterMessage = true,
            ForceCloseBombSiteAnnouncementCenterOnPlant = false,
            BombSiteAnnouncementCenterDelay = 3.5f,
            BombSiteAnnouncementCenterShowTimer = 6.0f,
            EnableBombSiteAnnouncementChat = true,
            EnableNextRoundTypeVoting = true,
            EnableCanAcquireHook = false,
            ChatMessagePluginName = "Allocator",
            ChatMessagePluginPrefix = "[RA]",
            InGameGunMenuCenterCommands = "!guns2",
            AutoUpdateSignatures = false,
            RoundTypeSelection = RoundTypeSelectionOption.ManualOrdering,
            RoundTypeRandomFixedCounts = new Dictionary<RoundType, int>
            {
                {RoundType.Pistol, 2},
                {RoundType.HalfBuy, 5},
                {RoundType.FullBuy, 10},
            },
            RoundTypeManualOrdering = new List<RoundTypeManualOrderingItem>
            {
                new(RoundType.Pistol, 2),
                new(RoundType.FullBuy, 3),
            },
            UsableWeapons = new List<CsItem> {CsItem.AWP, CsItem.Deagle},
            AllowedWeaponSelectionTypes = new List<WeaponSelectionType> {WeaponSelectionType.Random},
            DefaultWeapons = new Dictionary<CsTeam, Dictionary<WeaponAllocationType, CsItem>>
            {
                [CsTeam.Terrorist] = new()
                {
                    {WeaponAllocationType.PistolRound, CsItem.Glock},
                    {WeaponAllocationType.Secondary, CsItem.Tec9},
                    {WeaponAllocationType.HalfBuyPrimary, CsItem.Galil},
                    {WeaponAllocationType.FullBuyPrimary, CsItem.AK47},
                },
                [CsTeam.CounterTerrorist] = new()
                {
                    {WeaponAllocationType.PistolRound, CsItem.USPS},
                    {WeaponAllocationType.Secondary, CsItem.FiveSeven},
                    {WeaponAllocationType.HalfBuyPrimary, CsItem.MP5},
                    {WeaponAllocationType.FullBuyPrimary, CsItem.M4A1S},
                },
            },
            EnableAllWeaponsForEveryone = true,
            MaxNades = new Dictionary<string, Dictionary<CsTeam, Dictionary<CsItem, int>>>
            {
                [NadeHelpers.GlobalSettingName] = new()
                {
                    [CsTeam.Terrorist] = new()
                    {
                        {CsItem.Flashbang, 1},
                        {CsItem.Smoke, 2},
                    },
                    [CsTeam.CounterTerrorist] = new()
                    {
                        {CsItem.HE, 1},
                    },
                }
            },
            MaxTeamNades = new Dictionary<string, Dictionary<CsTeam, Dictionary<RoundType, MaxTeamNadesSetting>>>
            {
                [NadeHelpers.GlobalSettingName] = new()
                {
                    [CsTeam.Terrorist] = new()
                    {
                        {RoundType.Pistol, MaxTeamNadesSetting.Two},
                    }
                }
            },
            EnableAwp = 1,
            AwpPermission = "@css/custom-awp",
            ChanceForAwpWeapon = 55,
            MaxAwpWeaponsPerTeam = new Dictionary<CsTeam, int>
            {
                [CsTeam.Terrorist] = 2,
                [CsTeam.CounterTerrorist] = 2,
            },
            MinPlayersPerTeamForAwpWeapon = new Dictionary<CsTeam, int>
            {
                [CsTeam.Terrorist] = 2,
                [CsTeam.CounterTerrorist] = 3,
            },
            EnableSsg = 1,
            SsgPermission = "@css/custom-ssg",
            ChanceForSsgWeapon = 65,
            MaxSsgWeaponsPerTeam = new Dictionary<CsTeam, int>
            {
                [CsTeam.Terrorist] = 3,
                [CsTeam.CounterTerrorist] = 3,
            },
            MinPlayersPerTeamForSsgWeapon = new Dictionary<CsTeam, int>
            {
                [CsTeam.Terrorist] = 1,
                [CsTeam.CounterTerrorist] = 2,
            },
            EnableEnemyStuff = 1,
            EnemyStuffPermission = "@css/custom",
            ChanceForEnemyStuff = 80,
            MaxEnemyStuffPerTeam = new Dictionary<CsTeam, int>
            {
                {CsTeam.Terrorist, 3},
                {CsTeam.CounterTerrorist, 3},
            },
            EnableZeus = 1,
            ChanceForZeusWeapon = 45,
            MaxZeusPerTeam = new Dictionary<CsTeam, int>
            {
                [CsTeam.Terrorist] = 1,
                [CsTeam.CounterTerrorist] = 2,
            },
            DatabaseProvider = DatabaseProvider.MySql,
            DatabaseConnectionString = "Server=test;",
            MigrateOnStartup = false,
        };

        var layout = ConfigFileLayout.FromConfigData(config);
        var roundTrip = layout.ToConfigData();

        Assert.Multiple(() =>
        {
            Assert.That(roundTrip.ResetStateOnGameRestart, Is.False);
            Assert.That(roundTrip.EnableBombSiteAnnouncementCenter, Is.True);
            Assert.That(roundTrip.BombSiteAnnouncementCenterDelay, Is.EqualTo(3.5f));
            Assert.That(roundTrip.EnableNextRoundTypeVoting, Is.True);
            Assert.That(roundTrip.ChatMessagePluginPrefix, Is.EqualTo("[RA]"));
            Assert.That(roundTrip.AutoUpdateSignatures, Is.False);
            Assert.That(roundTrip.RoundTypeSelection, Is.EqualTo(RoundTypeSelectionOption.ManualOrdering));
            Assert.That(roundTrip.RoundTypeRandomFixedCounts[RoundType.FullBuy], Is.EqualTo(10));
            Assert.That(roundTrip.RoundTypeManualOrdering.Count, Is.EqualTo(2));
            Assert.That(roundTrip.UsableWeapons, Is.EqualTo(config.UsableWeapons));
            Assert.That(roundTrip.AllowedWeaponSelectionTypes, Is.EqualTo(config.AllowedWeaponSelectionTypes));
            Assert.That(roundTrip.DefaultWeapons[CsTeam.Terrorist][WeaponAllocationType.HalfBuyPrimary], Is.EqualTo(CsItem.Galil));
            Assert.That(roundTrip.EnableAllWeaponsForEveryone, Is.True);
            Assert.That(roundTrip.MaxNades[NadeHelpers.GlobalSettingName][CsTeam.Terrorist][CsItem.Smoke], Is.EqualTo(2));
            Assert.That(roundTrip.MaxTeamNades[NadeHelpers.GlobalSettingName][CsTeam.Terrorist][RoundType.Pistol], Is.EqualTo(MaxTeamNadesSetting.Two));
            Assert.That(roundTrip.EnableAwp, Is.EqualTo(1));
            Assert.That(roundTrip.AwpPermission, Is.EqualTo("@css/custom-awp"));
            Assert.That(roundTrip.ChanceForAwpWeapon, Is.EqualTo(55));
            Assert.That(roundTrip.MaxAwpWeaponsPerTeam[CsTeam.CounterTerrorist], Is.EqualTo(2));
            Assert.That(roundTrip.MinPlayersPerTeamForAwpWeapon[CsTeam.CounterTerrorist], Is.EqualTo(3));
            Assert.That(roundTrip.EnableSsg, Is.EqualTo(1));
            Assert.That(roundTrip.SsgPermission, Is.EqualTo("@css/custom-ssg"));
            Assert.That(roundTrip.MaxSsgWeaponsPerTeam[CsTeam.Terrorist], Is.EqualTo(3));
            Assert.That(roundTrip.EnableEnemyStuff, Is.EqualTo(1));
            Assert.That(roundTrip.EnemyStuffPermission, Is.EqualTo("@css/custom"));
            Assert.That(roundTrip.ChanceForEnemyStuff, Is.EqualTo(80));
            Assert.That(roundTrip.MaxEnemyStuffPerTeam[CsTeam.CounterTerrorist], Is.EqualTo(3));
            Assert.That(roundTrip.EnableZeus, Is.EqualTo(1));
            Assert.That(roundTrip.ChanceForZeusWeapon, Is.EqualTo(45));
            Assert.That(roundTrip.MaxZeusPerTeam[CsTeam.CounterTerrorist], Is.EqualTo(2));
            Assert.That(roundTrip.DatabaseProvider, Is.EqualTo(DatabaseProvider.MySql));
            Assert.That(roundTrip.DatabaseConnectionString, Is.EqualTo("Server=test;"));
            Assert.That(roundTrip.MigrateOnStartup, Is.False);
        });
    }

    [Test]
    public void StringifyConfigOutputsCategorizedJson()
    {
        Configs.OverrideConfigDataForTests(new ConfigData());

        var json = Configs.StringifyConfig(null);
        Assert.That(json, Is.Not.Null);

        using var document = JsonDocument.Parse(json!);
        Assert.That(document.RootElement.TryGetProperty("Config", out _), Is.True);
        Assert.That(document.RootElement.TryGetProperty("AWP", out _), Is.True);
        Assert.That(document.RootElement.TryGetProperty("Weapons", out _), Is.True);
    }
}
