using RetakesAllocatorCore;
using RetakesAllocatorCore.Managers;

namespace RetakesAllocator.Managers;

public class NextRoundVoteManager : AbstractVoteManager
{
    private readonly IEnumerable<string> _options = RoundTypeHelpers
        .GetRoundTypes()
        .Select(r => r.ToString());

    public NextRoundVoteManager() : base("vote.subject.next_round", "!nextround")
    {
    }


    public override IEnumerable<string> GetVoteOptions()
    {
        return _options;
    }

    protected override void HandleVoteResult(string option)
    {
        RoundTypeManager.Instance.SetNextRoundTypeOverride(RoundTypeHelpers.ParseRoundType(option));
        PrintToServer(Translator.Instance["vote.complete_next_round", option]);
    }
}
