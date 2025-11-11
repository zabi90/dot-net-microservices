
using System.Text;
using CommandsService.EventProcessing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class MessageBusSubscriber : BackgroundService
{
    private readonly IEventProcessor _eventProcessor;
    private readonly IConfiguration _configuration;

    private IConnection _connection;
    private IChannel _channel;

    private string _queueName;

    public MessageBusSubscriber(IConfiguration configuration, IEventProcessor eventProcessor)
    {
        _eventProcessor = eventProcessor;
        _configuration = configuration;
        InititalizeRabbitMQ().GetAwaiter().GetResult();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        Console.WriteLine(" [*] Waiting for logs.");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" Recevied new message from Bus : {message}");
            _eventProcessor.ProcessEvent(message);
            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(_queueName, autoAck: true, consumer: consumer);
    }

    private async Task InititalizeRabbitMQ()
    {
        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RabbitMQHost"],
            Port = int.Parse(_configuration["RabbitMQPort"])
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout);
        // declare a server-named queue
        QueueDeclareOk queueDeclareResult = await _channel.QueueDeclareAsync();
        _queueName = queueDeclareResult.QueueName;
        await _channel.QueueBindAsync(queue: _queueName, exchange: "trigger", routingKey: string.Empty);
        Console.WriteLine("--> Listenting on the Message Bus...");
        _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShitdown;
    }

    private async Task RabbitMQ_ConnectionShitdown(object sender, ShutdownEventArgs e)
    {
        Console.WriteLine("--> Connection Shutdown");
    }

    public override void Dispose()
    {
        if (_channel.IsOpen)
        {
            _channel.CloseAsync();
            _connection.CloseAsync();
        }

        base.Dispose();
    }
}