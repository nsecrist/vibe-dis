using System.Collections.ObjectModel;
using System.CommandLine;
using SisoDis.Core.Pdu;
using Terminal.Gui;

namespace SisoDis.Receiver;

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

        var staleTimeoutOption = new Option<int>("--stale-timeout")
        {
            Description = "Seconds before entity is marked stale",
            DefaultValueFactory = _ => 5,
        };

        var rootCommand = new RootCommand("SisoDis.NET DIS PDU Receiver — receives and displays DIS PDUs via UDP multicast")
        {
            addressOption,
            portOption,
            staleTimeoutOption,
        };

        rootCommand.SetAction(async (parseResult, _) =>
        {
            var address = parseResult.GetValue(addressOption) ?? "239.1.2.3";
            var port = parseResult.GetValue(portOption);
            var staleTimeout = parseResult.GetValue(staleTimeoutOption);

            if (staleTimeout <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(staleTimeout), "Stale timeout must be greater than zero.");
            }

            // Run the TUI
            await RunTuiAsync(address, port, staleTimeout);
            return 0;
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static Task RunTuiAsync(string multicastAddress, int port, int staleTimeoutSeconds)
    {
        // Initialize Terminal.Gui
        Application.Init();

        // Create shared state
        var state = new ReceiverState
        {
            MulticastAddress = multicastAddress,
            Port = port,
            StaleTimeout = TimeSpan.FromSeconds(staleTimeoutSeconds),
        };

        // Create receiver and tracker
        state.Receiver = new MulticastReceiver(multicastAddress, port);
        state.EntityTracker = new EntityTracker(state.StaleTimeout);

        // Set up PDU handler
        state.Receiver.PduReceived += (pdu, rawData) =>
        {
            Application.Invoke(() =>
            {
                state.EntityTracker.ProcessPdu(pdu);
                state.PduLog.Add(new PduLogEntry(pdu.PdType, pdu.GetType().Name, DateTime.Now));
                
                // Trim log if too large
                while (state.PduLog.Count > 1000)
                {
                    state.PduLog.RemoveAt(0);
                }

                UpdateEntityTable(state);
                UpdatePduLog(state);
                UpdateStatusBar(state);
            });
        };

        // Build the UI
        var top = BuildUi(state);

        // Handle Ctrl+Q to quit
        top.KeyDown += (object? sender, Key key) =>
        {
            if (key == Key.Q.WithCtrl)
            {
                Application.RequestStop();
            }
        };

        // Start the receiver
        state.Receiver.Start();

        // Run the application
        Application.Run(top);

        // Cleanup
        state.Receiver.Dispose();
        state.EntityTracker.Dispose();
        Application.Shutdown();

        return Task.CompletedTask;
    }

    private static Window BuildUi(ReceiverState state)
    {
        // Main window
        var top = new Window
        {
            Title = $"SisoDis.NET Receiver — {state.MulticastAddress}:{state.Port}",
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        // Status bar at bottom
        var statusBar = new Label
        {
            Text = $"Entities: 0 | PDUs: 0 | Errors: 0 | Ctrl+Q to quit",
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
        };
        top.Add(statusBar);
        state.StatusLabel = statusBar;

        // Entity table (top area)
        var entityFrame = new FrameView
        {
            Title = "Entities",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(4),
        };

        var entityTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        
        // Configure columns using Terminal.Gui v2 TableViewStyle
        var tableSource = new EntityTableSource();
        entityTable.Table = tableSource;

        entityFrame.Add(entityTable);
        top.Add(entityFrame);
        state.EntityTable = entityTable;
        state.TableSource = tableSource;

        // PDU log (bottom area)
        var logFrame = new FrameView
        {
            Title = "PDU Log",
            X = 0,
            Y = Pos.AnchorEnd(4),
            Width = Dim.Fill(),
            Height = 3,
        };

        var logList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        logFrame.Add(logList);
        top.Add(logFrame);
        state.LogList = logList;

        return top;
    }

    private static void UpdateEntityTable(ReceiverState state)
    {
        if (state.TableSource == null) return;

        var rows = state.EntityTracker.Entities
            .OrderBy(e => e.EntityId)
            .Select(e => new EntityRow(
                e.EntityId,
                e.TypeString,
                e.Position.X,
                e.Position.Y,
                e.Position.Z,
                e.IsStale ? "STALE" : "ALIVE"))
            .ToList();

        state.TableSource.UpdateRows(rows);
        state.EntityTable?.SetNeedsDisplay();
    }

    private static void UpdatePduLog(ReceiverState state)
    {
        if (state.LogList == null) return;

        var logItems = state.PduLog
            .Select(e => $"[{e.Timestamp:HH:mm:ss}] Type {e.PduTypeId}: {e.PduTypeName}")
            .Reverse()
            .Take(100)
            .ToList();

        state.LogList.SetSource(new ObservableCollection<string>(logItems));
    }

    private static void UpdateStatusBar(ReceiverState state)
    {
        if (state.StatusLabel == null || state.Receiver == null) return;

        state.StatusLabel.Text = $"Entities: {state.EntityTracker.Count} | " +
            $"PDUs: {state.Receiver.TotalPdusReceived} | " +
            $"Errors: {state.Receiver.TotalErrors} | " +
            $"Ctrl+Q to quit";
    }
}

/// <summary>
/// Custom table source for entity display in Terminal.Gui v2.
/// </summary>
internal class EntityTableSource : ITableSource
{
    private List<EntityRow> _rows = new();

    public void UpdateRows(List<EntityRow> rows)
    {
        _rows = rows;
    }

    public int Rows => _rows.Count;
    public int Columns => 6;

    public System.Type[] ColumnTypes => new System.Type[] 
    { 
        typeof(int), typeof(string), typeof(double), typeof(double), typeof(double), typeof(string) 
    };

    public object? GetCell(int row, int col)
    {
        if (row < 0 || row >= _rows.Count) return null;
        var entity = _rows[row];
        return col switch
        {
            0 => entity.Id,
            1 => entity.Type,
            2 => entity.X,
            3 => entity.Y,
            4 => entity.Z,
            5 => entity.Status,
            _ => null
        };
    }

    public string[] ColumnNames => new[] { "ID", "Type", "X", "Y", "Z", "Status" };

    public object? this[int row, int col] => GetCell(row, col);
}

/// <summary>
/// Represents a single entity row in the table.
/// </summary>
internal record EntityRow(int Id, string Type, double X, double Y, double Z, string Status);

/// <summary>
/// Holds UI state for the receiver application.
/// </summary>
internal sealed class ReceiverState
{
    public required string MulticastAddress { get; init; }
    public required int Port { get; init; }
    public required TimeSpan StaleTimeout { get; init; }

    public MulticastReceiver? Receiver { get; set; }
    public EntityTracker? EntityTracker { get; set; }
    public TableView? EntityTable { get; set; }
    public EntityTableSource? TableSource { get; set; }
    public ListView? LogList { get; set; }
    public Label? StatusLabel { get; set; }

    public readonly List<PduLogEntry> PduLog = new();
}

/// <summary>
/// A single entry in the PDU log.
/// </summary>
internal sealed record PduLogEntry(ushort PduTypeId, string PduTypeName, DateTime Timestamp);
