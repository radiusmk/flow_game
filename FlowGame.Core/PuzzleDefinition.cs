namespace FlowGame.Core;

public sealed record PuzzleDefinition(
    int Level,
    int Size,
    IReadOnlyList<FlowPair> Pairs,
    IReadOnlyDictionary<int, IReadOnlyList<CellPosition>> SolutionPaths)
{
    public const int MinSize = 5;
    public const int MaxSize = 15;

    public FlowPair GetPair(int pairId)
    {
        return Pairs.First(pair => pair.Id == pairId);
    }

    public int? PairIdAtEndpoint(CellPosition position)
    {
        foreach (var pair in Pairs)
        {
            if (pair.Start == position || pair.End == position)
            {
                return pair.Id;
            }
        }

        return null;
    }

    public bool IsEndpointForPair(int pairId, CellPosition position)
    {
        var pair = GetPair(pairId);
        return pair.Start == position || pair.End == position;
    }
}
