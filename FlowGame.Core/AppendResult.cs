namespace FlowGame.Core;

public enum AppendResult
{
    Blocked,
    Unchanged,
    Added,
    TrimmedOwnPath,
    OverwroteOtherPath,
    CompletedPair
}
