using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;

namespace RetakesAllocatorTest;

public class RoundStartTests : BaseTestFixture
{
    [Test]
    public void TestRoundStartCanRunInCore()
    {
        OnRoundPostStartHelper.Handle(
            new List<int>(),
            i => 1,
            x => CsTeam.None,
            x => {},
            (x, y, z) => {},
            x => true,
            x => true,
            x => true,
            out _
        );
    }
}
