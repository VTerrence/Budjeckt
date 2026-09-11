using Budjeckt;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Bud = global::Budjeckt.Budjeckt;

namespace BudjecktFrontend;

/// <summary>
/// Vue principale de l'application : charge l'année Budjeckt depuis le fichier JSON,
/// navigue entre les 12 mois, gère l'ajout/suppression de factures, le budget mensuel
/// et le filtrage par catégorie. Toute mutation est immédiatement sauvegardée.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // NOTE : CommunityToolkit.Mvvm ne copie pas les commentaires XML des champs
    // [ObservableProperty] ni des méthodes [RelayCommand] vers les membres publics
    // générés (propriétés et commandes). La documentation source ci-dessous est
    // exhaustive pour la lecture du code, mais l'XML doc de l'assembly ne contiendra
    // pas ces résumés sur les membres générés.

    private readonly Bud _budjeckt;
    private readonly string _cheminFichier;
    private Tuple<int, string, float>[] _categoriesMois = Array.Empty<Tuple<int, string, float>>();

    /// <summary>
    /// Message d'une action métier ou de sauvegarde en cours d'affichage : conservé jusqu'à
    /// une action réussie, il n'est jamais écrasé par la validation de saisie au clavier.
    /// </summary>
    private string _erreurAction = string.Empty;

    /// <summary>Noms des 12 mois de l'année (ordre de navigation), issus du backend.</summary>
    public ObservableCollection<string> NomsMois { get; } = new(Bud.NomsDesMois);

    /// <summary>Factures affichées dans l'historique (triées de la plus récente à la plus ancienne).</summary>
    public ObservableCollection<ApercuFacture> FacturesAffichees { get; } = new();

    /// <summary>Catégories du mois affiché, pour le formulaire d'ajout.</summary>
    public ObservableCollection<string> CategoriesAjout { get; } = new();

    /// <summary>Options du filtre : « Toutes les catégories » puis chaque catégorie du mois.</summary>
    public ObservableCollection<string> CategoriesFiltre { get; } = new();

    /// <summary>Index (0-11) du mois affiché.</summary>
    [ObservableProperty]
    private int _indexMoisSelectionne;

    /// <summary>Libellé d'année (ex. « Année 2026 »).</summary>
    [ObservableProperty]
    private string _nomAnnee;

    /// <summary>Montant saisi dans le formulaire d'ajout.</summary>
    [ObservableProperty]
    private string _montantSaisi = string.Empty;

    /// <summary>Index de la catégorie sélectionnée dans le formulaire d'ajout.</summary>
    [ObservableProperty]
    private int _indexCategorieAjout = -1;

    /// <summary>Date sélectionnée pour la nouvelle facture (nulle = date par défaut du mois).</summary>
    [ObservableProperty]
    private DateTime? _dateSaisie;

    /// <summary>Heure saisie au format « HH:mm », vide si aucune heure.</summary>
    [ObservableProperty]
    private string _heureSaisie = string.Empty;

    /// <summary>Message d'erreur ou d'aide affiché sous le formulaire.</summary>
    [ObservableProperty]
    private string _messageErreur = string.Empty;

    /// <summary>Budget mensuel saisi avant enregistrement.</summary>
    [ObservableProperty]
    private string _revenueSaisi = string.Empty;

    /// <summary>Index de l'option de filtre sélectionnée (0 = toutes les catégories).</summary>
    [ObservableProperty]
    private int _indexCategorieFiltre;

    /// <summary>Facture sélectionnée dans l'historique (pour la suppression).</summary>
    [ObservableProperty]
    private ApercuFacture? _factureSelectionnee;

    /// <summary>Total des factures actuellement affichées (après filtre).</summary>
    [ObservableProperty]
    private string _totalAfficheTexte = string.Empty;

    /// <summary>Total des dépenses du mois affiché (sans filtre).</summary>
    [ObservableProperty]
    private string _totalMoisTexte = string.Empty;

    /// <summary>Reste du budget du mois affiché (revenue - total).</summary>
    [ObservableProperty]
    private string _resteTexte = string.Empty;

    /// <summary><c>true</c> si le reste du budget est positif ou nul (vert), sinon <c>false</c> (rouge).</summary>
    [ObservableProperty]
    private bool _restePositif;

    /// <summary><c>true</c> si le mois affiché a au moins une catégorie (active le formulaire d'ajout).</summary>
    [ObservableProperty]
    private bool _categoriesDisponibles;

    /// <summary>
    /// Construit la vue : charge l'année depuis le fichier JSON (défauts si absent,
    /// message si corrompu) puis sélectionne le mois courant.
    /// </summary>
    public MainViewModel()
    {
        _budjeckt = new Bud();
        _cheminFichier = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Budjeckt",
            "depenses.json");

        try
        {
            _budjeckt.ChargerJson(_cheminFichier);
        }
        catch (InvalidDataException)
        {
            // Un fichier illisible peut contenir des données récupérables : il est mis de côté
            // (renommé) pour ne pas être écrasé par la première sauvegarde, puis des valeurs
            // par défaut sont chargées.
            MettreDeCoteFichierCorrompu(_cheminFichier);
            _erreurAction = "Le fichier de données était illisible ou corrompu : il a été mis de côté ";
            MessageErreur = _erreurAction;
        }

        _nomAnnee = $"Année {_budjeckt.Annee}";
        IndexMoisSelectionne = DateTime.Today.Month - 1;
        // Appel explicite : en janvier (index 0 = valeur par défaut du champ), le hook de
        // changement de mois n'est pas déclenché et la liste resterait vide au démarrage.
        ActualiserEtatsMois();
    }

    /// <summary>
    /// Renomme le fichier corrompu avec un horodatage pour le conserver sans que la prochaine
    /// sauvegarde ne l'écrase. Un échec (fichier verrouillé, permissions) est ignoré silencieusement :
    /// l'application démarre quand même, au risque de perdre un fichier déjà illisible.
    /// </summary>
    private static void MettreDeCoteFichierCorrompu(string cheminFichier)
    {
        try
        {
            string cible = $"{cheminFichier}.corrompu-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
            File.Move(cheminFichier, cible);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Affiche le message d'une action échouée (règle métier, sauvegarde) et le marque
    /// comme durable : la prochaine frappe ne l'effacera pas avant une action réussie.
    /// </summary>
    private void AfficherErreurAction(string message)
    {
        _erreurAction = message;
        MessageErreur = message;
    }

    /// <summary>Mois actuellement affiché (getter reliant le tableau des 12 mois).</summary>
    public MonthBudget MoisCourant => _budjeckt.Months[IndexMoisSelectionne];

    /// <summary>
    /// Passe au mois précédent (le navigateur reboucle janvier ↔ décembre).
    /// </summary>
    [RelayCommand]
    private void PrecedentMois()
    {
        IndexMoisSelectionne = (IndexMoisSelectionne - 1 + NomsMois.Count) % NomsMois.Count;
    }

    /// <summary>
    /// Passe au mois suivant (le navigateur reboucle janvier ↔ décembre).
    /// </summary>
    [RelayCommand]
    private void SuivantMois()
    {
        IndexMoisSelectionne = (IndexMoisSelectionne + 1) % NomsMois.Count;
    }

    private bool PeutAjouterFacture()
    {
        if (IndexCategorieAjout < 0 || IndexCategorieAjout >= _categoriesMois.Length)
        {
            return false;
        }

        return EssayerMontant(MontantSaisi, out float montant)
               && float.IsFinite(montant) && montant > 0f
               && HeureSaisieEstValideOuAbsente()
               && DateSaisieEstDansLeMois();
    }

    /// <summary>
    /// Valide et ajoute la facture saisie au mois affiché, puis sauvegarde.
    /// Les erreurs métier (date hors mois, heure hors plage…) s'affichent sans planter.
    /// </summary>
    [RelayCommand(CanExecute = nameof(PeutAjouterFacture))]
    private void AjouterFacture()
    {
        if (!EssayerMontant(MontantSaisi, out float montant) || !float.IsFinite(montant) || montant <= 0f)
        {
            return;
        }

        TimeSpan? heure = null;
        if (!string.IsNullOrWhiteSpace(HeureSaisie) && HeureSaisieEstValideOuAbsente())
        {
            TimeSpan.TryParse(HeureSaisie.Trim(), CultureInfo.CurrentCulture, out TimeSpan valeur);
            heure = valeur;
        }

        try
        {
            int idCategorie = _categoriesMois[IndexCategorieAjout].Item1;
            MoisCourant.AjouterFacture(idCategorie, montant, DateSaisie, heure);
            Sauvegarder();
            MontantSaisi = string.Empty;
            HeureSaisie = string.Empty;
            _erreurAction = string.Empty;
            MessageErreur = string.Empty;
            ActualiserListe();
            ActualiserSynthese();
        }
        catch (ArgumentException exception)
        {
            AfficherErreurAction(exception.Message);
        }
        catch (InvalidDataException exception)
        {
            AfficherErreurAction(exception.Message);
        }
    }

    private bool PeutSupprimerFacture()
    {
        return FactureSelectionnee is not null;
    }

    /// <summary>
    /// Supprime définitivement la facture sélectionnée puis sauvegarde.
    /// </summary>
    [RelayCommand(CanExecute = nameof(PeutSupprimerFacture))]
    private void SupprimerFacture()
    {
        if (FactureSelectionnee is null)
        {
            return;
        }

        try
        {
            MoisCourant.SupprimerFacture(FactureSelectionnee.Source.Id);
            Sauvegarder();
            _erreurAction = string.Empty;
            MessageErreur = string.Empty;
            ActualiserListe();
            ActualiserSynthese();
        }
        catch (ArgumentException exception)
        {
            AfficherErreurAction(exception.Message);
        }
        catch (InvalidDataException exception)
        {
            AfficherErreurAction(exception.Message);
        }
    }

    /// <summary>
    /// Enregistre le budget mensuel saisi (nombre fini) puis sauvegarde.
    /// </summary>
    [RelayCommand]
    private void EnregistrerBudget()
    {
        if (!EssayerMontant(RevenueSaisi, out float revenue))
        {
            MessageErreur = "Budget invalide : saisissez un nombre (ex. 1500).";
            return;
        }

        try
        {
            MoisCourant.ChangerRevenue(revenue);
            Sauvegarder();
            _erreurAction = string.Empty;
            MessageErreur = string.Empty;
            ActualiserSynthese();
        }
        catch (ArgumentException exception)
        {
            AfficherErreurAction(exception.Message);
        }
        catch (InvalidDataException exception)
        {
            AfficherErreurAction(exception.Message);
        }
    }

    /// <summary>
    /// Reconstruit l'état du mois affiché : catégories, filtre, date par défaut, budget
    /// puis liste et synthèse. Appelé à chaque changement de mois.
    /// </summary>
    private void ActualiserEtatsMois()
    {
        MonthBudget mois = MoisCourant;
        _categoriesMois = mois.ExpenseCategories;

        CategoriesAjout.Clear();
        foreach (Tuple<int, string, float> categorie in _categoriesMois)
        {
            CategoriesAjout.Add(categorie.Item2);
        }

        IndexCategorieAjout = 0;

        CategoriesFiltre.Clear();
        CategoriesFiltre.Add("Toutes les catégories");
        foreach (Tuple<int, string, float> categorie in _categoriesMois)
        {
            CategoriesFiltre.Add(categorie.Item2);
        }

        IndexCategorieFiltre = 0;
        CategoriesDisponibles = _categoriesMois.Length > 0;

        DateSaisie = DateParDefaut(IndexMoisSelectionne);
        RevenueSaisi = mois.Revenue.ToString(CultureInfo.CurrentCulture);
        ActualiserSynthese();
        ActualiserListe();
    }

    /// <summary>
    /// Reconstruit la liste des factures affichées (filtre appliqué, tri de la plus
    /// récente à la plus ancienne) et le total affiché.
    /// </summary>
    private void ActualiserListe()
    {
        MonthBudget mois = MoisCourant;
        _categoriesMois = mois.ExpenseCategories;
        Dictionary<int, string> nomsCategories = _categoriesMois.ToDictionary(categorie => categorie.Item1, categorie => categorie.Item2);

        IEnumerable<Facture> source = mois.Factures;
        if (IndexCategorieFiltre > 0 && IndexCategorieFiltre <= _categoriesMois.Length)
        {
            int idCategorie = _categoriesMois[IndexCategorieFiltre - 1].Item1;
            source = source.Where(facture => facture.IdCategorie == idCategorie);
        }

        FacturesAffichees.Clear();
        foreach (Facture facture in source
                     .OrderByDescending(f => f.Date)
                     .ThenByDescending(f => f.Heure)
                     .ThenByDescending(f => f.Id))
        {
            string nomCategorie = nomsCategories.TryGetValue(facture.IdCategorie, out string? nom) ? nom : "?";
            FacturesAffichees.Add(new ApercuFacture(facture, nomCategorie));
        }

        TotalAfficheTexte = Formatage.Montant(FacturesAffichees.Sum(apercu => apercu.Source.Montant));
    }

    /// <summary>
    /// Met à jour les indicateurs du mois : total du mois, reste du budget et sa couleur.
    /// </summary>
    private void ActualiserSynthese()
    {
        MonthBudget mois = MoisCourant;
        TotalMoisTexte = Formatage.Montant(mois.TotalExpenses);
        ResteTexte = Formatage.Montant(mois.BudgetRemaining);
        RestePositif = mois.BudgetRemaining >= 0f;
    }

    /// <summary>
    /// Répercuté à chaque changement de mois : met à jour catégories, filtre et budget.
    /// </summary>
    partial void OnIndexMoisSelectionneChanged(int value)
    {
        if (value >= 0 && value < NomsMois.Count)
        {
            ActualiserEtatsMois();
        }
    }

    /// <summary>Répercute un changement de filtre sur la liste affichée.</summary>
    partial void OnIndexCategorieFiltreChanged(int value)
    {
        ActualiserListe();
    }

    /// <summary>Répercute un changement de sélection sur la disponibilité de la suppression.</summary>
    partial void OnFactureSelectionneeChanged(ApercuFacture? value)
    {
        SupprimerFactureCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Met à jour le message d'erreur pendant la saisie du montant.</summary>
    partial void OnMontantSaisiChanged(string value)
    {
        MettreAJourErreurSaisie();
        AjouterFactureCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Met à jour le message d'erreur pendant la saisie de l'heure.</summary>
    partial void OnHeureSaisieChanged(string value)
    {
        MettreAJourErreurSaisie();
        AjouterFactureCommand.NotifyCanExecuteChanged();
    }

    partial void OnIndexCategorieAjoutChanged(int value)
    {
        AjouterFactureCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Répercute un changement de date sur la disponibilité du bouton d'ajout.</summary>
    partial void OnDateSaisieChanged(DateTime? value)
    {
        MettreAJourErreurSaisie();
        AjouterFactureCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Positionne le message d'erreur selon la validité du montant, de l'heure et de la date saisis.
    /// Une erreur d'action en cours (sauvegarde, règle métier) reste prioritaire : elle n'est
    /// jamais effacée par la saisie, seulement remplacée par une action réussie.
    /// </summary>
    private void MettreAJourErreurSaisie()
    {
        if (!string.IsNullOrEmpty(_erreurAction))
        {
            return;
        }

        bool heureProbleme = !string.IsNullOrWhiteSpace(HeureSaisie) && !HeureSaisieEstValideOuAbsente();

        bool montantProbleme = false;
        if (!string.IsNullOrWhiteSpace(MontantSaisi))
        {
            bool montantValide = EssayerMontant(MontantSaisi, out float montant);
            montantProbleme = !montantValide || !float.IsFinite(montant) || montant <= 0f;
        }

        bool dateProbleme = !DateSaisieEstDansLeMois();

        if (heureProbleme)
        {
            MessageErreur = "Heure invalide : format attendu HH:mm (ex. 14:30), laisser vide si aucune heure.";
        }
        else if (montantProbleme)
        {
            MessageErreur = "Montant invalide : saisissez un nombre strictement positif (ex. 12,50).";
        }
        else if (dateProbleme)
        {
            MessageErreur = "La date doit appartenir au mois affiché.";
        }
        else
        {
            MessageErreur = string.Empty;
        }
    }

    private bool HeureSaisieEstValideOuAbsente()
    {
        if (string.IsNullOrWhiteSpace(HeureSaisie))
        {
            return true;
        }

        return TimeSpan.TryParse(HeureSaisie.Trim(), CultureInfo.CurrentCulture, out TimeSpan valeur)
               && valeur >= TimeSpan.Zero && valeur < TimeSpan.FromDays(1);
    }

    /// <summary>
    /// Indique si la date sélectionnée appartient au mois affiché. Le champ étant toujours
    /// initialisé par <see cref="ActualiserEtatsMois"/>, une valeur absente est invalide.
    /// </summary>
    private bool DateSaisieEstDansLeMois()
    {
        return DateSaisie is { } jour && jour.Month == IndexMoisSelectionne + 1;
    }

    private static bool EssayerMontant(string texte, out float montant)
    {
        return float.TryParse(texte.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out montant)
               || float.TryParse(texte.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out montant);
    }

    /// <summary>
    /// Date proposée par défaut pour le formulaire : aujourd'hui si elle appartient au mois
    /// affiché, sinon le 1er jour du mois (cohérent avec la règle de <see cref="MonthBudget"/>).
    /// </summary>
    private static DateTime DateParDefaut(int indexMois)
    {
        int mois = indexMois + 1;
        return mois == DateTime.Today.Month
            ? DateTime.Today
            : new DateTime(DateTime.Today.Year, mois, 1);
    }

    /// <summary>
    /// Sauvegarde l'année dans le fichier JSON ; en cas d'échec, affiche un message
    /// générique, sans exposer de détail technique interne (AGENTS.md).
    /// </summary>
    private void Sauvegarder()
    {
        try
        {
            _budjeckt.SauvegarderJson(_cheminFichier);
        }
        catch (Exception)
        {
            AfficherErreurAction("Impossible de sauvegarder les dépenses. Vérifiez les autorisations du dossier de l'application.");
        }
    }
}