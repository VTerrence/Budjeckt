using System.Windows;

namespace BudjecktFrontend
{
    /// <summary>
    /// Fenêtre principale : injecte la vue modèle comme contexte de données.
    /// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initialise la fenêtre et injecte la vue modèle comme contexte de données, avec la
    /// confirmation MessageBox comme délégué de validation de la suppression d'années.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(ConfirmerSuppression);
    }

    /// <summary>
    /// Demande confirmation avant une suppression définitive d'années (listées dans le message).
    /// </summary>
    /// <param name="annees">Années cochées prêtes à être supprimées.</param>
    /// <returns><c>true</c> si l'utilisateur confirme la suppression.</returns>
    private static bool ConfirmerSuppression(List<int> annees)
    {
        string libelle = annees.Count == 1
            ? $"l'année {annees[0]}"
            : $"les {annees.Count} années {string.Join(", ", annees)}";

        return MessageBox.Show(
                   $"Supprimer définitivement {libelle} ? Cette action est irréversible.",
                   "Confirmer la suppression",
                   MessageBoxButton.YesNo,
                   MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }
}
}