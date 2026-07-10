using UnityEngine;

namespace Services.Storage
{
    public class PlayerPrefsStorage
    {
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public float GetFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);

        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);

        public void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);

        public bool GetBool(string key, bool defaultValue = false) =>
            PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);

        public void Save() => PlayerPrefs.Save();
    }
}