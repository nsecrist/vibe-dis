using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class MunitionPduTests
{
    [Fact]
    public void MunitionPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new MunitionPdu(
            EntityId.Relative(1),
            EntityId.Relative(0),
            EntityId.Relative(0),
            EntityId.Relative(0),
            0,
            Vector3Double.Zero,
            Vector3Double.Zero,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 62, pdu.ComputedLength());
    }

    [Fact]
    public void MunitionPdu_RoundTripPreservesValues()
    {
        var original = new MunitionPdu(
            EntityId.Relative(42),
            EntityId.Relative(100),
            EntityId.Relative(200),
            EntityId.Relative(1),
            5,
            new Vector3Double(10.5, 20.7, 30.9),
            new Vector3Double(1.0, -2.0, 3.0),
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = MunitionPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.EntityId.Value);
        Assert.Equal(100, deserialized.TargetEntityId.Value);
        Assert.Equal(200, deserialized.MunitionId.Value);
        Assert.Equal(1, deserialized.EventId.Value);
        Assert.Equal(5u, deserialized.FireMissionIndex);
        Assert.Equal(10.5, deserialized.Location.X, precision: 10);
    }

    [Fact]
    public void MunitionPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => MunitionPdu.Deserialize(buffer));
    }

    [Fact]
    public void MunitionPdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = MunitionPdu.Create()
            .WithEntityId(EntityId.Relative(123))
            .WithTargetEntityId(EntityId.Relative(456))
            .WithMunitionId(EntityId.Relative(789))
            .WithEventId(EntityId.Relative(1))
            .WithFireMissionIndex(10)
            .WithLocation(5.0, 6.0, 7.0)
            .WithVelocity(1.0, 2.0, 3.0)
            .WithSimulationFederation(1, 2)
            .Build();

        Assert.Equal(123, pdu.EntityId.Value);
        Assert.Equal(456, pdu.TargetEntityId.Value);
        Assert.Equal(10u, pdu.FireMissionIndex);
    }

    [Fact]
    public void MunitionPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)20, MunitionPdu.PdTypeValue);
    }
}
