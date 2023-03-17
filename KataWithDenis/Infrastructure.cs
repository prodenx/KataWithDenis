using System;
using System.Text.Json;

namespace KataWithDenis
{
    public class StateBasedInfrastructure<T>
    {
        private string _fileName;
        private IPersistable<T> _repo;

        public StateBasedInfrastructure(
            string fileName,
            IPersistable<T> repo)
        {
            _fileName = fileName;
            _repo = repo;
        }

        public async Task Persist()
        {
            string json = JsonSerializer.Serialize(await _repo.GetAll());

            File.WriteAllText(_fileName, json);
        }

        public void Load()
        {
            if (File.Exists(_fileName))
            {
                string json = File.ReadAllText(_fileName);
                List<T> entities = JsonSerializer.Deserialize<List<T>>(json);

                _repo.AddRange(entities);
            }
        }
    }

    public class EventBasedInfrastructure<T>
    {
        private string _fileName;
        private IPersistableEventStream<T> _repo;

        public EventBasedInfrastructure(
            string fileName,
            IPersistableEventStream<T> repo)
        {
            _fileName = fileName;
            _repo = repo;
        }

        public async Task Persist()
        {
            string json = JsonSerializer.Serialize(_repo.GetStreams());

            File.WriteAllText(_fileName, json);
        }

        public void Load()
        {
            if (File.Exists(_fileName))
            {
                string json = File.ReadAllText(_fileName);
                List<List<T>> eventStreams = JsonSerializer.Deserialize<List<List<T>>>(json) ?? new List<List<T>>();

                //_repo.Replay(eventStreams);
            }
        }
    }
}

