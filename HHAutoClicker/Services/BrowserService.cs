using Microsoft.Playwright;

namespace HHAutoClicker.Services
{

    public interface IBrowserService : IAsyncDisposable
    {
        IPage Page { get; }
        Task InitAsync();
    }

    public sealed class BrowserService : IBrowserService
    {
        private IPlaywright _playwright;
        private IBrowserContext _context;
        private IBrowser _browser;
        public IPage Page { get; set; }
        private readonly string _storageStatePath;

        public BrowserService(string storageStatePath)
        {
            _storageStatePath = storageStatePath;
        }

        public async Task InitAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = false });

            if (!File.Exists(_storageStatePath))
            {
                _context = await _browser.NewContextAsync();
                Console.WriteLine("Сначала нужно залогиниться");
            }
            else
            {
                _context = await _browser.NewContextAsync(new()
                {
                    StorageStatePath = _storageStatePath,
                });
            }

                Page = await _context.NewPageAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_context != null)
            {
                await _context.CloseAsync();
            }
            
            if (_playwright != null)
            {
                _playwright.Dispose();
            }
        }   
    }
}
