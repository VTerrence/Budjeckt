namespace Budjeckt;

/// <summary>
/// Mois d'une année Budjeckt (lv1) : il contient les catégories de dépense, le budget
/// du mois (revenue), les factures enregistrées et les montants calculés (total, reste).
/// </summary>
public class MonthBudget
{
    private string _nom;
    private float _revenue;
    private Tuple<int, string, float>[] _expenseCategories;
    private readonly List<Facture> _factures;
    private float _totalExpenses;
    private float _budgetRemaining;

    /// <summary>Nom du mois (ex. « Janvier »).</summary>
    public string Nom => _nom;

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
    /// un budget à 0 et aucune facture.
    /// </summary>
    /// <param name="nom">Nom du mois.</param>
    /// <exception cref="ArgumentException">Si le nom du mois est vide.</exception>
    public MonthBudget(string nom)
        : this(nom, 0f, CreerCategoriesParDefaut(), Enumerable.Empty<Facture>())
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
    /// <exception cref="ArgumentException">Si le nom du mois est vide, si le revenue n'est pas
    /// un nombre fini, si une facture référence une catégorie inconnue ou n'appartient pas au mois.</exception>
    /// <exception cref="InvalidDataException">Si la somme des factures d'une catégorie ou du mois
    /// déborde de la plage flottante.</exception>
    public MonthBudget(string nom, float revenue, Tuple<int, string, float>[] categories, IEnumerable<Facture> factures)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(factures);
        Validation.VerifierNomNonVide(nom);
        Validation.VerifierRevenueFini(revenue);
        _nom = nom;
        _revenue = revenue;
        _expenseCategories = categories.ToArray();
        _factures = new List<Facture>(factures);

        // Une facture référençant une catégorie inconnue serait silencieusement exclue du recalcul
        // des dépenses par catégorie (filtrée par IdCategorie), donc de la dépense totale.
        // Une facture datée d'un autre mois fausserait le tri et l'affichage du mois.
        foreach (Facture facture in _factures)
        {
            Validation.VerifierCategorieExiste(facture.IdCategorie, _expenseCategories.Select(categorie => categorie.Item1));
            Validation.VerifierDateDansLeMois(facture.Date, _nom);
        }

        RecalculerTotaux();
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
    /// Résout la date effective d'une facture : la date fournie doit appartenir au mois affiché ;
    /// sans date, retourne aujourd'hui si aujourd'hui appartient au mois, sinon le 1er jour du mois.
    /// </summary>
    /// <param name="date">Date fournie, éventuellement absente.</param>
    /// <returns>Date effective, normalisée sans composante horaire.</returns>
    /// <exception cref="ArgumentException">Si la date fournie n'appartient pas au mois affiché ou est implausible.</exception>
    private DateTime ResoudreDate(DateTime? date)
    {
        if (date is { } valeur)
        {
            Validation.VerifierDatePlausible(valeur);
            Validation.VerifierDateDansLeMois(valeur, _nom);
            return valeur.Date;
        }

        // Sans date : aujourd'hui si aujourd'hui appartient au mois affiché, sinon le 1er du mois.
        // Importer une dépense dans un mois passé/futur avec la date du jour serait trompeur.
        DateTime aujourdhui = DateTime.Today;
        int? indexMois = Validation.IndexDuMois(_nom);
        if (indexMois is null)
        {
            throw new ArgumentException($"Le mois \"{_nom}\" est inconnu : impossible d'attribuer une date par défaut.", nameof(_nom));
        }

        return indexMois == aujourdhui.Month
            ? aujourdhui
            : new DateTime(aujourdhui.Year, indexMois.Value, 1);
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
    /// Calcule et modifie le reste du budget (revenue - total des dépenses).
    /// </summary>
    private void RecalculerReste()
    {
        _budgetRemaining = _revenue - _totalExpenses;
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
    /// </summary>
    private static Tuple<int, string, float>[] CreerCategoriesParDefaut()
    {
        string[] nomsParDefaut = { "Loyer", "Eau", "Electricite", "Chauffage", "Alimentation", "Transports", "Loisirs" };
        return nomsParDefaut
            .Select((nom, index) => new Tuple<int, string, float>(index + 1, nom, 0f))
            .ToArray();
    }
}