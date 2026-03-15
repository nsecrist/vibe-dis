using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class CollisionElasticPduTests
{
    [Fact]
    public void CollisionElasticPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new CollisionElasticPdu(
            EntityId.Relative(1),
            Vector3Double.Zero,
            Vector3Double.Zero,
            Vector3Double.Zero,
            Vector3Double.Zero,
            Vector3Double.Zero,
            0,
            0,
            new CollisionElasticPduAdditionalState(),
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 124, pdu.ComputedLength());
    }

    [Fact]
    public void CollisionElasticPdu_RoundTripPreservesValues()
    {
        var original = new CollisionElasticPdu(
            EntityId.Relative(42),
            new Vector3Double(10.5, 20.7, 30.9),
            new Vector3Double(1.0, -2.0, 3.0),
            new Vector3Double(4.0, -5.0, 6.0),
            new Vector3Double(-1.0, 2.0, -3.0),
            new Vector3Double(7.0, 8.0, -9.0),
            1,
            2,
            new CollisionElasticPduAdditionalState(),
            0
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = CollisionElasticPdu.Deserialize(buffer);

        Assert.Equal(original.EntityId.Value, deserialized.EntityId.Value);
        Assert.Equal(10.5, deserialized.ImpactLocation.X, precision: 10);
        Assert.Equal(-2.0, deserialized.VelocityBeforeImpactA.Y, precision: 10);
        Assert.Equal(4.0, deserialized.VelocityAfterImpactA.X, precision: 10);
        Assert.Equal(-3.0, deserialized.VelocityBeforeImpactB.Z, precision: 10);
    }

    [Fact]
    public void CollisionElasticPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => CollisionElasticPdu.Deserialize(buffer));
    }

    [Fact]
    public void CollisionElasticPdu_Deserialize_InvalidType_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 1;
        buffer[1] = 3;
        buffer[4] = 9;
        buffer[5] = 0;

        Assert.Throws<DisValidationException>(() => CollisionElasticPdu.Deserialize(buffer));
    }

    [Fact]
    public void CollisionElasticPdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = CollisionElasticPdu.Create()
            .WithEntityId(EntityId.Relative(123))
            .WithImpactLocation(5.0, 6.0, 7.0)
            .WithVelocityBeforeImpactA(1.0, 2.0, 3.0)
            .WithVelocityAfterImpactA(4.0, 5.0, 6.0)
            .WithVelocityBeforeImpactB(-1.0, -2.0, -3.0)
            .WithVelocityAfterImpactB(7.0, 8.0, 9.0)
            .WithSimulationFederation(1, 2)
            .Build();

        Assert.Equal(123, pdu.EntityId.Value);
        Assert.Equal(5.0, pdu.ImpactLocation.X, precision: 10);
        Assert.Equal(4.0, pdu.VelocityAfterImpactA.X, precision: 10);
    }

    [Fact]
    public void CollisionElasticPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)50, CollisionElasticPdu.PdTypeValue);
    }
}
