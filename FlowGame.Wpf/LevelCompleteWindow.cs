using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlowGame.Wpf;

public sealed class LevelCompleteWindow : Window
{
    public LevelCompleteWindow(int level, int bonus, int score, int nextLevel)
    {
        Title = "Nivel concluido";
        Width = 420;
        Height = 280;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(16, 20, 24));
        Foreground = Brushes.White;
        KeyDown += LevelCompleteWindow_KeyDown;

        var panel = new StackPanel
        {
            Margin = new Thickness(28),
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Parabens!",
            FontSize = 30,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 14)
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Voce concluiu o nivel {level}.",
            FontSize = 18,
            Margin = new Thickness(0, 0, 0, 8)
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"+{bonus} pontos | Score: {score}",
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.FromRgb(253, 216, 53)),
            Margin = new Thickness(0, 0, 0, 6)
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Proximo nivel: {nextLevel}",
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromRgb(170, 183, 193)),
            Margin = new Thickness(0, 0, 0, 22)
        });

        var continueButton = new Button
        {
            Content = "Continuar",
            MinWidth = 130,
            MinHeight = 38,
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = new SolidColorBrush(Color.FromRgb(38, 49, 58)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(59, 74, 85)),
            BorderThickness = new Thickness(1)
        };
        continueButton.Click += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
        panel.Children.Add(continueButton);

        Content = panel;
    }

    private void LevelCompleteWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Escape)
        {
            DialogResult = true;
            Close();
        }
    }
}
