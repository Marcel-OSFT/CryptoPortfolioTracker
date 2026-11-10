using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Dialogs
{
    public record RestorePortfolioDialogResult(Portfolio Portfolio, string FileName);
    public record CopyPortfolioDialogResult(string PortfolioName, bool NeedPartialCopy);
    public record AuthenticationDialogResult(string Password, AuthenticationDialogAction Action);


}
