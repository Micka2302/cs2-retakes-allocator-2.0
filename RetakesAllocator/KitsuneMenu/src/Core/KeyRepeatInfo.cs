
namespace KitsuneMenu.Core;

internal class KeyRepeatInfo
{
    public ulong LastButton { get; set; }
    public DateTime FirstPressTime { get; set; }
    public DateTime LastRepeatTime { get; set; }
    public int RepeatCount { get; set; }

    public void Reset()
    {
        LastButton = 0;
        FirstPressTime = DateTime.MinValue;
        LastRepeatTime = DateTime.MinValue;
        RepeatCount = 0;
    }
}