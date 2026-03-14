using System.Collections.Concurrent;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Receiver;

/// <summary>
/// Tracks entity state information received via DIS PDUs.
/// Detects stale entities that haven't been heard from within the timeout window.
/// </summary>
internal sealed class EntityTracker
{
    private readonly ConcurrentDictionary<int, TrackedEntity> _entities = new();
    private readonly TimeSpan _staleTimeout;
    private readonly Timer _cleanupTimer;

    /// <summary>
    /// Creates a new entity tracker with the specified stale timeout.
    /// </summary>
    /// <param name="staleTimeout">Duration after which an entity is considered stale.</param>
    public EntityTracker(TimeSpan staleTimeout)
    {
        _staleTimeout = staleTimeout;
        // Run cleanup every 1 second
        _cleanupTimer = new Timer(CleanupStaleEntities, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Gets the current list of tracked entities.
    /// </summary>
    public IEnumerable<TrackedEntity> Entities => _entities.Values;

    /// <summary>
    /// Gets the number of tracked entities.
    /// </summary>
    public int Count => _entities.Count;

    /// <summary>
    /// Gets entities that are currently stale (not heard from within timeout).
    /// </summary>
    public IEnumerable<TrackedEntity> StaleEntities
    {
        get
        {
            var now = DateTime.UtcNow;
            return _entities.Values.Where(e => now - e.LastSeen > _staleTimeout);
        }
    }

    /// <summary>
    /// Updates entity state from an EntityStatePdu.
    /// </summary>
    public void Update(EntityStatePdu pdu)
    {
        var entityId = pdu.EntityId.Value;
        var now = DateTime.UtcNow;

        var entity = new TrackedEntity(
            EntityId: entityId,
            EntityType: pdu.EntityType,
            Position: pdu.LinearPosition,
            Velocity: pdu.LinearVelocity,
            LastSeen: now,
            IsStale: false);

        _entities[entityId] = entity;
    }

    /// <summary>
    /// Processes a received PDU - extracts entity information if it's an Entity State PDU.
    /// </summary>
    public void ProcessPdu(IPdu pdu)
    {
        if (pdu is EntityStatePdu entityState)
        {
            Update(entityState);
        }
    }

    private void CleanupStaleEntities(object? state)
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _entities)
        {
            if (now - kvp.Value.LastSeen > _staleTimeout)
            {
                // Mark as stale (create new instance with IsStale = true)
                var staleEntity = kvp.Value with { IsStale = true };
                _entities[kvp.Key] = staleEntity;
            }
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }
}

/// <summary>
/// Represents a tracked entity's state.
/// </summary>
public sealed record TrackedEntity(
    int EntityId,
    EntityType EntityType,
    Vector3Double Position,
    Vector3Double Velocity,
    DateTime LastSeen,
    bool IsStale)
{
    /// <summary>Returns a human-readable entity type string.</summary>
    public string TypeString => EntityType.ToString();

    /// <summary>Returns the time since last seen.</summary>
    public TimeSpan TimeSinceLastSeen => DateTime.UtcNow - LastSeen;

    /// <summary>Returns true if the entity is still alive (not stale).</summary>
    public bool IsAlive => !IsStale;
}
