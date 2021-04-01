namespace Service
{
    public interface IFileManager
    {
        void DeleteFile(string path);
        bool FileExists(string path);
        byte[] ReadFile(string path);
        void WriteFile(string path, byte[] data);
    }
}
