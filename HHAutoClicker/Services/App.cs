using Microsoft.Playwright;

namespace HHAutoClicker.Services
{
    public sealed class App
    {
        private readonly IBrowserService _browserService;
        private readonly IBadWordsService _badWordsService;
        private readonly ISeenVacancies _seenVacancies;
        private readonly string _storageStatePath;

        public App()
        {
            var baseDir = Directory.GetCurrentDirectory();
            _storageStatePath = Path.Combine(baseDir, "Data", "creds.json");

            _browserService = new BrowserService(_storageStatePath);
            _badWordsService = new BadWordsService(Path.Combine(baseDir, "Data", "badwords.txt"));
            _seenVacancies = new SeenVacanciesService(Path.Combine(baseDir, "Data", "seen.json"));
        }

        public async Task LoginAsync(IPage page)
        {
            await page.GotoAsync("https://hh.ru/account/login?role=applicant&backurl=%2F&hhtmFrom=main",
                new PageGotoOptions { Timeout = 60000 });

            var joinBtn = page.Locator("button[data-qa=\"submit-button\"]");
            await joinBtn.ClickAsync();

            var phoneInput = page.Locator("input[data-qa=\"magritte-phone-input-national-number-input\"]");
            Console.WriteLine("Введите номер телефона в виде: 960000000");
            var phoneNumber = await Console.In.ReadLineAsync();
            await phoneInput.FillAsync(phoneNumber);

            var submitBth = page.Locator("button[data-qa=\"submit-button\"]");
            await submitBth.ClickAsync();

            Console.Write("Введите код из СМС: ");
            var phoneCode = await Console.In.ReadLineAsync();

            var pincodeInput = page.Locator("input[data-qa=\"applicant-login-input-otp\"], input[data-qa=\"magritte-pincode-input-field\"]");
            await pincodeInput.FillAsync(phoneCode);

            await pincodeInput.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 120_000 });

            await page.Context.StorageStateAsync(new()
            {
                Path = _storageStatePath,
                IndexedDB = true
            });

            Console.Clear();
            Console.WriteLine("Вы успешно вошли!");
        }

        public async Task RunAsync()
        {
            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\n⛔ Отмена операции...");
                Environment.Exit(0);
            };

            await _browserService.InitAsync();
            var responder = new VacancyResponderService(_browserService.Page, _badWordsService, _seenVacancies);

            var handle = WinApi.GetConsoleWindow();
            WinApi.SetForegroundWindow(handle);

            while (true)
            {
                Console.Clear();

                Console.WriteLine("1. Логин\n2. Откликаться\n3. Выход");
                var cmd = await Console.In.ReadLineAsync();

                Console.Clear();

                try
                {
                    switch (cmd)
                    {
                        case "1":
                            await LoginAsync(_browserService.Page);
                            continue;
                        case "2":
                            Console.Write("Введите ключевое слово: ");
                            var query = await Console.In.ReadLineAsync() ?? "";

                            await responder.RespondAllAsync(query, cts.Token);
                            continue;
                        case "3":
                            Environment.Exit(0);
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}