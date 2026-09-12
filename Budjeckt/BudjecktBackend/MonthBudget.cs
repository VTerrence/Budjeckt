namespace Budjeckt;

/// <summary>
/// Mois d'une année Budjeckt (lv1) : il contient les catégories de dépense, le budget
/// du mois (revenue), les factures enregistrées et les montants calculés (total, reste).
/// </summary>
public class MonthBudget
{
    private string _nom;
    private readonly int _annee;
    private float _revenue;
    private Tuple<int, string, float>[] _expenseCategories;
    private readonly List<Facture> _factures;
    private float _totalExpenses;
    private float _budgetRemaining;

    /// <summary>Nom du mois (ex. « Janvier »).</summary>
    public string Nom => _nom;

    /// <summary>Année du mois (ex. 2026). Définit les dates qui appartiennent au mois.</summary>
    public int Annee => _annee;

    /// <summary>Budget du mois (revenue).</summary>
    public float Revenue => _revenue;

    /// <summary>
    /// Catégories de dépense sous forme de triplet (id auto-incrémenté, nom, dépense faite).
    /// La dépense faite est recalculée à partir des factures (source unique de vérité).
    /// Le getter renvoie une copie : les mutations externes du tableau retourné n'affectent pas le mois.
    /// </summary>
    public Tuple<int, string, float>[] ExpenseCategories => _expenseCategories.ToArray();

    /// <summary>Factures (dépenses individuelles) du mois, en lecture seule.</summary>
    public IReadOnlyList<Facture> Factures => _factures.AsReadOnly();

    /// <summary>Total des dépenses du mois.</summary>
    public float TotalExpenses => _totalExpenses;

    /// <summary>Reste du budget (revenue - total des dépenses), potentiellement négatif si le budget est dépassé.</summary>
    public float BudgetRemaining => _budgetRemaining;

    /// <summary>
    /// Construit un mois avec des valeurs par défaut : les 7 catégories de base
    /// (Loyer, Eau, Electricite, Chauffage, Alimentation, Transports, Loisirs) à dépense 0,
    /// un budget à 0, aucune facture et l'année courante (horloge système).
    /// </summary>
    /// <param name="nom">Nom du mois.</param>
    /// <exception cref="ArgumentException">Si le nom du mois est vide.</exception>
    public MonthBudget(string nom)
        : this(nom, 0f, CreerCategoriesParDefaut(), Enumerable.Empty<Facture>(), DateTime.Today.Year)
    {
    }

    /// <summary>
    /// Construit un mois en remplissant tous les attributs d'un coup (notamment pour le chargement JSON).
    /// Le total, le reste et la dépense faite par catégorie sont recalculés depuis les factures.
    /// </summary>
    /// <param name="nom">Nom du mois.</param>
    /// <param name="revenue">Budget du mois.</param>
    /// <param name="categories">Catégories de dépense (la dépense faite fournie est ignorée et recalculée).</param>
    /// <param name="factures">Factures du mois, chacune référençant une catégorie existante.</param>
    /// <param name="annee">Année du mois (ex. 2026) ; l'année courante si absente.</param>
    /// <exception cref="ArgumentException">Si le nom du mois est vide, si le revenue n'est pas
    /// un nombre fini, si une facture référence une catégorie inconnue ou n'appartient pas
    /// au mois et à l'année du mois.</exception>
    /// <exception cref="InvalidDataException">Si la somme des factures d'une catégorie ou du mois
    /// déborde de la plage flottante.</exception>
    public MonthBudget(string nom, float revenue, Tuple<int, string, float>[] categories, IEnumerable<Facture> factures, int? annee = null)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(factures);
        Validation.VerifierNomNonVide(nom);
        Validation.VerifierRevenueFini(revenue);
        _nom = nom;
        _annee = annee ?? DateTime.Today.Year;
        _revenue = revenue;
        _expenseCategories = categories.ToArray();
        _factures = new List<Facture>(factures);

        // Une facture référençant une catégorie inconnue serait silencieusement exclue du recalcul
        // des dépenses par catégorie (filtrée par IdCategorie), donc de la dépense totale.
        // Une facture datée d'un autre mois ou d'une autre année fausserait le tri et l'affichage du mois.
        foreach (Facture facture in _factures)
        {
            Validation.VerifierCategorieExiste(facture.IdCategorie, _expenseCategories.Select(categorie => categorie.Item1));
            Validation.VerifierDateDansLeMois(facture.Date, _nom, _annee);
        }

        RecalculerTotaux();
    }

    /// <summary>
    /// Modifie le budget (revenue) du mois puis recalcule le reste. Le revenue peut être
    /// négatif (budget en déficit) mais doit rester un nombre fini.
    /// </summary>
    /// <param name="revenue">Nouveau budget du mois.</param>
    /// <exception cref="ArgumentException">Si le revenue est NaN ou infini.</exception>
    /// <exception cref="InvalidDataException">Si la différence revenue - dépenses déborde de la plage flottante.</exception>
    public void ChangerRevenue(float revenue)
    {
        Validation.VerifierRevenueFini(revenue);

        // Le nouveau reste est validé AVANT toute mutation : un débordement refusé ne doit
        // laisser ni revenue ni reste dans un état partiellement modifié (transactionnalité).
        if (float.IsInfinity(revenue - _totalExpenses))
        {
            throw new InvalidDataException("Le reste du budget déborde de la plage flottante.");
        }

        _revenue = revenue;
        RecalculerReste();
    }

    /// <summary>
    /// Ajoute une catégorie de dépense après vérification : nom non vide et unique.
    /// La dépense faite est mise à 0 et l'id vaut l'id précédent + 1.
    /// </summary>
    /// <param name="nom">Nom de la catégorie.</param>
    /// <exception cref="ArgumentException">Si le nom est vide ou déjà utilisé.</exception>
    public void AjouterCategorie(string nom)
    {
        Validation.VerifierNomNonVide(nom);
        Validation.VerifierNomUnique(nom, _expenseCategories.Select(categorie => categorie.Item2));

        int prochainId = _expenseCategories.Length == 0
            ? 1
            : _expenseCategories.Max(categorie => categorie.Item1) + 1;

        var nouvelleCategorie = new Tuple<int, string, float>(prochainId, nom, 0f);
        var tableau = new Tuple<int, string, float>[_expenseCategories.Length + 1];
        Array.Copy(_expenseCategories, tableau, _expenseCategories.Length);
        tableau[^1] = nouvelleCategorie;

        _expenseCategories = tableau;
        RecalculerTotaux();
    }

    /// <summary>
    /// Supprime définitivement une catégorie de dépense (par nom, comparaison insensible
    /// à la casse) ainsi que toutes les factures qui la référencent, puis recalcule
    /// les dépenses par catégorie, le total et le reste.
    /// Un mois doit toujours garder au moins une catégorie : la dernière ne peut pas être
    /// supprimée, sinon le chargement du fichier JSON rejetterait l'année entière.
    /// </summary>
    /// <param name="nom">Nom de la catégorie à supprimer.</param>
    /// <returns>Nombre de factures supprimées avec la catégorie.</returns>
    /// <exception cref="ArgumentException">Si le nom est vide, si la catégorie n'existe
    /// pas dans le mois ou si elle est la seule du mois. Aucune mutation n'est effectuée
    /// dans ce cas.</exception>
    public int SupprimerCategorie(string nom)
    {
        Validation.VerifierNomNonVide(nom);

        Tuple<int, string, float>? categorie = _expenseCategories
            .FirstOrDefault(categorie => string.Equals(categorie.Item2, nom, StringComparison.OrdinalIgnoreCase));

        if (categorie is null)
        {
            throw new ArgumentException($"La catégorie \"{nom}\" n'existe pas dans le mois.", nameof(nom));
        }

        if (_expenseCategories.Length == 1)
        {
            throw new ArgumentException(
                $"La catégorie \"{nom}\" est la seule du mois : la supprimer laisserait le mois sans catégorie.", nameof(nom));
        }

        int idCategorie = categorie.Item1;

        // La suppression de la catégorie emporte ses factures : sans cela, des factures
        // orphelines référenceraient une catégorie inconnue et seraient rejetées au prochain chargement.
        int nbFacturesSupprimees = _factures.RemoveAll(facture => facture.IdCategorie == idCategorie);

        _expenseCategories = _expenseCategories
            .Where(categorie => categorie.Item1 != idCategorie)
            .ToArray();

        RecalculerTotaux();

        return nbFacturesSupprimees;
    }

    /// <summary>
    /// Ajoute une facture à une catégorie de dépense (par id) sans préciser la date :
    /// la date par défaut est aujourd'hui si aujourd'hui appartient au mois, sinon le 1er jour du mois.
    /// </summary>
    /// <param name="idCategorie">Identifiant de la catégorie.</param>
    /// <param name="montant">Montant de la facture, strictement positif.</param>
    /// <exception cref="ArgumentException">Si la catégorie n'existe pas, si le montant n'est pas
    /// strictement positif, si l'heure est invalide, si la date n'appartient pas au mois ou est
    /// implausible (1900-2100), ou si le nom du mois est inconnu.</exception>
    /// <exception cref="InvalidDataException">Si la somme des factures déborde de la plage flottante.</exception>
    public void AjouterFacture(int idCategorie, float montant)
    {
        AjouterFacture(idCategorie, montant, null, null);
    }

    /// <summary>
    /// Ajoute une facture à une catégorie de dépense (par id) en précisant sa date. La date
    /// doit appartenir au mois affiché (ex. « Janvier »). L'heure reste optionnelle (absente).
    /// L'id de la facture vaut l'id maximum existant + 1 (1 si aucune facture) : les ids
    /// supprimés ou absents après chargement JSON ne sont jamais réutilisés.
    /// </summary>
    /// <param name="idCategorie">Identifiant de la catégorie.</param>
    /// <param name="montant">Montant de la facture, strictement positif.</param>
    /// <param name="date">Date de la facture (doit appartenir au mois).</param>
    /// <exception cref="ArgumentException">Si la catégorie n'existe pas, si le montant n'est pas
    /// strictement positif, ou si la date n'appartient pas au mois ou est implausible (1900-2100).</exception>
    /// <exception cref="InvalidDataException">Si la somme des factures déborde de la plage flottante.</exception>
    public void AjouterFacture(int idCategorie, float montant, DateTime date)
    {
        AjouterFacture(idCategorie, montant, date, null);
    }

    /// <summary>
    /// Ajoute une facture à une catégorie de dépense (par id) avec date et heure optionnelles.
    /// La date par défaut est aujourd'hui (si elle appartient au mois) ou le 1er du mois ; une date
    /// fournie doit appartenir au mois affiché. L'id vaut l'id maximum existant + 1 (1 si aucune
    /// facture) : les ids supprimés ou absents après chargement JSON ne sont jamais réutilisés.
    /// </summary>
    /// <param name="idCategorie">Identifiant de la catégorie.</param>
    /// <param name="montant">Montant de la facture, strictement positif.</param>
    /// <param name="date">Date de la facture, optionnelle.</param>
    /// <param name="heure">Heure de la facture, optionnelle.</param>
    /// <exception cref="ArgumentException">Si la catégorie n'existe pas, si le montant n'est pas
    /// strictement positif, si l'heure est invalide, si la date n'appartient pas au mois ou est
    /// implausible (1900-2100), ou si le nom du mois est inconnu.</exception>
    /// <exception cref="InvalidDataException">Si la somme des factures déborde de la plage flottante.</exception>
    public void AjouterFacture(int idCategorie, float montant, DateTime? date, TimeSpan? heure)
    {
        Validation.VerifierMontantPositif(montant);
        Validation.VerifierCategorieExiste(idCategorie, _expenseCategories.Select(categorie => categorie.Item1));
        Validation.VerifierHeureValide(heure);

        DateTime dateResolue = ResoudreDate(date);

        int prochainId = _factures.Count == 0
            ? 1
            : _factures.Max(facture => facture.Id) + 1;

        _factures.Add(new Facture(prochainId, idCategorie, montant, dateResolue, heure));
        RecalculerTotaux();
    }

    /// <summary>
    /// Résout la date effective d'une facture : la date fournie doit appartenir au mois et
    /// à l'année affichés ; sans date, retourne aujourd'hui si aujourd'hui appartient au mois
    /// et à l'année affichés, sinon le 1er jour du mois dans l'année affichée.
    /// </summary>
    /// <param name="date">Date fournie, éventuellement absente.</param>
    /// <returns>Date effective, normalisée sans composante horaire.</returns>
    /// <exception cref="ArgumentException">Si la date fournie n'appartient pas au mois ou
    /// à l'année affichés ou est implausible.</exception>
    private DateTime ResoudreDate(DateTime? date)
    {
        if (date is { } valeur)
        {
            Validation.VerifierDatePlausible(valeur);
            Validation.VerifierDateDansLeMois(valeur, _nom, _annee);
            return valeur.Date;
        }

        // Sans date : aujourd'hui si aujourd'hui appartient au mois ET à l'année affichés,
        // sinon le 1er du mois dans l'année affichée. Importer une dépense dans un mois passé/futur
        // avec la date du jour serait trompeur, tout comme attribuer aujourd'hui à une année différente.
        DateTime aujourdhui = DateTime.Today;
        int? indexMois = Validation.IndexDuMois(_nom);
        if (indexMois is null)
        {
            throw new ArgumentException($"Le mois \"{_nom}\" est inconnu : impossible d'attribuer une date par défaut.", nameof(_nom));
        }

        return indexMois == aujourdhui.Month && _annee == aujourdhui.Year
            ? aujourdhui
            : new DateTime(_annee, indexMois.Value, 1);
    }

    /// <summary>
    /// Supprime définitivement une facture du mois (par id).
    /// </summary>
    /// <param name="id">Identifiant de la facture à supprimer.</param>
    /// <exception cref="ArgumentException">Si aucune facture ne correspond à cet id.</exception>
    public void SupprimerFacture(int id)
    {
        Validation.VerifierFactureExiste(id, _factures.Select(facture => facture.Id));

        Facture aSupprimer = _factures.First(facture => facture.Id == id);
        _factures.Remove(aSupprimer);
        RecalculerTotaux();
    }

    /// <summary>
    /// Supprime définitivement plusieurs factures du mois (par ids). La liste est dédoublonnée
    /// et tous les ids sont vérifiés AVANT toute mutation : si un id est inconnu, aucune facture
    /// n'est supprimée (transactionnalité, même garde que <see cref="SupprimerCategorie"/>).
    /// Un seul recalcul des totaux est effectué pour l'ensemble supprimé.
    /// </summary>
    /// <param name="ids">Identifiants des factures à supprimer.</param>
    /// <returns>Nombre de factures supprimées.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="ids"/> est <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Si la liste est vide ou si un id ne correspond à
    /// aucune facture. Aucune mutation n'est effectuée dans ce cas.</exception>
    public int SupprimerFactures(IReadOnlyCollection<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            throw new ArgumentException("Aucun id de facture à supprimer.", nameof(ids));
        }

        List<int> idsUniques = ids.Distinct().ToList();
        List<int> idsExistants = _factures.Select(facture => facture.Id).ToList();
        foreach (int id in idsUniques)
        {
            Validation.VerifierFactureExiste(id, idsExistants);
        }

        _factures.RemoveAll(facture => idsUniques.Contains(facture.Id));
        RecalculerTotaux();

        return idsUniques.Count;
    }

    /// <summary>
    /// Recalcule et met à jour le montant dépensé de chaque catégorie à partir des factures.
    /// </summary>
    private void MettreAJourDepensesCategories()
    {
        _expenseCategories = _expenseCategories
            .Select(categorie => new Tuple<int, string, float>(
                categorie.Item1,
                categorie.Item2,
                CalculerDepenseCategorie(categorie.Item1)))
            .ToArray();
    }

    /// <summary>
    /// Calcule la dépense d'une catégorie en sommant ses factures, en refusant le débordement
    /// flottant (deux montants proches de float.Max déborderaient silencieusement vers +infini,
    /// corrompant les totaux et bloquant la sauvegarde JSON).
    /// </summary>
    /// <param name="idCategorie">Identifiant de la catégorie.</param>
    /// <returns>Dépense cumulée de la catégorie.</returns>
    /// <exception cref="InvalidDataException">Si la somme déborde de la plage float.</exception>
    private float CalculerDepenseCategorie(int idCategorie)
    {
        float depense = _factures
            .Where(facture => facture.IdCategorie == idCategorie)
            .Sum(facture => facture.Montant);

        if (float.IsInfinity(depense))
        {
            throw new InvalidDataException($"Les montants des factures de la catégorie d'id {idCategorie} débordent de la plage flottante.");
        }

        return depense;
    }

    /// <summary>
    /// Calcule et modifie le total des dépenses du mois.
    /// </summary>
    /// <exception cref="InvalidDataException">Si la somme des catégories déborde de la plage float.</exception>
    private void RecalculerTotal()
    {
        _totalExpenses = _expenseCategories.Sum(categorie => categorie.Item3);

        if (float.IsInfinity(_totalExpenses))
        {
            throw new InvalidDataException("Le total des dépenses du mois déborde de la plage flottante.");
        }
    }

    /// <summary>
    /// Calcule et modifie le reste du budget (revenue - total des dépenses), en refusant
    /// le débordement flottant silencieux (même garde que le total et les catégories).
    /// </summary>
    /// <exception cref="InvalidDataException">Si le reste déborde de la plage float.</exception>
    private void RecalculerReste()
    {
        _budgetRemaining = _revenue - _totalExpenses;

        if (float.IsInfinity(_budgetRemaining))
        {
            throw new InvalidDataException("Le reste du budget déborde de la plage flottante.");
        }
    }

    /// <summary>
    /// Enchaîne le recalcul de la dépense par catégorie, du total puis du reste.
    /// </summary>
    private void RecalculerTotaux()
    {
        MettreAJourDepensesCategories();
        RecalculerTotal();
        RecalculerReste();
    }

    /// <summary>
    /// Crée les 7 catégories de dépense par défaut avec leurs ids 1 à 7 et une dépense à 0.
    /// Visible par <see cref="Budjeckt"/> pour construire les mois lors de la création d'une nouvelle année.
    /// </summary>
    internal static Tuple<int, string, float>[] CreerCategoriesParDefaut()
    {
        string[] nomsParDefaut = { "Loyer", "Eau", "Electricite", "Chauffage", "Alimentation", "Transports", "Loisirs" };
        return nomsParDefaut
            .Select((nom, index) => new Tuple<int, string, float>(index + 1, nom, 0f))
            .ToArray();
    }
}