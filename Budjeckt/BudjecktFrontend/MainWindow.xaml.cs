using System.Windows;

namespace BudjecktFrontend
{
    /// <summary>
    /// Fenêtre principale : injecte la vue modèle comme contexte de données.
    /// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initialise la fenêtre et injecte la vue modèle comme contexte de données.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
}