using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class CollisionPduTests
{
    [Fact]
    public void CollisionPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new CollisionPdu(
            EntityId.Relative(1),
            Vector3Double.Zero,
            Vector3Double.Zero,
            0,
            0,
            new CollisionPduAdditionalState(),
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 52, pdu.ComputedLength());
    }

    [Fact]
    public void CollisionPdu_RoundTripPreservesValues()
    {
        var original = new CollisionPdu(
            EntityId.Relative(42),
            new Vector3Double(10.5, 20.7, 30.9),
            new Vector3Double(1.0, -2.0, 3.0),
            1,
            2,
            new CollisionPduAdditionalState(),
            0
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = CollisionPdu.Deserialize(buffer);

        Assert.Equal(original.EntityId.Value, deserialized.EntityId.Value);
        Assert.Equal(10.5, deserialized.ImpactLocation.X, precision: 10);
        Assert.Equal(20.7, deserialized.ImpactLocation.Y, precision: 10);
        Assert.Equal(30.9, deserialized.ImpactLocation.Z, precision: 10);
        Assert.Equal(1.0, deserialized.VelocityBeforeImpact.X, precision: 10);
        Assert.Equal(-2.0, deserialized.VelocityBeforeImpact.Y, precision: 10);
        Assert.Equal(3.0, deserialized.VelocityBeforeImpact.Z, precision: 10);
    }

    [Fact]
    public void CollisionPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => CollisionPdu.Deserialize(buffer));
    }

    [Fact]
    public void CollisionPdu_Deserialize_InvalidType_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 1;
        buffer[1] = 3;
        buffer[4] = 9;
        buffer[5] = 0;

        Assert.Throws<DisValidationException>(() => CollisionPdu.Deserialize(buffer));
    }

    [Fact]
    public void CollisionPdu_WithArticulatedParts_SerializesCorrectly()
    {
        var positions = new short[] { -100, 50 };
        var directions = new short[] { 200, -50 };
        var states = new byte[] { 1, 2 };
        var offsets = new byte[] { 10, 20 };

        var additionalData = new CollisionPduAdditionalState(positions, directions, states, offsets);

        var pdu = new CollisionPdu(
            EntityId.Relative(100),
            new Vector3Double(0, 0, 0),
            new Vector3Double(1, 2, 3),
            1,
            1,
            additionalData,
            2
        );

        byte[] buffer = new byte[pdu.ComputedLength()];
        pdu.Serialize(buffer);

        var deserialized = CollisionPdu.Deserialize(buffer);

        Assert.Equal(2, deserialized.NumberOfParts);
    }

    [Fact]
    public void CollisionPdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = CollisionPdu.Create()
            .WithEntityId(EntityId.Relative(123))
            .WithImpactLocation(5.0, 6.0, 7.0)
            .WithVelocityBeforeImpact(1.0, 2.0, 3.0)
            .WithSimulationFederation(1, 2)
            .Build();

        Assert.Equal(123, pdu.EntityId.Value);
        Assert.Equal(5.0, pdu.ImpactLocation.X, precision: 10);
        Assert.Equal(6.0, pdu.ImpactLocation.Y, precision: 10);
        Assert.Equal(7.0, pdu.ImpactLocation.Z, precision: 10);
    }

    [Fact]
    public void CollisionPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)4, CollisionPdu.PdTypeValue);
    }
}
