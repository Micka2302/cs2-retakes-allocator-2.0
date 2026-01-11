using System.Globalization;
using System.IO;
using CounterStrikeSharp.API.Core.Translations;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using NUnit.Framework;

namespace RetakesAllocatorTest;

[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public void Setup()
    {
        var culture = CultureInfo.GetCultureInfo("en");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        Configs.Load(TestDirectories.ModuleDirectory, true);
        Queries.Migrate();
        Translator.Initialize(new JsonStringLocalizer("../../../../RetakesAllocator/lang"));
    }
    [OneTimeTearDown]
    public void TearDown()
    {
        Queries.Disconnect();
    }
}

public abstract class BaseTestFixture
{
    [SetUp]
    public void GlobalSetup()
    {
        Configs.Load(TestDirectories.ModuleDirectory);
        Queries.Wipe();
    }
}

internal static class TestDirectories
{
    private static string? _moduleDirectory;

    public static string ModuleDirectory
    {
        get
        {
            if (_moduleDirectory is not null)
            {
                return _moduleDirectory;
            }

            var moduleDirectory = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestServer",
                "addons",
                "counterstrikesharp",
                "plugins",
                "RetakesAllocator"
            );
            Directory.CreateDirectory(moduleDirectory);
            _moduleDirectory = moduleDirectory;
            return _moduleDirectory;
        }
    }
}


