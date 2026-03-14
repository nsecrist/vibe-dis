using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class DetonationPduTests
{
    [Fact]
    public void DetonationPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new DetonationPdu(
            EntityId.Relative(1),
            EntityId.Relative(0),
            EntityId.Relative(0),
            EntityId.Relative(0),
            Vector3Double.Zero,
            Vector3Double.Zero,
            DetonationResult.Other,
            0,
            0,
            new DetonationPduAdditionalState()
        );

        Assert.Equal(PduHeader.HeaderLength + 60, pdu.ComputedLength());
    }

    [Fact]
    public void DetonationPdu_RoundTripPreservesValues()
    {
        var original = new DetonationPdu(
            EntityId.Relative(42),
            EntityId.Relative(100),
            EntityId.Relative(200),
            EntityId.Relative(1),
            new Vector3Double(1.0, -2.0, 3.0),
            new Vector3Double(10.5, 20.7, 30.9),
            DetonationResult.Kill,
            1,
            2,
            new DetonationPduAdditionalState()
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = DetonationPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.EntityId.Value);
        Assert.Equal(100, deserialized.TargetEntityId.Value);
        Assert.Equal(200, deserialized.MunitionId.Value);
        Assert.Equal(DetonationResult.Kill, deserialized.Result);
        Assert.Equal(10.5, deserialized.Location.X, precision: 10);
        Assert.Equal(1.0, deserialized.Velocity.X, precision: 10);
    }

    [Fact]
    public void DetonationPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => DetonationPdu.Deserialize(buffer));
    }

    [Fact]
    public void DetonationPdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = DetonationPdu.Create()
            .WithEntityId(EntityId.Relative(123))
            .WithTargetEntityId(EntityId.Relative(456))
            .WithMunitionId(EntityId.Relative(789))
            .WithEventId(EntityId.Relative(1))
            .WithLocation(5.0, 6.0, 7.0)
            .WithVelocity(1.0, 2.0, 3.0)
            .WithResult(DetonationResult.Impact)
            .WithSimulationFederation(1, 2)
            .Build();

        Assert.Equal(123, pdu.EntityId.Value);
        Assert.Equal(456, pdu.TargetEntityId.Value);
        Assert.Equal(DetonationResult.Impact, pdu.Result);
    }

    [Fact]
    public void DetonationPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)3, DetonationPdu.PdTypeValue);
    }
}
