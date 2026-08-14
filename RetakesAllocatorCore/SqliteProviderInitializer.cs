using SQLitePCL;

namespace RetakesAllocatorCore;

internal static class SqliteProviderInitializer
{
    private static readonly object Sync = new();
    private static bool _initialized;

    public static void Initialize()
    {
        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                raw.SetProvider(new SQLite3Provider_winsqlite3());
            }
            else if (OperatingSystem.IsLinux())
            {
                SQLite3Provider_dynamic_cdecl.Setup(
                    "sqlite3",
                    new NativeLibraryAdapter("libsqlite3.so.0"));
                raw.SetProvider(new SQLite3Provider_dynamic_cdecl());
            }
            else
            {
                throw new PlatformNotSupportedException("SQLite is only configured for Windows and Linux.");
            }

            _initialized = true;
        }
    }

    private sealed class NativeLibraryAdapter(string libraryName) : IGetFunctionPointer
    {
        private readonly IntPtr _library = NativeLibrary.Load(libraryName);

        public IntPtr GetFunctionPointer(string name) =>
            NativeLibrary.TryGetExport(_library, name, out var address)
                ? address
                : IntPtr.Zero;
    }
}
