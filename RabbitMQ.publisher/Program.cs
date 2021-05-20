using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Channels;
using RabbitMQ.Client;

namespace RabbitMQ.publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            using var connection = connectionFactory.CreateConnection();

            var channel = connection.CreateModel();

            var logsFanout = "logs-fanout";

            channel.ExchangeDeclare(logsFanout, durable: true, type: ExchangeType.Fanout);
            
            Enumerable.Range(1, 50).ToList().ForEach(x =>
            {
                // mesajlar byte dizin olarak alınır
                string message = $"Message {x}";
                var messageBody = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(logsFanout, "", null, messageBody);

                Console.WriteLine($"Mesaj gönderilmiştir. {message}");
            });

            Console.ReadLine();
        }
    }
}
