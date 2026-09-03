using System.Collections.Generic;
using System.IO;
using Server.Replay;
using UnityEngine;

namespace Server.Score
{
    public class ScoreQueue
    {
        private readonly string _path;

        private readonly List<PendingScore> _items = new List<PendingScore>();

        public int Count => _items.Count;

        public ScoreQueue()
        {
            ReplayPaths.EnsureDirectories();

            _path = ReplayPaths.ScoreQueueFile;

            Load();
        }

        public bool HasPendingScore => _items.Count > 0;

        public PendingScore? Peek()
        {
            if (_items.Count == 0)
                return null;

            return _items[0];
        }

        public void Enqueue(PendingScore score)
        {
            if (score == null)
                return;

            _items.Add(score);

            Save();
        }

        public void Update(PendingScore score)
        {
            int index = _items.IndexOf(score);

            if (index < 0)
                return;

            _items[index] = score;

            Save();
        }

        public void Dequeue()
        {
            if (_items.Count == 0)
                return;

            _items.RemoveAt(0);

            Save();
        }

        public void Clear()
        {
            _items.Clear();

            Save();
        }

        private void Save()
        {
            ScoreQueueData data = new ScoreQueueData { Items = _items };

            string json = JsonUtility.ToJson(data, true);

            File.WriteAllText(_path, json);
        }

        private void Load()
        {
            if (!File.Exists(_path))
                return;

            string json = File.ReadAllText(_path);

            ScoreQueueData? data = JsonUtility.FromJson<ScoreQueueData>(json);

            _items.Clear();

            if (data != null && data.Items != null)
            {
                _items.AddRange(data.Items);
            }
        }

        public PendingScore? PeekForUser(int userId)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].UserId == userId)
                    return _items[i];
            }

            return null;
        }

        public void Remove(PendingScore score)
        {
            if (score == null)
                return;

            if (_items.Remove(score))
            {
                Save();
            }
        }
    }
}
