using System;
using System.Collections.Generic;
using UnityEngine;

namespace Services.Time
{
    [Serializable]
    public class PersistentTimerData
    {
        public string Key;
        public double TargetTimestamp;
        public double DurationSeconds;
    }

    public class TimerService
    {
        private const string SAVE_KEY = "UniversalTimers";
        private readonly Dictionary<string, double> _activeTimers = new();
        private readonly Dictionary<string, double> _durations = new();
        private readonly HashSet<string> _finishedTimers = new();
        public event Action<string> OnTimerFinished;

        public void Init() => LoadTimers();

        internal void Update() => CheckTimers();
        
        public bool CheckDailyVisit()
        {
            const string lastVisitKey = "LastDailyVisitDate";
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string lastVisit = PlayerPrefs.GetString(lastVisitKey, "");

            Debug.Log($"[DailyVisit] Today: {today}, Last: {lastVisit}");

            if (lastVisit != today)
            {
                PlayerPrefs.SetString(lastVisitKey, today);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        public TimeSpan TimeUntilNextDailyEvent()
        {
            DateTime now = DateTime.UtcNow;
            DateTime nextDay = now.Date.AddDays(1);
            return nextDay - now;
        }

        public void StartTimer(string key, double durationSeconds)
        {
            double targetTimestamp = GetCurrentTimestamp() + durationSeconds;
            _activeTimers[key] = targetTimestamp;
            _durations[key] = durationSeconds;
            _finishedTimers.Remove(key);
            SaveTimers();
        }

        public bool HasActiveTimer(string key) => _activeTimers.ContainsKey(key);

        public TimeSpan GetRemainingTime(string key)
        {
            if (!_activeTimers.TryGetValue(key, out double timer))
                return TimeSpan.Zero;

            double remaining = timer - GetCurrentTimestamp();
            remaining = Math.Max(0, remaining);
            return TimeSpan.FromSeconds(remaining);
        }

        public int GetCyclesPassed(string key)
        {
            if (!_activeTimers.ContainsKey(key) || !_durations.ContainsKey(key))
                return 0;

            double targetTimestamp = _activeTimers[key];
            double duration = _durations[key];

            if (duration <= 0) return 0;

            double timePassed = GetCurrentTimestamp() - (targetTimestamp - duration);
            if (timePassed <= 0) return 0;

            var cycles = (int)Math.Floor(timePassed / duration);

            if (cycles > 0)
            {
                _activeTimers[key] = targetTimestamp + (cycles * duration);
                SaveTimers();
            }

            return cycles;
        }

        private void CheckTimers()
        {
            double now = GetCurrentTimestamp();
            List<string> keysToFinish = null;

            foreach (var pair in _activeTimers)
            {
                if (now >= pair.Value && !_finishedTimers.Contains(pair.Key))
                {
                    keysToFinish ??= new List<string>();
                    keysToFinish.Add(pair.Key);
                }
            }

            if (keysToFinish != null)
            {
                foreach (string key in keysToFinish)
                {
                    _finishedTimers.Add(key);
                    OnTimerFinished?.Invoke(key);
                }
            }
        }

        private double GetCurrentTimestamp()
        {
            return DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
        }

        #region LifeCycle & Persistence

        internal void OnApplicationPause(bool pause)
        {
            if (pause) SaveTimers();
        }

        internal void OnApplicationQuit() => SaveTimers();
        internal void OnDisable() => SaveTimers();
        internal void OnDestroy() => SaveTimers();

        private void SaveTimers()
        {
            List<PersistentTimerData> dataList = new();
            foreach (var pair in _activeTimers)
            {
                double duration = _durations.GetValueOrDefault(pair.Key, 0);
                dataList.Add(new PersistentTimerData
                    { Key = pair.Key, TargetTimestamp = pair.Value, DurationSeconds = duration });
            }

            string json = JsonUtility.ToJson(new Wrapper { Timers = dataList });
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadTimers()
        {
            _activeTimers.Clear();
            _durations.Clear();

            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

            string json = PlayerPrefs.GetString(SAVE_KEY);
            var wrapper = JsonUtility.FromJson<Wrapper>(json);
            if (wrapper?.Timers != null)
            {
                foreach (var timerData in wrapper.Timers)
                {
                    _activeTimers[timerData.Key] = timerData.TargetTimestamp;
                    _durations[timerData.Key] = timerData.DurationSeconds;
                }
            }
        }

        [Serializable]
        public class Wrapper
        {
            public List<PersistentTimerData> Timers;
        }

        #endregion
    }
}