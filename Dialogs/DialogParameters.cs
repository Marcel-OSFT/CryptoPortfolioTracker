using LanguageExt.ClassInstances;

namespace CryptoPortfolioTracker.Dialogs
{
    public record TransactionDialogParameters(DialogAction dialogAction, Transaction? transaction = null);
    public record AccountDialogParameters(DialogAction dialogAction, Account? account = null);
    public record NarrativeDialogParameters(DialogAction dialogAction, Narrative? narrative = null);
    public record PortfolioDialogParameters(DialogAction dialogAction, Portfolio? portfolio = null);
    public record AddCoinDialogParameters(DialogAction dialogAction, List<string> searchList, List<Narrative> narratives);
    public record AddPrereleaseCoinDialogParameters(DialogAction dialogAction);
    public record ContentDialogParameters(string title, string message, string primaryButtonText = "OK", string closeButtonText = "");
    public record CopyPortfolioDialogParameters(Portfolio portfolio);
    public record AddNoteDialogParameters(Coin coin);
    public record AddPriceLevelsDialogParameters(Coin coin);
    public record AssignNarrativeDialogParameters(Coin coin, List<Narrative> narratives);
    public record DescriptionDialogParameters(Coin coin);


}
