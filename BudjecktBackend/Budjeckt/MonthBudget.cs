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
    /// </summary>
    public Tuple<int, string, float>[] ExpenseCategories => _expenseCategories.ToArray();

    /// <summary>Factures (dépenses individuelles) du mois.</summary>
    public IReadOnlyList<Facture> Factures => _factures;

    /// <summary>Total des dépenses du mois.</summary>
    public float TotalExpenses => _totalExpenses;

    /// <summary>Reste du budget (revenue - total des dépenses).</summary>
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
    /// <param name="factures">Factures du mois.</param>
    /// <exception cref="ArgumentException">Si le nom du mois est vide.</exception>
    public MonthBudget(string nom, float revenue, Tuple<int, string, float>[] categories, IEnumerable<Facture> factures)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(factures);
        Validation.VerifierNomNonVide(nom);
        _nom = nom;
        _revenue = revenue;
        _expenseCategories = categories.ToArray();
        _factures = new List<Facture>(factures);
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
    /// Ajoute une facture datée d'aujourd'hui à une catégorie de dépense (par id).
    /// </summary>
    /// <param name="idCategorie">Identifiant de la catégorie.</param>
    /// <param name="montant">Montant de la facture, strictement positif.</param>
    /// <exception cref="ArgumentException">Si la catégorie n'existe pas ou si le montant n'est pas strictement positif.</exception>
    public void AjouterFacture(int idCategorie, float montant)
    {
        AjouterFacture(idCategorie, montant, DateTime.Today);
    }

    /// <summary>
    /// Ajoute une facture à une catégorie de dépense (par id) en précisant sa date.
    /// </summary>
    /// <param name="idCategorie">Identifiant de la catégorie.</param>
    /// <param name="montant">Montant de la facture, strictement positif.</param>
    /// <param name="date">Date de la facture.</param>
    /// <exception cref="ArgumentException">Si la catégorie n'existe pas ou si le montant n'est pas strictement positif.</exception>
    public void AjouterFacture(int idCategorie, float montant, DateTime date)
    {
        Validation.VerifierMontantPositif(montant);
        Validation.VerifierCategorieExiste(idCategorie, _expenseCategories.Select(categorie => categorie.Item1));

        int prochainId = _factures.Count == 0
            ? 1
            : _factures.Max(facture => facture.Id) + 1;

        _factures.Add(new Facture(prochainId, idCategorie, montant, date));
        RecalculerTotaux();
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
                _factures.Where(facture => facture.IdCategorie == categorie.Item1).Sum(facture => facture.Montant)))
            .ToArray();
    }

    /// <summary>
    /// Calcule et modifie le total des dépenses du mois.
    /// </summary>
    private void RecalculerTotal()
    {
        _totalExpenses = _expenseCategories.Sum(categorie => categorie.Item3);
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