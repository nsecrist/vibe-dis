using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class EntityStateUpdatePduTests
{
    [Fact]
    public void EntityStateUpdatePdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new EntityStateUpdatePdu(
            EntityId.Relative(1),
            EntityStateUpdateFlags.None,
            null, null, null, null, null,
            0, 0,
            new EntityStateUpdatePduAdditionalState()
        );

        Assert.Equal(PduHeader.HeaderLength + 13, pdu.ComputedLength());
    }

    [Fact]
    public void EntityStateUpdatePdu_WithAllFields_RoundTripPreservesValues()
    {
        var original = new EntityStateUpdatePdu(
            EntityId.Relative(42),
            EntityStateUpdateFlags.EntityType | EntityStateUpdateFlags.LinearPosition,
            EntityType.Generic,
            new Vector3Double(10.5, 20.7, 30.9),
            null, null, null,
            1,
            2,
            new EntityStateUpdatePduAdditionalState()
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = EntityStateUpdatePdu.Deserialize(buffer);

        Assert.Equal(original.EntityId.Value, deserialized.EntityId.Value);
        Assert.True(deserialized.Flags.HasFlag(EntityStateUpdateFlags.EntityType));
        Assert.True(deserialized.Flags.HasFlag(EntityStateUpdateFlags.LinearPosition));
        Assert.NotNull(deserialized.EntityType);
        Assert.Equal(10.5, deserialized.LinearPosition!.Value.X, precision: 10);
    }

    [Fact]
    public void EntityStateUpdatePdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => EntityStateUpdatePdu.Deserialize(buffer));
    }

    [Fact]
    public void EntityStateUpdatePdu_Deserialize_InvalidType_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 1;
        buffer[1] = 3;
        buffer[4] = 9;
        buffer[5] = 0;

        Assert.Throws<DisValidationException>(() => EntityStateUpdatePdu.Deserialize(buffer));
    }

    [Fact]
    public void EntityStateUpdatePdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = EntityStateUpdatePdu.Create()
            .WithEntityId(EntityId.Relative(123))
            .WithEntityType(EntityType.Generic)
            .WithLinearPosition(5.0, 6.0, 7.0)
            .WithSimulationFederation(1, 2)
            .Build();

        Assert.Equal(123, pdu.EntityId.Value);
        Assert.True(pdu.Flags.HasFlag(EntityStateUpdateFlags.EntityType));
        Assert.True(pdu.Flags.HasFlag(EntityStateUpdateFlags.LinearPosition));
        Assert.NotNull(pdu.EntityType);
    }

    [Fact]
    public void EntityStateUpdatePdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)20, EntityStateUpdatePdu.PdTypeValue);
    }

    [Fact]
    public void EntityStateUpdateFlags_Bitmask_CorrectlyIdentifiesFields()
    {
        var flags = EntityStateUpdateFlags.EntityType | EntityStateUpdateFlags.LinearPosition;

        Assert.True(flags.HasFlag(EntityStateUpdateFlags.EntityType));
        Assert.True(flags.HasFlag(EntityStateUpdateFlags.LinearPosition));
        Assert.False(flags.HasFlag(EntityStateUpdateFlags.AngularOrientation));
    }
}
