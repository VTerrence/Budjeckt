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

    /// <summary>
    /// Nom réservé de la catégorie qui porte les mouvements de cagnotte du mois (dépôts et
    /// retraits, montant signé). Non ajoutable/supprimable/renommable via l'interface ; une
    /// catégorie déjà nommée ainsi avant la cagnotte devient la catégorie de cagnotte du mois.
    /// </summary>
    public const string NomCategorieCagnotte = "Cagnotte";

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

    /// <summary>Total des dépenses du mois (inclut les mouvements signés de cagnotte).</summary>
    public float TotalExpenses => _totalExpenses;

    /// <summary>Reste du budget (revenue - total des dépenses), potentiellement négatif si le budget est dépassé.</summary>
    public float BudgetRemaining => _budgetRemaining;

    /// <summary>
    /// Montant net que ce mois a mis en cagnotte : positif = de l'argent de ce mois a été
    /// mis de côté (soustrait du reste), négatif = de l'argent de la cagnotte a été réinjecté
    /// dans ce mois (ajouté au reste). C'est la somme signée des mouvements de cagnotte du mois
    /// (dépôts comme factures positives, retraits comme factures négatives), équivalente à
    /// l'ancien champ persisté avant la cagnotte en factures. Le solde global de la cagnotte
    /// est la somme de ces montants sur tous les mois de toutes les années.
    /// </summary>
    public float MontantCagnotte => _factures
        .Where(facture => facture.EstMouvementCagnotte)
        .Sum(facture => facture.Montant);

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
    /// <param name="montantCagnotte">Montant net mis en cagnotte par ce mois (compatibilité des
    /// fichiers antérieurs à la cagnotte en factures) ; 0 si absent. Négatif autorisé (argent
    /// réinjecté de la cagnotte vers ce mois). Transcrit en une ligne synthétique de cagnotte
    /// uniquement si le mois ne porte aucun mouvement de cagnotte.</param>
    /// <exception cref="ArgumentException">Si le nom du mois est vide, si le revenue n'est pas
    /// un nombre fini, si le montant de cagnotte n'est pas fini, si une facture référence une
    /// catégorie inconnue ou n'appartient pas au mois et à l'année du mois.</exception>
    /// <exception cref="InvalidDataException">Si la somme des factures d'une catégorie ou du mois
    /// déborde de la plage flottante, ou si le reste du mois (revenue − dépenses − montant de
    /// cagnotte) déborde à cause du montant de cagnotte.</exception>
    public MonthBudget(string nom, float revenue, Tuple<int, string, float>[] categories, IEnumerable<Facture> factures, int? annee = null, float montantCagnotte = 0f)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(factures);
        Validation.VerifierNomNonVide(nom);
        Validation.VerifierRevenueFini(revenue);

        // Un montant de cagnotte NaN ou infini corromprait silencieusement le reste du mois
        // et bloquerait toute sauvegarde JSON ultérieure.
        if (float.IsNaN(montantCagnotte) || float.IsInfinity(montantCagnotte))
        {
            throw new ArgumentException("Le montant de cagnotte doit être un nombre fini.", nameof(montantCagnotte));
        }

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

        // Migration des fichiers antérieurs à la cagnotte en factures : le champ MontantCagnotte
        // est transcrit en une ligne synthétique signée, uniquement si le mois ne porte aucun
        // mouvement signé (sinon le champ est une redondance dérivée et on lui fait confiance).
        // Le reste est mathématiquement identique (revenue − dépenses − montantCagnotte), les
        // totaux changent de forme mais pas de valeur.
        if (montantCagnotte != 0f && !aDesMouvementsCagnotte())
        {
            CreerLigneCagnotte(montantCagnotte, date: null);
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
        // Les mouvements de cagnotte sont déjà comptés dans le total des dépenses : un
        // changement de revenue ne doit pas « ressusciter » l'argent déjà mis de côté.
        if (float.IsInfinity(revenue - _totalExpenses))
        {
            throw new InvalidDataException("Le reste du budget déborde de la plage flottante.");
        }

        _revenue = revenue;
        RecalculerReste();
    }

    /// <summary>
    /// Met un montant de côté dans la cagnotte : le montant est soustrait du reste du mois
    /// (il n'est plus disponible pour les dépenses et ne réapparaît pas dans la répartition
    /// hebdomadaire). On ne peut mettre de côté que l'argent encore disponible, d'où le
    /// plafond au reste actuel non négatif du mois. Un dépôt ajoute une facture POSITIVE
    /// (+ montant) dans la catégorie réservée « Cagnotte » du mois.
    /// </summary>
    /// <param name="montant">Montant à mettre de côté, strictement positif.</param>
    /// <exception cref="ArgumentException">Si le montant est nul, négatif, NaN, infini ou
    /// supérieur au reste actuel du mois.</exception>
    /// <exception cref="InvalidDataException">Si le nouveau total ou le nouveau reste du
    /// mois déborde de la plage flottante.</exception>
    public void MettreDeCote(float montant)
    {
        Validation.VerifierMontantPositif(montant);

        // Mettre de côté plus que le reste disponible ferait passer le budget du mois en
        // déficit pour alimenter la cagnotte : refusé avant toute mutation (transactionnalité).
        if (montant > _budgetRemaining)
        {
            throw new ArgumentException(
                $"Impossible de mettre {montant} € de côté : le reste du mois est de {_budgetRemaining} €.", nameof(montant));
        }

        // Le nouveau total et le nouveau reste sont vérifiés AVANT toute mutation, comme en
        // retrait (voir RecupererDeCagnotte) : un état débordant ne doit jamais corrompre le mois.
        // En pratique le plafond `montant ≤ reste` rend déjà ce cas inatteignable, la garde
        // reste par symétrie et défense en profondeur (transactionnalité).
        float nouveauTotal = _totalExpenses + montant;
        float nouveauReste = _revenue - nouveauTotal;

        if (float.IsInfinity(nouveauTotal) || float.IsInfinity(nouveauReste))
        {
            throw new InvalidDataException("Le reste du budget déborde de la plage flottante.");
        }

        CreerLigneCagnotte(montant, date: null);
        RecalculerTotaux();
    }

    /// <summary>
    /// Récupère un montant de la cagnotte vers le budget du mois : le montant est ajouté au
    /// reste du mois (il redevient disponible). Aucun plafond côté mois, seule la cagnotte
    /// globale (<see cref="Cagnotte.Retirer"/>) refuse un retrait supérieur à son solde.
    /// Un retrait ajoute une facture NÉGATIVE (− montant) dans la catégorie réservée « Cagnotte ».
    /// </summary>
    /// <param name="montant">Montant à récupérer, strictement positif.</param>
    /// <exception cref="ArgumentException">Si le montant est nul, négatif, NaN ou infini.</exception>
    /// <exception cref="InvalidDataException">Si le nouveau total ou le nouveau reste du
    /// mois déborde de la plage flottante.</exception>
    public void RecupererDeCagnotte(float montant)
    {
        Validation.VerifierMontantPositif(montant);

        // Le nouveau total et le nouveau reste sont vérifiés AVANT toute mutation : un retrait
        // qui pousse le total mis en cagnotte très négatif peut faire déborder le reste
        // (`revenue − dépenses` avec dépenses très négatives) vers +∞ alors même que le total
        // reste fini (ex. revenue = float.MaxValue, retrait = float.MaxValue).
        // Sans cette garde, le débordement éclaterait pendant RecalculerReste, APRÈS l'ajout
        // de la ligne de cagnotte — état corrompu et fichier d'année mis de côté au prochain
        // chargement (transactionnalité).
        float nouveauTotal = _totalExpenses - montant;
        float nouveauReste = _revenue - nouveauTotal;

        if (float.IsInfinity(nouveauTotal) || float.IsInfinity(nouveauReste))
        {
            throw new InvalidDataException("Le reste du budget déborde de la plage flottante.");
        }

        CreerLigneCagnotte(-montant, date: null);
        RecalculerTotaux();
    }

    /// <summary>
    /// Ajoute une catégorie de dépense après vérification : nom non vide et unique.
    /// La dépense faite est mise à 0 et l'id vaut l'id précédent + 1.
    /// </summary>
    /// <param name="nom">Nom de la catégorie.</param>
    /// <exception cref="ArgumentException">Si le nom est vide, déjà utilisé ou réservé à la
    /// cagnotte (« Cagnotte »).</exception>
    public void AjouterCategorie(string nom)
    {
        Validation.VerifierNomNonVide(nom);

        if (string.Equals(nom, NomCategorieCagnotte, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Le nom \"{nom}\" est réservé à la cagnotte : supprimez une dépense de la cagnotte par l'opération inverse.", nameof(nom));
        }

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
    /// pas dans le mois, si elle est la seule du mois ou si elle est la catégorie réservée
    /// de la cagnotte (« Cagnotte »). Aucune mutation n'est effectuée dans ce cas.</exception>
    public int SupprimerCategorie(string nom)
    {
        Validation.VerifierNomNonVide(nom);

        // La catégorie de cagnotte est protégée : la supprimer laisserait ses mouvements signés
        // orphelins (fichier d'année rejeté au prochain chargement) et désynchroniserait le pot.
        if (string.Equals(nom, NomCategorieCagnotte, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"La catégorie \"{nom}\" est réservée à la cagnotte : elle ne peut pas être supprimée.", nameof(nom));
        }

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
    /// <param name="estParDefaut">Marque la facture comme reproduction d'une facture par défaut (récurrente).</param>
    /// <exception cref="ArgumentException">Si la catégorie n'existe pas, si la catégorie est la
    /// catégorie réservée « Cagnotte » avec <paramref name="estParDefaut"/> à <c>true</c>, si le
    /// montant n'est pas strictement positif, si l'heure est invalide, si la date n'appartient pas
    /// au mois ou est implausible (1900-2100), ou si le nom du mois est inconnu. Aucune mutation
    /// n'est effectuée dans ce cas.</exception>
    /// <exception cref="InvalidDataException">Si la somme des factures déborde de la plage flottante.</exception>
    public void AjouterFacture(int idCategorie, float montant, DateTime? date, TimeSpan? heure, bool estParDefaut = false)
    {
        Validation.VerifierMontantPositif(montant);
        Validation.VerifierCategorieExiste(idCategorie, _expenseCategories.Select(categorie => categorie.Item1));
        Validation.VerifierHeureValide(heure);

        // Les mouvements de cagnotte sont des opérations ponctuelles : aucune facture par défaut
        // ne peut cibler la catégorie réservée, sans quoi un chargement JSON marquerait un
        // mouvement de cagnotte comme récurrent (état rejeté par ValiderJson, année bloquée).
        if (estParDefaut)
        {
            Tuple<int, string, float>? categorie = _expenseCategories
                .FirstOrDefault(categorie => categorie.Item1 == idCategorie);

            if (categorie is not null
                && string.Equals(categorie.Item2, NomCategorieCagnotte, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Les mouvements de cagnotte ne peuvent pas être des factures par défaut.", nameof(idCategorie));
            }
        }

        DateTime dateResolue = ResoudreDate(date);

        int prochainId = _factures.Count == 0
            ? 1
            : _factures.Max(facture => facture.Id) + 1;

        _factures.Add(new Facture(prochainId, idCategorie, montant, dateResolue, heure, estParDefaut));
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
    /// Supprime définitivement une facture du mois (par id). Un mouvement de cagnotte n'est
    /// jamais supprimable : l'annulation se fait par l'opération inverse sur la cagnotte,
    /// sinon le pot global divergerait de la ligne. (Retrait refusé avant toute mutation.)
    /// </summary>
    /// <param name="id">Identifiant de la facture à supprimer.</param>
    /// <exception cref="ArgumentException">Si aucune facture ne correspond à cet id ou si la
    /// facture est un mouvement de cagnotte.</exception>
    public void SupprimerFacture(int id)
    {
        Validation.VerifierFactureExiste(id, _factures.Select(facture => facture.Id));

        Facture aSupprimer = _factures.First(facture => facture.Id == id);
        VerifierMouvementSupprimable(aSupprimer);

        _factures.Remove(aSupprimer);
        RecalculerTotaux();
    }

    /// <summary>
    /// Supprime définitivement plusieurs factures du mois (par ids). La liste est dédoublonnée
    /// et tous les ids sont vérifiés AVANT toute mutation : si un id est inconnu, aucune facture
    /// n'est supprimée (transactionnalité, même garde que <see cref="SupprimerCategorie"/>).
    /// Un mouvement de cagnotte est toujours exclu (annulation = opération inverse). Un seul
    /// recalcul des totaux est effectué pour l'ensemble supprimé.
    /// </summary>
    /// <param name="ids">Identifiants des factures à supprimer.</param>
    /// <returns>Nombre de factures supprimées.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="ids"/> est <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Si la liste est vide, si un id ne correspond à
    /// aucune facture ou si un id désigne un mouvement de cagnotte. Aucune mutation n'est
    /// effectuée dans ce cas.</exception>
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

        foreach (Facture facture in _factures.Where(facture => idsUniques.Contains(facture.Id)))
        {
            VerifierMouvementSupprimable(facture);
        }

        _factures.RemoveAll(facture => idsUniques.Contains(facture.Id));
        RecalculerTotaux();

        return idsUniques.Count;
    }

    /// <summary>
    /// Refuse la suppression d'un mouvement de cagnotte identifié comme à supprimer.
    /// </summary>
    /// <param name="facture">Mouvement de cagnotte sélectionné.</param>
    /// <exception cref="ArgumentException">Si la facture est un mouvement de cagnotte.</exception>
    private static void VerifierMouvementSupprimable(Facture facture)
    {
        if (!facture.EstMouvementCagnotte)
        {
            return;
        }

        throw new ArgumentException(
            "Les mouvements de cagnotte ne se suppriment pas : annulez le retrait par un dépôt ou le dépôt par un retrait.");
    }

    /// <summary>
    /// Indique si le mois porte au moins un mouvement de cagnotte signé.
    /// </summary>
    private bool aDesMouvementsCagnotte()
    {
        return _factures.Any(facture => facture.EstMouvementCagnotte);
    }

    /// <summary>
    /// Identifiant de la catégorie réservée « Cagnotte » si elle existe dans le mois, sinon <c>null</c>.
    /// Une catégorie déjà nommée ainsi (fichier antérieur) devient la catégorie de cagnotte du mois.
    /// </summary>
    private int? IdCategorieReservee()
    {
        Tuple<int, string, float>? categorie = _expenseCategories
            .FirstOrDefault(categorie => string.Equals(categorie.Item2, NomCategorieCagnotte, StringComparison.OrdinalIgnoreCase));
        return categorie?.Item1;
    }

    /// <summary>
    /// Ajoute une ligne de cagnotte : catégorie réservée créée si absente, puis facture signée
    /// (montant positif = dépôt, négatif = retrait). Sans recalcul : l'appelant enchaîne
    /// <see cref="RecalculerTotaux"/> après avoir validé le nouvel état (transactionnalité).
    /// </summary>
    /// <param name="montant">Montant signé, non nul et fini.</param>
    /// <param name="date">Date du mouvement ; sans date, aujourd'hui ou le 1er du mois (règle par défaut).</param>
    private void CreerLigneCagnotte(float montant, DateTime? date)
    {
        int idCategorie = IdCategorieReservee() ?? AjouterCategorieReservee();

        int prochainId = _factures.Count == 0
            ? 1
            : _factures.Max(facture => facture.Id) + 1;

        _factures.Add(new Facture(prochainId, idCategorie, montant, ResoudreDate(date), mouvementCagnotte: true));
    }

    /// <summary>
    /// Ajoute la catégorie réservée « Cagnotte » (id précédent maximum + 1) sans aucune garde
    /// de nom : réservé à la logique interne de la cagnotte, contrairement à <see cref="AjouterCategorie"/>.
    /// </summary>
    private int AjouterCategorieReservee()
    {
        int prochainId = _expenseCategories.Length == 0
            ? 1
            : _expenseCategories.Max(categorie => categorie.Item1) + 1;

        var nouvelleCategorie = new Tuple<int, string, float>(prochainId, NomCategorieCagnotte, 0f);
        var tableau = new Tuple<int, string, float>[_expenseCategories.Length + 1];
        Array.Copy(_expenseCategories, tableau, _expenseCategories.Length);
        tableau[^1] = nouvelleCategorie;
        _expenseCategories = tableau;

        return prochainId;
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
    /// Calcule et modifie le reste du budget (revenue - total des dépenses), en refusant le
    /// débordement flottant silencieux (même garde que le total et les catégories). Les
    /// mouvements de cagnotte étant comptés dans le total (dépôt positif, retrait négatif),
    /// l'argent mis de côté par ce mois n'est pas disponible à la dépense, comme le stipule la
    /// règle métier historique (revenue − dépenses − montantCagnotte).
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