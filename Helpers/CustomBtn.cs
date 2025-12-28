
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace TemperatureMonitor.Helpers
{
    public partial class CustomBtn : AppBarButton
    {
        public CustomBtn()
        {
            this.DefaultStyleKey = typeof(AppBarButton);
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }
    }
}
