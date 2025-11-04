using HHAutoClicker.DTO;
using Microsoft.Playwright;
using System.Threading.Tasks;

namespace HHAutoClicker.Services
{
    public sealed class VacancyResponderService
    {
        private readonly IPage _page;
        private readonly IBadWordsService _badWords;
        private readonly ISeenVacancies _seen;

        public VacancyResponderService(IPage page, IBadWordsService badWords, ISeenVacancies seen)
        {
            _page = page;
            _badWords = badWords;
            _seen = seen;
        }

        public async Task RespondAllAsync(string query, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var encoded = query.Replace(" ", "+");
            var url = $"https://hh.ru/search/vacancy?text={encoded}&order_by=publication_time&search_field=name";

            await _page.GotoAsync(url);

            var totalPages = await GetPagesCountAsync();

            for (int page = 0; page < totalPages; page++ )
            {
                ct.ThrowIfCancellationRequested();
                await RespondOnPageAsync(query, page, ct);
            }

        }

        private async Task RespondOnPageAsync(string query, int page, CancellationToken ct)
        {
            Console.WriteLine($"Страница: {page + 1}");
            var url = $"https://hh.ru/search/vacancy?text={query}&order_by=publication_time&search_field=name&page={page}";

            await _page.GotoAsync(url);
            var vacancies = _page.Locator("div[data-qa=\"vacancy-serp__vacancy\"]");
            var count = await vacancies.CountAsync();

            for (int i = 0; i < count; i++)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var vacancy = vacancies.Nth(i);
                    await TryRespondVacancy(vacancy, ct);
                }
                finally
                {
                    await _seen.SaveAsync();
                }
            }
        }

        private async Task TryRespondVacancy(ILocator vacancy, CancellationToken ct)
        {
            string oldUrl = _page.Url;
            string name = await vacancy.Locator("span[data-qa=\"serp-item__title-text\"]").TextContentAsync();
            string employer = (await vacancy.Locator("span[data-qa=\"vacancy-serp__vacancy-employer-text\"]").AllInnerTextsAsync())[0];
            var key = $"{employer}:{name}";

            if (_badWords.HasBadWord(name) || _seen.IsSeen(key))
            {
                return;
            }

            _seen.Add(key);

            var btn = vacancy.Locator("a[data-qa=\"vacancy-serp__vacancy_response\"]").First;

            try
            {
                await btn.ClickAsync(new() { Timeout = 1500 });
                Console.WriteLine($"Клик по кнопке {key}");
                await Task.Delay(1500, ct);
                await ClickIfExists("button[data-qa=\"relocation-warning-confirm\"]", ct);
            }
            catch
            {
                return; 
            }

            await ClickIfExists("button[data-qa=\"response-popup-close\"]", ct);
            await ClickIfExists("button[data-qa=\"chatik-close-chatik\"]", ct);

            if (oldUrl != _page.Url)
            {
                await Task.Delay(3000);
                await AnswerVacancyQuestions();
                await _page.GoBackAsync();
            }

            await _page.WaitForTimeoutAsync(5000);
        }

        private async Task AnswerVacancyQuestions()
        {
            // TODO: сделать это потом с ИИ настроенным под меня
            var questions = _page.Locator("div[data-qa=\"task-body\"]");
            var questionsCount = await questions.CountAsync();

            for (int i = 0; i < questionsCount; i++)
            {
                var question = questions.Nth(i);
                var questionText = (await question.Locator("div[data-qa=\"task-question\"]").AllTextContentsAsync()).First();
                Console.WriteLine(questionText);

                var textArea = question.Locator("textarea[type=\"textarea\"]");
                await textArea.FillAsync("Я ни знаю ничиво");
            }

        }

        private async Task ClickIfExists(string locator, CancellationToken ct)
        {
            var btn = _page.Locator(locator);
            if (await btn.CountAsync() > 0)
            {
                await btn.ClickAsync(new() { Timeout = 1500 });
                await Task.Delay(1500, ct);
            }
        }

        private async Task<int> GetPagesCountAsync()
        {
            var pages = _page.Locator("a[data-qa=\"pager-page\"]").Last;
            string? pagesCount = await pages.TextContentAsync();
            return int.TryParse(pagesCount, out var pagesInt) ? pagesInt : 0;
        }
    }
}
