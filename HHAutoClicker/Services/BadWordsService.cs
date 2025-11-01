
namespace HHAutoClicker.Services
{
    public interface IBadWordsService
    {
        bool HasBadWord(string name);
    }

    public sealed class BadWordsService : IBadWordsService
    {
        private string _path;
        private List<string> _badWords;
        public BadWordsService(string badWordsPath)
        {
            _path = badWordsPath;
            _badWords = new();

            if (File.Exists(_path))
            {
                var badWords = File.ReadAllLines(_path);
                _badWords.AddRange(badWords);
            }
        }

        public bool HasBadWord(string key) => _badWords.Any(x => key.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
}
