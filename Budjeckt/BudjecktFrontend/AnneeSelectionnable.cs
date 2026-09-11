using CommunityToolkit.Mvvm.ComponentModel;

namespace BudjecktFrontend;

/// <summary>
/// Ligne de la liste multi-sélection des années à supprimer : une année et son état de
/// coche (bindé à une <c>CheckBox</c> dans le panneau de suppression).
/// </summary>
public sealed partial class AnneeSelectionnable : ObservableObject
{
    /// <summary>Année proposée à la suppression.</summary>
    public int Annee { get; }

    /// <summary>Libellé affiché (ex. « Année 2027 »).</summary>
    public string Libelle => $"Année {Annee}";

    /// <summary><c>true</c> si la case de cette année est cochée (marquée pour suppression).</summary>
    [ObservableProperty]
    private bool _estCochee;

    /// <summary>
    /// Construit une ligne sélectionnable pour la suppression d'une année.
    /// </summary>
    /// <param name="annee">Année concernée.</param>
    public AnneeSelectionnable(int annee)
    {
        Annee = annee;
    }
}