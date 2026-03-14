using System.Collections.ObjectModel;
using System.CommandLine;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;
using Terminal.Gui;

namespace SisoDis.Producer;

internal static class Program
{
    private static class Dracula
    {
        public static readonly Color Background = new Color(40, 42, 54);
        public static readonly Color Foreground = new Color(248, 248, 242);
        public static readonly Color Cyan = new Color(139, 233, 253);
        public static readonly Color Green = new Color(80, 250, 123);
        public static readonly Color Purple = new Color(189, 147, 249);
    }

    private static ColorScheme CreateDraculaScheme() => new()
    {
        Normal = new Terminal.Gui.Attribute(Dracula.Foreground, Dracula.Background),
        Focus = new Terminal.Gui.Attribute(Dracula.Background, Dracula.Purple),
        HotNormal = new Terminal.Gui.Attribute(Dracula.Cyan, Dracula.Background),
        HotFocus = new Terminal.Gui.Attribute(Dracula.Background, Dracula.Cyan),
    };

    private static ColorScheme CreateFrameScheme() => new()
    {
        Normal = new Terminal.Gui.Attribute(Dracula.Foreground, Dracula.Background),
        Focus = new Terminal.Gui.Attribute(Dracula.Background, Dracula.Purple),
        HotNormal = new Terminal.Gui.Attribute(Dracula.Purple, Dracula.Background),
        HotFocus = new Terminal.Gui.Attribute(Dracula.Background, Dracula.Purple),
    };

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

        var rootCommand = new RootCommand("SisoDis.NET DIS PDU Producer")
        {
            addressOption,
            portOption,
        };

        rootCommand.SetAction(async (parseResult, _) =>
        {
            var address = parseResult.GetValue(addressOption) ?? "239.1.2.3";
            var port = parseResult.GetValue(portOption);
            await RunTuiAsync(address, port);
            return 0;
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static Task RunTuiAsync(string multicastAddress, int port)
    {
        Application.Init();

        var state = new ProducerState
        {
            MulticastAddress = multicastAddress,
            Port = port,
        };

        state.Sender = new MulticastSender(multicastAddress, port);
        state.Buffer = new byte[1024];

        var top = BuildUi(state);

        top.KeyDown += (object? sender, Key key) =>
        {
            if (key == Key.Q.WithCtrl)
            {
                Application.RequestStop();
            }
            else if (key == Key.F5)
            {
                StartProducer(state);
            }
            else if (key == Key.F6)
            {
                StopProducer(state);
            }
            else if (key == Key.Enter)
            {
                AddEntityFromFields(state);
            }
            else if (key == Key.F2)
            {
                FireDialog(state);
            }
            else if (key == Key.F3)
            {
                MunitionDialog(state);
            }
            else if (key == Key.F4)
            {
                DesignatorDialog(state);
            }
        };

        Application.Run(top);

        state.Sender.Dispose();
        Application.Shutdown();
        return Task.CompletedTask;
    }

    private static Window BuildUi(ProducerState state)
    {
        var draculaScheme = CreateDraculaScheme();
        var frameScheme = CreateFrameScheme();

        var top = new Window
        {
            Title = $"SisoDis.NET Producer — {state.MulticastAddress}:{state.Port}",
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = draculaScheme,
        };

        var controlFrame = new FrameView
        {
            Title = "Entity Settings",
            X = 0,
            Y = 0,
            Width = 50,
            Height = 10,
            ColorScheme = frameScheme,
        };

        var entityIdLabel = new Label { Text = "Entity ID:", X = 1, Y = 0 };
        var entityIdField = new TextField { Text = "1", X = 12, Y = 0, Width = 10, ColorScheme = draculaScheme };
        controlFrame.Add(entityIdLabel, entityIdField);

        var patternLabel = new Label { Text = "Pattern:", X = 1, Y = 1 };
        var patternField = new TextField { Text = "Linear", X = 12, Y = 1, Width = 15, ColorScheme = draculaScheme };
        controlFrame.Add(patternLabel, patternField);

        var speedLabel = new Label { Text = "Speed:", X = 1, Y = 2 };
        var speedField = new TextField { Text = "10", X = 12, Y = 2, Width = 10, ColorScheme = draculaScheme };
        controlFrame.Add(speedLabel, speedField);

        var rateLabel = new Label { Text = "Rate (Hz):", X = 1, Y = 3 };
        var rateField = new TextField { Text = "5", X = 12, Y = 3, Width = 10, ColorScheme = draculaScheme };
        controlFrame.Add(rateLabel, rateField);

        state.EntityIdField = entityIdField;
        state.PatternField = patternField;
        state.SpeedField = speedField;
        state.RateField = rateField;

        controlFrame.Add(new Label { Text = "[Enter] Add [F2] Fire [F5] Start [F6] Stop", X = 1, Y = 6 });

        top.Add(controlFrame);

        var entityListFrame = new FrameView
        {
            Title = "Active Entities",
            X = 50,
            Y = 0,
            Width = Dim.Fill(),
            Height = 10,
            ColorScheme = frameScheme,
        };

        var entityList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = draculaScheme,
        };
        state.EntityList = entityList;
        entityListFrame.Add(entityList);
        top.Add(entityListFrame);

        var logFrame = new FrameView
        {
            Title = "PDU Log",
            X = 0,
            Y = 10,
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
            ColorScheme = frameScheme,
        };

        var logList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = draculaScheme,
        };
        state.LogList = logList;
        logFrame.Add(logList);
        top.Add(logFrame);

        var statusBar = new Label
        {
            Text = "Entities: 0 | PDUs: 0 | F5:Start F6:Stop Enter:Add F2:Fire F3:Munition F4:Designator",
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            ColorScheme = draculaScheme,
        };
        state.StatusBar = statusBar;
        top.Add(statusBar);

        return top;
    }

    private static void FireDialog(ProducerState state)
    {
        var dialog = new Window
        {
            Title = "Fire PDU",
            Width = 45,
            Height = 8,
            X = Pos.Center(),
            Y = Pos.Center(),
        };

        var targetLabel = new Label { Text = "Target ID:", X = 1, Y = 0 };
        var targetField = new TextField { Text = "0", X = 12, Y = 0, Width = 10 };

        var munitionLabel = new Label { Text = "Munition ID:", X = 1, Y = 1 };
        var munitionField = new TextField { Text = "1", X = 12, Y = 1, Width = 10 };

        var fireBtn = new Button { Text = "FIRE", X = 5, Y = 3, Width = 8 };
        var cancelBtn = new Button { Text = "Cancel", X = 15, Y = 3, Width = 8 };

        fireBtn.Accept += (s, e) =>
        {
            if (int.TryParse(targetField.Text, out int targetId) &&
                int.TryParse(munitionField.Text, out int munitionId))
            {
                var firePdu = FirePdu.Create()
                    .WithEntityId(EntityId.Relative(1))
                    .WithTargetEntityId(EntityId.Relative(targetId))
                    .WithMunitionId(EntityId.Relative(munitionId))
                    .WithEventId(EntityId.Relative(1))
                    .WithFireMissionIndex(1)
                    .WithLocation(0, 0, 0)
                    .WithVelocity(0, 0, 0)
                    .WithSimulationFederation(1, 1)
                    .Build();

                firePdu.Serialize(state.Buffer);
                state.Sender.Send(state.Buffer.AsSpan(0, firePdu.ComputedLength()));
                state.TotalPdusSent++;
                AddLog(state, $"[FIRE] Target:{targetId} Munition:{munitionId}");
                UpdateStatus(state);
            }
            Application.RequestStop();
        };

        cancelBtn.Accept += (s, e) => Application.RequestStop();

        dialog.Add(targetLabel, targetField, munitionLabel, munitionField, fireBtn, cancelBtn);
        Application.Run(dialog);
    }

    private static void MunitionDialog(ProducerState state)
    {
        var dialog = new Window
        {
            Title = "Munition PDU",
            Width = 45,
            Height = 8,
            X = Pos.Center(),
            Y = Pos.Center(),
        };

        var targetLabel = new Label { Text = "Target ID:", X = 1, Y = 0 };
        var targetField = new TextField { Text = "0", X = 12, Y = 0, Width = 10 };

        var munitionLabel = new Label { Text = "Munition ID:", X = 1, Y = 1 };
        var munitionField = new TextField { Text = "1", X = 12, Y = 1, Width = 10 };

        var fireBtn = new Button { Text = "SEND", X = 5, Y = 3, Width = 8 };
        var cancelBtn = new Button { Text = "Cancel", X = 15, Y = 3, Width = 8 };

        fireBtn.Accept += (s, e) =>
        {
            if (int.TryParse(targetField.Text, out int targetId) &&
                int.TryParse(munitionField.Text, out int munitionId))
            {
                var pdu = MunitionPdu.Create()
                    .WithEntityId(EntityId.Relative(1))
                    .WithTargetEntityId(EntityId.Relative(targetId))
                    .WithMunitionId(EntityId.Relative(munitionId))
                    .WithEventId(EntityId.Relative(1))
                    .WithFireMissionIndex(1)
                    .WithLocation(0, 0, 0)
                    .WithVelocity(0, 0, 0)
                    .WithSimulationFederation(1, 1)
                    .Build();

                pdu.Serialize(state.Buffer);
                state.Sender.Send(state.Buffer.AsSpan(0, pdu.ComputedLength()));
                state.TotalPdusSent++;
                AddLog(state, $"[MUNITION] Target:{targetId} Munition:{munitionId}");
                UpdateStatus(state);
            }
            Application.RequestStop();
        };

        cancelBtn.Accept += (s, e) => Application.RequestStop();

        dialog.Add(targetLabel, targetField, munitionLabel, munitionField, fireBtn, cancelBtn);
        Application.Run(dialog);
    }

    private static void DesignatorDialog(ProducerState state)
    {
        var dialog = new Window
        {
            Title = "Designator PDU",
            Width = 45,
            Height = 8,
            X = Pos.Center(),
            Y = Pos.Center(),
        };

        var targetLabel = new Label { Text = "Target ID:", X = 1, Y = 0 };
        var targetField = new TextField { Text = "0", X = 12, Y = 0, Width = 10 };

        var codeLabel = new Label { Text = "Code:", X = 1, Y = 1 };
        var codeField = new TextField { Text = "1", X = 12, Y = 1, Width = 10 };

        var sendBtn = new Button { Text = "SEND", X = 5, Y = 3, Width = 8 };
        var cancelBtn = new Button { Text = "Cancel", X = 15, Y = 3, Width = 8 };

        sendBtn.Accept += (s, e) =>
        {
            if (int.TryParse(targetField.Text, out int targetId) &&
                byte.TryParse(codeField.Text, out byte code))
            {
                var pdu = DesignatorPdu.Create()
                    .WithEntityId(EntityId.Relative(1))
                    .WithTargetEntityId(EntityId.Relative(targetId))
                    .WithDesignatorLocation(0, 0, 0)
                    .WithDesignatorOrientation(0, 0, 0)
                    .WithEntityLocation(0, 0, 0)
                    .WithDesignatorCode(code)
                    .WithDesignatorOutput(100)
                    .WithSimulationFederation(1, 1)
                    .Build();

                pdu.Serialize(state.Buffer);
                state.Sender.Send(state.Buffer.AsSpan(0, pdu.ComputedLength()));
                state.TotalPdusSent++;
                AddLog(state, $"[DESIGNATOR] Target:{targetId} Code:{code}");
                UpdateStatus(state);
            }
            Application.RequestStop();
        };

        cancelBtn.Accept += (s, e) => Application.RequestStop();

        dialog.Add(targetLabel, targetField, codeLabel, codeField, sendBtn, cancelBtn);
        Application.Run(dialog);
    }

    private static void AddEntityFromFields(ProducerState state)
    {
        if (state.EntityIdField == null || state.PatternField == null || state.SpeedField == null) return;

        if (int.TryParse(state.EntityIdField.Text, out int id) && id > 0)
        {
            var patternStr = state.PatternField.Text ?? "Linear";
            var pattern = patternStr.ToLower() switch
            {
                "stationary" => MovementPattern.Stationary,
                "circle" => MovementPattern.Circle,
                _ => MovementPattern.Linear
            };
            double.TryParse(state.SpeedField.Text, out double speed);
            var sim = new EntitySimulator(id, pattern, new Vector3Double(0, (id - 1) * 100, 0), speed);
            state.Simulators.Add(sim);
            UpdateEntityList(state);
        }
    }

    private static void StartProducer(ProducerState state)
    {
        if (state.RateField == null || state.Simulators.Count == 0) return;

        if (int.TryParse(state.RateField.Text, out int rate) && rate > 0)
        {
            state.IsRunning = true;
            state.Rate = rate;
            state.Period = TimeSpan.FromMilliseconds(1000.0 / rate);
            _ = RunProducerLoop(state);
            AddLog(state, $"[START] Rate:{rate}Hz Entities:{state.Simulators.Count}");
        }
    }

    private static void StopProducer(ProducerState state)
    {
        state.IsRunning = false;
        AddLog(state, "[STOP]");
    }

    private static void UpdateEntityList(ProducerState state)
    {
        if (state.EntityList == null) return;

        var items = state.Simulators
            .Select(s => $"ID:{s.EntityId} | Pos:({s.Position.X:F1}, {s.Position.Y:F1})")
            .ToList();

        state.EntityList.SetSource(new ObservableCollection<string>(items));
        UpdateStatus(state);
    }

    private static void UpdateStatus(ProducerState state)
    {
        if (state.StatusBar == null) return;
        state.StatusBar.Text = $"Entities: {state.Simulators.Count} | PDUs: {state.TotalPdusSent} | F5:Start F6:Stop Enter:Add F2:Fire F3:Munition F4:Designator";
    }

    private static void AddLog(ProducerState state, string message)
    {
        if (state.LogList == null) return;
        state.PduLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        while (state.PduLog.Count > 100) state.PduLog.RemoveAt(0);
        var reversed = state.PduLog.AsEnumerable().Reverse().ToList();
        state.LogList.SetSource(new ObservableCollection<string>(reversed));
    }

    private static async Task RunProducerLoop(ProducerState state)
    {
        byte simRef = 1;
        byte fedRef = 1;

        using var timer = new PeriodicTimer(state.Period);
        while (state.IsRunning)
        {
            await timer.WaitForNextTickAsync();

            foreach (var sim in state.Simulators)
            {
                sim.Tick(1.0 / state.Rate);
                var pdu = sim.BuildPdu(simRef, fedRef);
                pdu.Serialize(state.Buffer);
                state.Sender.Send(state.Buffer.AsSpan(0, pdu.ComputedLength()));
                state.TotalPdusSent++;
            }

            Application.Invoke(() =>
            {
                UpdateEntityList(state);
            });
        }
    }
}

internal class ProducerState
{
    public required string MulticastAddress { get; init; }
    public required int Port { get; init; }

    public MulticastSender? Sender { get; set; }
    public byte[]? Buffer { get; set; }
    public List<EntitySimulator> Simulators { get; } = new();
    public bool IsRunning { get; set; }
    public int Rate { get; set; } = 5;
    public TimeSpan Period { get; set; }
    public long TotalPdusSent { get; set; }

    public TextField? EntityIdField { get; set; }
    public TextField? PatternField { get; set; }
    public TextField? SpeedField { get; set; }
    public TextField? RateField { get; set; }

    public ListView? EntityList { get; set; }
    public ListView? LogList { get; set; }
    public Label? StatusBar { get; set; }
    public List<string> PduLog { get; } = new();
}
