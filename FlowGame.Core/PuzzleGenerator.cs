namespace FlowGame.Core;

public sealed class PuzzleGenerator
{
    private static readonly string[] Palette =
    [
        "#E53935", "#1E88E5", "#43A047", "#FDD835", "#8E24AA",
        "#FB8C00", "#00ACC1", "#D81B60", "#7CB342", "#5E35B1",
        "#00897B", "#C0CA33", "#6D4C41", "#3949AB", "#F4511E"
    ];

    public PuzzleDefinition Create(int level, int variant = 0)
    {
        level = Math.Max(1, level);
        if (variant == 0)
        {
            return level switch
            {
                1 => CreateFromSolution(1, 5, FixedLevelOne()),
                2 => CreateFromSolution(2, 5, FixedLevelTwo()),
                3 => CreateFromSolution(3, 6, FixedLevelThree()),
                _ => CreateGenerated(level, variant)
            };
        }

        return CreateGenerated(level, variant);
    }

    public PuzzleDefinition CreateFixedSize(int size, int variant)
    {
        size = Math.Clamp(size, PuzzleDefinition.MinSize, PuzzleDefinition.MaxSize);
        var effectiveLevel = 1 + (size - PuzzleDefinition.MinSize) * 3 + Math.Max(0, variant / 2);
        return CreateGenerated(effectiveLevel, variant, size);
    }

    public int GetRequiredSolvesForLevel(int level)
    {
        level = Math.Max(1, level);
        return level switch
        {
            <= 3 => 1,
            <= 6 => 2,
            <= 9 => 3,
            <= 12 => 4,
            <= 18 => 5,
            <= 24 => 6,
            <= 30 => 7,
            _ => 8
        };
    }

    public int GetSizeForLevel(int level)
    {
        return Math.Clamp(5 + (Math.Max(1, level) - 1) / 3, PuzzleDefinition.MinSize, PuzzleDefinition.MaxSize);
    }

    public int GetPairCountForLevel(int level, int size)
    {
        var baseCount = 3 + level / 4 + size / 2;
        var maximum = Math.Clamp(size, 5, Palette.Length);
        return Math.Clamp(baseCount, 4, maximum);
    }

    private PuzzleDefinition CreateGenerated(int level, int variant, int? fixedSize = null)
    {
        var size = fixedSize ?? GetSizeForLevel(level);
        var pairCount = GetPairCountForLevel(level, size);
        var seed = level * 101 + variant * 997;
        var cells = BuildComplexHamiltonianCells(size, seed);
        var lengths = SplitLengths(cells.Length, pairCount, seed);
        var paths = new Dictionary<int, IReadOnlyList<CellPosition>>();
        var cursor = 0;

        for (var pairId = 0; pairId < pairCount; pairId++)
        {
            var length = lengths[pairId];
            paths[pairId] = cells.Skip(cursor).Take(length).ToArray();
            cursor += length;
        }

        return CreateFromSolution(level, size, paths);
    }

    private static CellPosition[] BuildComplexHamiltonianCells(int size, int seed)
    {
        var random = new Random(seed * 7919 + size * 104729);
        var cells = BuildSerpentineCells(size, seed);
        var iterations = size * size * Math.Clamp(seed, 12, 64);

        for (var i = 0; i < iterations; i++)
        {
            TryBackbite(cells, random, size);
        }

        return cells;
    }

    private static void TryBackbite(CellPosition[] path, Random random, int size)
    {
        var useStart = random.Next(2) == 0;
        var endpointIndex = useStart ? 0 : path.Length - 1;
        var endpoint = path[endpointIndex];
        var candidates = GetNeighbors(endpoint, size)
            .Select(neighbor => Array.IndexOf(path, neighbor))
            .Where(index => index >= 0)
            .Where(index => useStart ? index > 1 : index < path.Length - 2)
            .OrderBy(_ => random.Next())
            .ToArray();

        if (candidates.Length == 0)
        {
            return;
        }

        var neighborIndex = candidates[0];
        if (useStart)
        {
            Array.Reverse(path, 0, neighborIndex);
        }
        else
        {
            Array.Reverse(path, neighborIndex + 1, path.Length - neighborIndex - 1);
        }
    }

    private PuzzleDefinition CreateFromSolution(int level, int size, IReadOnlyDictionary<int, IReadOnlyList<CellPosition>> paths)
    {
        if (size is < PuzzleDefinition.MinSize or > PuzzleDefinition.MaxSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Board size must be between 5 and 15.");
        }

        var pairs = paths
            .OrderBy(item => item.Key)
            .Select(item => new FlowPair(
                item.Key,
                Palette[item.Key % Palette.Length],
                item.Value[0],
                item.Value[^1]))
            .ToArray();

        var puzzle = new PuzzleDefinition(level, size, pairs, paths);
        ValidateGeneratedPuzzle(puzzle);
        return puzzle;
    }

    private static CellPosition[] BuildSerpentineCells(int size, int seed)
    {
        var cells = new List<CellPosition>(size * size);
        var vertical = seed % 2 == 0;

        if (vertical)
        {
            for (var column = 0; column < size; column++)
            {
                if (column % 2 == 0)
                {
                    for (var row = 0; row < size; row++)
                    {
                        cells.Add(new CellPosition(row, column));
                    }
                }
                else
                {
                    for (var row = size - 1; row >= 0; row--)
                    {
                        cells.Add(new CellPosition(row, column));
                    }
                }
            }
        }
        else
        {
            for (var row = 0; row < size; row++)
            {
                if (row % 2 == 0)
                {
                    for (var column = 0; column < size; column++)
                    {
                        cells.Add(new CellPosition(row, column));
                    }
                }
                else
                {
                    for (var column = size - 1; column >= 0; column--)
                    {
                        cells.Add(new CellPosition(row, column));
                    }
                }
            }
        }

        if (seed % 3 == 0)
        {
            cells.Reverse();
        }

        return cells.ToArray();
    }

    private static int[] SplitLengths(int totalCells, int pairCount, int seed)
    {
        var minimum = Math.Max(5, totalCells / (pairCount * 3));
        var lengths = Enumerable.Repeat(minimum, pairCount).ToArray();
        var remaining = totalCells - minimum * pairCount;
        var cursor = seed % pairCount;

        while (remaining > 0)
        {
            var add = Math.Min(remaining, 2 + (seed + cursor * 3) % 7);
            lengths[cursor] += add;
            remaining -= add;
            cursor = (cursor + 1) % pairCount;
        }

        return lengths;
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<CellPosition>> FixedLevelOne()
    {
        return new Dictionary<int, IReadOnlyList<CellPosition>>
        {
            [0] = Positions((0, 0), (0, 1), (0, 2), (0, 3), (0, 4)),
            [1] = Positions((1, 4), (1, 3), (1, 2), (1, 1), (1, 0), (2, 0)),
            [2] = Positions((2, 1), (2, 2), (2, 3), (2, 4), (3, 4), (3, 3), (3, 2)),
            [3] = Positions((3, 1), (3, 0), (4, 0), (4, 1), (4, 2), (4, 3), (4, 4)),
        };
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<CellPosition>> FixedLevelTwo()
    {
        return new Dictionary<int, IReadOnlyList<CellPosition>>
        {
            [0] = Positions((0, 0), (1, 0), (2, 0), (3, 0), (4, 0)),
            [1] = Positions((4, 1), (3, 1), (2, 1), (1, 1), (0, 1), (0, 2)),
            [2] = Positions((1, 2), (2, 2), (3, 2), (4, 2), (4, 3), (3, 3)),
            [3] = Positions((2, 3), (1, 3), (0, 3), (0, 4), (1, 4), (2, 4), (3, 4), (4, 4)),
        };
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<CellPosition>> FixedLevelThree()
    {
        return new Dictionary<int, IReadOnlyList<CellPosition>>
        {
            [0] = Positions((0, 0), (0, 1), (0, 2), (0, 3), (0, 4), (0, 5)),
            [1] = Positions((1, 5), (1, 4), (1, 3), (1, 2), (1, 1), (1, 0), (2, 0)),
            [2] = Positions((2, 1), (2, 2), (2, 3), (2, 4), (2, 5), (3, 5), (3, 4)),
            [3] = Positions((3, 3), (3, 2), (3, 1), (3, 0), (4, 0), (4, 1), (4, 2)),
            [4] = Positions((4, 3), (4, 4), (4, 5), (5, 5), (5, 4), (5, 3), (5, 2), (5, 1), (5, 0)),
        };
    }

    private static IReadOnlyList<CellPosition> Positions(params (int Row, int Column)[] cells)
    {
        return cells.Select(cell => new CellPosition(cell.Row, cell.Column)).ToArray();
    }

    private static IEnumerable<CellPosition> GetNeighbors(CellPosition position, int size)
    {
        var candidates = new[]
        {
            new CellPosition(position.Row - 1, position.Column),
            new CellPosition(position.Row + 1, position.Column),
            new CellPosition(position.Row, position.Column - 1),
            new CellPosition(position.Row, position.Column + 1)
        };

        return candidates.Where(cell =>
            cell.Row >= 0
            && cell.Column >= 0
            && cell.Row < size
            && cell.Column < size);
    }

    private static void ValidateGeneratedPuzzle(PuzzleDefinition puzzle)
    {
        var allCells = new HashSet<CellPosition>();

        foreach (var path in puzzle.SolutionPaths.Values)
        {
            if (path.Count < 2)
            {
                throw new InvalidOperationException("Every generated path must have endpoints.");
            }

            for (var i = 0; i < path.Count; i++)
            {
                var cell = path[i];
                if (cell.Row < 0 || cell.Column < 0 || cell.Row >= puzzle.Size || cell.Column >= puzzle.Size)
                {
                    throw new InvalidOperationException("Generated path contains a cell outside the board.");
                }

                if (!allCells.Add(cell))
                {
                    throw new InvalidOperationException("Generated paths must not overlap.");
                }

                if (i > 0 && path[i - 1].ManhattanDistance(cell) != 1)
                {
                    throw new InvalidOperationException("Generated paths must be contiguous.");
                }
            }
        }

        if (allCells.Count != puzzle.Size * puzzle.Size)
        {
            throw new InvalidOperationException("Generated puzzle solution must fill the board.");
        }
    }
}
