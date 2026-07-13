namespace FlowGame.Core;

public sealed class PlayerBoard
{
    private readonly Stack<Dictionary<int, List<CellPosition>>> _history = new();

    public PlayerBoard(PuzzleDefinition puzzle)
    {
        Puzzle = puzzle;
        Paths = puzzle.Pairs.ToDictionary(pair => pair.Id, _ => new List<CellPosition>());
    }

    public PuzzleDefinition Puzzle { get; }

    public Dictionary<int, List<CellPosition>> Paths { get; private set; }

    public bool CanUndo => _history.Count > 0;

    public bool IsInside(CellPosition position)
    {
        return position.Row >= 0
            && position.Column >= 0
            && position.Row < Puzzle.Size
            && position.Column < Puzzle.Size;
    }

    public int? OccupiedBy(CellPosition position)
    {
        foreach (var path in Paths)
        {
            if (path.Value.Contains(position))
            {
                return path.Key;
            }
        }

        return null;
    }

    public void StartPath(int pairId, CellPosition endpoint)
    {
        if (!Puzzle.IsEndpointForPair(pairId, endpoint))
        {
            throw new InvalidOperationException("A path must start from one endpoint of its own color.");
        }

        SaveHistory();
        Paths[pairId] = new List<CellPosition> { endpoint };
    }

    public bool CanContinuePathFrom(int pairId, CellPosition position)
    {
        var path = Paths[pairId];
        return path.Count > 0
            && path[^1] == position
            && !IsPairConnected(pairId);
    }

    public void StartPathContinuation(int pairId, CellPosition position)
    {
        if (!CanContinuePathFrom(pairId, position))
        {
            throw new InvalidOperationException("A path can only continue from its current loose end.");
        }

        SaveHistory();
    }

    public bool TryAppend(int pairId, CellPosition next)
    {
        return TryAppendDetailed(pairId, next) != AppendResult.Blocked;
    }

    public AppendResult TryAppendDetailed(int pairId, CellPosition next)
    {
        if (!IsInside(next))
        {
            return AppendResult.Blocked;
        }

        var path = Paths[pairId];
        if (path.Count == 0)
        {
            if (!Puzzle.IsEndpointForPair(pairId, next))
            {
                return AppendResult.Blocked;
            }

            SaveHistory();
            path.Add(next);
            return AppendResult.Added;
        }

        var current = path[^1];
        if (current == next)
        {
            return AppendResult.Unchanged;
        }

        if (current.ManhattanDistance(next) != 1)
        {
            return AppendResult.Blocked;
        }

        if (IsPairConnected(pairId))
        {
            return AppendResult.Blocked;
        }

        var endpointPairId = Puzzle.PairIdAtEndpoint(next);
        if (endpointPairId is not null && endpointPairId != pairId)
        {
            return AppendResult.Blocked;
        }

        var occupant = OccupiedBy(next);
        if (occupant is not null && occupant != pairId)
        {
            if (endpointPairId is not null)
            {
                return AppendResult.Blocked;
            }

            Paths[occupant.Value] = new List<CellPosition>();
            path = Paths[pairId];
        }

        var existingIndex = path.IndexOf(next);
        if (existingIndex >= 0)
        {
            path.RemoveRange(existingIndex + 1, path.Count - existingIndex - 1);
            return AppendResult.TrimmedOwnPath;
        }

        path.Add(next);
        return Puzzle.IsEndpointForPair(pairId, next)
            ? AppendResult.CompletedPair
            : occupant is not null
                ? AppendResult.OverwroteOtherPath
                : AppendResult.Added;
    }

    public void ClearPath(int pairId)
    {
        SaveHistory();
        Paths[pairId] = new List<CellPosition>();
    }

    public void Reset()
    {
        SaveHistory();
        Paths = Puzzle.Pairs.ToDictionary(pair => pair.Id, _ => new List<CellPosition>());
    }

    public bool Undo()
    {
        if (_history.Count == 0)
        {
            return false;
        }

        Paths = _history.Pop();
        return true;
    }

    public bool IsSolved()
    {
        var occupied = new HashSet<CellPosition>();

        foreach (var pair in Puzzle.Pairs)
        {
            var path = Paths[pair.Id];
            if (path.Count < 2)
            {
                return false;
            }

            var endpointsMatch = path[0] == pair.Start && path[^1] == pair.End
                || path[0] == pair.End && path[^1] == pair.Start;
            if (!endpointsMatch)
            {
                return false;
            }

            for (var i = 0; i < path.Count; i++)
            {
                if (!IsInside(path[i]) || !occupied.Add(path[i]))
                {
                    return false;
                }

                if (i > 0 && path[i - 1].ManhattanDistance(path[i]) != 1)
                {
                    return false;
                }
            }
        }

        return occupied.Count == Puzzle.Size * Puzzle.Size;
    }

    public bool IsPairConnected(int pairId)
    {
        var pair = Puzzle.GetPair(pairId);
        var path = Paths[pairId];
        if (path.Count < 2)
        {
            return false;
        }

        return path[0] == pair.Start && path[^1] == pair.End
            || path[0] == pair.End && path[^1] == pair.Start;
    }

    public int? ApplyNextMissingSolutionPath()
    {
        var pair = Puzzle.Pairs.FirstOrDefault(pair => !IsPairConnected(pair.Id));
        if (pair is null)
        {
            return null;
        }

        SaveHistory();
        var solution = Puzzle.SolutionPaths[pair.Id].ToList();
        var solutionCells = solution.ToHashSet();

        foreach (var otherPair in Puzzle.Pairs.Where(otherPair => otherPair.Id != pair.Id))
        {
            if (Paths[otherPair.Id].Any(solutionCells.Contains))
            {
                Paths[otherPair.Id] = new List<CellPosition>();
            }
        }

        Paths[pair.Id] = solution;
        return pair.Id;
    }

    public void ApplySolution()
    {
        SaveHistory();
        Paths = Puzzle.SolutionPaths.ToDictionary(
            item => item.Key,
            item => item.Value.ToList());
    }

    private void SaveHistory()
    {
        _history.Push(Paths.ToDictionary(
            item => item.Key,
            item => item.Value.ToList()));
    }
}
