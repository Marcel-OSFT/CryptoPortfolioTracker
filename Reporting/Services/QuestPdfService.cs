using LanguageExt.Pretty;
using QuestPDF.Companion;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Reporting.Services;
public class QuestPdfService : IQuestPdfService
{
    public QuestPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> RenderAsync(IDocument document, CancellationToken cancellationToken = default)
    {
        // Offload blocking rendering to a background thread
        return Task.Run(() =>
        {
            using var ms = new MemoryStream();
            document.GeneratePdf(ms); // blocking API
            return ms.ToArray();
        }, cancellationToken);
    }

    public async Task SaveAsync(IDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        var bytes = await RenderAsync(document, cancellationToken).ConfigureAwait(false);
        document.ShowInCompanion();
        var dir = Path.GetDirectoryName(filePath) ?? ".";
        Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken).ConfigureAwait(false);
    }
}
