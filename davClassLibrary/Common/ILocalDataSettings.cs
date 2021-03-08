namespace davClassLibrary.Common
{
    public interface ILocalDataSettings
    {
        void Set(string key, string value);
        void Set(string key, int value);
        void Set(string key, long value);

        string GetString(string key);
        int GetInt(string key);
        long GetLong(string key);
        void Remove(string key);
    }
}
