using Microsoft.Extensions.DependencyInjection;

namespace CryptoPortfolioTracker.Services
{
    public interface IDialog<TResult>
    {
        TResult? Result { get; }
    }
    public interface IDialogService
    {
        XamlRoot? XamlRoot { get; set; }

        Task<TResult?> ShowAsync<TDialog, TResult>(params object[] parameters) where TDialog : ContentDialog, IDialog<TResult>;
        Task<ContentDialogResult> ShowMessageAsync(string title, string message, string primaryButtonText = "OK", string closeButtonText = "");
    }


    public class DialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Settings _settings;
        private readonly SemaphoreSlim _dialogSemaphore = new(1, 1);

        public XamlRoot? XamlRoot { get; set; }

        public DialogService(IServiceProvider serviceProvider, Settings settings)
        {
            _serviceProvider = serviceProvider;
            _settings = settings;
        }

        public async Task<TResult?> ShowAsync<TDialog, TResult>(params object[] parameters) where TDialog : ContentDialog, IDialog<TResult>
        {
            if (XamlRoot == null)
                throw new InvalidOperationException("XamlRoot not initialized.");

            // Wait until any previously opened dialog has closed.
            await _dialogSemaphore.WaitAsync();

            try
            {
                //var dialog = _serviceProvider.GetRequiredService<TDialog>();
                // Create dialog:
                // - Resolve registered dependencies through DI.
                // - Pass runtime parameters to the constructor.
                var dialog = ActivatorUtilities.CreateInstance<TDialog>(_serviceProvider, parameters);

                dialog.RequestedTheme = _settings.AppTheme;
                dialog.XamlRoot = XamlRoot;

                await dialog.ShowAsync();

                return dialog.Result;
            }
            finally
            {
                // Always release the semaphore, even if an exception occurs.
                _dialogSemaphore.Release();
            }
        }

        // For simple message dialogs
        public async Task<ContentDialogResult> ShowMessageAsync(string title, string message, string primaryButtonText = "OK", string closeButtonText = "")
        {
            if (XamlRoot == null)
                throw new InvalidOperationException("XamlRoot not initialized.");

            await _dialogSemaphore.WaitAsync();

            try
            {
                ContentDialog dialog = new()
                {
                    Title = title,
                    Content = new TextBlock
                    {
                        Text = message,
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap
                    },
                    CloseButtonText = closeButtonText,
                    PrimaryButtonText = primaryButtonText,
                    RequestedTheme = _settings.AppTheme,
                    XamlRoot = XamlRoot,

                };

                return await dialog.ShowAsync();
            }
            finally
            {
                _dialogSemaphore.Release();
            }
        }
    }



}
