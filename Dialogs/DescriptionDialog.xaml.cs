using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Dialogs
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DescriptionDialog : ContentDialog
    {
        public DescriptionDialog(string description)
        {
            this.InitializeComponent();

            Run run = new Run();
            run.Text = description;
            par.Inlines.Clear();
            par.Inlines.Add(run);

        }
    }
}
