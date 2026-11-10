using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Dialogs
{
    public partial class BaseDialog : ContentDialog
    {
        protected readonly ILocalizer loc = Localizer.Get();

        public BaseDialog()
        {
            

        }

    }
}
