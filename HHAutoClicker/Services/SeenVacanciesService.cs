using HHAutoClicker.DTO;
using Newtonsoft.Json;


namespace HHAutoClicker.Services
{
    public interface ISeenVacancies
    {
        bool IsSeen(string key);
        void Add(string key);
        Task SaveAsync();
    }

    public sealed class SeenVacanciesService : ISeenVacancies
    {
        private readonly string _path;
        private readonly HashSet<string> _seen;
        private readonly SeenVacanciesModel _seenModel;

        public SeenVacanciesService(string path)
        {
            _path = path;
            _seenModel = new SeenVacanciesModel
            {
                seen = new()
            };

            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                _seen = (JsonConvert.DeserializeObject<SeenVacanciesModel>(json)).seen ?? new();
            }
            else
            {
                File.WriteAllText(_path, JsonConvert.SerializeObject(_seenModel));
                _seen = new();
            }
        }

        public bool IsSeen(string key) => _seen.Contains(key);

        public void Add(string key) => _seen.Add(key);

        public async Task SaveAsync()
        {
            _seenModel.seen = _seen;
            await File.WriteAllTextAsync(_path, JsonConvert.SerializeObject(_seenModel, Formatting.Indented));
        }
    }
}
