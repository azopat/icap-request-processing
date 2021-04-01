using Glasswall.Core.Engine.Common.PolicyConfig;
using Service.StoreMessages.Enums;
using System;

namespace Service
{
    public interface IFileProcessorConfig
    {
        string FileId { get; }
        string InputPath { get; }
        string OutputPath { get; }
        bool GenerateReport { get; }
        string ReplyTo { get; }
        TimeSpan ProcessingTimeoutDuration { get; set; }
        Guid PolicyId { get; }
        ContentManagementFlags ContentManagementFlags { get; }
        NcfsOption UnprocessableFileTypeAction { get; }
        NcfsOption GlasswallBlockedFilesAction { get; }
        string NcfsRoutingUrl { get; }
        string MessageBrokerUser { get; }
        string MessageBrokerPassword { get; }
        string AdaptationRequestQueueHostname { get; }
        int AdaptationRequestQueuePort { get; }
        string ArchiveAdaptationRequestQueueHostname { get; }
        int ArchiveAdaptationRequestQueuePort { get; }
        string TransactionEventQueueHostname { get; }
        int TransactionEventQueuePort { get; }
        string RebuildReportMessage { get; }
    }
}