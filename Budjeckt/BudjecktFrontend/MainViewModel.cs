using Budjeckt;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using Bud = global::Budjeckt.Budjeckt;

namespace BudjecktFrontend;

/// <summary>
/// Vue principale de l'application : gère le dossier de données multi-années (un fichier
/// JSON par année, migration de l'ancien fichier unique), navigue entre les années et les
/// 12 mois, gère l'ajout/suppression de factures, le budget mensuel et le filtrage par
/// catégorie. Toute mutation est immédiatement sauvegardée.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // NOTE : CommunityToolkit.Mvvm ne copie pas les commentaires XML des champs
    // [ObservableProperty] ni des méthodes [RelayCommand] vers les membres publics
    // générés (propriétés et commandes). La documentation source ci-dessous est
    // exhaustive pour la lecture du code, mais l'XML doc de l'assembly ne contiendra
    // pas ces résumés sur les membres générés.

    private Bud _budjeckt = new();
    private string _cheminFichier = string.Empty;
    private readonly string _repertoireDonnees;
    private readonly Func<List<int>, bool>? _confirmerSuppression;
    private Tuple<int, string, float>[] _categoriesMois = Array.Empty<Tuple<int, string, float>>();

    /// <summary>
    /// Message d'une action métier ou de sauvegarde en cours d'affichage : conservé jusqu'à
    /// une action réussie, il n'est jamais écrasé par la validation de saisie au clavier.
    /// </summary>
    private string _erreurAction = string.Empty;

    /// <summary>Noms des 12 mois de l'année (ordre de navigation), issus du backend.</summary>
    public ObservableCollection<string> NomsMois { get; } = new(Bud.NomsDesMois);

    /// <summary>Années disponibles du dossier de données, triées de la plus récente à la plus ancienne.</summary>
    public ObservableCollection<int> Annees { get; } = new();

    /// <summary>Cases de la liste multi-sélection du panneau de suppression des années.</summary>
    public ObservableCollection<AnneeSelectionnable> AnneesSuppression { get; } = new();

    /// <summary>Factures affichées dans l'historique (triées de la plus récente à la plus ancienne).</summary>
    public ObservableCollection<ApercuFacture> FacturesAffichees { get; } = new();

    /// <summary>Dépenses cumulées par catégorie du mois affiché (triées par montant décroissant).</summary>
    public ObservableCollection<ApercuCategorie> TotauxParCategorie { get; } = new();

    /// <summary>Catégories du mois affiché, pour le formulaire d'ajout.</summary>
    public ObservableCollection<string> CategoriesAjout { get; } = new();

    /// <summary>Options du filtre : « Toutes les catégories » puis chaque catégorie du mois.</summary>
    public ObservableCollection<string> CategoriesFiltre { get; } = new();

    /// <summary>Index (0-11) du mois affiché.</summary>
    [ObservableProperty]
    private int _indexMoisSelectionne;

    /// <summary>Index de l'année sélectionnée dans <see cref="Annees"/>.</summary>
    [ObservableProperty]
    private int _indexAnneeSelectionnee;

    /// <summary>Borne inférieure du sélecteur de date (1er janvier de l'année affichée).</summary>
    [ObservableProperty]
    private DateTime _dateMin;

    /// <summary>Borne supérieure du sélecteur de date (31 décembre de l'année affichée).</summary>
    [ObservableProperty]
    private DateTime _dateMax;

    /// <summary>Libellé d'année (ex. « Année 2026 »).</summary>
    [ObservableProperty]
    private string _nomAnnee = string.Empty;

    /// <summary><c>true</c> si le panneau de suppression des années est affiché.</summary>
    [ObservableProperty]
    private bool _panneauSuppressionVisible;

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

    /// <summary>Message de succès d'une action récente (ex. ajout de catégorie), en vert.</summary>
    [ObservableProperty]
    private string _messageSucces = string.Empty;

    /// <summary>Budget mensuel saisi avant enregistrement.</summary>
    [ObservableProperty]
    private string _revenueSaisi = string.Empty;

    /// <summary>Nom de la catégorie à ajouter pour toute l'année (formulaire).</summary>
    [ObservableProperty]
    private string _nouvelleCategorieSaisie = string.Empty;

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
    /// Construit la vue : migre l'ancien fichier unique vers le format par année, détecte
    /// les années disponibles, crée l'année courante si elle manque, puis ouvre l'année
    /// la plus récente (ou l'année courante) et sélectionne le mois courant.
    /// </summary>
    /// <param name="confirmerSuppression">
    /// Fonction de confirmation de la suppression d'années (ex. une MessageBox « Oui/Non »).
    /// Si <c>null</c>, aucune suppression n'est confirmée : la commande de suppression refuse
    /// alors d'agir (échec sûr), ce qui permet de tester le flux sans interface.
    /// </param>
    public MainViewModel(Func<List<int>, bool>? confirmerSuppression = null)
    {
        _confirmerSuppression = confirmerSuppression;
        _repertoireDonnees = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Budjeckt");
        Directory.CreateDirectory(_repertoireDonnees);

        MigrerFichierHeriteSiNecessaire();

        int anneeCourante = ArchivesBudjeckt.AnneeCourante();
        RafraichirAnneesDepuisDisque();

        // Auto-création de l'année courante au lancement (défauts sauvegardés sur disque
        // si elle est absente) pour que la navigation proposer toujours l'année en cours.
        if (!Annees.Contains(anneeCourante))
        {
            try
            {
                ArchivesBudjeckt.OuvrirOuCreerAnnee(_repertoireDonnees, anneeCourante);
            }
            catch (InvalidDataException)
            {
                // Fichier de l'année courante corrompu : il est mis de côté puis régénéré
                // (le contenu était déjà illisible, rien de récupérable n'est perdu).
                MettreDeCoteFichierCorrompu(Path.Combine(_repertoireDonnees, Bud.NomFichierPourAnnee(anneeCourante)));
                ArchivesBudjeckt.OuvrirOuCreerAnnee(_repertoireDonnees, anneeCourante);
            }

            AjouterAnneeCreee(anneeCourante);
        }

        int anneeCible = Annees.Contains(anneeCourante) ? anneeCourante : Annees[0];
        IndexAnneeSelectionnee = Annees.IndexOf(anneeCible);
        // Appel explicite si la cible est déjà l'index par défaut (0) : le hook de
        // changement d'année ne serait sinon pas déclenché au démarrage.
        if (IndexAnneeSelectionnee == 0)
        {
            ChangerAnnee(anneeCible);
        }
    }

    /// <summary>
    /// Migre l'ancien fichier <c>depenses.json</c> vers <c>depenses-&lt;année&gt;.json</c>.
    /// Un fichier hérité illisible est mis de côté (renommé) au lieu d'être ignoré en silence.
    /// </summary>
    private void MigrerFichierHeriteSiNecessaire()
    {
        string heritage = Path.Combine(_repertoireDonnees, Bud.NomFichierHerite);
        if (!File.Exists(heritage))
        {
            return;
        }

        if (ArchivesBudjeckt.MigrerFichierHerite(_repertoireDonnees))
        {
            return;
        }

        MettreDeCoteFichierCorrompu(heritage);
        _erreurAction = "Un ancien fichier de données était illisible : il a été mis de côté ";
        MessageErreur = _erreurAction;
    }

    /// <summary>
    /// Ouvre l'année demandée : chargement du fichier existant ou création d'une année
    /// vierge, puis rafraîchit l'état du mois affiché. En cas de fichier corrompu, il est
    /// mis de côté et l'année est réinitialisée (l'application démarre quand même).
    /// </summary>
    private void ChangerAnnee(int annee)
    {
        string nouveauChemin = Path.Combine(_repertoireDonnees, Bud.NomFichierPourAnnee(annee));

        // Garde anti double-chargement : re-sélectionner l'année déjà affichée ne recharge pas
        // le fichier. Le chemin (vide au départ) garantit qu'une année n'est jamais sautée au
        // premier chargement, même si le modèle par défaut porte déjà le même nom d'année.
        if (nouveauChemin == _cheminFichier && _budjeckt.Annee == annee.ToString())
        {
            return;
        }

        _cheminFichier = nouveauChemin;

        try
        {
            _budjeckt = ArchivesBudjeckt.OuvrirOuCreerAnnee(_repertoireDonnees, annee);
        }
        catch (InvalidDataException)
        {
            MettreDeCoteFichierCorrompu(_cheminFichier);
            _budjeckt = ArchivesBudjeckt.OuvrirOuCreerAnnee(_repertoireDonnees, annee);
            _erreurAction = $"Le fichier de l'année {annee} était illisible : il a été mis de côté et réinitialisé.";
            MessageErreur = _erreurAction;
        }
        catch (IOException)
        {
            AfficherErreurAction("Impossible d'ouvrir le dossier de données. Vérifiez les autorisations.");
            return;
        }
        catch (UnauthorizedAccessException)
        {
            AfficherErreurAction("Impossible d'ouvrir le dossier de données. Vérifiez les autorisations.");
            return;
        }

        NomAnnee = $"Année {_budjeckt.Annee}";
        IndexMoisSelectionne = DateTime.Today.Month - 1;
        // Appel explicite : si le mois courant est déjà l'index sélectionné (donc inchangé),
        // le hook de changement de mois n'est pas déclenché et la liste resterait vide.
        ActualiserEtatsMois();
    }

    /// <summary>
    /// Ajoute une année dans la liste affichée en la gardant triée par ordre décroissant.
    /// </summary>
    private void AjouterAnneeCreee(int annee)
    {
        if (!Annees.Contains(annee))
        {
            Annees.Add(annee);
        }

        List<int> trie = Annees.OrderByDescending(a => a).ToList();
        Annees.Clear();
        foreach (int a in trie)
        {
            Annees.Add(a);
        }
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

    /// <summary>
    /// Crée (si besoin) l'année suivant l'année affichée puis bascule dessus : permet de
    /// préparer l'année suivante avant son arrivée, sans attendre le premier lancement
    /// de l'année concernée.
    /// </summary>
    [RelayCommand]
    private void CreerAnneeSuivante()
    {
        if (IndexAnneeSelectionnee < 0 || IndexAnneeSelectionnee >= Annees.Count)
        {
            return;
        }

        int anneeSuivante = Annees[IndexAnneeSelectionnee] + 1;
        if (!Annees.Contains(anneeSuivante))
        {
            try
            {
                ArchivesBudjeckt.OuvrirOuCreerAnnee(_repertoireDonnees, anneeSuivante);
            }
            catch (InvalidDataException)
            {
                return;
            }
            catch (IOException)
            {
                AfficherErreurAction("Impossible de sauvegarder la nouvelle année. Vérifiez les autorisations du dossier.");
                return;
            }

            AjouterAnneeCreee(anneeSuivante);
        }

        IndexAnneeSelectionnee = Annees.IndexOf(anneeSuivante);
    }

    /// <summary>
    /// Ouvre le panneau de multi-sélection des années à supprimer : chaque année disponible
    /// apparaît avec une case à cocher, sans aucune modification tant que rien n'est confirmé.
    /// </summary>
    [RelayCommand]
    private void OuvrirPanneauSuppression()
    {
        AnneesSuppression.Clear();
        foreach (int annee in Annees)
        {
            AnneesSuppression.Add(new AnneeSelectionnable(annee));
        }

        PanneauSuppressionVisible = true;
    }

    /// <summary>Ferme le panneau de suppression sans toucher aux fichiers.</summary>
    [RelayCommand]
    private void AnnulerSuppression()
    {
        AnneesSuppression.Clear();
        PanneauSuppressionVisible = false;
    }

    /// <summary>
    /// Supprime définitivement les années cochées (toutes, y compris l'année courante), après
    /// confirmation. Si l'année courante est supprimée, elle est recréée avec les valeurs par
    /// défaut (il existe donc toujours au moins une année). L'affichage est repositionné sur
    /// l'année qui survit ou, à défaut, sur la plus récente restante.
    /// </summary>
    [RelayCommand]
    private void SupprimerAnnees()
    {
        List<int> choisies = AnneesSuppression
            .Where(item => item.EstCochee)
            .Select(item => item.Annee)
            .OrderByDescending(annee => annee)
            .ToList();
        if (choisies.Count == 0)
        {
            MessageBox.Show("Sélectionnez au moins une année à supprimer.", "Budjeckt",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!ConfirmerSuppression(choisies))
        {
            return;
        }

        int anneeCourante = ArchivesBudjeckt.AnneeCourante();
        int anneeAffichee = int.TryParse(_budjeckt.Annee.Trim(), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int entiere)
            ? entiere
            : -1;
        bool affichageSupprime = anneeAffichee >= 0 && choisies.Contains(anneeAffichee);

        HashSet<int> echecs = new();
        foreach (int annee in choisies)
        {
            if (!ArchivesBudjeckt.SupprimerAnnee(_repertoireDonnees, annee))
            {
                echecs.Add(annee);
            }
        }

        if (choisies.Contains(anneeCourante))
        {
            try
            {
                // Fichier supprimé → recréé par défaut ; fichier encore présent (échec de
                // suppression) → simplement rechargé tel quel. Idempotent dans les deux cas.
                ArchivesBudjeckt.OuvrirOuCreerAnnee(_repertoireDonnees, anneeCourante);
            }
            catch (InvalidDataException)
            {
                echecs.Add(anneeCourante);
            }
            catch (IOException)
            {
                echecs.Add(anneeCourante);
            }
            catch (UnauthorizedAccessException)
            {
                echecs.Add(anneeCourante);
            }
        }

        if (echecs.Count == choisies.Count)
        {
            // Aucune suppression n'a abouti : le panneau reste ouvert pour réessayer.
            AfficherErreurAction($"Aucune année supprimée ({string.Join(", ", echecs)}). Fichier verrouillé ou permissions insuffisantes.");
            return;
        }

        RafraichirAnneesDepuisDisque();
        if (Annees.Count == 0)
        {
            // La liste est vide (la recréation de l'année courante n'a pas été vue) : on
            // retente une fois avant d'abandonner.
            try
            {
                ArchivesBudjeckt.OuvrirOuCreerAnnee(_repertoireDonnees, anneeCourante);
            }
            catch (InvalidDataException)
            {
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            RafraichirAnneesDepuisDisque();
        }

        if (Annees.Count == 0)
        {
            // Reconstitution impossible : on détache l'état en mémoire pour empêcher toute
            // réécriture fantôme d'une année supprimée lors d'une prochaine sauvegarde.
            _cheminFichier = string.Empty;
            _budjeckt = new();
            PanneauSuppressionVisible = false;
            AnneesSuppression.Clear();
            AfficherErreurAction("Impossible de reconstituer une année de données. Vérifiez les permissions du dossier.");
            return;
        }

        PanneauSuppressionVisible = false;
        AnneesSuppression.Clear();

        int cible = Annees.Contains(anneeAffichee) ? anneeAffichee : Annees[0];
        if (affichageSupprime)
        {
            // Le fichier de l'année affichée n'existe plus (ou vient d'être recréé) : on force
            // le rechargement pour ne pas garder l'ancien modèle en mémoire (contourne la garde
            // anti double-chargement de ChangerAnnee).
            _cheminFichier = string.Empty;
        }

        int indexCible = Annees.IndexOf(cible);
        if (IndexAnneeSelectionnee != indexCible)
        {
            IndexAnneeSelectionnee = indexCible;
        }
        else
        {
            ChangerAnnee(cible);
        }

        if (echecs.Count > 0)
        {
            string liste = string.Join(", ", echecs);
            AfficherErreurAction($"Suppression partielle : impossible de supprimer {echecs.Count} année(s) ({liste}). Fichier verrouillé ou permissions insuffisantes.");
        }
        else
        {
            _erreurAction = string.Empty;
            MessageErreur = $"{choisies.Count} année(s) supprimée(s).";
        }
    }

    /// <summary>
    /// Demande confirmation avant une suppression définitive et liste les années concernées.
    /// Sans fonction de confirmation injectée (mode sans interface pour les tests), la
    /// suppression est refusée : comportement « échec sûr ».
    /// </summary>
    /// <param name="annees">Années cochées (triées décroissant).</param>
    /// <returns><c>true</c> si l'utilisateur confirme, sinon <c>false</c>.</returns>
    private bool ConfirmerSuppression(List<int> annees)
    {
        return _confirmerSuppression?.Invoke(annees) ?? false;
    }

    /// <summary>
    /// Reconstruit la liste des années depuis le disque (la vérité du disque devient
    /// celle de l'UI, même en cas d'échec partiel de suppression). Un échec d'accès au
    /// dossier laisse la liste vide à la place de remonter une exception.
    /// </summary>
    private void RafraichirAnneesDepuisDisque()
    {
        Annees.Clear();
        try
        {
            foreach (int annee in ArchivesBudjeckt.AnneesExistantes(_repertoireDonnees))
            {
                Annees.Add(annee);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>Répercute un changement d'année sur le chargement et l'affichage.</summary>
    partial void OnIndexAnneeSelectionneeChanged(int value)
    {
        if (value >= 0 && value < Annees.Count)
        {
            ChangerAnnee(Annees[value]);
        }
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

    private bool PeutAjouterCategorie()
    {
        return !string.IsNullOrWhiteSpace(NouvelleCategorieSaisie);
    }

    /// <summary>
    /// Ajoute une catégorie de dépense aux 12 mois de l'année affichée puis sauvegarde.
    /// Un mois qui possède déjà le nom (comparaison insensible à la casse) est ignoré :
    /// l'opération est idempotente même si les mois ont des jeux de catégories différents.
    /// La vérification d'unicité est faite sur les 12 mois avant la première mutation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(PeutAjouterCategorie))]
    private void AjouterCategorie()
    {
        string nom = NouvelleCategorieSaisie.Trim();

        // Pré-validation sur les 12 mois avant toute mutation : une exception en cours de
        // boucle ne laisserait sinon qu'une partie de l'année modifiée en mémoire, jamais
        // sauvegardée et susceptible d'être persistée par la prochaine action.
        bool[] moisAAjouter = _budjeckt.Months
            .Select(mois => mois.ExpenseCategories.Any(
                categorie => string.Equals(categorie.Item2, nom, StringComparison.OrdinalIgnoreCase))
                == false)
            .ToArray();

        try
        {
            int nbModifies = 0;
            for (int i = 0; i < _budjeckt.Months.Length; i++)
            {
                if (moisAAjouter[i])
                {
                    _budjeckt.Months[i].AjouterCategorie(nom);
                    nbModifies++;
                }
            }

            if (Sauvegarder())
            {
                MessageSucces = nbModifies == 0
                    ? $"La catégorie « {nom} » existe déjà pour tous les mois de l'année."
                    : $"Catégorie « {nom} » ajoutée pour toute l'année.";
            }

            NouvelleCategorieSaisie = string.Empty;
            ActualiserListesCategories(false);
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
        ActualiserListesCategories(true);

        CategoriesDisponibles = _categoriesMois.Length > 0;

        DateSaisie = DateParDefaut();
        DateMin = new DateTime(mois.Annee, 1, 1);
        DateMax = new DateTime(mois.Annee, 12, 31);
        RevenueSaisi = mois.Revenue.ToString(CultureInfo.CurrentCulture);
        ActualiserSynthese();
        ActualiserListe();
    }

    /// <summary>
    /// Reconstruit les listes de catégories du formulaire et du filtre depuis le mois affiché.
    /// Les indices de sélection sont soit réinitialisés (changement de mois), soit conservés
    /// si la sélection existe encore (ex. après l'ajout d'une catégorie).
    /// </summary>
    /// <param name="reinitialiserIndices"><c>true</c> pour repartir sur le premier élément.</param>
    private void ActualiserListesCategories(bool reinitialiserIndices)
    {
        _categoriesMois = MoisCourant.ExpenseCategories;

        CategoriesAjout.Clear();
        foreach (Tuple<int, string, float> categorie in _categoriesMois)
        {
            CategoriesAjout.Add(categorie.Item2);
        }

        CategoriesFiltre.Clear();
        CategoriesFiltre.Add("Toutes les catégories");
        foreach (Tuple<int, string, float> categorie in _categoriesMois)
        {
            CategoriesFiltre.Add(categorie.Item2);
        }

        if (reinitialiserIndices)
        {
            IndexCategorieAjout = 0;
            IndexCategorieFiltre = 0;
        }
        else
        {
            if (IndexCategorieAjout >= CategoriesAjout.Count)
            {
                IndexCategorieAjout = CategoriesAjout.Count - 1;
            }

            if (IndexCategorieFiltre >= CategoriesFiltre.Count)
            {
                IndexCategorieFiltre = CategoriesFiltre.Count - 1;
            }
        }
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

        TotauxParCategorie.Clear();
        foreach (Tuple<int, string, float> categorie in mois.ExpenseCategories
                     .OrderByDescending(categorie => categorie.Item3)
                     .ThenBy(categorie => categorie.Item2, StringComparer.CurrentCulture))
        {
            TotauxParCategorie.Add(new ApercuCategorie(categorie.Item2, categorie.Item3));
        }
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

    /// <summary>Répercute une saisie de catégorie sur la disponibilité de l'ajout.</summary>
    partial void OnNouvelleCategorieSaisieChanged(string value)
    {
        AjouterCategorieCommand.NotifyCanExecuteChanged();
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
    /// Indique si la date sélectionnée appartient au mois affiché (mois ET année). Le champ
    /// étant toujours initialisé par <see cref="ActualiserEtatsMois"/>, une valeur absente
    /// est invalide.
    /// </summary>
    private bool DateSaisieEstDansLeMois()
    {
        return DateSaisie is { } jour
               && jour.Month == IndexMoisSelectionne + 1
               && jour.Year == MoisCourant.Annee;
    }

    private static bool EssayerMontant(string texte, out float montant)
    {
        return float.TryParse(texte.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out montant)
               || float.TryParse(texte.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out montant);
    }

    /// <summary>
    /// Date proposée par défaut pour le formulaire : aujourd'hui si elle appartient au mois et
    /// à l'année affichés, sinon le 1er jour du mois dans l'année affichée (cohérent avec la
    /// règle de <see cref="MonthBudget"/>).
    /// </summary>
    private DateTime DateParDefaut()
    {
        int annee = MoisCourant.Annee;
        int mois = IndexMoisSelectionne + 1;
        return mois == DateTime.Today.Month && annee == DateTime.Today.Year
            ? DateTime.Today
            : new DateTime(annee, mois, 1);
    }

    /// <summary>
    /// Sauvegarde l'année dans le fichier JSON ; en cas d'échec, affiche un message
    /// générique (sans exposer de détail technique interne) et retourne <c>false</c>.
    /// </summary>
    /// <returns><c>true</c> si la sauvegarde a abouti, sinon <c>false</c>.</returns>
    private bool Sauvegarder()
    {
        try
        {
            _budjeckt.SauvegarderJson(_cheminFichier);
            return true;
        }
        catch (Exception)
        {
            AfficherErreurAction("Impossible de sauvegarder les dépenses. Vérifiez les autorisations du dossier de l'application.");
            return false;
        }
    }
}