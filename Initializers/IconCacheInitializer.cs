using CryptoPortfolioTracker.Services;
using Serilog;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Initializers;
public class IconCacheService
{
    private readonly string _iconsFolderPath;
    private readonly PortfolioService _portfolioService;
    private readonly ILogger _logger;

    public IconCacheService(string iconsFolderPath, PortfolioService portfolioService, ILogger logger)
    {
        _iconsFolderPath = iconsFolderPath;
        _portfolioService = portfolioService;
        _logger = logger;
    }

    public async Task CacheLibraryIconsAsync()
    {
        if (Directory.Exists(_iconsFolderPath))
        {
            foreach (var file in Directory.GetFiles(_iconsFolderPath))
                File.Delete(file);
        }
        else
        {
            Directory.CreateDirectory(_iconsFolderPath);
        }

        var context = _portfolioService.Context;
        var coins = context?.Coins.Where(coin => !string.IsNullOrEmpty(coin.ImageUri)).ToList();

        if (coins != null)
        {
            var tasks = coins.Select(async coin =>
            {
                var fileName = Path.GetFileName(coin.ImageUri.Split('?')[0]);
                if (fileName != "QuestionMarkBlue.png")
                {
                    var iconPath = Path.Combine(_iconsFolderPath, fileName);
                    if (!File.Exists(iconPath))
                    {
                        if (!await RetrieveCoinIconAsync(coin.ImageUri, iconPath))
                        {
                            _logger?.Warning("Failed to cache icon for {0}", coin.Name);
                        }
                    }
                }
            });
            await Task.WhenAll(tasks);
        }
    }

    private async Task<bool> RetrieveCoinIconAsync(string imageUri, string iconPath)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(imageUri);
            if (!response.IsSuccessStatusCode) return false;
            await using var fs = new FileStream(iconPath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
            return true;
        }
        catch
        {
            return false;
        }
    }
}