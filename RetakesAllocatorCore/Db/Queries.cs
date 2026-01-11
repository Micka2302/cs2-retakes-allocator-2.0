using System.Data;
using System.Reflection;
using System.Threading;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySqlConnector;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorCore.Db;

public class Queries
{
    private static readonly SemaphoreSlim UpsertSemaphore = new(1, 1);

    public static async Task<UserSetting?> GetUserSettings(ulong userId)
    {
        return await Db.GetInstance().UserSettings.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
    }

    private static async Task<UserSetting?> UpsertUserSettings(ulong userId, Action<UserSetting> mutation)
    {
        if (userId == 0)
        {
            Log.Debug("Encountered userid 0, not upserting user settings");
            return null;
        }

        await UpsertSemaphore.WaitAsync();
        try
        {
            Log.Debug($"Upserting settings for {userId}");

            var instance = Db.GetInstance();
            var isNew = false;
            var userSettings = await instance.UserSettings.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (userSettings is null)
            {
                userSettings = new UserSetting {UserId = userId};
                await instance.UserSettings.AddAsync(userSettings);
                isNew = true;
            }

            instance.Entry(userSettings).State = isNew ? EntityState.Added : EntityState.Modified;

            mutation(userSettings);

            await instance.SaveChangesAsync();
            instance.Entry(userSettings).State = EntityState.Detached;

            return userSettings;
        }
        finally
        {
            UpsertSemaphore.Release();
        }
    }

    public static async Task SetWeaponPreferenceForUserAsync(ulong userId, CsTeam team,
        WeaponAllocationType weaponAllocationType,
        CsItem? item)
    {
        await UpsertUserSettings(userId,
            userSetting => { userSetting.SetWeaponPreference(team, weaponAllocationType, item); });
    }

    public static void SetWeaponPreferenceForUser(ulong userId, CsTeam team, WeaponAllocationType weaponAllocationType,
        CsItem? item)
    {
        Task.Run(async () => { await SetWeaponPreferenceForUserAsync(userId, team, weaponAllocationType, item); });
    }

    public static async Task ClearWeaponPreferencesForUserAsync(ulong userId)
    {
        await UpsertUserSettings(userId, userSetting => { userSetting.WeaponPreferences = new(); });
    }

    public static void ClearWeaponPreferencesForUser(ulong userId)
    {
        Task.Run(async () => { await ClearWeaponPreferencesForUserAsync(userId); });
    }

    public static async Task SetAwpWeaponPreferenceAsync(ulong userId, CsItem? item)
    {
        await UpsertUserSettings(userId, userSetting =>
        {
            userSetting.SetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Preferred,
                WeaponHelpers.CoercePreferredTeam(item, CsTeam.Terrorist));
            userSetting.SetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.Preferred,
                WeaponHelpers.CoercePreferredTeam(item, CsTeam.CounterTerrorist));
        });
    }

    public static void SetAwpWeaponPreference(ulong userId, CsItem? item)
    {
        Task.Run(async () => { await SetAwpWeaponPreferenceAsync(userId, item); });
    }

    public static async Task SetZeusPreferenceAsync(ulong userId, bool enabled)
    {
        await UpsertUserSettings(userId, userSetting => { userSetting.ZeusEnabled = enabled; });
    }

    public static void SetZeusPreference(ulong userId, bool enabled)
    {
        Task.Run(async () => { await SetZeusPreferenceAsync(userId, enabled); });
    }

    public static async Task SetEnemyStuffPreferenceAsync(ulong userId, EnemyStuffTeamPreference preference)
    {
        await UpsertUserSettings(userId, userSetting => { userSetting.EnemyStuffTeamPreference = preference; });
    }

    public static void SetEnemyStuffPreference(ulong userId, EnemyStuffTeamPreference preference)
    {
        Task.Run(async () => { await SetEnemyStuffPreferenceAsync(userId, preference); });
    }
    public static IDictionary<ulong, UserSetting> GetUsersSettings(ICollection<ulong> userIds)
    {
        var userSettingsList = Db.GetInstance()
            .UserSettings
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToList();
        if (userSettingsList.Count == 0)
        {
            return new Dictionary<ulong, UserSetting>();
        }

        return userSettingsList
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private const int MySqlTableNotFoundError = 1146;

    private static readonly string EfProductVersion =
        typeof(DbContext).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(DbContext).Assembly.GetName().Version?.ToString()
        ?? "7.0.0";

    public static void Migrate()
    {
        var db = Db.GetInstance();
        var config = Configs.GetConfigData();

        var isMySql = config.DatabaseProvider == DatabaseProvider.MySql;

        if (isMySql && !MySqlUserSettingsTableExists(db))
        {
            Log.Warn("UserSettings table was not found. Creating the schema and seeding migration history.");
            EnsureMySqlUserSettingsTable(db);
        }

        try
        {
            db.Database.Migrate();
        }
        catch (MySqlException ex) when (isMySql && IsMissingUserSettingsTable(ex))
        {
            Log.Warn(
                $"UserSettings table was missing when applying migrations ({ex.Message}). Attempting to recreate the schema.");
            EnsureMySqlUserSettingsTable(db);
            db.Database.Migrate();
        }
    }

    public static void Wipe()
    {
        Db.GetInstance().UserSettings.ExecuteDelete();
    }

    public static void Disconnect()
    {
        Db.Disconnect();
    }

    private static bool IsMissingUserSettingsTable(MySqlException exception)
    {
        return exception.Number == MySqlTableNotFoundError &&
               exception.Message.Contains("UserSettings", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MySqlUserSettingsTableExists(Db context)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
SELECT COUNT(*)
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name = 'UserSettings';
""";
            var result = command.ExecuteScalar();
            return Convert.ToInt64(result) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    private static void EnsureMySqlUserSettingsTable(Db context)
    {
        using var transaction = context.Database.BeginTransaction();

        context.Database.ExecuteSqlRaw("""
CREATE TABLE IF NOT EXISTS UserSettings
(
    UserId BIGINT UNSIGNED NOT NULL,
    WeaponPreferences LONGTEXT NULL,
    ZeusEnabled TINYINT(1) NOT NULL DEFAULT 0,
    EnemyStuffTeamPreference INT NOT NULL DEFAULT 0,
    CONSTRAINT PK_UserSettings PRIMARY KEY (UserId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
""");

        context.Database.ExecuteSqlRaw("""
ALTER TABLE UserSettings
    MODIFY COLUMN UserId BIGINT UNSIGNED NOT NULL;
""");

        context.Database.ExecuteSqlRaw(
            MySqlColumnSqlHelper.BuildAddColumnIfMissingSql(
                "UserSettings",
                "WeaponPreferences",
                "LONGTEXT NULL"));

        context.Database.ExecuteSqlRaw(
            MySqlColumnSqlHelper.BuildAddColumnIfMissingSql(
                "UserSettings",
                "ZeusEnabled",
                "TINYINT(1) NOT NULL DEFAULT 0"));

        context.Database.ExecuteSqlRaw(
            MySqlColumnSqlHelper.BuildAddColumnIfMissingSql(
                "UserSettings",
                "EnemyStuffTeamPreference",
                "INT NOT NULL DEFAULT 0"));

        SeedMigrationHistory(context);

        transaction.Commit();
    }

    private static void SeedMigrationHistory(Db context)
    {
        var historyRepository = context.GetService<IHistoryRepository>();
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();

        if (!historyRepository.Exists())
        {
            context.Database.ExecuteSqlRaw(historyRepository.GetCreateScript());
        }

        var appliedMigrations = historyRepository
            .GetAppliedMigrations()
            .Select(row => row.MigrationId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var migrationId in migrationsAssembly.Migrations.Keys.OrderBy(id => id, StringComparer.Ordinal))
        {
            if (appliedMigrations.Contains(migrationId))
            {
                continue;
            }

            var insertScript = historyRepository.GetInsertScript(new HistoryRow(migrationId, EfProductVersion));
            if (!string.IsNullOrWhiteSpace(insertScript))
            {
                context.Database.ExecuteSqlRaw(insertScript);
            }
        }
    }
}
