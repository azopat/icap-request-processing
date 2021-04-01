using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Service.StoreMessages.Events;
using System;
using System.Text;

namespace Service.Messaging
{
    public class TransactionEventSender : ITransactionEventSender, IDisposable
    {
        private const string Exchange = "adaptation-exchange";
        private const string RoutingKey = "transaction-event";

        private bool disposedValue;

        private readonly ILogger<TransactionEventSender> _logger;

        private readonly IModel _channel;
        private readonly IConnection _connection;

        public TransactionEventSender(IFileProcessorConfig fileProcessorConfig, ILogger<TransactionEventSender> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (fileProcessorConfig == null) throw new ArgumentNullException(nameof(fileProcessorConfig));
            var connectionFactory = new ConnectionFactory()
            {
                HostName = fileProcessorConfig.TransactionEventQueueHostname,
                Port = fileProcessorConfig.TransactionEventQueuePort,
                UserName = fileProcessorConfig.MessageBrokerUser,
                Password = fileProcessorConfig.MessageBrokerPassword
            };
            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _logger.LogInformation($"TransactionEventSender Connection established to {fileProcessorConfig.TransactionEventQueueHostname}");
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

        public void Send(Event transactionEvent)
        {
            _channel.ExchangeDeclare(Exchange, "direct", true);

            var message = Encoding.UTF8.GetBytes(transactionEvent.ToJson());

            _channel.BasicPublish(Exchange, RoutingKey, null, message);
            _logger.LogInformation($"Sent Transaction Event, FileId: {transactionEvent.FileId}, EventId: {transactionEvent.EventId}");
        }
    }
}
