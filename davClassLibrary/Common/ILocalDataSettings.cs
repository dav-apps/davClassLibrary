namespace davClassLibrary.Common
{
    public interface ILocalDataSettings
    {
        void SetValue(string key, string value);
        string GetValue(string key);
    }
}
