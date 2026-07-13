using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FlowGame.Core;

namespace FlowGame.Wpf;

public partial class MainWindow : Window
{
    private readonly PuzzleGenerator _generator = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly Dictionary<int, SolidColorBrush> _brushes = new();
    private GameSettings _settings = new();
    private PlayerBoard? _board;
    private int _currentLevel = 1;
    private int? _activePairId;
    private bool _isDrawing;
    private bool _isInitializing = true;
    private bool _isCompletingLevel;
    private bool _isUpdatingControls;
    private CellPosition? _lastPenalizedInvalidCell;

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsStore.Load();
        PopulateModes();
        PopulateStartLevels();
        PopulateFixedSizes();
        ModeCombo.SelectedItem = GetModeLabel(_settings.Mode);
        StartLevelCombo.SelectedItem = Math.Clamp(_settings.SelectedStartLevel, 1, 31);
        FixedSizeCombo.SelectedItem = Math.Clamp(_settings.FixedBoardSize, 5, 15);
        _isInitializing = false;
        LoadCurrentPuzzle();
    }

    private void PopulateModes()
    {
        ModeCombo.Items.Add(GetModeLabel(GameMode.Progression));
        ModeCombo.Items.Add(GetModeLabel(GameMode.FixedSize));
    }

    private void PopulateStartLevels()
    {
        for (var level = 1; level <= 31; level++)
        {
            StartLevelCombo.Items.Add(level);
        }
    }

    private void PopulateFixedSizes()
    {
        for (var size = 5; size <= 15; size++)
        {
            FixedSizeCombo.Items.Add(size);
        }
    }

    private void LoadCurrentPuzzle()
    {
        if (_settings.Mode == GameMode.FixedSize)
        {
            LoadFixedSizePuzzle();
        }
        else
        {
            LoadLevel(_settings.SelectedStartLevel);
        }
    }

    private void LoadLevel(int level)
    {
        _currentLevel = Math.Max(1, level);
        _settings.SelectedStartLevel = _currentLevel;
        _board = new PlayerBoard(_generator.Create(_currentLevel, _settings.CurrentPuzzleVariant));
        _activePairId = null;
        _isDrawing = false;
        _isCompletingLevel = false;
        _lastPenalizedInvalidCell = null;
        ConfigureBrushes();
        UpdateHud("Puzzle pronto.");
        DrawBoard();
    }

    private void LoadFixedSizePuzzle()
    {
        var size = Math.Clamp(_settings.FixedBoardSize, 5, 15);
        _currentLevel = 1 + (size - 5) * 3;
        _board = new PlayerBoard(_generator.CreateFixedSize(size, _settings.FixedSizePuzzleVariant));
        _activePairId = null;
        _isDrawing = false;
        _isCompletingLevel = false;
        _lastPenalizedInvalidCell = null;
        ConfigureBrushes();
        UpdateHud("Puzzle pronto.");
        DrawBoard();
    }

    private void ConfigureBrushes()
    {
        _brushes.Clear();
        if (_board is null)
        {
            return;
        }

        foreach (var pair in _board.Puzzle.Pairs)
        {
            _brushes[pair.Id] = (SolidColorBrush)new BrushConverter().ConvertFromString(pair.ColorHex)!;
        }
    }

    private void DrawBoard()
    {
        if (_board is null)
        {
            return;
        }

        BoardCanvas.Children.Clear();
        var size = _board.Puzzle.Size;
        var cell = BoardCanvas.Width / size;

        DrawGrid(size, cell);
        DrawPaths(cell);
        DrawEndpoints(cell);
    }

    private void DrawGrid(int size, double cell)
    {
        var gridBrush = new SolidColorBrush(Color.FromRgb(47, 59, 68));

        for (var i = 0; i <= size; i++)
        {
            var offset = i * cell;
            BoardCanvas.Children.Add(new Line
            {
                X1 = offset,
                Y1 = 0,
                X2 = offset,
                Y2 = BoardCanvas.Height,
                Stroke = gridBrush,
                StrokeThickness = i == 0 || i == size ? 2 : 1
            });
            BoardCanvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = offset,
                X2 = BoardCanvas.Width,
                Y2 = offset,
                Stroke = gridBrush,
                StrokeThickness = i == 0 || i == size ? 2 : 1
            });
        }
    }

    private void DrawPaths(double cell)
    {
        if (_board is null)
        {
            return;
        }

        foreach (var item in _board.Paths)
        {
            var path = item.Value;
            if (path.Count == 0)
            {
                continue;
            }

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(GetCenter(path[0], cell), false, false);
                foreach (var position in path.Skip(1))
                {
                    context.LineTo(GetCenter(position, cell), true, false);
                }
            }

            geometry.Freeze();
            BoardCanvas.Children.Add(new Path
            {
                Data = geometry,
                Stroke = _brushes[item.Key],
                StrokeThickness = Math.Max(10, cell * 0.28),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Opacity = _activePairId == item.Key ? 1.0 : 0.82
            });
        }
    }

    private void DrawEndpoints(double cell)
    {
        if (_board is null)
        {
            return;
        }

        foreach (var pair in _board.Puzzle.Pairs)
        {
            DrawEndpoint(pair.Id, pair.Start, cell);
            DrawEndpoint(pair.Id, pair.End, cell);
        }
    }

    private void DrawEndpoint(int pairId, CellPosition position, double cell)
    {
        var center = GetCenter(position, cell);
        var diameter = Math.Max(22, cell * 0.58);
        var endpoint = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = _brushes[pairId],
            Stroke = Brushes.White,
            StrokeThickness = _activePairId == pairId ? 4 : 2
        };

        Canvas.SetLeft(endpoint, center.X - diameter / 2);
        Canvas.SetTop(endpoint, center.Y - diameter / 2);
        BoardCanvas.Children.Add(endpoint);
    }

    private static Point GetCenter(CellPosition position, double cell)
    {
        return new Point((position.Column + 0.5) * cell, (position.Row + 0.5) * cell);
    }

    private CellPosition? GetCell(Point point)
    {
        if (_board is null)
        {
            return null;
        }

        var cell = BoardCanvas.Width / _board.Puzzle.Size;
        var row = (int)(point.Y / cell);
        var column = (int)(point.X / cell);
        var position = new CellPosition(row, column);
        return _board.IsInside(position) ? position : null;
    }

    private void BoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_board is null)
        {
            return;
        }

        var position = GetCell(e.GetPosition(BoardCanvas));
        if (position is null)
        {
            return;
        }

        var occupiedPairId = _board.OccupiedBy(position.Value);
        if (occupiedPairId is not null && _board.CanContinuePathFrom(occupiedPairId.Value, position.Value))
        {
            _activePairId = occupiedPairId;
            _board.StartPathContinuation(occupiedPairId.Value, position.Value);
            _isDrawing = true;
            _lastPenalizedInvalidCell = null;
            BoardCanvas.CaptureMouse();
            DrawBoard();
            return;
        }

        var pairId = _board.Puzzle.PairIdAtEndpoint(position.Value);
        if (pairId is null)
        {
            return;
        }

        if (_board.Paths[pairId.Value].Count > 1)
        {
            ApplyPenalty(GetRedoPenalty(_board.Paths[pairId.Value].Count), "Ligacao refeita.");
        }

        _activePairId = pairId;
        _board.StartPath(pairId.Value, position.Value);
        _isDrawing = true;
        _lastPenalizedInvalidCell = null;
        BoardCanvas.CaptureMouse();
        DrawBoard();
    }

    private void BoardCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing || _board is null || _activePairId is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var position = GetCell(e.GetPosition(BoardCanvas));
        if (position is null)
        {
            return;
        }

        var appendResult = _board.TryAppendDetailed(_activePairId.Value, position.Value);
        if (appendResult != AppendResult.Blocked)
        {
            _lastPenalizedInvalidCell = null;
            if (appendResult == AppendResult.OverwroteOtherPath)
            {
                ApplyPenalty(50, "Linha substituida.");
            }

            DrawBoard();
            CheckSolved();
        }
        else if (_lastPenalizedInvalidCell != position)
        {
            ApplyPenalty(25, "Movimento bloqueado.");
            _lastPenalizedInvalidCell = position;
        }
    }

    private void BoardCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = false;
        BoardCanvas.ReleaseMouseCapture();
        CheckSolved();
    }

    private void CheckSolved()
    {
        if (_board is null || _isCompletingLevel || !_board.IsSolved())
        {
            return;
        }

        _isCompletingLevel = true;
        var completedLevel = _currentLevel;
        var bonus = GetSolveBonus(_board.Puzzle);
        ApplyScoreDelta(bonus);
        _settings.PuzzlesSolved++;
        var nextDescription = AdvanceAfterSolve();
        _settingsStore.Save(_settings);
        UpdateHud($"Nivel {completedLevel} concluido. +{bonus} pontos.");

        var completedDescription = _settings.Mode == GameMode.FixedSize
            ? $"Voce concluiu um tabuleiro {_settings.FixedBoardSize}x{_settings.FixedBoardSize}."
            : $"Voce concluiu o nivel {completedLevel}.";
        var completionWindow = new LevelCompleteWindow(completedDescription, bonus, _settings.Score, nextDescription)
        {
            Owner = this
        };
        completionWindow.ShowDialog();

        LoadCurrentPuzzle();
        UpdateHud(GetCurrentProgressStatus());
    }

    private void UpdateHud(string status)
    {
        if (_board is null)
        {
            return;
        }

        LevelText.Text = $"Nivel {_currentLevel}";
        SizeText.Text = $"Tabuleiro {_board.Puzzle.Size}x{_board.Puzzle.Size} | {_board.Puzzle.Pairs.Count} cores";
        SolvedText.Text = GetProgressText();
        ScoreText.Text = $"Score: {_settings.Score}";
        CreditsText.Text = $"Dicas: {_settings.HintCredits}";
        StatusText.Text = status;
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        if (_settings.Mode == GameMode.FixedSize)
        {
            _settings.FixedSizePuzzleVariant++;
        }
        else
        {
            var level = StartLevelCombo.SelectedItem is int selected ? selected : 1;
            _settings.SelectedStartLevel = level;
            _settings.CurrentPuzzleVariant = 0;
            _settings.ProgressionSolvedAtCurrentLevel = 0;
            _settings.LastUnlockedLevel = Math.Max(_settings.LastUnlockedLevel, level);
        }

        _settingsStore.Save(_settings);
        LoadCurrentPuzzle();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (_board is null)
        {
            return;
        }

        if (_board.Paths.Values.Any(path => path.Count > 1))
        {
            ApplyPenalty(100, "Tabuleiro reiniciado com progresso.");
        }

        _board.Reset();
        UpdateHud("Puzzle reiniciado.");
        DrawBoard();
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_board?.Undo() == true)
        {
            ApplyPenalty(25, "Ultima acao desfeita.");
            DrawBoard();
        }
    }

    private void UseHint_Click(object sender, RoutedEventArgs e)
    {
        if (_board is null)
        {
            return;
        }

        if (_settings.HintCredits <= 0)
        {
            UpdateHud($"Sem dicas disponiveis. Proxima dica em {_settings.NextHintCreditScore} pontos.");
            return;
        }

        var hintedPairId = _board.ApplyNextMissingSolutionPath();
        if (hintedPairId is null)
        {
            UpdateHud("Todas as cores ja estao ligadas.");
            return;
        }

        _settings.HintCredits--;
        _settingsStore.Save(_settings);
        _activePairId = hintedPairId;
        UpdateHud("Dica usada: uma ligacao valida foi incluida.");
        DrawBoard();
        CheckSolved();
    }

    private void ShowSolution_Click(object sender, RoutedEventArgs e)
    {
        if (_board is null)
        {
            return;
        }

        var solutionWindow = new SolutionWindow(_board.Puzzle)
        {
            Owner = this
        };
        solutionWindow.Show();
    }

    private void StartLevelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        if (StartLevelCombo.SelectedItem is int selected)
        {
            _settings.SelectedStartLevel = selected;
            _settings.CurrentPuzzleVariant = 0;
            _settings.ProgressionSolvedAtCurrentLevel = 0;
            _settings.LastUnlockedLevel = Math.Max(_settings.LastUnlockedLevel, selected);
            _settingsStore.Save(_settings);

            if (!_isInitializing && _settings.Mode == GameMode.Progression)
            {
                LoadLevel(selected);
                UpdateHud($"Nivel {selected} carregado.");
            }
        }
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        if (ModeCombo.SelectedItem is not string selected)
        {
            return;
        }

        _settings.Mode = selected == GetModeLabel(GameMode.FixedSize)
            ? GameMode.FixedSize
            : GameMode.Progression;
        _settingsStore.Save(_settings);

        if (!_isInitializing)
        {
            LoadCurrentPuzzle();
            UpdateHud(_settings.Mode == GameMode.FixedSize ? "Modo tamanho fixo carregado." : "Modo progressao carregado.");
        }
    }

    private void FixedSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        if (FixedSizeCombo.SelectedItem is int selected)
        {
            _settings.FixedBoardSize = selected;
            _settings.FixedSizePuzzleVariant = 0;
            _settingsStore.Save(_settings);

            if (!_isInitializing && _settings.Mode == GameMode.FixedSize)
            {
                LoadFixedSizePuzzle();
                UpdateHud($"Tamanho fixo {selected}x{selected} carregado.");
            }
        }
    }

    private void BoardCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawBoard();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.R)
        {
            Reset_Click(sender, e);
        }
        else if (e.Key == Key.Z && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            Undo_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            _isDrawing = false;
            BoardCanvas.ReleaseMouseCapture();
            UpdateHud("Acao cancelada.");
            DrawBoard();
        }
    }

    private static int GetSolveBonus(PuzzleDefinition puzzle)
    {
        return puzzle.Size * puzzle.Size * 10 + puzzle.Pairs.Count * 50 + puzzle.Level * 25;
    }

    private static int GetRedoPenalty(int pathLength)
    {
        return Math.Clamp(pathLength * 10, 25, 150);
    }

    private void ApplyPenalty(int points, string reason)
    {
        ApplyScoreDelta(-points);
        UpdateHud($"{reason} -{points} pontos.");
    }

    private void ApplyScoreDelta(int delta)
    {
        _settings.Score = Math.Max(0, _settings.Score + delta);
        if (delta > 0)
        {
            AwardHintCreditsFromScore();
        }

        _settingsStore.Save(_settings);
    }

    private void AwardHintCreditsFromScore()
    {
        while (_settings.Score >= _settings.NextHintCreditScore)
        {
            _settings.HintCredits++;
            _settings.HintCreditsEarned++;
            _settings.NextHintCreditScore += GetNextHintCreditStep(_settings.HintCreditsEarned);
        }
    }

    private static int GetNextHintCreditStep(int creditsEarned)
    {
        return 2500 + Math.Min(creditsEarned, 8) * 750;
    }

    private string AdvanceAfterSolve()
    {
        if (_settings.Mode == GameMode.FixedSize)
        {
            _settings.FixedSizePuzzleVariant++;
            return $"Proximo puzzle: {_settings.FixedBoardSize}x{_settings.FixedBoardSize}";
        }

        var required = _generator.GetRequiredSolvesForLevel(_currentLevel);
        _settings.ProgressionSolvedAtCurrentLevel++;

        if (_settings.ProgressionSolvedAtCurrentLevel >= required)
        {
            _settings.SelectedStartLevel = _currentLevel + 1;
            _settings.CurrentPuzzleVariant = 0;
            _settings.ProgressionSolvedAtCurrentLevel = 0;
            _settings.LastUnlockedLevel = Math.Max(_settings.LastUnlockedLevel, _settings.SelectedStartLevel);
            _isUpdatingControls = true;
            StartLevelCombo.SelectedItem = Math.Clamp(_settings.SelectedStartLevel, 1, 31);
            _isUpdatingControls = false;
            return $"Proximo nivel: {_settings.SelectedStartLevel}";
        }

        _settings.CurrentPuzzleVariant++;
        var remaining = required - _settings.ProgressionSolvedAtCurrentLevel;
        return remaining == 1
            ? $"Mais 1 puzzle no nivel {_currentLevel}"
            : $"Mais {remaining} puzzles no nivel {_currentLevel}";
    }

    private string GetProgressText()
    {
        if (_settings.Mode == GameMode.FixedSize)
        {
            return $"Resolvidos: {_settings.PuzzlesSolved} | Fixo {_settings.FixedBoardSize}x{_settings.FixedBoardSize}";
        }

        var required = _generator.GetRequiredSolvesForLevel(_currentLevel);
        return $"Resolvidos: {_settings.PuzzlesSolved} | Nivel: {_settings.ProgressionSolvedAtCurrentLevel}/{required}";
    }

    private string GetCurrentProgressStatus()
    {
        if (_settings.Mode == GameMode.FixedSize)
        {
            return $"Novo puzzle {_settings.FixedBoardSize}x{_settings.FixedBoardSize} carregado.";
        }

        var required = _generator.GetRequiredSolvesForLevel(_currentLevel);
        return $"Nivel {_currentLevel} carregado. Progresso {Math.Min(_settings.ProgressionSolvedAtCurrentLevel, required)}/{required}.";
    }

    private static string GetModeLabel(GameMode mode)
    {
        return mode == GameMode.FixedSize ? "Tamanho fixo" : "Progressao";
    }
}
