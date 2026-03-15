using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class DesignatorPduTests
{
    [Fact]
    public void DesignatorPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new DesignatorPdu(
            EntityId.Relative(1),
            EntityId.Relative(0),
            Vector3Double.Zero,
            Vector3Double.Zero,
            Vector3Double.Zero,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 80, pdu.ComputedLength());
    }

    [Fact]
    public void DesignatorPdu_RoundTripPreservesValues()
    {
        var original = new DesignatorPdu(
            EntityId.Relative(42),
            EntityId.Relative(100),
            new Vector3Double(1.0, 2.0, 3.0),
            new Vector3Double(0.1, 0.2, 0.3),
            new Vector3Double(10.5, 20.7, 30.9),
            5,
            200,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = DesignatorPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.EntityId.Value);
        Assert.Equal(100, deserialized.TargetEntityId.Value);
        Assert.Equal(1.0, deserialized.DesignatorLocation.X, precision: 10);
        Assert.Equal(0.1, deserialized.DesignatorOrientation.X, precision: 10);
        Assert.Equal(5, deserialized.DesignatorCode);
        Assert.Equal(200, deserialized.DesignatorOutput);
    }

    [Fact]
    public void DesignatorPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => DesignatorPdu.Deserialize(buffer));
    }

    [Fact]
    public void DesignatorPdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = DesignatorPdu.Create()
            .WithEntityId(EntityId.Relative(123))
            .WithTargetEntityId(EntityId.Relative(456))
            .WithDesignatorLocation(1.0, 2.0, 3.0)
            .WithDesignatorOrientation(0.1, 0.2, 0.3)
            .WithEntityLocation(10.0, 20.0, 30.0)
            .WithDesignatorCode(7)
            .WithDesignatorOutput(150)
            .WithSimulationFederation(1, 2)
            .Build();

        Assert.Equal(123, pdu.EntityId.Value);
        Assert.Equal(456, pdu.TargetEntityId.Value);
        Assert.Equal(7, pdu.DesignatorCode);
    }

    [Fact]
    public void DesignatorPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)24, DesignatorPdu.PdTypeValue);
    }
}
