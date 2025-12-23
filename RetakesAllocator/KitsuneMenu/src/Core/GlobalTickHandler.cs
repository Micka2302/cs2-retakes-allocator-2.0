using CounterStrikeSharp.API.Core;

namespace KitsuneMenu.Core;

internal class GlobalTickHandler
{
    private static GlobalTickHandler? _instance;
    private readonly Action _tickCallback;
    private readonly FunctionReference _functionReference;

    private GlobalTickHandler()
    {
        _tickCallback = GlobalMenuManager.OnGlobalTick;
        _functionReference = FunctionReference.Create(_tickCallback);
        NativeAPI.AddListener("OnTick", _functionReference);
    }

    public static void Initialize()
    {
        _instance ??= new GlobalTickHandler();
    }

    public static void Cleanup()
    {
        if (_instance != null)
        {
            NativeAPI.RemoveListener("OnTick", _instance._functionReference);
            _instance = null;
        }
        
        // Clean up all menu sessions
        GlobalMenuManager.Cleanup();
    }
}