using QuestPDF.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Reporting.Services;
public interface IQuestPdfService
{
    Task<byte[]> RenderAsync(IDocument document, CancellationToken cancellationToken = default);
    Task SaveAsync(IDocument document, string filePath, CancellationToken cancellationToken = default);
}