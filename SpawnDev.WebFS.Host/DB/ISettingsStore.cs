namespace SpawnDev.DB
{
    public interface ISettingsStore
    {
        T GetSetting<T>(string id);
        T GetSetting<T>(string id, T defaultValue);
        void RemoveSetting(string id);
        void SetSetting<T>(string id, T value);
        bool SettingExists(string id);
    }
}
