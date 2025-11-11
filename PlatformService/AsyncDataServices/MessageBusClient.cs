using PlatformService.Dtos;
using RabbitMQ.Client;
using System;
namespace PlatformService.AsyncDataServices
{
    public class MessageBusClient : IMessageBusClient
    {
        private readonly IConfiguration _configuration;
        private  IConnection _connection;
        private  IChannel _channel;
        public MessageBusClient(IConfiguration configuration)
        {
            _configuration = configuration;
            CreateRabbitMQConnection().GetAwaiter().GetResult();
            
        }
        public void PublishNewPlatform(PlatformPublishedDto platformPublishedDto)
        {
            var message = System.Text.Json.JsonSerializer.Serialize(platformPublishedDto);
            if (_connection.IsOpen)
            {
                Console.WriteLine("--> RabbitMQ Connection Open, sending message...");
          
                SendMessageAsync(message).GetAwaiter().GetResult();
            }
            else
            {
                Console.WriteLine("--> RabbitMQ connection is closed, not sending");
            }
        }

        private async Task SendMessageAsync(string message)
        {
            var body = System.Text.Encoding.UTF8.GetBytes(message);

            await _channel.BasicPublishAsync(exchange: "trigger", routingKey: string.Empty, body: body);
            Console.WriteLine($"--> We have sent {message}");

        }
        private async Task CreateRabbitMQConnection()
        {

            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQHost"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQPort"] ?? "5672")
            };
            try
            {
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
                await _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout);
                _connection.ConnectionShutdownAsync += async (sender, args) =>
                {
                    Console.WriteLine("--> RabbitMQ Connection Shutdown");
                };
                Console.WriteLine("--> RabbitMQ Connected!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
            }
        }

        public async Task Dispose()
        {
            Console.WriteLine("MessageBus Disposed");
            if (_channel.IsOpen)
            {
                await _channel.CloseAsync();
                await _connection.CloseAsync();
            }
        }
    }
}