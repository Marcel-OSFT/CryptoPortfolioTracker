
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Helpers
{
    public class CustomBtn : AppBarButton
    {
        public CustomBtn()
        {
            this.DefaultStyleKey = typeof(AppBarButton);
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            
        }
    }
}
