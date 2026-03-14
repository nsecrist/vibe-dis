using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Producer;

internal enum MovementPattern
{
    Stationary,
    Linear,
    Circle,
}

internal sealed class EntitySimulator
{
    private readonly MovementPattern _pattern;
    private readonly Vector3Double _circleCenter;
    private readonly double _speed;
    private double _angle;

    public int EntityId { get; }

    public Vector3Double Position { get; private set; }

    public Vector3Double Velocity { get; private set; }

    public Vector3Double Orientation { get; private set; }

    public string Marking { get; }

    public EntitySimulator(int entityId, MovementPattern pattern, Vector3Double startPosition, double speed)
    {
        EntityId = entityId;
        _pattern = pattern;
        _circleCenter = startPosition;
        _speed = speed;

        Position = startPosition;
        Velocity = pattern == MovementPattern.Linear
            ? new Vector3Double(speed, 0, 0)
            : Vector3Double.Zero;
        Orientation = Vector3Double.Zero;
        Marking = $"ENT_{entityId:D4}";
    }

    public void Tick(double deltaSeconds)
    {
        switch (_pattern)
        {
            case MovementPattern.Stationary:
                Velocity = Vector3Double.Zero;
                break;

            case MovementPattern.Linear:
                Position = new Vector3Double(
                    Position.X + (Velocity.X * deltaSeconds),
                    Position.Y + (Velocity.Y * deltaSeconds),
                    Position.Z);
                break;

            case MovementPattern.Circle:
                const double radius = 500.0;
                _angle += (_speed / radius) * deltaSeconds;

                Position = new Vector3Double(
                    _circleCenter.X + (Math.Cos(_angle) * radius),
                    _circleCenter.Y + (Math.Sin(_angle) * radius),
                    _circleCenter.Z);

                Velocity = new Vector3Double(
                    -Math.Sin(_angle) * _speed,
                    Math.Cos(_angle) * _speed,
                    0);

                Orientation = new Vector3Double(_angle, Orientation.Y, Orientation.Z);
                break;
        }
    }

    public EntityStatePdu BuildPdu(byte simulationRef, byte federationRef)
    {
        return EntityStatePdu.Create()
            .WithEntityId(SisoDis.Core.Common.EntityId.Relative(EntityId))
            .WithEntityType(EntityType.PhysicalWithLocation)
            .WithLinearPosition(Position.X, Position.Y, Position.Z)
            .WithAngularOrientation(Orientation.X, Orientation.Y, Orientation.Z)
            .WithLinearVelocity(Velocity.X, Velocity.Y, Velocity.Z)
            .WithAngularVelocity(0, 0, 0)
            .WithSimulationFederation(simulationRef, federationRef)
            .WithAdditionalState(new EntityStatePduAdditionalState())
            .WithNumberOfParts(0)
            .Build();
    }
}
