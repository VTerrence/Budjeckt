using System.Text.Json;

namespace Budjeckt;

/// <summary>
/// Année Budjeckt (lv0) : classe principale regroupant le nom de l'année et les 12 mois
/// (<see cref="MonthBudget"/>). Elle sait charger et sauvegarder ses données dans un fichier
/// JSON (ex. <c>depenses-2026.json</c>) via des sous-méthodes privées.
/// </summary>
public class Budjeckt
{
    private string _annee;
    private MonthBudget[] _mois;
    private List<FactureParDefaut> _facturesParDefaut = new();

    /// <summary>Nom du fichier hérité, sans année (« depenses.json »).</summary>
    public const string NomFichierHerite = "depenses.json";

    /// <summary>
    /// Noms des 12 mois de l'année dans l'ordre de navigation (source unique, utilisée
    /// pour créer les mois et par l'interface pour ne pas dupliquer la liste).
    /// </summary>
    public static IReadOnlyList<string> NomsDesMois { get; } = new[]
    {
        "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
        "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
    };

    /// <summary>
    /// Nom du fichier JSON d'une année donnée (ex. « depenses-2026.json »).
    /// </summary>
    /// <param name="annee">Année (ex. 2026).</param>
    public static string NomFichierPourAnnee(int annee) => $"depenses-{annee}.json";

    /// <summary>
    /// Extrait l'année déclarée d'un fichier JSON sans valider les mois (utilisé pour la
    /// migration du fichier hérité). Retourne null si le fichier est absent, trop volumineux,
    /// illisible, si l'année n'est pas un nombre entier ou si elle est hors de la plage
    /// plausible (1900-2100).
    /// </summary>
    /// <param name="chemin">Chemin du fichier JSON.</param>
    internal static int? LireAnneeDuFichier(string chemin)
    {
        if (!File.Exists(chemin))
        {
            return null;
        }

        if (new FileInfo(chemin).Length > TailleMaximaleFichier)
        {
            return null;
        }

        try
        {
            string contenu = File.ReadAllText(chemin);
            BudjecktJson donnees = Deserialiser(contenu);
            int? annee = int.TryParse(donnees?.Annee?.Trim(), out int valeur) ? valeur : null;
            return annee is >= 1900 and <= 2200 ? annee : null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Construit une année Budjeckt avec le nom de l'année récupéré par le programme (date système).
    /// </summary>
    public Budjeckt()
        : this(DateTime.Now.Year.ToString())
    {
    }

    /// <summary>
    /// Construit une année Budjeckt avec le nom d'année fourni et crée les 12 mois
    /// <see cref="MonthBudget"/> (un par mois de l'année) avec les catégories par défaut.
    /// </summary>
    /// <param name="annee">Nom de l'année (ex. « 2024 », « 2026 »).</param>
    /// <exception cref="ArgumentException">Si le nom de l'année est vide.</exception>
    public Budjeckt(string annee)
    {
        Validation.VerifierNomNonVide(annee);
        _annee = annee;
        int anneeEntiere = int.Parse(annee, System.Globalization.CultureInfo.InvariantCulture);
        _mois = CreerDouzeMois(anneeEntiere);
    }

    /// <summary>Nom de l'année (ex. « 2026 »).</summary>
    public string Annee => _annee;

    /// <summary>
    /// Tableau des mois de l'année. Le setter remplace intégralement le tableau
    /// (utilisé notamment pour injecter les mois chargés depuis le JSON).
    /// </summary>
    public MonthBudget[] Months
    {
        get => _mois;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _mois = value;
        }
    }

    /// <summary>
    /// Factures par défaut (récurrentes) de l'année, en lecture seule. Chaque défaut est
    /// reproduit automatiquement dans les mois dont le rang est supérieur ou égal à son mois
    /// de création et qui possèdent la catégorie ciblée, à condition qu'il soit actif.
    /// </summary>
    public IReadOnlyList<FactureParDefaut> FacturesParDefaut => _facturesParDefaut.AsReadOnly();

    /// <summary>
    /// Enregistre (ou réactive s'il existait déjà, même désactivé) un défaut récurrent pour
    /// une catégorie. Le mois de création est retenu pour ne reproduire le défaut que dans
    /// les mois de rang supérieur ou égal à celui-ci. La catégorie « Cagnotte » est refusée :
    /// les mouvements de cagnotte ne sont pas des factures par défaut.
    /// </summary>
    /// <param name="nomCategorie">Nom de la catégorie ciblée.</param>
    /// <param name="montant">Montant de la facture à reproduire (strictement positif).</param>
    /// <param name="moisCreation">Rang (1 à 12) du mois où le défaut est posé.</param>
    /// <exception cref="ArgumentException">Si le nom est vide, si le montant n'est pas strictement
    /// positif, si le mois de création est hors de la plage [1, 12] ou si la catégorie est
    /// la catégorie réservée « Cagnotte ».</exception>
    public void AjouterFactureParDefaut(string nomCategorie, float montant, int moisCreation)
    {
        Validation.VerifierNomNonVide(nomCategorie);
        Validation.VerifierMontantPositif(montant);

        if (moisCreation < 1 || moisCreation > 12)
        {
            throw new ArgumentException("Le mois de création d'un défaut doit être compris entre 1 et 12.", nameof(moisCreation));
        }

        string nom = nomCategorie.Trim();

        if (string.Equals(nom, MonthBudget.NomCategorieCagnotte, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Les mouvements de cagnotte ne peuvent pas être des factures par défaut.", nameof(nomCategorie));
        }

        FactureParDefaut? existant = _facturesParDefaut.FirstOrDefault(
            defaut => string.Equals(defaut.NomCategorie, nom, StringComparison.OrdinalIgnoreCase)
                      && defaut.Montant == montant);

        if (existant is not null)
        {
            existant.Reactiver();
            return;
        }

        _facturesParDefaut.Add(new FactureParDefaut(nom, montant, moisCreation));
    }

    /// <summary>
    /// Désactive le défaut récurrent correspondant au couple (catégorie, montant) s'il existe.
    /// Les mois de rang supérieur ou égal au défaut n'obtiendront plus de factures reproduites,
    /// mais les factures déjà matérialisées ne sont pas touchées.
    /// </summary>
    /// <param name="nomCategorie">Nom de la catégorie ciblée.</param>
    /// <param name="montant">Montant du défaut à désactiver.</param>
    public void DesactiverFactureParDefaut(string nomCategorie, float montant)
    {
        if (string.IsNullOrWhiteSpace(nomCategorie))
        {
            return;
        }

        FactureParDefaut? defaut = _facturesParDefaut.FirstOrDefault(
            defaut => string.Equals(defaut.NomCategorie, nomCategorie.Trim(), StringComparison.OrdinalIgnoreCase)
                      && defaut.Montant == montant);

        defaut?.Desactiver();
    }

    /// <summary>
    /// Désactive tous les défauts récurrents dont la catégorie correspond (comparaison
    /// insensible à la casse) : utilisé lors de la suppression d'une catégorie pour que les
    /// mois suivants ne reproduisent plus des factures d'une catégorie disparue.
    /// </summary>
    /// <param name="nomCategorie">Nom de la catégorie supprimée.</param>
    public void DesactiverFacturesParDefautDeCategorie(string nomCategorie)
    {
        if (string.IsNullOrWhiteSpace(nomCategorie))
        {
            return;
        }

        string nom = nomCategorie.Trim();
        foreach (FactureParDefaut defaut in _facturesParDefaut.Where(
                     defaut => string.Equals(defaut.NomCategorie, nom, StringComparison.OrdinalIgnoreCase)))
        {
            defaut.Desactiver();
        }
    }

    /// <summary>
    /// Applique au mois fourni les défauts récurrents actifs dont le mois de création est
    /// antérieur ou égal au rang du mois, et dont la catégorie existe dans ce mois. La
    /// reproduction est idempotente : un défaut déjà matérialisé dans le mois (facture marquée
    /// <see cref="Facture.EstParDefaut"/>) n'est pas dupliqué. Ne reproduit jamais les
    /// mouvements de cagnotte.
    /// </summary>
    /// <param name="mois">Mois cible (de l'année courante).</param>
    /// <returns>Nombre de factures effectivement créées (0 si le mois est inconnu, si toutes les
    /// catégories sont absentes, ou si tous les défauts sont déjà appliqués).</returns>
    public int AppliquerFacturesParDefaut(MonthBudget mois)
    {
        ArgumentNullException.ThrowIfNull(mois);

        int? indexMois = Validation.IndexDuMois(mois.Nom);
        if (indexMois is null)
        {
            return 0;
        }

        int nb = 0;
        foreach (FactureParDefaut defaut in _facturesParDefaut.Where(
                     defaut => defaut.EstActive && defaut.MoisCreation <= indexMois.Value))
        {
            if (string.Equals(defaut.NomCategorie, MonthBudget.NomCategorieCagnotte, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Tuple<int, string, float>? categorie = mois.ExpenseCategories
                .FirstOrDefault(c => string.Equals(c.Item2, defaut.NomCategorie, StringComparison.OrdinalIgnoreCase));

            if (categorie is null)
            {
                continue;
            }

            bool dejaApplique = mois.Factures.Any(
                f => f.EstParDefaut && f.IdCategorie == categorie.Item1 && f.Montant == defaut.Montant);

            if (dejaApplique)
            {
                continue;
            }

            mois.AjouterFacture(categorie.Item1, defaut.Montant, date: null, heure: null, estParDefaut: true);
            nb++;
        }

        return nb;
    }

    /// <summary>
    /// Charge les données depuis un fichier JSON. Décomposée en sous-méthodes :
    /// lecture du fichier, désérialisation, validation de la structure puis application au modèle.
    /// Si le fichier n'existe pas, les données par défaut sont conservées (premier lancement).
    /// </summary>
    /// <param name="chemin">Chemin du fichier JSON.</param>
    /// <exception cref="InvalidDataException">Si le fichier existe mais est corrompu, mal formé,
    /// contient une année non numérique ou hors de la plage plausible (1900-2100), ou une
    /// facture datée d'une année ne correspondant pas à l'année déclarée dans le fichier.</exception>
    public void ChargerJson(string chemin)
    {
        string? contenu = LireFichier(chemin);
        if (contenu is null)
        {
            return;
        }

        BudjecktJson donnees = Deserialiser(contenu);
        ValiderJson(donnees);
        AppliquerModele(donnees);
    }

    /// <summary>
    /// Sauvegarde les données de l'année dans un fichier JSON. Décomposée en sous-méthodes :
    /// construction du DTO, sérialisation puis écriture du fichier.
    /// </summary>
    /// <param name="chemin">Chemin du fichier JSON.</param>
    /// <exception cref="InvalidDataException">Si l'état mémoire ne satisfait pas les mêmes
    /// invariants que le chargement (exactement 12 mois, au moins une catégorie par mois,
    /// factures rattachées à des catégories existantes). Rien n'est écrit dans ce cas.</exception>
    public void SauvegarderJson(string chemin)
    {
        // Le même contrôle structurel qu'au chargement (ValiderJson) est appliqué avant
        // l'écriture : un état mémoire invalide (ex. un mois sans catégorie via le setter
        // Months) ne doit jamais produire un fichier que l'application rejetterait elle-même
        // au prochain lancement — quarantaine et perte de l'année à la clé.
        BudjecktJson donnees = ConstruireJson();
        ValiderJson(donnees);

        string contenu = JsonSerializer.Serialize(donnees);
        EcrireFichier(chemin, contenu);
    }

    /// <summary>
    /// Lit le contenu du fichier ; retourne <c>null</c> si le fichier n'existe pas.
    /// </summary>
    private static string? LireFichier(string chemin)
    {
        if (!File.Exists(chemin))
        {
            return null;
        }

        // Borner la taille lue éviter qu'un fichier corrompu ou gonflé ne provoque un
        // OutOfMemoryException non gérée lors du chargement.
        if (new FileInfo(chemin).Length > TailleMaximaleFichier)
        {
            throw new InvalidDataException($"Le fichier JSON est trop volumineux (limite {TailleMaximaleFichier / (1024 * 1024)} Mo).");
        }

        return File.ReadAllText(chemin);
    }

    private const long TailleMaximaleFichier = 10 * 1024 * 1024;

    /// <summary>
    /// Désérialise le contenu JSON vers le DTO de l'année.
    /// </summary>
    private static BudjecktJson Deserialiser(string contenu)
    {
        try
        {
            return JsonSerializer.Deserialize<BudjecktJson>(contenu)
                   ?? throw new JsonException("Le JSON ne contient aucune donnée.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Le fichier JSON est corrompu ou mal formé.", exception);
        }
    }

    /// <summary>
    /// Vérifie que la structure du JSON est correcte : année présente et numérique, exactement 12 mois
    /// dont le nom est connu, chaque mois avec des catégories nommées à ids uniques, aucun élément nul,
    /// montants finis et strictement positifs, heure optionnelle dans [00:00, 24:00), date plausible
    /// appartenant au mois affiché et à l'année déclarée, chaque facture liée à une catégorie
    /// existante à ids uniques, et factures par défaut (optionnelles) bien formées, sans doublon
    /// catégorie/montant, ni cible réservée « Cagnotte ».
    /// </summary>
    private static void ValiderJson(BudjecktJson donnees)
    {
        if (string.IsNullOrWhiteSpace(donnees.Annee))
        {
            throw new InvalidDataException("Le fichier JSON ne contient pas d'année valide.");
        }

        if (!int.TryParse(donnees.Annee.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int annee))
        {
            throw new InvalidDataException($"L'année du fichier JSON n'est pas un nombre valide : \"{donnees.Annee}\".");
        }

        if (annee < 1900 || annee > 2100)
        {
            throw new InvalidDataException($"L'année {annee} du fichier JSON est hors de la plage plausible (1900-2100).");
        }

        if (donnees.Mois is null || donnees.Mois.Count != 12)
        {
            throw new InvalidDataException("Le fichier JSON doit contenir exactement 12 mois.");
        }

        HashSet<string> nomsMoisVus = new(StringComparer.OrdinalIgnoreCase);
        foreach (MonthJson mois in donnees.Mois)
        {
            if (mois is null)
            {
                throw new InvalidDataException("Un mois du fichier JSON est nul.");
            }

            if (string.IsNullOrWhiteSpace(mois.Nom))
            {
                throw new InvalidDataException("Un mois du fichier JSON n'a pas de nom.");
            }

            // Deux mois portant le même nom (ex. deux « Janvier ») casseraient l'affichage et le tri.
            if (!nomsMoisVus.Add(mois.Nom))
            {
                throw new InvalidDataException($"Le nom de mois \"{mois.Nom}\" est présent plusieurs fois dans le fichier JSON.");
            }

            int? indexMois = Validation.IndexDuMois(mois.Nom);
            if (indexMois is null)
            {
                throw new InvalidDataException($"Le mois \"{mois.Nom}\" du fichier JSON est inconnu.");
            }

            if (mois.Categories is null || mois.Categories.Count == 0)
            {
                throw new InvalidDataException($"Le mois \"{mois.Nom}\" n'a pas de catégories.");
            }

            HashSet<int> idsCategorie = new();
            foreach (CategoryJson categorie in mois.Categories)
            {
                if (categorie is null || string.IsNullOrWhiteSpace(categorie.Nom))
                {
                    throw new InvalidDataException($"Le mois \"{mois.Nom}\" a une catégorie sans nom.");
                }

                // Des ids négatifs, nuls ou dupliqués casseraient l'auto-incrément
                // (max + 1) et la suppression par id (First) au sein du mois.
                if (categorie.Id < 1 || !idsCategorie.Add(categorie.Id))
                {
                    throw new InvalidDataException(
                        $"Le mois \"{mois.Nom}\" a des catégories avec des ids négatifs, nuls ou dupliqués.");
                }
            }

            // Un revenue infini (ex. « 1e39 » désérialisé en float) corromprait le reste du budget
            // et bloquerait toute sauvegarde JSON ultérieure.
            if (float.IsNaN(mois.Revenue) || float.IsInfinity(mois.Revenue))
            {
                throw new InvalidDataException($"Le mois \"{mois.Nom}\" a un revenue invalide (NaN ou infini).");
            }

            // Un montant de cagnotte infini corromprait le reste du mois et bloquerait la
            // sauvegarde ; un montant négatif est admis (argent réinjecté depuis la cagnotte).
            if (float.IsNaN(mois.MontantCagnotte) || float.IsInfinity(mois.MontantCagnotte))
            {
                throw new InvalidDataException($"Le mois \"{mois.Nom}\" a un montant de cagnotte invalide (NaN ou infini).");
            }

            HashSet<int> idsFacture = new();
            // La catégorie réservée « Cagnotte » autorise seule les montants signés (dépôt/retrait).
            int? idCategorieCagnotte = mois.Categories
                .FirstOrDefault(categorie => string.Equals(categorie.Nom, MonthBudget.NomCategorieCagnotte, StringComparison.OrdinalIgnoreCase))
                ?.Id;

            foreach (FactureJson facture in mois.Factures ?? Enumerable.Empty<FactureJson>())
            {
                if (facture is null)
                {
                    throw new InvalidDataException($"Le mois \"{mois.Nom}\" a une facture nulle.");
                }

                // Comme pour les catégories : un id négatif/nul ou dupliqué casserait
                // l'auto-incrément et la suppression par id au sein du mois.
                if (facture.Id < 1 || !idsFacture.Add(facture.Id))
                {
                    throw new InvalidDataException(
                        $"Le mois \"{mois.Nom}\" a des factures avec des ids négatifs, nuls ou dupliqués.");
                }

                // NaN et infini échappent aux comparaisons (IEEE 754) ; un montant infini
                // corromprait les totaux et empêcherait toute sauvegarde JSON ultérieure.
                // Hors de la catégorie cagnotte, un montant ≤ 0 est une dépense incohérente ;
                // dans la catégorie cagnotte, un montant nul est un mouvement dépourvu de sens.
                bool estMouvementCagnotte = facture.IdCategorie == idCategorieCagnotte;
                bool montantInvalide = estMouvementCagnotte
                    ? facture.Montant == 0f
                    : facture.Montant <= 0f;
                if (montantInvalide || float.IsNaN(facture.Montant) || float.IsInfinity(facture.Montant))
                {
                    throw new InvalidDataException(
                        estMouvementCagnotte
                            ? $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" est un mouvement de cagnotte à montant nul, NaN ou infini."
                            : $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" a un montant invalide (inférieur ou égal à 0, NaN ou infini).");
                }

                if (!mois.Categories.Any(categorie => categorie.Id == facture.IdCategorie))
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" référence une catégorie inconnue.");
                }

                // Un mouvement de cagnotte est sans heure : seule la date du mouvement compte.
                if (!estMouvementCagnotte && !Validation.HeureEstValide(facture.Heure))
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" a une heure invalide.");
                }

                // Un mouvement de cagnotte ne peut pas être une facture par défaut : les mouvements
                // de cagnotte sont des opérations ponctuelles générées par l'interface.
                if (estMouvementCagnotte && facture.EstParDefaut)
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" est un mouvement de cagnotte marqué comme facture par défaut.");
                }

                // La date doit rester plausible (armure contre les dates 9999) et tomber sur le mois affiché.
                if (!Validation.DateEstPlausible(facture.Date))
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" a une date hors de l'intervalle plausible (1900-2100).");
                }

                if (facture.Date.Month != indexMois.Value)
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" a une date qui n'appartient pas à ce mois.");
                }

                if (facture.Date.Year != annee)
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" a une date dont l'année {facture.Date.Year} ne correspond pas à l'année déclarée {annee}.");
                }
            }
        }

        // Validation de la liste des factures par défaut (optionnelle — absente dans les
        // fichiers antérieurs à la fonctionnalité).
        if (donnees.FacturesParDefaut is not null)
        {
            HashSet<string> vus = new(StringComparer.OrdinalIgnoreCase);
            foreach (FactureParDefautJson defaut in donnees.FacturesParDefaut)
            {
                if (defaut is null)
                {
                    throw new InvalidDataException("Un défaut de facture récurrente du fichier JSON est nul.");
                }

                if (string.IsNullOrWhiteSpace(defaut.Categorie))
                {
                    throw new InvalidDataException("Un défaut de facture récurrente n'a pas de nom de catégorie.");
                }

                if (defaut.Montant <= 0f || float.IsNaN(defaut.Montant) || float.IsInfinity(defaut.Montant))
                {
                    throw new InvalidDataException(
                        $"Le défaut pour \"{defaut.Categorie}\" a un montant invalide (inférieur ou égal à 0, NaN ou infini).");
                }

                if (defaut.MoisCreation < 1 || defaut.MoisCreation > 12)
                {
                    throw new InvalidDataException(
                        $"Le défaut pour \"{defaut.Categorie}\" a un mois de création hors de la plage [1, 12].");
                }

                if (string.Equals(defaut.Categorie.Trim(), MonthBudget.NomCategorieCagnotte, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Le défaut pour \"{defaut.Categorie}\" cible la catégorie réservée « Cagnotte ».");
                }

                // Deux défauts identiques (catégorie + montant) casseraient l'idempotence
                // de l'application (deux factures identiques produites au lieu d'une).
                string cle = $"{defaut.Categorie.Trim()}|{defaut.Montant.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}";
                if (!vus.Add(cle))
                {
                    throw new InvalidDataException(
                        $"Le défaut pour \"{defaut.Categorie}\" au montant {defaut.Montant} est présent plusieurs fois.");
                }
            }
        }
    }

    /// <summary>
    /// Applique les données du DTO validé au modèle de l'année. L'année est parsée depuis le DTO
    /// et passée à chaque mois. Les deux champs sont assignés après construction complète des mois :
    /// un échec (ex. débordement float) laisse alors l'objet dans son état précédent.
    /// </summary>
    private void AppliquerModele(BudjecktJson donnees)
    {
        int annee = int.Parse(donnees.Annee!, System.Globalization.CultureInfo.InvariantCulture);

        MonthBudget[] nouveauxMois = donnees.Mois!
            .Select(mois =>
            {
                int? idCategorieCagnotte = mois.Categories!
                    .FirstOrDefault(categorie => string.Equals(categorie.Nom, MonthBudget.NomCategorieCagnotte, StringComparison.OrdinalIgnoreCase))
                    ?.Id;

                return new MonthBudget(
                    mois.Nom!,
                    mois.Revenue,
                    mois.Categories!.Select(categorie => new Tuple<int, string, float>(categorie.Id, categorie.Nom!, 0f)).ToArray(),
                    (mois.Factures ?? Enumerable.Empty<FactureJson>())
                        .Select(facture => facture.IdCategorie == idCategorieCagnotte
                            ? new Facture(facture.Id, facture.IdCategorie, facture.Montant, facture.Date.Date, mouvementCagnotte: true)
                            : new Facture(facture.Id, facture.IdCategorie, facture.Montant, facture.Date.Date, facture.Heure, facture.EstParDefaut)),
                    annee,
                    mois.MontantCagnotte);
            })
            .ToArray();

        List<FactureParDefaut> defauts = (donnees.FacturesParDefaut ?? Enumerable.Empty<FactureParDefautJson>())
            .Select(defaut => new FactureParDefaut(defaut.Categorie!, defaut.Montant, defaut.MoisCreation, defaut.Active))
            .ToList();

        _annee = donnees.Annee!;
        _mois = nouveauxMois;
        _facturesParDefaut = defauts;
    }

    /// <summary>
    /// Construit le DTO de l'année à partir du modèle.
    /// </summary>
    private BudjecktJson ConstruireJson()
    {
        return new BudjecktJson
        {
            Annee = _annee,
            Mois = _mois
                .Select(mois => new MonthJson
                {
                    Nom = mois.Nom,
                    Revenue = mois.Revenue,
                    MontantCagnotte = mois.MontantCagnotte,
                    Categories = mois.ExpenseCategories
                        .Select(categorie => new CategoryJson { Id = categorie.Item1, Nom = categorie.Item2 })
                        .ToList(),
                    Factures = mois.Factures
                        .Select(facture => new FactureJson
                        {
                            Id = facture.Id,
                            IdCategorie = facture.IdCategorie,
                            Montant = facture.Montant,
                            Date = facture.Date,
                            Heure = facture.Heure,
                            EstParDefaut = facture.EstParDefaut
                        })
                        .ToList()
                })
                .ToList(),
            FacturesParDefaut = _facturesParDefaut
                .Select(defaut => new FactureParDefautJson
                {
                    Categorie = defaut.NomCategorie,
                    Montant = defaut.Montant,
                    MoisCreation = defaut.MoisCreation,
                    Active = defaut.EstActive
                })
                .ToList()
        };
    }

    /// <summary>
    /// Écrit le contenu JSON dans le fichier en créant le répertoire parent si nécessaire.
    /// L'écriture est atomique : le contenu est d'abord écrit dans un fichier temporaire puis
    /// déplacé par-dessus la cible, afin qu'une coupure en plein écriture ne tronque pas le fichier.
    /// </summary>
    private static void EcrireFichier(string chemin, string contenu)
    {
        string? repertoire = Path.GetDirectoryName(chemin);
        if (!string.IsNullOrEmpty(repertoire))
        {
            Directory.CreateDirectory(repertoire);
        }

        string cheminTemporaire = chemin + ".tmp";
        File.WriteAllText(cheminTemporaire, contenu);
        File.Move(cheminTemporaire, chemin, true);
    }

    /// <summary>
    /// Crée les 12 <see cref="MonthBudget"/> de l'année avec les catégories par défaut.
    /// </summary>
    private static MonthBudget[] CreerDouzeMois(int annee)
    {
        return NomsDesMois
            .Select(nom => new MonthBudget(nom, 0f, MonthBudget.CreerCategoriesParDefaut(), Enumerable.Empty<Facture>(), annee))
            .ToArray();
    }
}