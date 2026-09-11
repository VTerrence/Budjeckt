using System.Globalization;

namespace BudjecktFrontend;

/// <summary>
/// Formatage d'affichage commun à la vue modèle et aux lignes de l'historique.
/// </summary>
internal static class Formatage
{
    /// <summary>
    /// Formate un montant en euros avec deux décimales selon la culture courante (ex. « 1 250,00 € »).
    /// </summary>
    /// <param name="valeur">Montant à formater.</param>
    /// <returns>Chaîne prête à afficher.</returns>
    internal static string Montant(float valeur)
    {
        return valeur.ToString("N2", CultureInfo.CurrentCulture) + " €";
    }
}