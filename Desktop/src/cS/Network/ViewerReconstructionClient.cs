using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;
using System.Text.Json;

using mLogger;


namespace Dplus_Desktop
{
    public class MqttWorker
    {
        private readonly string _broker;
        private readonly int _port;
        private readonly string _topic;

        private IMqttClient _client;
        private MQTTnet.Client.MqttClientOptions _options;
        private bool _lastAttemptFailed = false;

        private CancellationTokenSource _cts;

        private Logger logger = Logger.Instance;

        public event Func<MqttApplicationMessageReceivedEventArgs, Task> OnMessage;

        public MqttWorker(string broker, int port, string topic)
        {
            _broker = broker;
            _port = port;
            _topic = topic;
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(_broker, _port)
                .WithCleanStart()
                .Build();

            _client.ApplicationMessageReceivedAsync += async e =>
            {
                if (OnMessage != null)
                    await OnMessage.Invoke(e);
            };

            _client.ConnectedAsync += async e =>
            {
                logger.Log(LogLevel.INFO, "MQTT Worker", "MQTT Connected");

                await _client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(_topic)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build());

                logger.Log(LogLevel.INFO, "MQTT Worker", $"Subscribed to {_topic}");
            };

            _client.DisconnectedAsync += e =>
            {
                logger.Log(LogLevel.WARN, "MQTT Worker", "MQTT Disconnected");
                return Task.CompletedTask;
            };

            // 🔁 START THE MAIN LOOP
            _ = Task.Run(() => RunAsync(_cts.Token));
        }

        public async Task StopAsync()
        {
            _cts.Cancel();

            if (_client?.IsConnected == true)
            {
                await _client.DisconnectAsync();
            }
        }

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!_client.IsConnected)
                {
                    try
                    {
                        if (!_lastAttemptFailed)
                            logger.Log(LogLevel.INFO, "MQTT Worker", "Attempting MQTT connection...");

                        await _client.ConnectAsync(_options, token);
                        _lastAttemptFailed = false;
                    }
                    catch (Exception ex)
                    {
                        if (!_lastAttemptFailed)
                        {
                            logger.Log(LogLevel.WARN, "MQTT Worker", $"MQTT unavailable: {ex.Message}");
                        }

                        _lastAttemptFailed = true;
                        await Task.Delay(2000, token);
                        continue;
                    }
                }

                // Stay alive while connected
                await Task.Delay(1000, token);
            }
        }
    }
}
