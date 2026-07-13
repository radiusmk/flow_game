using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FlowGame.Core;

namespace FlowGame.Wpf;

public sealed class SolutionWindow : Window
{
    private readonly PuzzleDefinition _puzzle;
    private readonly Canvas _canvas = new()
    {
        Width = 720,
        Height = 720,
        Background = new SolidColorBrush(Color.FromRgb(18, 23, 28))
    };

    public SolutionWindow(PuzzleDefinition puzzle)
    {
        _puzzle = puzzle;
        Title = $"Solucao - Nivel {puzzle.Level}";
        Width = 820;
        Height = 860;
        MinWidth = 620;
        MinHeight = 660;
        Background = new SolidColorBrush(Color.FromRgb(16, 20, 24));
        Foreground = Brushes.White;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        KeyDown += SolutionWindow_KeyDown;

        var layout = new Grid
        {
            Margin = new Thickness(24)
        };
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var boardFrame = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(12, 15, 18)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(41, 52, 60)),
            BorderThickness = new Thickness(1),
            Child = new Viewbox
            {
                Stretch = Stretch.Uniform,
                Child = _canvas
            }
        };
        Grid.SetRow(boardFrame, 0);
        layout.Children.Add(boardFrame);

        var closeButton = new Button
        {
            Content = "Fechar",
            MinWidth = 110,
            MinHeight = 36,
            Margin = new Thickness(0, 14, 0, 0),
            Padding = new Thickness(14, 6, 14, 6),
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = new SolidColorBrush(Color.FromRgb(38, 49, 58)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(59, 74, 85)),
            BorderThickness = new Thickness(1)
        };
        closeButton.Click += (_, _) => Close();
        Grid.SetRow(closeButton, 1);
        layout.Children.Add(closeButton);

        Content = layout;

        Loaded += (_, _) => DrawSolution();
    }

    private void SolutionWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void DrawSolution()
    {
        _canvas.Children.Clear();
        var cell = _canvas.Width / _puzzle.Size;
        DrawGrid(cell);
        DrawSolutionPaths(cell);
        DrawEndpoints(cell);
    }

    private void DrawGrid(double cell)
    {
        var gridBrush = new SolidColorBrush(Color.FromRgb(47, 59, 68));
        for (var i = 0; i <= _puzzle.Size; i++)
        {
            var offset = i * cell;
            _canvas.Children.Add(new Line
            {
                X1 = offset,
                Y1 = 0,
                X2 = offset,
                Y2 = _canvas.Height,
                Stroke = gridBrush,
                StrokeThickness = i == 0 || i == _puzzle.Size ? 2 : 1
            });
            _canvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = offset,
                X2 = _canvas.Width,
                Y2 = offset,
                Stroke = gridBrush,
                StrokeThickness = i == 0 || i == _puzzle.Size ? 2 : 1
            });
        }
    }

    private void DrawSolutionPaths(double cell)
    {
        foreach (var pair in _puzzle.Pairs)
        {
            var path = _puzzle.SolutionPaths[pair.Id];
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
            _canvas.Children.Add(new Path
            {
                Data = geometry,
                Stroke = GetBrush(pair.ColorHex),
                StrokeThickness = Math.Max(10, cell * 0.28),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Opacity = 0.88
            });
        }
    }

    private void DrawEndpoints(double cell)
    {
        foreach (var pair in _puzzle.Pairs)
        {
            DrawEndpoint(pair, pair.Start, cell);
            DrawEndpoint(pair, pair.End, cell);
        }
    }

    private void DrawEndpoint(FlowPair pair, CellPosition position, double cell)
    {
        var center = GetCenter(position, cell);
        var diameter = Math.Max(22, cell * 0.58);
        var endpoint = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = GetBrush(pair.ColorHex),
            Stroke = Brushes.White,
            StrokeThickness = 2
        };

        Canvas.SetLeft(endpoint, center.X - diameter / 2);
        Canvas.SetTop(endpoint, center.Y - diameter / 2);
        _canvas.Children.Add(endpoint);
    }

    private static Point GetCenter(CellPosition position, double cell)
    {
        return new Point((position.Column + 0.5) * cell, (position.Row + 0.5) * cell);
    }

    private static SolidColorBrush GetBrush(string colorHex)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex)!;
    }
}
