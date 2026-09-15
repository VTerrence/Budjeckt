using System.Text.Json;

namespace Budjeckt;

/// <summary>
/// Cagnotte Budjeckt : épargne globale indépendante des années, commune à tous les mois.
/// Le solde vit dans un fichier dédié (<c>cagnotte.json</c>) et évolue par dépôts — retirés
/// du budget du mois affiché via <see cref="MonthBudget.MettreDeCote"/> — et retraits —
/// réinjectés dans le budget du mois affiché via <see cref="MonthBudget.RecupererDeCagnotte"/>.
/// </summary>
public class Cagnotte
{
    private float _solde;

    /// <summary>Nom du fichier JSON de la cagnotte (« cagnotte.json »).</summary>
    public const string NomFichier = "cagnotte.json";

    /// <summary>Borne haute (inclusive) de la taille lue d'un fichier de cagnotte.</summary>
    public const long TailleMaximaleFichier = 1024 * 1024;

    /// <summary>Construit une cagnotte vide (solde à 0,00 €).</summary>
    public Cagnotte()
    {
    }

    /// <summary>Solde actuel de la cagnotte (jamais négatif : les retraits sont plafonnés).</summary>
    public float Solde => _solde;

    /// <summary>
    /// Charge le solde depuis un fichier JSON. Si le fichier n'existe pas, le solde courant
    /// (0 au départ) est conservé. Un solde non fini ou négatif rend le fichier invalide.
    /// </summary>
    /// <param name="chemin">Chemin du fichier JSON.</param>
    /// <exception cref="InvalidDataException">Si le fichier existe mais est corrompu, mal formé,
    /// trop volumineux, ou porte un solde invalide (NaN, infini ou négatif).</exception>
    public void ChargerJson(string chemin)
    {
        string? contenu = LireFichier(chemin);
        if (contenu is null)
        {
            return;
        }

        CagnotteJson donnees = Deserialiser(contenu);
        if (EstSoldeInvalide(donnees.Solde))
        {
            throw new InvalidDataException("Le fichier JSON de la cagnotte a un solde invalide (NaN, infini ou négatif).");
        }

        _solde = donnees.Solde;
    }

    /// <summary>
    /// Sauvegarde le solde dans un fichier JSON. Le solde est d'abord vérifié (jamais négatif,
    /// toujours fini) : un état invalide lève <see cref="InvalidDataException"/> sans rien écrire.
    /// </summary>
    /// <param name="chemin">Chemin du fichier JSON.</param>
    /// <exception cref="InvalidDataException">Si le solde courant est invalide (NaN, infini ou négatif).</exception>
    public void SauvegarderJson(string chemin)
    {
        // Mêmes gardes qu'au chargement : un solde non conforme ne doit jamais produire un
        // fichier que l'application rejetterait elle-même au prochain lancement.
        if (EstSoldeInvalide(_solde))
        {
            throw new InvalidDataException("Le solde de la cagnotte est invalide (NaN, infini ou négatif) : rien n'a été écrit.");
        }

        string contenu = JsonSerializer.Serialize(new CagnotteJson { Solde = _solde });
        EcrireFichier(chemin, contenu);
    }

    /// <summary>
    /// Dépose un montant dans la cagnotte (somme retirée du budget du mois affiché par
    /// l'appelant). Le montant doit être strictement positif et fini.
    /// </summary>
    /// <param name="montant">Montant à déposer.</param>
    /// <exception cref="ArgumentException">Si le montant est nul, négatif, NaN ou infini.</exception>
    /// <exception cref="InvalidDataException">Si le nouveau solde déborde de la plage flottante.</exception>
    public void Deposer(float montant)
    {
        Validation.VerifierMontantPositif(montant);

        // Garde du même type que les totaux du mois : deux sommes proches de float.Max
        // déborderaient silencieusement vers +infini, corrompant le solde.
        if (float.IsInfinity(_solde + montant))
        {
            throw new InvalidDataException("Le solde de la cagnotte déborde de la plage flottante.");
        }

        _solde += montant;
    }

    /// <summary>
    /// Retire un montant de la cagnotte (somme réinjectée dans le budget du mois affiché par
    /// l'appelant). Le montant doit être strictement positif, fini et ne pas dépasser le solde.
    /// </summary>
    /// <param name="montant">Montant à retirer.</param>
    /// <exception cref="ArgumentException">Si le montant est nul, négatif, NaN, infini ou
    /// supérieur au solde de la cagnotte.</exception>
    public void Retirer(float montant)
    {
        Validation.VerifierMontantPositif(montant);

        if (montant > _solde)
        {
            throw new ArgumentException(
                $"Impossible de retirer {montant} € : le solde de la cagnotte est de {_solde} €.", nameof(montant));
        }

        _solde -= montant;
    }

    /// <summary>
    /// Indique si un solde de cagnotte est invalide : NaN, infini ou négatif.
    /// </summary>
    /// <param name="solde">Solde à vérifier.</param>
    private static bool EstSoldeInvalide(float solde) =>
        float.IsNaN(solde) || float.IsInfinity(solde) || solde < 0f;

    /// <summary>
    /// Lit le contenu du fichier ; retourne <c>null</c> si le fichier n'existe pas.
    /// La taille est bornée pour éviter qu'un fichier corrompu ou gonflé provoque une
    /// <see cref="OutOfMemoryException"/> non gérée lors du chargement.
    /// </summary>
    private static string? LireFichier(string chemin)
    {
        if (!File.Exists(chemin))
        {
            return null;
        }

        if (new FileInfo(chemin).Length > TailleMaximaleFichier)
        {
            throw new InvalidDataException(
                $"Le fichier JSON de la cagnotte est trop volumineux (limite {TailleMaximaleFichier / (1024 * 1024)} Mo).");
        }

        return File.ReadAllText(chemin);
    }

    /// <summary>
    /// Désérialise le contenu JSON vers le DTO de la cagnotte.
    /// </summary>
    private static CagnotteJson Deserialiser(string contenu)
    {
        try
        {
            return JsonSerializer.Deserialize<CagnotteJson>(contenu)
                   ?? throw new JsonException("Le JSON ne contient aucune donnée.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Le fichier JSON de la cagnotte est corrompu ou mal formé.", exception);
        }
    }

    /// <summary>
    /// Écrit le contenu JSON dans le fichier en créant le répertoire parent si nécessaire.
    /// L'écriture est atomique : contenu écrit dans un fichier temporaire puis déplacé par-dessus
    /// la cible, afin qu'une coupure en plein écriture ne tronque pas le fichier.
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
}