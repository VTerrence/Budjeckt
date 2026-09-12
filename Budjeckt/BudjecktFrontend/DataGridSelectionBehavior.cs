using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BudjecktFrontend;

/// <summary>
/// Propriété attachée synchronisant la collection multi-sélection d'un <see cref="DataGrid"/>
/// (lecture seule dans WPF) vers une <see cref="ObservableCollection{T}"/> du contexte de données.
/// Avec <c>SelectionMode="Extended"</c>, chaque changement de sélection (clic simple, Shift+clic,
/// Ctrl+clic ou cliquer-glisser) remplit la collection cible au complet.
/// </summary>
public static class DataGridSelectionBehavior
{
    /// <summary>Collection cible remplie avec les lignes sélectionnées du DataGrid.</summary>
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(ObservableCollection<ApercuFacture>),
            typeof(DataGridSelectionBehavior),
            new PropertyMetadata(null, OnSelectedItemsChanged));

    /// <summary>Obtient la collection cible associée à un élément.</summary>
    /// <param name="element">Élément porteur (le DataGrid).</param>
    /// <returns>Collection cible, ou <c>null</c> si aucune n'est attachée.</returns>
    public static ObservableCollection<ApercuFacture>? GetSelectedItems(DependencyObject element)
        => (ObservableCollection<ApercuFacture>?)element.GetValue(SelectedItemsProperty);

    /// <summary>Associe la collection cible à un élément.</summary>
    /// <param name="element">Élément porteur (le DataGrid).</param>
    /// <param name="valeur">Collection cible, ou <c>null</c> pour se détacher.</param>
    public static void SetSelectedItems(DependencyObject element, ObservableCollection<ApercuFacture>? valeur)
        => element.SetValue(SelectedItemsProperty, valeur);

    /// <summary>
    /// Réagit au remplacement de la collection cible : l'ancienne collection (appartenant au
    /// ViewModel) est vidée pour ne pas garder des lignes d'un autre DataGrid, l'abonnement à
    /// <see cref="DataGrid.SelectionChanged"/> est réaccroché, puis la cible est resynchronisée
    /// avec la sélection actuelle.
    /// </summary>
    private static void OnSelectedItemsChanged(DependencyObject porteur, DependencyPropertyChangedEventArgs arguments)
    {
        if (porteur is not DataGrid grille)
        {
            return;
        }

        if (arguments.OldValue is ObservableCollection<ApercuFacture> ancienne)
        {
            ancienne.Clear();
        }

        grille.SelectionChanged -= SurSelectionChanged;
        if (arguments.NewValue is ObservableCollection<ApercuFacture> cible)
        {
            grille.SelectionChanged += SurSelectionChanged;
            Synchroniser(grille, cible);
        }
    }

    private static void SurSelectionChanged(object sender, SelectionChangedEventArgs arguments)
    {
        if (sender is DataGrid grille && GetSelectedItems(grille) is { } cible)
        {
            Synchroniser(grille, cible);
        }
    }

    private static void Synchroniser(DataGrid grille, ObservableCollection<ApercuFacture> cible)
    {
        cible.Clear();
        foreach (ApercuFacture ligne in grille.SelectedItems.OfType<ApercuFacture>())
        {
            cible.Add(ligne);
        }
    }
}