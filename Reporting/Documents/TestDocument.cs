// Reports/WhatsNewDocument.cs
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using QuestPDF.Fluent;

namespace CryptoPortfolioTracker.Reporting.Documents;
public class TestDocument : IDocument
{
    private readonly string _title;
    public TestDocument(string title) => _title = title;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(20));
            page.Header().Text(_title).SemiBold().FontSize(36).FontColor(Colors.Blue.Medium);
            page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
            {
                x.Spacing(20);
                x.Item().Text(Placeholders.LoremIpsum());
                x.Item().Image(Placeholders.Image(200, 110));
            });
            page.Footer().AlignCenter().Text(x => { x.Span("Page "); x.CurrentPageNumber(); });
        });
    }
}
