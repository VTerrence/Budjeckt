using System.Windows;

namespace BudjecktFrontend
{
    /// <summary>
    /// Fenêtre principale : injecte la vue modèle comme contexte de données.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initialise la fenêtre et injecte la vue modèle comme contexte de données, avec les
        /// MessageBox de confirmation comme délégués des suppressions (années, catégorie).
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(ConfirmerSuppression, ConfirmerSuppressionCategorie, ConfirmerSuppressionFactures);
        }

        /// <summary>
        /// Demande confirmation avant la suppression définitive des dépenses sélectionnées,
        /// en rappelant leur nombre et leur montant total. Demandée systématiquement, même
        /// pour une seule dépense (action irréversible).
        /// </summary>
        /// <param name="nbFactures">Nombre de dépenses sélectionnées.</param>
        /// <param name="total">Montant total des dépenses sélectionnées.</param>
        /// <returns><c>true</c> si l'utilisateur confirme la suppression.</returns>
        private static bool ConfirmerSuppressionFactures(int nbFactures, double total)
        {
            string libelle = nbFactures == 1
                ? "cette dépense"
                : $"ces {nbFactures} dépenses";

            return MessageBox.Show(
                       $"Supprimer {libelle} (total {Formatage.Montant(total)}) ? Cette action est irréversible.",
                       "Confirmer la suppression",
                       MessageBoxButton.YesNo,
                       MessageBoxImage.Warning) == MessageBoxResult.Yes;
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

        /// <summary>
        /// Demande confirmation avant la suppression définitive d'une catégorie, en rappelant
        /// que ses factures sont supprimées avec elle.
        /// </summary>
        /// <param name="nom">Nom de la catégorie à supprimer.</param>
        /// <param name="nbFactures">Nombre de factures associées, supprimées avec la catégorie.</param>
        /// <returns><c>true</c> si l'utilisateur confirme la suppression.</returns>
        private static bool ConfirmerSuppressionCategorie(string nom, int nbFactures)
        {
            string detail = nbFactures switch
            {
                0 => "Aucune facture n'y est associée.",
                1 => "Sa facture sera aussi supprimée.",
                _ => $"Ses {nbFactures} factures seront aussi supprimées."
            };

            return MessageBox.Show(
                       $"Supprimer la catégorie « {nom} » ? {detail} Cette action est irréversible.",
                       "Confirmer la suppression",
                       MessageBoxButton.YesNo,
                       MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }
    }
}