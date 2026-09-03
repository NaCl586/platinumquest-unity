using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server.Replay
{
    public class ReplayQueue
    {
        private readonly string _path;

        private readonly List<PendingReplay> _items = new List<PendingReplay>();
        public int Count => _items.Count;

        public ReplayQueue()
        {
            ReplayPaths.EnsureDirectories();

            _path = ReplayPaths.QueueFile;

            Load();
        }

        public bool HasPendingReplay => _items.Count > 0;

        public void Enqueue(PendingReplay replay)
        {
            int index = _items.FindIndex(x => x.ScoreId == replay.ScoreId);

            if (index >= 0)
            {
                _items[index] = replay;
            }
            else
            {
                _items.Add(replay);
            }

            Save();
        }

        public void Update(PendingReplay replay)
        {
            int index = _items.FindIndex(x => x.ScoreId == replay.ScoreId);

            if (index < 0)
                return;

            _items[index] = replay;

            Save();
        }

        private void Save()
        {
            ReplayQueueData data = new ReplayQueueData { Items = _items };

            string json = JsonUtility.ToJson(data, true);

            File.WriteAllText(_path, json);
        }

        private void Load()
        {
            if (!File.Exists(_path))
                return;

            string json = File.ReadAllText(_path);

            ReplayQueueData? data = JsonUtility.FromJson<ReplayQueueData>(json);

            _items.Clear();

            if (data != null)
            {
                _items.AddRange(data.Items);
            }
        }

        public void RemovePendingReplayForUserAndLevel(int userId, string level)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                PendingReplay replay = _items[i];

                if (replay.UserId != userId)
                    continue;

                if (!string.Equals(replay.Level, level, System.StringComparison.Ordinal))
                {
                    continue;
                }

                string filePath = ReplayPaths.GetAbsolutePath(replay.FileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);

                    Debug.Log(
                        $"Deleted obsolete pending replay: " + $"UserId={userId}, File={filePath}"
                    );
                }

                _items.RemoveAt(i);
            }

            Save();
        }

        public PendingReplay? PeekForUser(int userId)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].UserId == userId)
                    return _items[i];
            }

            return null;
        }

        public void Remove(PendingReplay replay)
        {
            if (replay == null)
                return;

            if (_items.Remove(replay))
            {
                Save();
            }
        }
    }
}
