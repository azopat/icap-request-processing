using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Messaging;
using Service.StoreMessages.Enums;
using Service.StoreMessages.Events;
using Service.Messaging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Service.ErrorReport;
using System.Text;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Service
{
    public class TransactionEventProcessor : ITransactionEventProcessor
    {
        private readonly IGlasswallFileProcessor _fileProcessor;
        private readonly IGlasswallVersionService _glasswallVersionService;
        private readonly IOutcomeSender _outcomeSender;
        private readonly ITransactionEventSender _transactionEventSender;
        private readonly IArchiveRequestSender _archiveRequestSender;
        private readonly IFileManager _fileManager;
        private readonly IErrorReportGenerator _errorReportGenerator;
        private readonly IFileProcessorConfig _config;
        private readonly ILogger<TransactionEventProcessor> _logger;

        private readonly TimeSpan _processingTimeoutDuration;

        private readonly List<FileType> _archiveTypes = new List<FileType>() { FileType.Zip, FileType.Rar, FileType.Tar, FileType.SevenZip, FileType.Gzip };

        public TransactionEventProcessor(IGlasswallFileProcessor fileProcessor, IGlasswallVersionService versionService, 
            IOutcomeSender outcomeSender, ITransactionEventSender transactionEventSender, IArchiveRequestSender archiveRequestSender,
            IFileManager fileManager, IErrorReportGenerator errorReportGenerator, IFileProcessorConfig config, ILogger<TransactionEventProcessor> logger)
        {
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
            _glasswallVersionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
            _outcomeSender = outcomeSender ?? throw new ArgumentNullException(nameof(outcomeSender));
            _transactionEventSender = transactionEventSender ?? throw new ArgumentNullException(nameof(transactionEventSender));
            _archiveRequestSender = archiveRequestSender ?? throw new ArgumentNullException(nameof(archiveRequestSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _errorReportGenerator = errorReportGenerator ?? throw new ArgumentNullException(nameof(errorReportGenerator));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(config));

            _processingTimeoutDuration = _config.ProcessingTimeoutDuration;
        }

        public void Process()
        {
            var task = Task.Run(() =>
             {
                 return ProcessTransaction();
             });

            try
            {
                bool isCompletedSuccessfully = task.Wait(_processingTimeoutDuration);

                if (!isCompletedSuccessfully)
                {
                    _logger.LogError($"File Id: {_config.FileId} Processing exceeded {_processingTimeoutDuration}s");
                    ClearRebuiltStore(_config.OutputPath);
                    CreateErrorReport();
                    _outcomeSender.Send(FileOutcome.Failed, _config.FileId, _config.ReplyTo);
                }
            }
            catch (Exception e) 
            {
                _logger.LogError($"File Id: {_config.FileId} Processing threw exception {e.Message}");
                ClearRebuiltStore(_config.OutputPath);
                CreateErrorReport();
                _outcomeSender.Send(FileOutcome.Failed, _config.FileId, _config.ReplyTo);
            }
        }

        private Task ProcessTransaction()
        {
            var timestamp = DateTime.UtcNow;
            _transactionEventSender.Send(new NewDocumentEvent(_config.PolicyId.ToString(), RequestMode.Response, _config.FileId, timestamp));

            _logger.LogInformation($"File Id: {_config.FileId} Using Glasswall Version: {_glasswallVersionService.GetVersion()}");

            if (!_fileManager.FileExists(_config.InputPath))
            {
                throw new FileNotFoundException($"File Id: {_config.FileId} does not exist at {_config.InputPath}");
            }

            var file = _fileManager.ReadFile(_config.InputPath);

            var fileType = _fileProcessor.GetFileType(file);
            _transactionEventSender.Send(new FileTypeDetectionEvent(fileType.FileTypeName, _config.FileId, timestamp));

            string status;
            if (fileType.FileType == FileType.Unknown)
            {
                status = GetUnmanagedAction();

                _transactionEventSender.Send(new UnmanagedFileTypeActionEvent(status, _config.FileId, timestamp));
            }
            else if (_archiveTypes.Contains(fileType.FileType))
            {
                _archiveRequestSender.Send(_config.FileId, fileType.FileTypeName, _config.InputPath, _config.OutputPath, _config.ReplyTo);
                return Task.CompletedTask;
            }
            else
            {
                status = ProcessFile(file, fileType.FileTypeName, timestamp);
            }

            if (status == FileOutcome.Failed)
            {
                CreateErrorReport();
            }

            _outcomeSender.Send(status, _config.FileId, _config.ReplyTo);

            return Task.CompletedTask;
        }

        private void CreateErrorReport()
        {
            if (_config.GenerateReport)
            {
                var report = _errorReportGenerator.CreateReport(_config.FileId);
                _fileManager.WriteFile(_config.OutputPath, Encoding.UTF8.GetBytes(report));
            }
        }

        private string ProcessFile(byte[] file, string filetype, DateTime timestamp)
        {
            string status;

            var report = _fileProcessor.AnalyseFile(file, filetype);
            _transactionEventSender.Send(new AnalysisCompletedEvent(report, _config.FileId, timestamp));

            _transactionEventSender.Send(new RebuildStartingEvent(_config.FileId, timestamp));

            var rebuiltFile = _fileProcessor.RebuildFile(file, filetype);

            if (rebuiltFile == null)
            {
                status = GetBlockedAction();
                _transactionEventSender.Send(new BlockedFiletypeActionEvent(status, _config.FileId, timestamp));
            }
            else
            {
                _fileManager.WriteFile(_config.OutputPath, rebuiltFile);
                status = FileOutcome.Replace;
            }

            _transactionEventSender.Send(new RebuildCompletedEvent(status, _config.FileId, timestamp));

            return status;
        }

        private void ClearRebuiltStore(string path)
        {
            if (_fileManager.FileExists(path))
            {
                _fileManager.DeleteFile(path);
            }
        }

        private string GetUnmanagedAction()
        {
            // Will be extended to include Refer Action & Decision from NCFS Service
            return _config.UnprocessableFileTypeAction == NcfsOption.Block 
                ? FileOutcome.Failed 
                : FileOutcome.Unmodified;
        }

        private string GetBlockedAction()
        {
            // Will be extended to include Refer Action & Decision from NCFS Service
            return _config.GlasswallBlockedFilesAction == NcfsOption.Block
                ? FileOutcome.Failed
                : FileOutcome.Unmodified;
        }
    } 
}