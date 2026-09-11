using System.Windows;
using System.Windows.Threading;

namespace BudjecktFrontend;

/// <summary>
/// Point d'entrée de l'application WPF. Un gestionnaire d'exceptions non gérées affiche
/// un message générique (jamais de stack trace) et évite la boîte de dialogue système.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// S'abonne aux exceptions non gérées sur le thread d'interface avant l'initialisation :
    /// toute erreur inattendue est signalée proprement sans exposer de détail technique.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "Une erreur interne est survenue. Vos dernières données n'ont pas été enregistrées.",
                "Budjeckt",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}