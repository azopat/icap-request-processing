using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Service.Messaging
{
    public class ArchiveRequestSender : IArchiveRequestSender, IDisposable
    {
        private const string Exchange = "adaptation-exchange";
        private const string RoutingKey = "archive-adaptation-request";

        private readonly ILogger<ArchiveRequestSender> _logger;

        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        private bool disposedValue;

        public ArchiveRequestSender(IFileProcessorConfig fileProcessorConfig, ILogger<ArchiveRequestSender> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (fileProcessorConfig == null) throw new ArgumentNullException(nameof(fileProcessorConfig));
            _connectionFactory = new ConnectionFactory()
            {
                HostName = fileProcessorConfig.ArchiveAdaptationRequestQueueHostname,
                Port = fileProcessorConfig.ArchiveAdaptationRequestQueuePort,
                UserName = fileProcessorConfig.MessageBrokerUser,
                Password = fileProcessorConfig.MessageBrokerPassword
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _channel?.Dispose();
                    _connection?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Send(string fileId, string fileType, string sourceLocation, string rebuiltLocation, string replyTo)
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            var headers = new Dictionary<string, object>()
                {
                    { "archive-file-id", fileId },
                    { "archive-file-type", fileType },
                    { "source-file-location", sourceLocation },
                    { "rebuilt-file-location", rebuiltLocation },
                    { "outcome-reply-to", replyTo }
                };

            var replyProps = _channel.CreateBasicProperties();
            replyProps.Headers = headers;
            _channel.BasicPublish(Exchange, RoutingKey, basicProperties: replyProps);

            _logger.LogInformation($"Sent Archive Request, FileId: {fileId}, SourceFileLocation: {sourceLocation}, " +
                $"RebuiltFileLocation: {rebuiltLocation}, ReplyTo: {replyTo}");
        }
    }
}
