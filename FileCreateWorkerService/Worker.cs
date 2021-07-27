using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using FileCreateWorkerService.Models;
using FileCreateWorkerService.Services;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;

namespace FileCreateWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RabbitMqClientService _rabbitMqClientService;
        private readonly IServiceProvider _serviceProvider;
        private IModel _channel;

        public Worker(ILogger<Worker> logger, RabbitMqClientService rabbitMqClientService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _rabbitMqClientService = rabbitMqClientService;
            _serviceProvider = serviceProvider;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _channel = _rabbitMqClientService.Connect();
            _channel.BasicQos(0, 1, false);
            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            _channel.BasicConsume(RabbitMqClientService.QueueName, false, consumer);

            consumer.Received += Consumer_Received;

            return Task.CompletedTask;
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            await Task.Delay(5000);
            var createExcelMessage = JsonSerializer.Deserialize<CreateExcelMessage>(Encoding.UTF8.GetString(@event.Body.ToArray()));

            using MemoryStream ms = new();
            XLWorkbook wb = new();
            DataSet ds = new();
            ds.Tables.Add(GetTable("Products"));
            wb.Worksheets.Add(ds);
            wb.SaveAs(ms);

            MultipartFormDataContent multipartFormDataContent = new();
            multipartFormDataContent.Add(new ByteArrayContent(ms.ToArray()), "file", Guid.NewGuid().ToString() + ".xlsx");

            var baseUrl = "https://localhost:5001";

            using(HttpClient httpClient = new())
            {
                var response = await httpClient.PostAsync($"{baseUrl}/api/files?fileId={createExcelMessage.FileId}", multipartFormDataContent);

                if(response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"File (Id: {createExcelMessage.FileId} wa created by successful");
                    _channel.BasicAck(@event.DeliveryTag, false);
                }
            }
        }

        private DataTable GetTable(string tableName)
        {
            List<Product> products;
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AdventureWorks2019Context>();
                products = context.Products.ToList();
            }
            DataTable table = new(tableName);

            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("ProductNumber", typeof(string));
            table.Columns.Add("Color", typeof(string));

            products.ForEach(product =>
            {
                table.Rows.Add(product.ProductId, product.Name, product.ProductNumber, product.Color);
            });

            return table;
        }
    }
}
