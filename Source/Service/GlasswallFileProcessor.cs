using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;
using Microsoft.Extensions.Logging;
using System;

namespace Service
{
    public class GlasswallFileProcessor : IGlasswallFileProcessor
    {
        private readonly IFileTypeDetector _fileTypeDetector;
        private readonly IFileProtector _fileProtector;
        private readonly IFileAnalyser _fileAnalyser;
        private readonly IFileProcessorConfig _config;
        private readonly ILogger<GlasswallFileProcessor> _logger;

        public GlasswallFileProcessor(IFileTypeDetector fileTypeDetector, IFileProtector fileProtector, IFileAnalyser fileAnalyser, IFileProcessorConfig config, ILogger<GlasswallFileProcessor> logger)
        {
            _fileTypeDetector = fileTypeDetector ?? throw new ArgumentNullException(nameof(fileTypeDetector));
            _fileProtector = fileProtector ?? throw new ArgumentNullException(nameof(fileProtector));
            _fileAnalyser = fileAnalyser ?? throw new ArgumentNullException(nameof(fileAnalyser));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public FileTypeDetectionResponse GetFileType(byte[] file)
        {
            var fileType = _fileTypeDetector.DetermineFileType(file);

            _logger.LogInformation($"FileId: {_config.FileId}, Filetype Detected: {fileType.FileTypeName}");

            return fileType;
        }

        public string AnalyseFile(byte[] file, string fileType)
        {
            return _fileAnalyser.GetReport(GetDefaultContentManagement(), fileType, file);
        }

        public byte[] RebuildFile(byte[] file, string fileType)
        {
            var protectedFileResponse = _fileProtector.GetProtectedFile(_config.ContentManagementFlags ?? GetDefaultContentManagement(), fileType, file);

            if (!string.IsNullOrWhiteSpace(protectedFileResponse.ErrorMessage) || protectedFileResponse.ProtectedFile == null)
            {
                if (protectedFileResponse.IsDisallowed)
                    _logger.LogInformation($"File {_config.FileId} is disallowed by Content Management Policy");

                _logger.LogInformation($"File {_config.FileId} could not be rebuilt: {protectedFileResponse.ErrorMessage}.");

                return null;
            }
            else
            {
                _logger.LogInformation($"FileId: {_config.FileId}, successfully rebuilt.");

                return protectedFileResponse.ProtectedFile;
            }
        }

        private ContentManagementFlags GetDefaultContentManagement()
        {
            return new ContentManagementFlags
            {
                ExcelContentManagement = new ExcelContentManagement
                {
                    DynamicDataExchange = ContentManagementFlagAction.Sanitise,
                    EmbeddedFiles = ContentManagementFlagAction.Sanitise,
                    EmbeddedImages = ContentManagementFlagAction.Sanitise,
                    ExternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    InternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    Macros = ContentManagementFlagAction.Sanitise,
                    Metadata = ContentManagementFlagAction.Sanitise,
                    ReviewComments = ContentManagementFlagAction.Sanitise
                },
                PdfContentManagement = new PdfContentManagement
                {
                    Acroform = ContentManagementFlagAction.Sanitise,
                    ActionsAll = ContentManagementFlagAction.Sanitise,
                    EmbeddedFiles = ContentManagementFlagAction.Sanitise,
                    EmbeddedImages = ContentManagementFlagAction.Sanitise,
                    ExternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    InternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    Javascript = ContentManagementFlagAction.Sanitise,
                    Metadata = ContentManagementFlagAction.Sanitise,
                    Watermark = "Glasswall Processed"
                },
                PowerPointContentManagement = new PowerPointContentManagement
                {
                    EmbeddedFiles = ContentManagementFlagAction.Sanitise,
                    EmbeddedImages = ContentManagementFlagAction.Sanitise,
                    ExternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    InternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    Macros = ContentManagementFlagAction.Sanitise,
                    Metadata = ContentManagementFlagAction.Sanitise,
                    ReviewComments = ContentManagementFlagAction.Sanitise
                },
                WordContentManagement = new WordContentManagement
                {
                    DynamicDataExchange = ContentManagementFlagAction.Sanitise,
                    EmbeddedFiles = ContentManagementFlagAction.Sanitise,
                    EmbeddedImages = ContentManagementFlagAction.Sanitise,
                    ExternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    InternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    Macros = ContentManagementFlagAction.Sanitise,
                    Metadata = ContentManagementFlagAction.Sanitise,
                    ReviewComments = ContentManagementFlagAction.Sanitise
                }
            };
        }
    }
}
