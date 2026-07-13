using FlowGame.Core;

var tests = new FlowGameTestSuite();
tests.RunAll();

internal sealed class FlowGameTestSuite
{
    private readonly PuzzleGenerator _generator = new();
    private int _passed;

    public void RunAll()
    {
        Run(BoardSizeRange);
        Run(SolutionConnectsAndFillsBoard);
        Run(CrossingAnotherColorIsBlocked);
        Run(OwnEndpointStopsFurtherDrawing);
        Run(PathCanContinueFromLooseEnd);
        Run(NewLineCanReplaceExistingLine);
        Run(HintAppliesMissingValidPath);
        Run(HintClearsPathsThatWouldBreakCanonicalSolution);
        Run(UndoRestoresPreviousPath);
        Run(LevelProgressionReachesMaximumBoard);
        Run(ProgressionRequiresMoreSolvesOverTime);
        Run(FixedSizeGenerationKeepsBoardSize);
        Run(GeneratedPuzzlesAreValidAcrossRange);
        Run(AdvancedLevelsUseMoreColorsWithBalancedPathLengths);

        Console.WriteLine($"{_passed} tests passed.");
    }

    private void Run(Action test)
    {
        test();
        _passed++;
    }

    private void BoardSizeRange()
    {
        Assert(_generator.Create(1).Size == 5, "Level 1 must start at 5x5.");
        Assert(_generator.Create(31).Size == 15, "Level 31 must reach 15x15.");
        Assert(_generator.Create(80).Size == 15, "Advanced levels must stay capped at 15x15.");
    }

    private void SolutionConnectsAndFillsBoard()
    {
        var board = new PlayerBoard(_generator.Create(1));
        Assert(!board.IsSolved(), "A new board must not start solved.");
        board.ApplySolution();
        Assert(board.IsSolved(), "The stored solution must solve the board.");
    }

    private void CrossingAnotherColorIsBlocked()
    {
        var puzzle = _generator.Create(1);
        var board = new PlayerBoard(puzzle);
        var first = puzzle.Pairs[0];
        var second = puzzle.Pairs[1];

        board.StartPath(first.Id, first.Start);
        Assert(board.TryAppend(first.Id, puzzle.SolutionPaths[first.Id][1]), "First color should draw into a free neighbor.");

        board.StartPath(second.Id, second.Start);
        var blockedCell = puzzle.SolutionPaths[first.Id][1];
        Assert(!board.TryAppend(second.Id, blockedCell), "Second color must not enter a cell occupied by another color.");
    }

    private void OwnEndpointStopsFurtherDrawing()
    {
        var puzzle = CreateSmallRulesPuzzle();
        var board = new PlayerBoard(puzzle);

        board.StartPath(0, new CellPosition(0, 0));
        Assert(board.TryAppend(0, new CellPosition(0, 1)), "Path should move through a free cell.");
        Assert(board.TryAppend(0, new CellPosition(0, 2)), "Path should be allowed to finish on its matching endpoint.");
        Assert(!board.TryAppend(0, new CellPosition(0, 3)), "A connected path must not continue past its matching endpoint.");
    }

    private void PathCanContinueFromLooseEnd()
    {
        var puzzle = CreateSmallRulesPuzzle();
        var board = new PlayerBoard(puzzle);

        board.StartPath(0, new CellPosition(0, 0));
        Assert(board.TryAppend(0, new CellPosition(0, 1)), "Path should move through a free cell.");
        Assert(board.CanContinuePathFrom(0, new CellPosition(0, 1)), "A partial path should continue from its loose end.");

        board.StartPathContinuation(0, new CellPosition(0, 1));
        Assert(board.TryAppend(0, new CellPosition(0, 2)), "Continuation should append from the loose end.");
        Assert(board.IsPairConnected(0), "Continuation should be able to complete the pair.");
    }

    private void NewLineCanReplaceExistingLine()
    {
        var puzzle = CreateSmallRulesPuzzle();
        var board = new PlayerBoard(puzzle);

        board.StartPath(0, new CellPosition(0, 0));
        Assert(board.TryAppend(0, new CellPosition(0, 1)), "First path should occupy an intermediate cell.");

        board.StartPath(1, new CellPosition(1, 0));
        Assert(board.TryAppend(1, new CellPosition(1, 1)), "Second path should move through a free cell.");
        Assert(board.TryAppendDetailed(1, new CellPosition(0, 1)) == AppendResult.OverwroteOtherPath, "Crossing a non-endpoint line should replace that line.");
        Assert(board.Paths[0].Count == 0, "The replaced line should disappear.");
    }

    private void HintAppliesMissingValidPath()
    {
        var puzzle = _generator.Create(1);
        var board = new PlayerBoard(puzzle);

        var hintedPairId = board.ApplyNextMissingSolutionPath();
        Assert(hintedPairId == 0, "The first missing pair should receive the hint.");
        Assert(board.Paths[0].SequenceEqual(puzzle.SolutionPaths[0]), "Hint should apply the stored valid solution path.");
    }

    private void HintClearsPathsThatWouldBreakCanonicalSolution()
    {
        var puzzle = _generator.Create(4);
        var board = new PlayerBoard(puzzle);
        var pairToHint = puzzle.Pairs[0];
        var incompatiblePair = puzzle.Pairs[1];
        var incompatiblePath = puzzle.SolutionPaths[pairToHint.Id]
            .Skip(1)
            .Take(3)
            .ToList();

        board.Paths[incompatiblePair.Id] = incompatiblePath;

        var hintedPairId = board.ApplyNextMissingSolutionPath();
        Assert(hintedPairId == pairToHint.Id, "Hint should still choose the first missing pair.");
        Assert(board.Paths[pairToHint.Id].SequenceEqual(puzzle.SolutionPaths[pairToHint.Id]), "Hint should apply the canonical solution path.");
        Assert(board.Paths[incompatiblePair.Id].Count == 0, "Hint should clear paths that are incompatible with the canonical solution.");

        board.ApplySolution();
        Assert(board.IsSolved(), "After a safe hint, the canonical solution must remain reachable.");
    }

    private void UndoRestoresPreviousPath()
    {
        var puzzle = _generator.Create(2);
        var board = new PlayerBoard(puzzle);
        var pair = puzzle.Pairs[0];

        board.StartPath(pair.Id, pair.Start);
        Assert(board.Paths[pair.Id].Count == 1, "Path should contain the starting endpoint.");
        Assert(board.Undo(), "Undo should be available after starting a path.");
        Assert(board.Paths[pair.Id].Count == 0, "Undo should restore the empty path.");
    }

    private void LevelProgressionReachesMaximumBoard()
    {
        var previousSize = 5;
        for (var level = 1; level <= 40; level++)
        {
            var size = _generator.Create(level).Size;
            Assert(size >= previousSize, "Board size should not decrease as levels advance.");
            Assert(size is >= 5 and <= 15, "Board size must stay between 5 and 15.");
            previousSize = size;
        }
    }

    private void ProgressionRequiresMoreSolvesOverTime()
    {
        Assert(_generator.GetRequiredSolvesForLevel(1) == 1, "Early levels should progress after one solve.");
        Assert(_generator.GetRequiredSolvesForLevel(4) == 2, "Level 4 should require more solves.");
        Assert(_generator.GetRequiredSolvesForLevel(10) == 4, "Higher levels should require several solves.");
        Assert(_generator.GetRequiredSolvesForLevel(31) == 8, "Advanced levels should require the capped maximum.");
    }

    private void FixedSizeGenerationKeepsBoardSize()
    {
        for (var size = 5; size <= 15; size++)
        {
            var puzzle = _generator.CreateFixedSize(size, 3);
            Assert(puzzle.Size == size, "Fixed-size mode must keep the selected board size.");
        }
    }


    private void GeneratedPuzzlesAreValidAcrossRange()
    {
        for (var level = 1; level <= 40; level++)
        {
            var puzzle = _generator.Create(level);
            var occupied = new HashSet<CellPosition>();

            Assert(puzzle.Pairs.Count >= 4, "Every puzzle should have at least four colors.");

            foreach (var pair in puzzle.Pairs)
            {
                Assert(puzzle.SolutionPaths.ContainsKey(pair.Id), "Every pair must have a solution path.");
                var path = puzzle.SolutionPaths[pair.Id];
                Assert(path[0] == pair.Start, "Solution path must begin at the visible start point.");
                Assert(path[^1] == pair.End, "Solution path must end at the visible end point.");

                for (var i = 0; i < path.Count; i++)
                {
                    Assert(occupied.Add(path[i]), "Solution paths must not overlap.");
                    Assert(path[i].Row >= 0 && path[i].Row < puzzle.Size, "Rows must stay inside the board.");
                    Assert(path[i].Column >= 0 && path[i].Column < puzzle.Size, "Columns must stay inside the board.");

                    if (i > 0)
                    {
                        Assert(path[i - 1].ManhattanDistance(path[i]) == 1, "Solution paths must be orthogonal and contiguous.");
                    }
                }
            }

            Assert(occupied.Count == puzzle.Size * puzzle.Size, "Solution must fill the board.");
        }
    }

    private void AdvancedLevelsUseMoreColorsWithBalancedPathLengths()
    {
        var puzzle = _generator.Create(31);
        var averageLength = puzzle.SolutionPaths.Values.Average(path => path.Count);
        var totalTurns = puzzle.SolutionPaths.Values.Sum(CountTurns);

        Assert(puzzle.Size == 15, "Level 31 should use the largest board.");
        Assert(puzzle.Pairs.Count >= 14, "Advanced boards should use more colors to avoid excessive zig-zag paths.");
        Assert(averageLength is >= 12 and <= 17, "Advanced paths should be substantial without becoming too long.");
        Assert(totalTurns >= 45, "Advanced boards should include many bends across the solution.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static PuzzleDefinition CreateSmallRulesPuzzle()
    {
        var paths = new Dictionary<int, IReadOnlyList<CellPosition>>
        {
            [0] = new[]
            {
                new CellPosition(0, 0),
                new CellPosition(0, 1),
                new CellPosition(0, 2)
            },
            [1] = new[]
            {
                new CellPosition(1, 0),
                new CellPosition(1, 1),
                new CellPosition(1, 2)
            }
        };
        var pairs = new[]
        {
            new FlowPair(0, "#E53935", new CellPosition(0, 0), new CellPosition(0, 2)),
            new FlowPair(1, "#1E88E5", new CellPosition(1, 0), new CellPosition(1, 2))
        };

        return new PuzzleDefinition(1, 5, pairs, paths);
    }

    private static int CountTurns(IReadOnlyList<CellPosition> path)
    {
        var turns = 0;
        for (var i = 2; i < path.Count; i++)
        {
            var previousDirection = (
                Row: path[i - 1].Row - path[i - 2].Row,
                Column: path[i - 1].Column - path[i - 2].Column);
            var currentDirection = (
                Row: path[i].Row - path[i - 1].Row,
                Column: path[i].Column - path[i - 1].Column);

            if (previousDirection != currentDirection)
            {
                turns++;
            }
        }

        return turns;
    }
}
