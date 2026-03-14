using System.Buffers;
using System.CommandLine;
using System.Diagnostics;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Producer;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var addressOption = new Option<string>("--address", "-a")
        {
            Description = "Multicast address",
            DefaultValueFactory = _ => "239.1.2.3",
        };

        var portOption = new Option<int>("--port", "-p")
        {
            Description = "UDP port",
            DefaultValueFactory = _ => 3000,
        };

        var rateOption = new Option<int>("--rate", "-r")
        {
            Description = "Update rate in Hz",
            DefaultValueFactory = _ => 5,
        };

        var entitiesOption = new Option<int>("--entities", "-n")
        {
            Description = "Number of entities",
            DefaultValueFactory = _ => 1,
        };

        var exerciseOption = new Option<int>("--exercise", "-e")
        {
            Description = "Exercise ID",
            DefaultValueFactory = _ => 1,
        };

        var siteOption = new Option<int>("--site")
        {
            Description = "Site ID",
            DefaultValueFactory = _ => 1,
        };

        var appOption = new Option<int>("--app")
        {
            Description = "Application ID",
            DefaultValueFactory = _ => 1,
        };

        var patternOption = new Option<MovementPattern>("--pattern")
        {
            Description = "Movement pattern",
            DefaultValueFactory = _ => MovementPattern.Linear,
        };

        var speedOption = new Option<double>("--speed")
        {
            Description = "Entity speed in m/s",
            DefaultValueFactory = _ => 10.0,
        };

        var durationOption = new Option<int>("--duration", "-d")
        {
            Description = "Duration in seconds (0=infinite)",
            DefaultValueFactory = _ => 0,
        };

        var rootCommand = new RootCommand("SisoDis.NET DIS PDU Producer — sends Entity State PDUs over UDP multicast")
        {
            addressOption,
            portOption,
            rateOption,
            entitiesOption,
            exerciseOption,
            siteOption,
            appOption,
            patternOption,
            speedOption,
            durationOption,
        };

        rootCommand.SetAction(async (parseResult, _) =>
            {
                var address = parseResult.GetValue(addressOption) ?? "239.1.2.3";
                var port = parseResult.GetValue(portOption);
                var rate = parseResult.GetValue(rateOption);
                var entities = parseResult.GetValue(entitiesOption);
                var exercise = parseResult.GetValue(exerciseOption);
                var site = parseResult.GetValue(siteOption);
                var app = parseResult.GetValue(appOption);
                var pattern = parseResult.GetValue(patternOption);
                var speed = parseResult.GetValue(speedOption);
                var duration = parseResult.GetValue(durationOption);

                if (rate <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be greater than zero.");
                }

                if (entities <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(entities), "Entities must be greater than zero.");
                }

                Console.WriteLine("SisoDis.NET PDU Producer");
                Console.WriteLine("========================");
                Console.WriteLine($"Network:  {address}:{port}");
                Console.WriteLine($"Exercise: {exercise} | Site: {site} | App: {app}");
                Console.WriteLine($"Entities: {entities} | Rate: {rate} Hz | Pattern: {pattern}");
                Console.WriteLine($"Speed:    {speed} m/s");
                Console.WriteLine("Press Ctrl+C to stop...");
                Console.WriteLine();

                MulticastSender? sender = null;
                byte[]? buffer = null;
                var stopwatch = Stopwatch.StartNew();

                using var cts = new CancellationTokenSource();
                ConsoleCancelEventHandler? cancelHandler = null;
                cancelHandler = (_, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

                Console.CancelKeyPress += cancelHandler;

                try
                {
                    if (duration > 0)
                    {
                        cts.CancelAfter(TimeSpan.FromSeconds(duration));
                    }

                    sender = new MulticastSender(address, port);

                    var simulators = new List<EntitySimulator>(entities);
                    for (int i = 0; i < entities; i++)
                    {
                        var startPosition = new Vector3Double(0, i * 100, 0);
                        simulators.Add(new EntitySimulator(i + 1, pattern, startPosition, speed));
                    }

                    buffer = ArrayPool<byte>.Shared.Rent(1024);

                    var period = TimeSpan.FromMilliseconds(1000.0 / rate);
                    using var timer = new PeriodicTimer(period);

                    long tickCount = 0;
                    double deltaSeconds = 1.0 / rate;
                    byte simulationRef = (byte)exercise;
                    byte federationRef = (byte)site;

                    while (await timer.WaitForNextTickAsync(cts.Token))
                    {
                        tickCount++;

                        foreach (var entity in simulators)
                        {
                            entity.Tick(deltaSeconds);

                            var pdu = entity.BuildPdu(simulationRef, federationRef);
                            pdu.Serialize(buffer);
                            sender.Send(buffer.AsSpan(0, pdu.ComputedLength()));
                        }

                        if (tickCount % rate == 0)
                        {
                            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                            var actualRate = elapsedSeconds > 0
                                ? sender.TotalPdusSent / (elapsedSeconds * entities)
                                : 0;

                            var timestamp = DateTime.Now.ToString("HH:mm:ss");
                            var formattedBytes = $"{sender.TotalBytesSent / 1024.0:F1} KB";

                            Console.WriteLine($"[{timestamp}] Entities: {entities} | PDUs: {sender.TotalPdusSent} | Bytes: {formattedBytes} | Rate: {actualRate:F1}Hz");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    if (cancelHandler is not null)
                    {
                        Console.CancelKeyPress -= cancelHandler;
                    }

                    Console.WriteLine();
                    Console.WriteLine("Shutdown complete.");
                    Console.WriteLine($"Total PDUs sent: {sender?.TotalPdusSent ?? 0}");
                    Console.WriteLine($"Total bytes:     {(sender is null ? "0.0 KB" : $"{sender.TotalBytesSent / 1024.0:F1} KB")}");
                    Console.WriteLine($"Duration:        {stopwatch.Elapsed:hh\\:mm\\:ss}");

                    if (buffer is not null)
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }

                    sender?.Dispose();
                }
                return 0;
            });

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
