using Glasswall.Core.Engine.Messaging;
using Service.StoreMessages.Enums;

namespace Service
{
    public interface IGlasswallFileProcessor
    {
        FileTypeDetectionResponse GetFileType(byte[] file);
        string AnalyseFile(byte[] file, string fileType);
        byte[] RebuildFile(byte[] file, string fileType);
    }
}
