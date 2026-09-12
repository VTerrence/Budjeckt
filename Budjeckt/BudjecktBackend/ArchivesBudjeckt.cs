namespace Budjeckt;

/// <summary>
/// Gère le répertoire des données multi-années : détection des années disponibles,
/// création d'une nouvelle année, ouverture ou création, suppression, et migration du
/// fichier hérité <c>depenses.json</c> vers <c>depenses-&lt;année&gt;.json</c>.
/// </summary>
public static class ArchivesBudjeckt
{
    private const string Prefixe = "depenses-";
    private const string Suffixe = ".json";

    /// <summary>Borne inférieure (inclusive) de la plage d'années plausibles.</summary>
    private const int AnneeMin = 1900;

    /// <summary>Borne supérieure (inclusive) de la plage d'années plausibles.</summary>
    private const int AnneeMax = 2200;

    /// <summary>Année actuelle (horloge système).</summary>
    public static int AnneeCourante() => DateTime.Today.Year;

    /// <summary>
    /// Retourne la liste des années disponibles dans le dossier, triées par ordre décroissant.
    /// Seuls les fichiers <c>depenses-&lt;année&gt;.json</c> dont l'année est un nombre entier
    /// dans la plage plausible (1900–2200) sont retenus.
    /// </summary>
    /// <param name="dossier">Chemin du dossier de données.</param>
    public static List<int> AnneesExistantes(string dossier)
    {
        List<int> annees = new();

        if (!Directory.Exists(dossier))
        {
            return annees;
        }

        foreach (string chemin in Directory.EnumerateFiles(dossier, Prefixe + "*" + Suffixe))
        {
            string nom = Path.GetFileName(chemin);
            string numero = nom[(Prefixe.Length)..^Suffixe.Length];

            if (int.TryParse(numero, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int annee)
                && annee >= AnneeMin && annee <= AnneeMax)
            {
                annees.Add(annee);
            }
        }

        annees.Sort();
        annees.Reverse();
        return annees;
    }

    /// <summary>
    /// Ouvre l'année demandée : si le fichier existe déjà, il est chargé et validé, et l'année
    /// déclarée dans le fichier doit correspondre à celle demandée ; sinon, une année vierge
    /// est créée et sauvegardée sur disque.
    /// </summary>
    /// <param name="dossier">Chemin du dossier de données.</param>
    /// <param name="annee">Année à ouvrir ou créer.</param>
    /// <returns>Le modèle <see cref="Budjeckt"/> chargé ou créé.</returns>
    /// <exception cref="InvalidDataException">Si le fichier existant est corrompu, mal formé, ou si
    /// l'année qu'il déclare ne correspond pas à celle demandée.</exception>
    public static Budjeckt OuvrirOuCreerAnnee(string dossier, int annee)
    {
        string chemin = Path.Combine(dossier, Budjeckt.NomFichierPourAnnee(annee));
        Budjeckt modele = new(annee.ToString());

        if (File.Exists(chemin))
        {
            modele.ChargerJson(chemin);

            if (!string.Equals(modele.Annee.Trim(), annee.ToString(), StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Le fichier {Path.GetFileName(chemin)} déclare l'année « {modele.Annee} » au lieu de « {annee} ».");
            }
        }
        else
        {
            modele.SauvegarderJson(chemin);
        }

        return modele;
    }

    /// <summary>
    /// Supprime définitivement le fichier de données de l'année demandée
    /// (<c>depenses-&lt;année&gt;.json</c>). Ne fait rien si l'année est hors de la plage
    /// plausible (1900-2200), si le fichier est absent, ou si l'accès au fichier échoue.
    /// </summary>
    /// <param name="dossier">Chemin du dossier de données.</param>
    /// <param name="annee">Année à supprimer.</param>
    /// <returns><c>true</c> si le fichier a effectivement été supprimé ; <c>false</c> sinon
    /// (année hors plage, fichier absent, ou suppression impossible).</returns>
    public static bool SupprimerAnnee(string dossier, int annee)
    {
        if (annee < AnneeMin || annee > AnneeMax)
        {
            return false;
        }

        string chemin = Path.Combine(dossier, Budjeckt.NomFichierPourAnnee(annee));
        if (!File.Exists(chemin))
        {
            return false;
        }

        try
        {
            File.Delete(chemin);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Migre le fichier hérité <c>depenses.json</c> vers <c>depenses-&lt;année&gt;.json</c>
    /// d'après le champ « Annee » du fichier. Si l'année ne peut être lue ou est hors de la
    /// plage plausible (1900-2100), ou si le fichier n'existe pas, rien n'est fait. Si la cible
    /// existe déjà, l'ancien fichier est renommé avec le suffixe <c>-legacy</c> (horodaté si ce
    /// nom est déjà pris) au lieu d'être écrasé.
    /// </summary>
    /// <param name="dossier">Chemin du dossier de données.</param>
    /// <returns><c>true</c> si une migration a eu lieu ; <c>false</c> sinon (fichier absent,
    /// illisible, année hors plage, ou déplacement impossible).</returns>
    public static bool MigrerFichierHerite(string dossier)
    {
        string heritage = Path.Combine(dossier, Budjeckt.NomFichierHerite);
        int? annee = Budjeckt.LireAnneeDuFichier(heritage);

        if (annee is null)
        {
            return false;
        }

        try
        {
            string cible = Path.Combine(dossier, Budjeckt.NomFichierPourAnnee(annee.Value));

            if (File.Exists(cible))
            {
                // Éviter l'écrasement : renommer en -legacy, horodaté en cas de collision.
                string nomSansSuffixe = Path.GetFileNameWithoutExtension(cible);
                string nomLegacy = $"{nomSansSuffixe}-legacy{Suffixe}";
                string cheminLegacy = Path.Combine(dossier, nomLegacy);

                if (File.Exists(cheminLegacy))
                {
                    cheminLegacy = Path.Combine(dossier, $"{nomSansSuffixe}-legacy-{DateTime.Now:yyyyMMdd-HHmmss}{Suffixe}");
                }

                File.Move(heritage, cheminLegacy);
            }
            else
            {
                File.Move(heritage, cible);
            }

            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
