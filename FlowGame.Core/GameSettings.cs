namespace FlowGame.Core;

public sealed class GameSettings
{
    public int SelectedStartLevel { get; set; } = 1;
    public int LastUnlockedLevel { get; set; } = 1;
    public GameMode Mode { get; set; } = GameMode.Progression;
    public int FixedBoardSize { get; set; } = 8;
    public int ProgressionSolvedAtCurrentLevel { get; set; }
    public int CurrentPuzzleVariant { get; set; }
    public int FixedSizePuzzleVariant { get; set; }
    public int PuzzlesSolved { get; set; }
    public int Score { get; set; }
    public int HintCredits { get; set; } = 3;
    public int NextHintCreditScore { get; set; } = 2500;
    public int HintCreditsEarned { get; set; }
    public bool HighContrastGrid { get; set; }
}
