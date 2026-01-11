using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace RetakesAllocatorCore.Config;

public static class Configs
{
    public static class Shared
    {
        public static string? Module { get; set; }
    }

    private static readonly string ConfigDirectoryName = "config";
    private static readonly string ConfigFileName = "config.json";

    private static string? _configFilePath;
    private static ConfigData? _configData;

    private static readonly JsonSerializerOptions SerializationOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        },
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static readonly Dictionary<string, Func<ConfigFileLayout, object>> CategorizedConfigExtractors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Config"] = layout => layout.Config!,
            ["RoundTypes"] = layout => layout.RoundTypes!,
            ["Weapons"] = layout => layout.Weapons!,
            ["Nades"] = layout => layout.Nades!,
            ["AWP"] = layout => layout.AWP!,
            ["SSG"] = layout => layout.SSG!,
            ["EnemyStuff"] = layout => layout.EnemyStuff!,
            ["Zeus"] = layout => layout.Zeus!,
            ["Warmup"] = layout => layout.Warmup!,
            ["Database"] = layout => layout.Database!,
        };

    public static bool IsLoaded()
    {
        return _configData is not null;
    }

    public static ConfigData GetConfigData()
    {
        if (_configData is null)
        {
            throw new Exception("Config not yet loaded.");
        }

        return _configData;
    }

    public static ConfigData Load(string modulePath, bool saveAfterLoad = false)
    {
        var configFileDirectory = ResolveConfigDirectory(modulePath);
        Directory.CreateDirectory(configFileDirectory);

        _configFilePath = Path.Combine(configFileDirectory, ConfigFileName);
        if (File.Exists(_configFilePath))
        {
            var json = File.ReadAllText(_configFilePath);
            _configData = DeserializeConfigData(json);
        }
        else
        {
            _configData = new ConfigData();
        }

        if (_configData is null)
        {
            throw new Exception("Failed to load configs.");
        }

        if (saveAfterLoad)
        {
            SaveConfigData(_configData);
        }

        _configData.Validate();

        return _configData;
    }

    public static ConfigData OverrideConfigDataForTests(
        ConfigData configData
    )
    {
        configData.Validate();
        _configData = configData;
        return _configData;
    }

    private static void SaveConfigData(ConfigData configData)
    {
        if (_configFilePath is null)
        {
            throw new Exception("Config not yet loaded.");
        }

        var layout = ConfigFileLayout.FromConfigData(configData);
        File.WriteAllText(_configFilePath, JsonSerializer.Serialize(layout, SerializationOptions));
    }

    public static string? StringifyConfig(string? configName)
    {
        var configData = GetConfigData();
        if (configName is null)
        {
            return JsonSerializer.Serialize(ConfigFileLayout.FromConfigData(configData), SerializationOptions);
        }

        if (CategorizedConfigExtractors.TryGetValue(configName, out var extractor))
        {
            var layout = ConfigFileLayout.FromConfigData(configData);
            return JsonSerializer.Serialize(extractor(layout), SerializationOptions);
        }
        var property = configData.GetType().GetProperty(configName);
        if (property is null)
        {
            return null;
        }
        return JsonSerializer.Serialize(property.GetValue(configData), SerializationOptions);
    }

    private static ConfigData DeserializeConfigData(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in CategorizedConfigExtractors.Keys)
            {
                if (!document.RootElement.TryGetProperty(propertyName, out _))
                {
                    continue;
                }

                var layout = JsonSerializer.Deserialize<ConfigFileLayout>(json, SerializationOptions);
                if (layout is null)
                {
                    throw new Exception("Failed to parse categorized config.");
                }

                return layout.ToConfigData();
            }
        }

        var legacyConfig = JsonSerializer.Deserialize<ConfigData>(json, SerializationOptions);
        if (legacyConfig is null)
        {
            throw new Exception("Failed to load configs.");
        }

        return legacyConfig;
    }

    public static string GetConfigDirectory(string modulePath) => ResolveConfigDirectory(modulePath);

    private static string ResolveConfigDirectory(string modulePath)
    {
        var moduleFullPath = Path.GetFullPath(modulePath);
        var (cssDirectory, pluginName) = TryGetCounterStrikeSharpDirectory(moduleFullPath);
        if (cssDirectory is not null && pluginName is not null)
        {
            return Path.Combine(cssDirectory, "configs", "plugins", pluginName);
        }

        return Path.Combine(moduleFullPath, ConfigDirectoryName);
    }

    private static (string? CssDirectory, string? PluginName) TryGetCounterStrikeSharpDirectory(string moduleFullPath)
    {
        var current = new DirectoryInfo(moduleFullPath);
        while (current is not null)
        {
            var parent = current.Parent;
            var grandParent = parent?.Parent;
            if (parent is not null &&
                grandParent is not null &&
                string.Equals(parent.Name, "plugins", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(grandParent.Name, "counterstrikesharp", StringComparison.OrdinalIgnoreCase))
            {
                return (grandParent.FullName, current.Name);
            }

            current = parent;
        }

        return (null, null);
    }
}

public enum WeaponSelectionType
{
    PlayerChoice,
    Random,
    Default,
}

public enum DatabaseProvider
{
    Sqlite,
    MySql,
}

public enum RoundTypeSelectionOption
{
    Random,
    RandomFixedCounts,
    ManualOrdering,
}

public enum AccessMode
{
    Disabled = 0,
    Everyone = 1,
    VipOnly = 2,
}

public record RoundTypeManualOrderingItem(RoundType Type, int Count);

public record ConfigData
{
    public List<CsItem> UsableWeapons { get; set; } = WeaponHelpers.AllWeapons;
    public bool EnableWeaponShotguns { get; set; } = false;
    public bool EnableWeaponPms { get; set; } = false;

    public List<WeaponSelectionType> AllowedWeaponSelectionTypes { get; set; } =
        Enum.GetValues<WeaponSelectionType>().ToList();

    public Dictionary<CsTeam, Dictionary<WeaponAllocationType, CsItem>> DefaultWeapons { get; set; } =
        WeaponHelpers.DefaultWeaponsByTeamAndAllocationType;

    public Dictionary<
        string,
        Dictionary<
            CsTeam,
            Dictionary<CsItem, int>
        >
    > MaxNades { get; set; } = new()
    {
        {
            NadeHelpers.GlobalSettingName, new()
            {
                {
                    CsTeam.Terrorist, new()
                    {
                        {CsItem.Flashbang, 2},
                        {CsItem.Smoke, 1},
                        {CsItem.Molotov, 1},
                        {CsItem.HE, 1},
                    }
                },
                {
                    CsTeam.CounterTerrorist, new()
                    {
                        {CsItem.Flashbang, 2},
                        {CsItem.Smoke, 1},
                        {CsItem.Incendiary, 2},
                        {CsItem.HE, 1},
                    }
                },
            }
        }
    };

    public Dictionary<
        string,
        Dictionary<
            CsTeam,
            Dictionary<RoundType, MaxTeamNadesSetting>
        >
    > MaxTeamNades { get; set; } = new()
    {
        {
            NadeHelpers.GlobalSettingName, new()
            {
                {
                    CsTeam.Terrorist, new()
                    {
                        {RoundType.Pistol, MaxTeamNadesSetting.AverageOnePerPlayer},
                        {RoundType.HalfBuy, MaxTeamNadesSetting.AverageOnePointFivePerPlayer},
                        {RoundType.FullBuy, MaxTeamNadesSetting.AverageOnePointFivePerPlayer},
                    }
                },
                {
                    CsTeam.CounterTerrorist, new()
                    {
                        {RoundType.Pistol, MaxTeamNadesSetting.AverageOnePerPlayer},
                        {RoundType.HalfBuy, MaxTeamNadesSetting.AverageOnePointFivePerPlayer},
                        {RoundType.FullBuy, MaxTeamNadesSetting.AverageOnePointFivePerPlayer},
                    }
                },
            }
        }
    };

    public RoundTypeSelectionOption RoundTypeSelection { get; set; } = RoundTypeSelectionOption.Random;

    public Dictionary<RoundType, int> RoundTypePercentages { get; set; } = new()
    {
        {RoundType.Pistol, 15},
        {RoundType.HalfBuy, 25},
        {RoundType.FullBuy, 60},
    };

    public Dictionary<RoundType, int> RoundTypeRandomFixedCounts { get; set; } = new()
    {
        {RoundType.Pistol, 5},
        {RoundType.HalfBuy, 10},
        {RoundType.FullBuy, 15},
    };

    public List<RoundTypeManualOrderingItem> RoundTypeManualOrdering { get; set; } = new()
    {
        new RoundTypeManualOrderingItem(RoundType.Pistol, 5),
        new RoundTypeManualOrderingItem(RoundType.HalfBuy, 10),
        new RoundTypeManualOrderingItem(RoundType.FullBuy, 15),
    };

    public bool MigrateOnStartup { get; set; } = true;
    public bool ResetStateOnGameRestart { get; set; } = true;
    public bool AllowAllocationAfterFreezeTime { get; set; } = true;
    public bool UseOnTickFeatures { get; set; } = true;
    public bool CapabilityWeaponPaints { get; set; } = true;
    public bool EnableRoundTypeAnnouncement { get; set; } = true;
    public bool EnableRoundTypeAnnouncementCenter { get; set; } = false;
    public bool EnableBombSiteAnnouncementCenter { get; set; } = false;
    public bool BombSiteAnnouncementCenterToCTOnly { get; set; } = false;
    public bool DisableDefaultBombPlantedCenterMessage { get; set; } = false;
    public bool ForceCloseBombSiteAnnouncementCenterOnPlant { get; set; } = true;
    public float BombSiteAnnouncementCenterDelay { get; set; } = 1.0f;
    public float BombSiteAnnouncementCenterShowTimer { get; set; } = 5.0f;
    public bool EnableBombSiteAnnouncementChat { get; set; } = false;
    public bool CustomWarmup { get; set; } = false;
    public CsItem CustomWarmupWeaponCT { get; set; } = CsItem.M4A4;
    public CsItem CustomWarmupWeaponT { get; set; } = CsItem.AK47;
    public bool EnableNextRoundTypeVoting { get; set; } = false;
    public bool EnableAllWeaponsForEveryone { get; set; } = false;
    public int EnableZeus { get; set; } = 0;
    public double ChanceForZeusWeapon { get; set; } = 100;
    public Dictionary<CsTeam, int> MaxZeusPerTeam { get; set; } = new()
    {
        {CsTeam.Terrorist, 2},
        {CsTeam.CounterTerrorist, 2},
    };
    public int EnableEnemyStuff { get; set; } = 2;
    public string EnemyStuffPermission { get; set; } = "@css/vip";
    public int EnableAwp { get; set; } = 2;
    public string AwpPermission { get; set; } = "@css/vip";
    public int EnableSsg { get; set; } = 2;
    public string SsgPermission { get; set; } = "@css/vip";

    public double ChanceForAwpWeapon { get; set; } = 100;

    public double ChanceForSsgWeapon { get; set; } = 100;

    public double ChanceForEnemyStuff { get; set; } = 0;
    [JsonConverter(typeof(PerTeamLimitConverter))]
    public Dictionary<CsTeam, int> MaxEnemyStuffPerTeam { get; set; } = new()
    {
        {CsTeam.Terrorist, -1},
        {CsTeam.CounterTerrorist, -1},
    };

    public Dictionary<CsTeam, int> MaxAwpWeaponsPerTeam { get; set; } = new()
    {
        {CsTeam.Terrorist, 1},
        {CsTeam.CounterTerrorist, 1},
    };

    public Dictionary<CsTeam, int> MaxSsgWeaponsPerTeam { get; set; } = new()
    {
        {CsTeam.Terrorist, 1},
        {CsTeam.CounterTerrorist, 1},
    };

    public Dictionary<CsTeam, int> MinPlayersPerTeamForAwpWeapon { get; set; } = new()
    {
        {CsTeam.Terrorist, 1},
        {CsTeam.CounterTerrorist, 1},
    };

    public Dictionary<CsTeam, int> MinPlayersPerTeamForSsgWeapon { get; set; } = new()
    {
        {CsTeam.Terrorist, 1},
        {CsTeam.CounterTerrorist, 1},
    };

    public bool EnableCanAcquireHook { get; set; } = true;

    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public string ChatMessagePluginName { get; set; } = "Retakes";
    public string? ChatMessagePluginPrefix { get; set; }

    public string InGameGunMenuCenterCommands { get; set; } =
        "guns,!guns,/guns,gun,!gun,!gun";
    public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.Sqlite;
    public string DatabaseConnectionString { get; set; } = "Data Source=data.db; Pooling=False";
    public bool AutoUpdateSignatures { get; set; } = true;

    public bool IsZeusEnabled() => EnableZeus > 0;

    public AccessMode GetAwpMode() => ToAccessMode(EnableAwp);
    public AccessMode GetSsgMode() => ToAccessMode(EnableSsg);
    public AccessMode GetEnemyStuffMode() => ToAccessMode(EnableEnemyStuff);
    public int GetMaxEnemyStuffForTeam(CsTeam team) =>
        MaxEnemyStuffPerTeam.TryGetValue(team, out var max) ? max : -1;

    private static AccessMode ToAccessMode(int value)
    {
        return value switch
        {
            <= 0 => AccessMode.Disabled,
            1 => AccessMode.Everyone,
            _ => AccessMode.VipOnly,
        };
    }

    public IList<string> Validate()
    {
        if (RoundTypePercentages.Values.Sum() != 100)
        {
            throw new Exception("'RoundTypePercentages' values must add up to 100");
        }

        if (ChanceForEnemyStuff is < 0 or > 100)
        {
            throw new Exception("'ChanceForEnemyStuff' must be between 0 and 100");
        }

        if (ChanceForZeusWeapon is < 0 or > 100)
        {
            throw new Exception("'ChanceForZeusWeapon' must be between 0 and 100");
        }

        if (!WeaponHelpers.IsWeapon(CustomWarmupWeaponCT))
        {
            throw new Exception("'CustomWarmupWeaponCT' must be a valid weapon");
        }

        if (!WeaponHelpers.IsWeapon(CustomWarmupWeaponT))
        {
            throw new Exception("'CustomWarmupWeaponT' must be a valid weapon");
        }

        if (EnableAwp is < 0 or > 2)
        {
            throw new Exception("'EnableAwp' must be 0 (disabled), 1 (everyone), or 2 (vip)");
        }

        if (EnableSsg is < 0 or > 2)
        {
            throw new Exception("'EnableSsg' must be 0 (disabled), 1 (everyone), or 2 (vip)");
        }

        if (EnableEnemyStuff is < 0 or > 2)
        {
            throw new Exception("'EnableEnemyStuff' must be 0 (disabled), 1 (everyone), or 2 (vip)");
        }

        foreach (var (team, maxEnemyStuff) in MaxEnemyStuffPerTeam)
        {
            if (maxEnemyStuff < -1)
            {
                throw new Exception($"'MaxEnemyStuffPerTeam.{team}' must be -1 (for unlimited) or a non-negative number");
            }
        }

        foreach (var (team, maxZeus) in MaxZeusPerTeam)
        {
            if (maxZeus < 0)
            {
                throw new Exception($"'MaxZeusPerTeam.{team}' must be a non-negative number");
            }
        }

        var warnings = new List<string>();
        warnings.AddRange(ValidateDefaultWeapons(CsTeam.Terrorist));
        warnings.AddRange(ValidateDefaultWeapons(CsTeam.CounterTerrorist));

        foreach (var warning in warnings)
        {
            Log.Warn($"[CONFIG WARNING] {warning}");
        }

        return warnings;
    }

    private ICollection<string> ValidateDefaultWeapons(CsTeam team)
    {
        var warnings = new List<string>();
        if (!DefaultWeapons.TryGetValue(team, out var defaultWeapons))
        {
            warnings.Add($"Missing {team} in DefaultWeapons config.");
            return warnings;
        }

        if (defaultWeapons.ContainsKey(WeaponAllocationType.Preferred))
        {
            throw new Exception(
                $"Preferred is not a valid default weapon allocation type " +
                $"for config DefaultWeapons.{team}.");
        }

        var allocationTypes = WeaponHelpers.WeaponAllocationTypes;
        allocationTypes.Remove(WeaponAllocationType.Preferred);

        foreach (var allocationType in allocationTypes)
        {
            if (!defaultWeapons.TryGetValue(allocationType, out var w))
            {
                warnings.Add($"Missing {allocationType} in DefaultWeapons.{team} config.");
                continue;
            }

            if (!WeaponHelpers.IsWeapon(w))
            {
                throw new Exception($"{w} is not a valid weapon in config DefaultWeapons.{team}.{allocationType}.");
            }

            if (!UsableWeapons.Contains(w))
            {
                warnings.Add(
                    $"{w} in the DefaultWeapons.{team}.{allocationType} config " +
                    $"is not in the UsableWeapons list.");
            }
        }

        return warnings;
    }

    public double GetRoundTypePercentage(RoundType roundType)
    {
        return Math.Round(RoundTypePercentages[roundType] / 100.0, 2);
    }

    public bool CanPlayersSelectWeapons()
    {
        return AllowedWeaponSelectionTypes.Contains(WeaponSelectionType.PlayerChoice);
    }

    public bool CanAssignRandomWeapons()
    {
        return AllowedWeaponSelectionTypes.Contains(WeaponSelectionType.Random);
    }

    public bool CanAssignDefaultWeapons()
    {
        return AllowedWeaponSelectionTypes.Contains(WeaponSelectionType.Default);
    }
}

public record ConfigFileLayout
{
    public ConfigCategory? Config { get; set; }
    public RoundTypesCategory? RoundTypes { get; set; }
    public WeaponsCategory? Weapons { get; set; }
    public NadesCategory? Nades { get; set; }
    public AwpCategory? AWP { get; set; }
    public SsgCategory? SSG { get; set; }
    public EnemyStuffCategory? EnemyStuff { get; set; }
    public ZeusCategory? Zeus { get; set; }
    public WarmupCategory? Warmup { get; set; }
    public DatabaseCategory? Database { get; set; }

    public static ConfigFileLayout FromConfigData(ConfigData data) => new()
    {
        Config = new ConfigCategory
        {
            ResetStateOnGameRestart = data.ResetStateOnGameRestart,
            AllowAllocationAfterFreezeTime = data.AllowAllocationAfterFreezeTime,
            UseOnTickFeatures = data.UseOnTickFeatures,
            CapabilityWeaponPaints = data.CapabilityWeaponPaints,
            EnableRoundTypeAnnouncement = data.EnableRoundTypeAnnouncement,
            EnableRoundTypeAnnouncementCenter = data.EnableRoundTypeAnnouncementCenter,
            EnableBombSiteAnnouncementCenter = data.EnableBombSiteAnnouncementCenter,
            BombSiteAnnouncementCenterToCTOnly = data.BombSiteAnnouncementCenterToCTOnly,
            DisableDefaultBombPlantedCenterMessage = data.DisableDefaultBombPlantedCenterMessage,
            ForceCloseBombSiteAnnouncementCenterOnPlant = data.ForceCloseBombSiteAnnouncementCenterOnPlant,
            BombSiteAnnouncementCenterDelay = data.BombSiteAnnouncementCenterDelay,
            BombSiteAnnouncementCenterShowTimer = data.BombSiteAnnouncementCenterShowTimer,
            EnableBombSiteAnnouncementChat = data.EnableBombSiteAnnouncementChat,
            EnableNextRoundTypeVoting = data.EnableNextRoundTypeVoting,
            EnableCanAcquireHook = data.EnableCanAcquireHook,
            LogLevel = data.LogLevel,
            ChatMessagePluginName = data.ChatMessagePluginName,
            ChatMessagePluginPrefix = data.ChatMessagePluginPrefix,
            InGameGunMenuCenterCommands = data.InGameGunMenuCenterCommands,
            AutoUpdateSignatures = data.AutoUpdateSignatures,
        },
        Warmup = new WarmupCategory
        {
            CustomWarmup = data.CustomWarmup,
            CustomWarmupWeaponCT = data.CustomWarmupWeaponCT,
            CustomWarmupWeaponT = data.CustomWarmupWeaponT,
        },
        RoundTypes = new RoundTypesCategory
        {
            RoundTypeSelection = data.RoundTypeSelection,
            RoundTypePercentages = data.RoundTypePercentages,
            RoundTypeRandomFixedCounts = data.RoundTypeRandomFixedCounts,
            RoundTypeManualOrdering = data.RoundTypeManualOrdering,
        },
        Weapons = new WeaponsCategory
        {
            UsableWeapons = data.UsableWeapons,
            AllowedWeaponSelectionTypes = data.AllowedWeaponSelectionTypes,
            DefaultWeapons = data.DefaultWeapons,
            EnableAllWeaponsForEveryone = data.EnableAllWeaponsForEveryone,
            EnableWeaponShotguns = data.EnableWeaponShotguns,
            EnableWeaponPms = data.EnableWeaponPms,
        },
        Nades = new NadesCategory
        {
            MaxNades = data.MaxNades,
            MaxTeamNades = data.MaxTeamNades,
        },
        AWP = new AwpCategory
        {
            EnableAwp = data.EnableAwp,
            AwpPermission = data.AwpPermission,
            ChanceForAwpWeapon = data.ChanceForAwpWeapon,
            MaxAwpWeaponsPerTeam = data.MaxAwpWeaponsPerTeam,
            MinPlayersPerTeamForAwpWeapon = data.MinPlayersPerTeamForAwpWeapon,
        },
        SSG = new SsgCategory
        {
            EnableSsg = data.EnableSsg,
            SsgPermission = data.SsgPermission,
            ChanceForSsgWeapon = data.ChanceForSsgWeapon,
            MaxSsgWeaponsPerTeam = data.MaxSsgWeaponsPerTeam,
            MinPlayersPerTeamForSsgWeapon = data.MinPlayersPerTeamForSsgWeapon,
        },
        EnemyStuff = new EnemyStuffCategory
        {
            EnableEnemyStuff = data.EnableEnemyStuff,
            EnemyStuffPermission = data.EnemyStuffPermission,
            ChanceForEnemyStuff = data.ChanceForEnemyStuff,
            MaxEnemyStuffPerTeam = data.MaxEnemyStuffPerTeam,
        },
        Zeus = new ZeusCategory
        {
            EnableZeus = data.EnableZeus,
            ChanceForZeusWeapon = data.ChanceForZeusWeapon,
            MaxZeusPerTeam = data.MaxZeusPerTeam,
        },
        Database = new DatabaseCategory
        {
            DatabaseProvider = data.DatabaseProvider,
            DatabaseConnectionString = data.DatabaseConnectionString,
            MigrateOnStartup = data.MigrateOnStartup,
        }
    };

    public ConfigData ToConfigData()
    {
        var data = new ConfigData();

        if (Config is not null)
        {
            if (Config.ResetStateOnGameRestart is bool resetStateOnGameRestart)
            {
                data.ResetStateOnGameRestart = resetStateOnGameRestart;
            }
            if (Config.AllowAllocationAfterFreezeTime is bool allowAllocationAfterFreezeTime)
            {
                data.AllowAllocationAfterFreezeTime = allowAllocationAfterFreezeTime;
            }
            if (Config.UseOnTickFeatures is bool useOnTick)
            {
                data.UseOnTickFeatures = useOnTick;
            }
            if (Config.CapabilityWeaponPaints is bool capabilityWeaponPaints)
            {
                data.CapabilityWeaponPaints = capabilityWeaponPaints;
            }
            if (Config.EnableRoundTypeAnnouncement is bool enableRoundTypeAnnouncement)
            {
                data.EnableRoundTypeAnnouncement = enableRoundTypeAnnouncement;
            }
            if (Config.EnableRoundTypeAnnouncementCenter is bool enableRoundTypeAnnouncementCenter)
            {
                data.EnableRoundTypeAnnouncementCenter = enableRoundTypeAnnouncementCenter;
            }
            if (Config.EnableBombSiteAnnouncementCenter is bool enableBombSiteAnnouncementCenter)
            {
                data.EnableBombSiteAnnouncementCenter = enableBombSiteAnnouncementCenter;
            }
            if (Config.BombSiteAnnouncementCenterToCTOnly is bool bombSiteAnnouncementCenterToCtOnly)
            {
                data.BombSiteAnnouncementCenterToCTOnly = bombSiteAnnouncementCenterToCtOnly;
            }
            if (Config.DisableDefaultBombPlantedCenterMessage is bool disableDefaultBombPlantedCenterMessage)
            {
                data.DisableDefaultBombPlantedCenterMessage = disableDefaultBombPlantedCenterMessage;
            }
            if (Config.ForceCloseBombSiteAnnouncementCenterOnPlant is bool forceCloseBombSiteAnnouncementCenterOnPlant)
            {
                data.ForceCloseBombSiteAnnouncementCenterOnPlant = forceCloseBombSiteAnnouncementCenterOnPlant;
            }
            if (Config.BombSiteAnnouncementCenterDelay is float bombSiteAnnouncementCenterDelay)
            {
                data.BombSiteAnnouncementCenterDelay = bombSiteAnnouncementCenterDelay;
            }
            if (Config.BombSiteAnnouncementCenterShowTimer is float bombSiteAnnouncementCenterShowTimer)
            {
                data.BombSiteAnnouncementCenterShowTimer = bombSiteAnnouncementCenterShowTimer;
            }
            if (Config.EnableBombSiteAnnouncementChat is bool enableBombSiteAnnouncementChat)
            {
                data.EnableBombSiteAnnouncementChat = enableBombSiteAnnouncementChat;
            }
            if (Config.CustomWarmup is bool customWarmup)
            {
                data.CustomWarmup = customWarmup;
            }
            if (Config.CustomWarmupWeaponCT is CsItem customWarmupWeaponCt)
            {
                data.CustomWarmupWeaponCT = customWarmupWeaponCt;
            }
            if (Config.CustomWarmupWeaponT is CsItem customWarmupWeaponT)
            {
                data.CustomWarmupWeaponT = customWarmupWeaponT;
            }
            if (Config.EnableNextRoundTypeVoting is bool enableNextRoundTypeVoting)
            {
                data.EnableNextRoundTypeVoting = enableNextRoundTypeVoting;
            }
            if (Config.EnableCanAcquireHook is bool enableCanAcquireHook)
            {
                data.EnableCanAcquireHook = enableCanAcquireHook;
            }
            if (Config.LogLevel is LogLevel logLevel)
            {
                data.LogLevel = logLevel;
            }
            if (Config.ChatMessagePluginName is not null)
            {
                data.ChatMessagePluginName = Config.ChatMessagePluginName;
            }
            if (Config.ChatMessagePluginPrefix is not null)
            {
                data.ChatMessagePluginPrefix = Config.ChatMessagePluginPrefix;
            }
            if (Config.InGameGunMenuCenterCommands is not null)
            {
                data.InGameGunMenuCenterCommands = Config.InGameGunMenuCenterCommands;
            }
            if (Config.AutoUpdateSignatures is bool autoUpdateSignatures)
            {
                data.AutoUpdateSignatures = autoUpdateSignatures;
            }
        }

        if (RoundTypes is not null)
        {
            if (RoundTypes.RoundTypeSelection is RoundTypeSelectionOption roundTypeSelection)
            {
                data.RoundTypeSelection = roundTypeSelection;
            }
            if (RoundTypes.RoundTypePercentages is not null)
            {
                data.RoundTypePercentages = RoundTypes.RoundTypePercentages;
            }
            if (RoundTypes.RoundTypeRandomFixedCounts is not null)
            {
                data.RoundTypeRandomFixedCounts = RoundTypes.RoundTypeRandomFixedCounts;
            }
            if (RoundTypes.RoundTypeManualOrdering is not null)
            {
                data.RoundTypeManualOrdering = RoundTypes.RoundTypeManualOrdering;
            }
        }

        if (Weapons is not null)
        {
            if (Weapons.UsableWeapons is not null)
            {
                data.UsableWeapons = Weapons.UsableWeapons;
            }
            if (Weapons.AllowedWeaponSelectionTypes is not null)
            {
                data.AllowedWeaponSelectionTypes = Weapons.AllowedWeaponSelectionTypes;
            }
            if (Weapons.DefaultWeapons is not null)
            {
                data.DefaultWeapons = Weapons.DefaultWeapons;
            }
            if (Weapons.EnableAllWeaponsForEveryone is bool enableAllWeaponsForEveryone)
            {
                data.EnableAllWeaponsForEveryone = enableAllWeaponsForEveryone;
            }
            if (Weapons.EnableWeaponShotguns is bool enableWeaponShotguns)
            {
                data.EnableWeaponShotguns = enableWeaponShotguns;
            }
            if (Weapons.EnableWeaponPms is bool enableWeaponPms)
            {
                data.EnableWeaponPms = enableWeaponPms;
            }
        }

        if (Nades is not null)
        {
            if (Nades.MaxNades is not null)
            {
                data.MaxNades = Nades.MaxNades;
            }
            if (Nades.MaxTeamNades is not null)
            {
                data.MaxTeamNades = Nades.MaxTeamNades;
            }
        }

        if (Warmup is not null)
        {
            if (Warmup.CustomWarmup is bool customWarmup)
            {
                data.CustomWarmup = customWarmup;
            }
            if (Warmup.CustomWarmupWeaponCT is CsItem customWarmupWeaponCt)
            {
                data.CustomWarmupWeaponCT = customWarmupWeaponCt;
            }
            if (Warmup.CustomWarmupWeaponT is CsItem customWarmupWeaponT)
            {
                data.CustomWarmupWeaponT = customWarmupWeaponT;
            }
        }

        // Backwards compatibility: legacy warmup fields under Config
        if (Config is not null)
        {
            if (Config.CustomWarmup is bool legacyCustomWarmup)
            {
                data.CustomWarmup = legacyCustomWarmup;
            }
            if (Config.CustomWarmupWeaponCT is CsItem legacyWarmupWeaponCt)
            {
                data.CustomWarmupWeaponCT = legacyWarmupWeaponCt;
            }
            if (Config.CustomWarmupWeaponT is CsItem legacyWarmupWeaponT)
            {
                data.CustomWarmupWeaponT = legacyWarmupWeaponT;
            }
        }

        if (AWP is not null)
        {
            if (AWP.EnableAwp is int enableAwp)
            {
                data.EnableAwp = enableAwp;
            }
            else if (AWP.LegacyAllowAwpWeaponForEveryone is not null ||
                     AWP.LegacyNumberOfExtraVipChancesForAwpWeapon is not null)
            {
                data.EnableAwp = ConvertLegacySniperSettings(
                    AWP.LegacyAllowAwpWeaponForEveryone,
                    AWP.LegacyNumberOfExtraVipChancesForAwpWeapon,
                    data.EnableAwp
                );
            }
            if (AWP.AwpPermission is not null)
            {
                data.AwpPermission = AWP.AwpPermission;
            }
            if (AWP.ChanceForAwpWeapon is double awpChance)
            {
                data.ChanceForAwpWeapon = awpChance;
            }
            if (AWP.MaxAwpWeaponsPerTeam is not null)
            {
                data.MaxAwpWeaponsPerTeam = AWP.MaxAwpWeaponsPerTeam;
            }
            if (AWP.MinPlayersPerTeamForAwpWeapon is not null)
            {
                data.MinPlayersPerTeamForAwpWeapon = AWP.MinPlayersPerTeamForAwpWeapon;
            }
        }

        if (SSG is not null)
        {
            if (SSG.EnableSsg is int enableSsg)
            {
                data.EnableSsg = enableSsg;
            }
            else if (SSG.LegacyAllowSsgWeaponForEveryone is not null ||
                     SSG.LegacyNumberOfExtraVipChancesForSsgWeapon is not null)
            {
                data.EnableSsg = ConvertLegacySniperSettings(
                    SSG.LegacyAllowSsgWeaponForEveryone,
                    SSG.LegacyNumberOfExtraVipChancesForSsgWeapon,
                    data.EnableSsg
                );
            }
            if (SSG.SsgPermission is not null)
            {
                data.SsgPermission = SSG.SsgPermission;
            }
            if (SSG.ChanceForSsgWeapon is double ssgChance)
            {
                data.ChanceForSsgWeapon = ssgChance;
            }
            if (SSG.MaxSsgWeaponsPerTeam is not null)
            {
                data.MaxSsgWeaponsPerTeam = SSG.MaxSsgWeaponsPerTeam;
            }
            if (SSG.MinPlayersPerTeamForSsgWeapon is not null)
            {
                data.MinPlayersPerTeamForSsgWeapon = SSG.MinPlayersPerTeamForSsgWeapon;
            }
        }

        if (EnemyStuff is not null)
        {
            if (EnemyStuff.EnableEnemyStuff is int enableEnemyStuff)
            {
                data.EnableEnemyStuff = enableEnemyStuff;
            }
            else if (EnemyStuff.LegacyEnableEnemyStuffPreference is bool legacyEnableEnemyStuff)
            {
                data.EnableEnemyStuff = legacyEnableEnemyStuff ? 1 : 0;
            }
            if (EnemyStuff.EnemyStuffPermission is not null)
            {
                data.EnemyStuffPermission = EnemyStuff.EnemyStuffPermission;
            }
            if (EnemyStuff.ChanceForEnemyStuff is double enemyChance)
            {
                data.ChanceForEnemyStuff = enemyChance;
            }
            if (EnemyStuff.MaxEnemyStuffPerTeam is not null)
            {
                data.MaxEnemyStuffPerTeam = EnemyStuff.MaxEnemyStuffPerTeam;
            }
        }

        if (Zeus is not null)
        {
            if (Zeus.EnableZeus is int enableZeus)
            {
                data.EnableZeus = enableZeus;
            }
            else if (Zeus.LegacyEnableZeusPreference is bool legacyEnableZeus)
            {
                data.EnableZeus = legacyEnableZeus ? 1 : 0;
            }
            if (Zeus.ChanceForZeusWeapon is double zeusChance)
            {
                data.ChanceForZeusWeapon = zeusChance;
            }
            if (Zeus.MaxZeusPerTeam is not null)
            {
                data.MaxZeusPerTeam = Zeus.MaxZeusPerTeam;
            }
        }

        if (Database is not null)
        {
            if (Database.DatabaseProvider is DatabaseProvider databaseProvider)
            {
                data.DatabaseProvider = databaseProvider;
            }
            if (Database.DatabaseConnectionString is not null)
            {
                data.DatabaseConnectionString = Database.DatabaseConnectionString;
            }
            if (Database.MigrateOnStartup is bool migrateOnStartup)
            {
                data.MigrateOnStartup = migrateOnStartup;
            }
        }

        return data;
    }

    private static int ConvertLegacySniperSettings(bool? allowForEveryone, int? legacyExtraVipChances, int defaultValue)
    {
        if (allowForEveryone is true)
        {
            return 1;
        }

        if (legacyExtraVipChances == -1)
        {
            return 2;
        }

        if (allowForEveryone.HasValue || legacyExtraVipChances.HasValue)
        {
            return 1;
        }

        return defaultValue;
    }
}

public record ConfigCategory
{
    public bool? ResetStateOnGameRestart { get; set; }
    public bool? AllowAllocationAfterFreezeTime { get; set; }
    public bool? UseOnTickFeatures { get; set; }
    public bool? CapabilityWeaponPaints { get; set; }
    public bool? EnableRoundTypeAnnouncement { get; set; }
    public bool? EnableRoundTypeAnnouncementCenter { get; set; }
    public bool? EnableBombSiteAnnouncementCenter { get; set; }
    public bool? BombSiteAnnouncementCenterToCTOnly { get; set; }
    public bool? DisableDefaultBombPlantedCenterMessage { get; set; }
    public bool? ForceCloseBombSiteAnnouncementCenterOnPlant { get; set; }
    public float? BombSiteAnnouncementCenterDelay { get; set; }
    public float? BombSiteAnnouncementCenterShowTimer { get; set; }
    public bool? EnableBombSiteAnnouncementChat { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CustomWarmup { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CsItem? CustomWarmupWeaponCT { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CsItem? CustomWarmupWeaponT { get; set; }
    public bool? EnableNextRoundTypeVoting { get; set; }
    public bool? EnableCanAcquireHook { get; set; }
    public LogLevel? LogLevel { get; set; }
    public string? ChatMessagePluginName { get; set; }
    public string? ChatMessagePluginPrefix { get; set; }
    public string? InGameGunMenuCenterCommands { get; set; }
    public bool? AutoUpdateSignatures { get; set; }
}

public record WarmupCategory
{
    public bool? CustomWarmup { get; set; }
    public CsItem? CustomWarmupWeaponCT { get; set; }
    public CsItem? CustomWarmupWeaponT { get; set; }
}

public record RoundTypesCategory
{
    public RoundTypeSelectionOption? RoundTypeSelection { get; set; }
    public Dictionary<RoundType, int>? RoundTypePercentages { get; set; }
    public Dictionary<RoundType, int>? RoundTypeRandomFixedCounts { get; set; }
    public List<RoundTypeManualOrderingItem>? RoundTypeManualOrdering { get; set; }
}

public record WeaponsCategory
{
    public List<CsItem>? UsableWeapons { get; set; }
    public List<WeaponSelectionType>? AllowedWeaponSelectionTypes { get; set; }
    public Dictionary<CsTeam, Dictionary<WeaponAllocationType, CsItem>>? DefaultWeapons { get; set; }
    public bool? EnableAllWeaponsForEveryone { get; set; }
    public bool? EnableWeaponShotguns { get; set; }
    public bool? EnableWeaponPms { get; set; }
}

public record NadesCategory
{
    public Dictionary<
        string,
        Dictionary<
            CsTeam,
            Dictionary<CsItem, int>
        >
    >? MaxNades { get; set; }

    public Dictionary<
        string,
        Dictionary<
            CsTeam,
            Dictionary<RoundType, MaxTeamNadesSetting>
        >
    >? MaxTeamNades { get; set; }
}

public record AwpCategory
{
    public int? EnableAwp { get; set; }
    public string? AwpPermission { get; set; }
    public double? ChanceForAwpWeapon { get; set; }
    public Dictionary<CsTeam, int>? MaxAwpWeaponsPerTeam { get; set; }
    public Dictionary<CsTeam, int>? MinPlayersPerTeamForAwpWeapon { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("NumberOfExtraVipChancesForAwpWeapon")]
    public int? LegacyNumberOfExtraVipChancesForAwpWeapon { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("AllowAwpWeaponForEveryone")]
    public bool? LegacyAllowAwpWeaponForEveryone { get; set; }
}

public record SsgCategory
{
    public int? EnableSsg { get; set; }
    public string? SsgPermission { get; set; }
    public double? ChanceForSsgWeapon { get; set; }
    public Dictionary<CsTeam, int>? MaxSsgWeaponsPerTeam { get; set; }
    public Dictionary<CsTeam, int>? MinPlayersPerTeamForSsgWeapon { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("NumberOfExtraVipChancesForSsgWeapon")]
    public int? LegacyNumberOfExtraVipChancesForSsgWeapon { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("AllowSsgWeaponForEveryone")]
    public bool? LegacyAllowSsgWeaponForEveryone { get; set; }
}

public record EnemyStuffCategory
{
    public int? EnableEnemyStuff { get; set; }
    public string? EnemyStuffPermission { get; set; }
    public double? ChanceForEnemyStuff { get; set; }
    [JsonConverter(typeof(PerTeamLimitConverter))]
    public Dictionary<CsTeam, int>? MaxEnemyStuffPerTeam { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("EnableEnemyStuffPreference")]
    public bool? LegacyEnableEnemyStuffPreference { get; set; }
}

public record ZeusCategory
{
    public int? EnableZeus { get; set; }
    public double? ChanceForZeusWeapon { get; set; }
    public Dictionary<CsTeam, int>? MaxZeusPerTeam { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("EnableZeusPreference")]
    public bool? LegacyEnableZeusPreference { get; set; }
}

public record DatabaseCategory
{
    public DatabaseProvider? DatabaseProvider { get; set; }
    public string? DatabaseConnectionString { get; set; }
    public bool? MigrateOnStartup { get; set; }
}

public class PerTeamLimitConverter : JsonConverter<Dictionary<CsTeam, int>>
{
    public override Dictionary<CsTeam, int>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            return new Dictionary<CsTeam, int>
            {
                {CsTeam.Terrorist, value},
                {CsTeam.CounterTerrorist, value},
            };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var json = document.RootElement.GetRawText();
            return JsonSerializer.Deserialize<Dictionary<CsTeam, int>>(json, options);
        }

        throw new JsonException("Invalid MaxEnemyStuffPerTeam format.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<CsTeam, int> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key.ToString());
            writer.WriteNumberValue(kvp.Value);
        }
        writer.WriteEndObject();
    }
}
