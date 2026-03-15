using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class TransmitterPduTests
{
    [Fact]
    public void TransmitterPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new TransmitterPdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            new Vector3Double(0, 0, 0),
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 74, pdu.ComputedLength());
    }

    [Fact]
    public void TransmitterPdu_RoundTripPreservesValues()
    {
        var original = new TransmitterPdu(
            EntityId.Relative(42),
            1,
            100,
            1,
            0,
            0,
            225000000.0,
            2000000.0,
            100,
            1,
            2,
            new Vector3Double(10.0, 20.0, 30.0),
            1,
            2,
            0,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = TransmitterPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.EntityId.Value);
        Assert.Equal(1, deserialized.RadioId);
        Assert.Equal(100, deserialized.RadioReference);
        Assert.Equal(1, deserialized.TransmitState);
        Assert.Equal(225000000.0, deserialized.Frequency, precision: 10);
        Assert.Equal(100u, deserialized.Power);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void TransmitterPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => TransmitterPdu.Deserialize(buffer));
    }

    [Fact]
    public void TransmitterPdu_Deserialize_InvalidType_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 1;
        buffer[1] = 3;
        buffer[4] = 99;
        buffer[5] = 0;

        Assert.Throws<DisValidationException>(() => TransmitterPdu.Deserialize(buffer));
    }

    [Fact]
    public void TransmitterPdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = TransmitterPdu.Create()
            .WithEntityId(EntityId.Relative(100))
            .WithRadioId(5)
            .WithRadioReference(200)
            .WithTransmitState(1)
            .WithFrequency(225000000.0)
            .WithPower(50)
            .WithSimulationFederation(1, 1)
            .Build();

        Assert.Equal(100, pdu.EntityId.Value);
        Assert.Equal(5, pdu.RadioId);
        Assert.Equal(200, pdu.RadioReference);
        Assert.Equal(1, pdu.TransmitState);
    }

    [Fact]
    public void TransmitterPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)25, TransmitterPdu.PdTypeValue);
    }
}

public sealed class SignalPduTests
{
    [Fact]
    public void SignalPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new SignalPdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            Array.Empty<byte>(),
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 16, pdu.ComputedLength());
    }

    [Fact]
    public void SignalPdu_RoundTripPreservesValues()
    {
        var original = new SignalPdu(
            EntityId.Relative(42),
            1,
            0,
            1,
            4,
            new byte[] { 0x01, 0x02, 0x03, 0x04 },
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = SignalPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.EntityId.Value);
        Assert.Equal(1, deserialized.RadioId);
        Assert.Equal(4u, deserialized.DataLength);
        Assert.Equal(4, deserialized.Data.Length);
        Assert.Equal(0x01, deserialized.Data[0]);
        Assert.Equal(0x02, deserialized.Data[1]);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void SignalPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)26, SignalPdu.PdTypeValue);
    }
}

public sealed class ReceiverPduTests
{
    [Fact]
    public void ReceiverPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new ReceiverPdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            new Vector3Double(0, 0, 0),
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 35, pdu.ComputedLength());
    }

    [Fact]
    public void ReceiverPdu_RoundTripPreservesValues()
    {
        var original = new ReceiverPdu(
            EntityId.Relative(42),
            1,
            2,
            0,
            new Vector3Double(10.0, 20.0, 30.0),
            5,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = ReceiverPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.EntityId.Value);
        Assert.Equal(1, deserialized.RadioId);
        Assert.Equal(2, deserialized.ReceiverState);
        Assert.Equal(10.0, deserialized.AntennaLocation.X, precision: 10);
        Assert.Equal(20.0, deserialized.AntennaLocation.Y, precision: 10);
        Assert.Equal(30.0, deserialized.AntennaLocation.Z, precision: 10);
        Assert.Equal(5, deserialized.RadioSystem);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void ReceiverPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)27, ReceiverPdu.PdTypeValue);
    }
}