using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.subscriber
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

            var model = connection.CreateModel();

            var helloQueue = "hello-queue";
            //model.QueueDeclare(helloQueue, true, false, false);
            //global true; subscriberlara toplamda kaça bölüneceğini belirtir
            model.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(model);
            //autoAck = kuyruktan bir mesajı gönderildiğinde direk siliyor, false işleminde haberleşme bittikten sonra kontrolllü silinmeyi sağlar
            model.BasicConsume(helloQueue, false, consumer);

            consumer.Received += (sender, eventArgs) =>
            {
                var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                Thread.Sleep(1500);
                Console.WriteLine("Gelen Mesaj: " + message);

                model.BasicAck(eventArgs.DeliveryTag, false);
            };
            Console.ReadLine();
        }
    }
}
