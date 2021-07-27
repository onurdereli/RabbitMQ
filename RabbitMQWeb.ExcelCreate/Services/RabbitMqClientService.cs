using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace RabbitMQWeb.ExcelCreate.Services
{
    public class RabbitMqClientService : IDisposable
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        public static string ExchangeName = "ExcelDirectExchange";
        public static string RoutingExcel = "excel-route-file";
        public static string QueueName = "queue-excel-file";
        private readonly ILogger<RabbitMqClientService> _logger;

        public RabbitMqClientService(ConnectionFactory connectionFactory, ILogger<RabbitMqClientService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public IModel Connect()
        {
            _connection = _connectionFactory.CreateConnection();
            if (_channel != null && _channel.IsOpen)
            {
                return _channel;
            }
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName, type: "direct", true, false);

            _channel.QueueDeclare(QueueName, true, false, false, null);

            _channel.QueueBind(exchange: ExchangeName, queue: QueueName, routingKey: RoutingExcel);

            _logger.LogInformation("RabbitMQ ile bağlantı kuruldu");

            return _channel;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _channel?.Dispose();

            _logger.LogInformation("RabbitMQ ile bağlantı koptu");
        }
    }
}
