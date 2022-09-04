using System;
using System.Dynamic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mono.Options;
using MQTTnet;
using MQTTnet.Client;

string mqttHost = null;
string mqttUsername = null;
string mqttPassword = null;
string host = null;
byte count = 1;
string mqttPrefix = "pylontech";
bool debug = false;
bool showHelp = false;

var jsonOptions = new JsonSerializerOptions
{
    Converters =
    {
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
    },
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var options = new OptionSet
{
    {"m|mqttServer=", "MQTT Server", x => mqttHost = x},
    {"mqttuser=", "MQTT username", x => mqttUsername = x},
    {"mqttpass=", "MQTT password", x => mqttPassword = x},
    {"i|ip=", "TCP2serial bridge hostname or ip", x => host = x},
    {"c|count=", $"number of modules - defaults to {count}", (byte x) => count = x},
    {"p|prefix=", $"MQTT topic prefix - defaults to {mqttPrefix}", x => mqttPrefix = x.TrimEnd('/')},
    {"d|debug", "enable debug logging", x => debug = x != null},
    {"h|help", "show help", x => showHelp = x != null},
};

try
{
    if (options.Parse(args).Count > 0)
    {
        showHelp = true;
    }
}
catch (OptionException ex)
{
    Console.Error.Write("pylontech2mqtt: ");
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("Try 'pylontech2mqtt --help' for more information");
    return;
}
if (showHelp || mqttHost is null || host is null || count <= 0)
{
    options.WriteOptionDescriptions(Console.Out);
    return;
}

using (var cts = new CancellationTokenSource())
{
    Console.CancelKeyPress += (s, e) =>
    {
        cts.Cancel();
        e.Cancel = true;
    };
    using (var mqttClient = new MqttFactory().CreateMqttClient())
    {
        var mqttOptionBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttHost)
            .WithClientId("pylontech2mqtt");
        if (!String.IsNullOrEmpty(mqttUsername) || !String.IsNullOrEmpty(mqttPassword))
        {
            mqttOptionBuilder = mqttOptionBuilder.WithCredentials(mqttUsername, mqttPassword);
        }
        var mqttOptions = mqttOptionBuilder.Build();
        mqttClient.DisconnectedAsync += (async _ =>
        {
            Console.Error.WriteLine("mqtt disconnected - reconnecting in 5 seconds");
            await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
            try
            {
                await mqttClient.ConnectAsync(mqttOptions, cts.Token);
            }
            catch
            {
                Console.Error.WriteLine("reconnect failed");
            }
        });
        await mqttClient.ConnectAsync(mqttOptions, cts.Token);
        await RunConnectionsAsync(mqttClient, host, count, mqttPrefix, cts.Token);
    }
}

Task PublishJsonAsync(IMqttClient client, string topic, object message, CancellationToken cancellationToken)
{
    return PublishAsync(client, topic, JsonSerializer.Serialize(message, jsonOptions), "application/json", cancellationToken);
}

static Task PublishTextAsync(IMqttClient client, string topic, string message, CancellationToken cancellationToken)
{
    return PublishAsync(client, topic, message, "text/plain", cancellationToken);

}
static Task PublishAsync(IMqttClient client, string topic, string message, string contentType, CancellationToken cancellationToken)
{
    if (!client.IsConnected)
    {
        Console.Error.WriteLine($"MQTT disconnected - dropping '{topic}': '{message}'");
        return Task.CompletedTask;
    }
    var payload = new MqttApplicationMessageBuilder()
        .WithTopic(topic)
        .WithPayload(message)
        .WithContentType(contentType)
        .Build();
    return client.PublishAsync(payload, cancellationToken);
}

async Task RunConnectionsAsync(IMqttClient mqttClient, string host, byte count, string mqttPrefix, CancellationToken cancellationToken)
{
    var client = new TcpClient();
    await client.ConnectAsync(host, 8888);
    var channel = client.GetStream();
    client.ReceiveTimeout = 1000;
    var writer = new StreamWriter(channel, Encoding.ASCII) { NewLine = "\r" };
    var reader = new RealStreamReader(channel, Encoding.ASCII);
    while (!cancellationToken.IsCancellationRequested)
    {
        for (byte i = 0; i < count; i++)
        {
            var version = await SendAsync(writer, reader, new Pylonframe
            {
                Version = new(0, 0),
                Address = (byte)(i+2),
                CommandInformation = CommandInformation.ProtocolVersion,
            }, cancellationToken);
            await PublishTextAsync(mqttClient, $"{mqttPrefix}/{i}/version", version.Version.ToString(), cancellationToken);
            var frame = await SendAsync(writer, reader, new Pylonframe
            {
                Version = version.Version,
                Address = (byte)(i+2),
                CommandInformation = CommandInformation.ManufacturerInfo,
            }, cancellationToken);
            var m = new PylonManufacturerInfo(frame.Info);
            await PublishJsonAsync(mqttClient, $"{mqttPrefix}/{i}/manufacturer", new {m.Battery, software = m.SoftwareVersion, m.Manufacturer} , cancellationToken);
            frame = await SendAsync(writer, reader, new Pylonframe
            {
                Version = version.Version,
                Address = (byte)(i+2),
                CommandInformation = CommandInformation.FirmwareInfo,
                Info = new byte[] { (byte)(i+2) }
            }, cancellationToken);
            var f = new PylonFirmwareInfo(frame.Info);
            await PublishJsonAsync(mqttClient, $"{mqttPrefix}/{i}/firmware", new {mainline = f.MainlineVersion, manufacture = f.ManufactureVersion} , cancellationToken);
            frame = await SendAsync(writer, reader, new Pylonframe
            {
                Version = version.Version,
                Address = (byte)(i+2),
                CommandInformation = CommandInformation.Serialnumber,
                Info = new byte[] { (byte)(i+2) }
            }, cancellationToken);
            var s = new PylonSerialnumber(frame.Info);
            await PublishTextAsync(mqttClient, $"{mqttPrefix}/{i}/serialnumber", s.Serialnumber, cancellationToken);
            frame = await SendAsync(writer, reader, new Pylonframe
            {
                Version = version.Version,
                Address = (byte)(i+2),
                CommandInformation = CommandInformation.SystemParameterFixedPoint,
            }, cancellationToken);
            var p = new PylonSystemParameter(frame.Info);
            await PublishJsonAsync(mqttClient, $"{mqttPrefix}/{i}/system", 
                new
                {
                    p.CellHighVoltageLimit,
                    p.CellLowVoltageLimit,
                    p.CellUnderVoltageLimit,
                    p.ChargeCurrentLimit,
                    p.ChargeHighTemperatureLimit,
                    p.ChargeLowTemperatureLimit,
                    p.DischargeCurrentLimit,
                    p.DischargeHighTemperatureLimit,
                    p.DischargeLowTemperatureLimit,
                    p.ModuleHighVoltageLimit,
                    p.ModuleLowVoltageLimit,
                    p.ModuleUnderVoltageLimit,
                } , cancellationToken);
            await PublishJsonAsync(mqttClient, $"{mqttPrefix}/{i}/info",
                new
                {
                    UnreadAlarmValueChange = p.InfoFlags.HasFlag(PylonInfoFlag.UnreadAlarmValueChange),
                    UnreadSwitchingValueChange = p.InfoFlags.HasFlag(PylonInfoFlag.UnreadSwitchingValueChange)
                }, cancellationToken);
            frame = await SendAsync(writer, reader, new Pylonframe
            {
                Version = version.Version,
                Address = (byte)(i+2),
                CommandInformation = CommandInformation.GetChargeDischargeManagementInfo,
                Info = new byte[] { (byte)(i+2) }
            }, cancellationToken);
            var c = new PylonChargeDischargeManagementInfo(frame.Info);
            await PublishJsonAsync(mqttClient, $"{mqttPrefix}/{i}/system",
                new
                {
                    c.ChargeVoltageLimit,
                    c.DischargeVoltageLimit,
                    c.ChargeCurrentLimit,
                    c.DischargeCurrentLimit,
                    ChargeEnabled = c.Status.HasFlag(ChargeDischargeStatus.ChargeEnabled),
                    DischargeEnabled = c.Status.HasFlag(ChargeDischargeStatus.DischargeEnabled),
                    ChargeImmediately1 = c.Status.HasFlag(ChargeDischargeStatus.ChargeImmediately1),
                    ChargeImmediately2 = c.Status.HasFlag(ChargeDischargeStatus.ChargeImmediately2),
                    FullChargeRequest = c.Status.HasFlag(ChargeDischargeStatus.FullChargeRequest),
                }, cancellationToken);
            frame = await SendAsync(writer, reader, new Pylonframe
            {
                Version = version.Version,
                Address = (byte)(i+2),
                CommandInformation = CommandInformation.AnalogValueFixedPoint,
                Info = new byte[] { (byte)(i+2) }
            }, cancellationToken);
            var v = new PylonAnalogValueFixedPoint(frame.Info);

            dynamic values = new ExpandoObject();
            values.bmsTemperature = v.BmsTemperature;
            values.avgTemperatureCell1to4 = v.AvgTemperatureCell1to4;
            values.avgTemperatureCell5to8 = v.AvgTemperatureCell5to8;
            values.avgTemperatureCell9to12 = v.AvgTemperatureCell9to12;
            values.avgTemperatureCell13to15 = v.AvgTemperatureCell13to15;
            values.current = v.Current;
            values.moduleVoltage = v.ModuleVoltage;
            values.cycleNumber = v.CycleNumber;
            
            if (v.UserDefined == 2)
            {
                values.remainingCapacity = v.RemainingCapacity1;
                values.totalCapacity = v.TotalCapacity1;
            }
            else if (v.UserDefined == 4)
            {
                values.remainingCapacity = v.RemainingCapacity2;
                values.totalCapacity = v.TotalCapacity2;
            }
            int id = 1;
            foreach (var voltage in v.Voltages)
            {
                ((IDictionary<string, object>)values)[$"cellVoltage{id}"] = voltage;
                id++;
            }
            await PublishJsonAsync(mqttClient, $"{mqttPrefix}/{i}/analog", values, cancellationToken);
            frame = await SendAsync(writer, reader, new Pylonframe
            {
                Version = version.Version,
                Address = (byte)(i+2),
                CommandInformation = CommandInformation.AlarmInfo,
                Info = new byte[] { (byte)(i+2) }
            }, cancellationToken);
            var a = new PylonAlarmInfo(frame.Info);
            var alarm = new
            {
                a.BmsTemperature,
                a.TemperatureCell1to4,
                a.TemperatureCell5to8,
                a.TemperatureCell9to12,
                a.TemperatureCell13to15,
                a.MosfetTemperature,
                a.ChargeCurrent,
                a.ModuleVoltage,
                a.DischargeCurrent,
                Status1 = new
                {
                    ModuleUnderVoltage = a.Status1.HasFlag(AlarmStatus1.ModuleUnderVoltage),
                    ChargeOverTemperature = a.Status1.HasFlag(AlarmStatus1.ChargeOverTemperature),
                    DischargeOverTemperature = a.Status1.HasFlag(AlarmStatus1.DischargeOverTemperature),
                    DischargeOverCurrent = a.Status1.HasFlag(AlarmStatus1.DischargeOverCurrent),
                    ChargeOverCurrent = a.Status1.HasFlag(AlarmStatus1.ChargeOverCurrent),
                    CellUnderVoltage = a.Status1.HasFlag(AlarmStatus1.CellUnderVoltage),
                    ModuleOverVoltage = a.Status1.HasFlag(AlarmStatus1.ModuleOverVoltage),
                },
                Status2 = new
                {
                    PreMosfet = a.Status2.HasFlag(AlarmStatus2.PreMosfet),
                    ChargeMosfet = a.Status2.HasFlag(AlarmStatus2.ChargeMosfet),
                    DischargeMosfet = a.Status2.HasFlag(AlarmStatus2.DischargeMosfet),
                    UsingBatteryModulePower = a.Status2.HasFlag(AlarmStatus2.UsingBatteryModulePower),
                },
                Status3 = new
                {
                    EffectiveChargeCurrent = a.Status3.HasFlag(AlarmStatus3.EffectiveChargeCurrent),
                    EffectiveDischargeCurrent = a.Status3.HasFlag(AlarmStatus3.EffectiveDischargeCurrent),
                    Heater = a.Status3.HasFlag(AlarmStatus3.Heater),
                    FullyCharged = a.Status3.HasFlag(AlarmStatus3.FullyCharged),
                    Buzzer = a.Status3.HasFlag(AlarmStatus3.Buzzer),
                },
                CellErrors = new
                {
                    Cell1 = a.Status4.HasFlag(AlarmCellError4.Cell1),
                    Cell2 = a.Status4.HasFlag(AlarmCellError4.Cell2),
                    Cell3 = a.Status4.HasFlag(AlarmCellError4.Cell3),
                    Cell4 = a.Status4.HasFlag(AlarmCellError4.Cell4),
                    Cell5 = a.Status4.HasFlag(AlarmCellError4.Cell5),
                    Cell6 = a.Status4.HasFlag(AlarmCellError4.Cell6),
                    Cell7 = a.Status4.HasFlag(AlarmCellError4.Cell7),
                    Cell8 = a.Status4.HasFlag(AlarmCellError4.Cell8),
                    Cell9 = a.Status5.HasFlag(AlarmCellError5.Cell9),
                    Cell10 = a.Status5.HasFlag(AlarmCellError5.Cell10),
                    Cell11 = a.Status5.HasFlag(AlarmCellError5.Cell11),
                    Cell12 = a.Status5.HasFlag(AlarmCellError5.Cell12),
                    Cell13 = a.Status5.HasFlag(AlarmCellError5.Cell13),
                    Cell14 = a.Status5.HasFlag(AlarmCellError5.Cell14),
                    Cell15 = a.Status5.HasFlag(AlarmCellError5.Cell15),
                    Cell16 = a.Status5.HasFlag(AlarmCellError5.Cell16),
                },
                CellVoltages = new ExpandoObject()
            };
            for (int j = 0; j < a.CellCount; j++)
            {
                ((IDictionary<string, object>)alarm.CellVoltages)[$"cell{j + 1}"] = a.CellVoltage[i];
            }
            await PublishJsonAsync(mqttClient, $"{mqttPrefix}/{i}/alarm", alarm, cancellationToken);
        }
        await Task.Delay(10000);
    }
}

async Task<Pylonframe> SendAsync(TextWriter writer, RealStreamReader reader, Pylonframe request, CancellationToken cancellationToken)
{
    await writer.WriteLineAsync(request.GetData());
    await writer.FlushAsync();
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
    try
    {
        var response = await reader.ReadLineAsync(linked.Token);
        var result = Pylonframe.Parse(response);
        if (result.ResponseInformation != ResponseInformation.Normal)
            throw new InvalidDataException();
        return result;
    }
    catch (OperationCanceledException)
    {
        if (timeout.Token.IsCancellationRequested) throw new TimeoutException();
        throw;
    }
}