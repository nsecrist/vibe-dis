using System;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;
using Shouldly;

namespace SisoDis.Tests;

public class EntityStatePduTests
{
    [Fact]
    public void Constructor_CreatesValidInstance()
    {
        var pdu = CreateTestEntityStatePdu();

        pdu.Magic.ShouldBe((byte)1);
        pdu.ProtocolVersion.ShouldBe((byte)3);
        pdu.PdType.ShouldBe(EntityStatePdu.PdTypeValue);
        pdu.EntityId.Value.ShouldBe(42);
        pdu.EntityType.ShouldBe(EntityType.PhysicalWithLocation);
    }

    [Fact]
    public void ComputedLength_ReturnsCorrectSize()
    {
        var pdu = CreateTestEntityStatePdu();

        int expectedLength = PduHeader.HeaderLength + 110;
        pdu.ComputedLength().ShouldBe(expectedLength);
    }

    [Fact]
    public void ComputedLength_WithArticulatedParts_IncludesPartSize()
    {
        var additionalState = new EntityStatePduAdditionalState(
            Flags: 0,
            AmmoState: 0,
            LaunchIndicator: 0,
            EmitterState: 0,
            ArticulationCount: 2,
            ArticulatedPartId: 1,
            ArticulationPositions: new short[] { 0, 1 },
            ArticulationDirections: new short[] { 0, 1 },
            ArticulatedPartStates: new byte[] { 0, 1 },
            ArticulationOffsets: new byte[] { 0, 1 }
        );

        var pdu = EntityStatePdu.Create()
            .WithEntityId(EntityId.Relative(42))
            .WithEntityType(EntityType.PhysicalWithLocation)
            .WithLinearPosition(100.0, 200.0, 300.0)
            .WithAngularOrientation(0.1, 0.2, 0.3)
            .WithLinearVelocity(1.0, 2.0, 3.0)
            .WithAngularVelocity(0.01, 0.02, 0.03)
            .WithSimulationFederation(1, 2)
            .WithAdditionalState(additionalState)
            .WithNumberOfParts(2)
            .Build();

        int expectedLength = PduHeader.HeaderLength + 110 + (2 * 8);
        pdu.ComputedLength().ShouldBe(expectedLength);
    }

    [Fact]
    public void Serialize_ProducesValidBuffer()
    {
        var additionalState = new EntityStatePduAdditionalState(0, 0, 0, 0, 0, 0);

        var pdu = EntityStatePdu.Create()
            .WithEntityId(EntityId.Relative(42))
            .WithEntityType(EntityType.PhysicalWithLocation)
            .WithLinearPosition(100.5, 200.5, 300.5)
            .WithAngularOrientation(0.1, 0.2, 0.3)
            .WithLinearVelocity(1.0, 2.0, 3.0)
            .WithAngularVelocity(0.01, 0.02, 0.03)
            .WithSimulationFederation(5, 10)
            .WithAdditionalState(additionalState)
            .WithNumberOfParts(0)
            .Build();

        var buffer = new byte[pdu.ComputedLength()];
        pdu.Serialize(buffer);

        buffer[0].ShouldBe((byte)1);
        buffer[1].ShouldBe((byte)3);

        ushort pduType = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(4, 2));
        pduType.ShouldBe(EntityStatePdu.PdTypeValue);

        ushort entityId = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(PduHeader.HeaderLength, 2));
        entityId.ShouldBe((ushort)42);
    }

    [Fact]
    public void Deserialize_WithValidBuffer_DeserializesCorrectly()
    {
        var additionalState = new EntityStatePduAdditionalState(0, 0, 100, 200, 0, 0);

        var originalPdu = EntityStatePdu.Create()
            .WithEntityId(EntityId.Relative(42))
            .WithEntityType(EntityType.PhysicalWithLocation)
            .WithLinearPosition(100.5, 200.5, 300.5)
            .WithAngularOrientation(0.1, 0.2, 0.3)
            .WithLinearVelocity(1.0, 2.0, 3.0)
            .WithAngularVelocity(0.01, 0.02, 0.03)
            .WithSimulationFederation(5, 10)
            .WithAdditionalState(additionalState)
            .WithNumberOfParts(0)
            .Build();

        var buffer = new byte[originalPdu.ComputedLength()];
        originalPdu.Serialize(buffer);

        var deserializedPdu = EntityStatePdu.Deserialize(buffer.AsSpan());

        deserializedPdu.EntityId.Value.ShouldBe(originalPdu.EntityId.Value);
        deserializedPdu.EntityType.ShouldBe(originalPdu.EntityType);
        deserializedPdu.SimulationReference.ShouldBe(originalPdu.SimulationReference);
        deserializedPdu.FederationReference.ShouldBe(originalPdu.FederationReference);
    }

    [Fact]
    public void Deserialize_WithInvalidMagic_ThrowsException()
    {
        var buffer = new byte[]
        {
            0, 3, 0, 0, 1, 0,
            0, 42,
            0, 2,
        };

        Action act = () => EntityStatePdu.Deserialize(buffer.AsSpan());

        act.ShouldThrow<DisValidationException>()
            .Message.ShouldContain("Invalid magic");
    }

    [Fact]
    public void Deserialize_WithInvalidVersion_ThrowsException()
    {
        var buffer = new byte[]
        {
            1, 2, 0, 0, 1, 0,
            0, 42,
            0, 2,
        };

        Action act = () => EntityStatePdu.Deserialize(buffer.AsSpan());

        act.ShouldThrow<DisValidationException>()
            .Message.ShouldContain("Invalid protocol version");
    }

    [Fact]
    public void Deserialize_WithInvalidPduType_ThrowsException()
    {
        var buffer = new byte[]
        {
            1, 3, 0, 0, 2, 0,
            0, 42,
            0, 2,
        };

        Action act = () => EntityStatePdu.Deserialize(buffer.AsSpan());

        act.ShouldThrow<DisValidationException>()
            .Message.ShouldContain("Invalid PDU type");
    }

    [Fact]
    public void Deserialize_BufferTooSmall_ThrowsException()
    {
        var buffer = new byte[] { 1, 3 };

        Action act = () => EntityStatePdu.Deserialize(buffer.AsSpan());

        act.ShouldThrow<ArgumentException>()
            .Message.ShouldContain("Buffer too small");
    }

    [Fact]
    public void Builder_Pattern_CreatesValidInstance()
    {
        var pdu = EntityStatePdu.Create()
            .WithEntityId(EntityId.Relative(100))
            .WithEntityType(EntityType.Generic)
            .WithLinearPosition(500.0, 600.0, 700.0)
            .WithAngularOrientation(0.5, 0.6, 0.7)
            .WithLinearVelocity(10.0, 20.0, 30.0)
            .WithAngularVelocity(0.1, 0.2, 0.3)
            .WithSimulationFederation(99, 88)
            .WithAdditionalState(new EntityStatePduAdditionalState())
            .WithNumberOfParts(1)
            .Build();

        pdu.EntityId.Value.ShouldBe(100);
        pdu.EntityType.ShouldBe(EntityType.Generic);
        pdu.LinearPosition.X.ShouldBe(500.0);
        pdu.SimulationReference.ShouldBe((byte)99);
        pdu.FederationReference.ShouldBe((byte)88);
    }

    [Fact]
    public void AdditionalState_WithKillFlag_IsKilledReturnsTrue()
    {
        var additionalState = new EntityStatePduAdditionalState(Flags: 0x01, AmmoState: 0, LaunchIndicator: 0, EmitterState: 0, ArticulationCount: 0, ArticulatedPartId: 0);

        additionalState.IsKilled.ShouldBeTrue();
    }

    [Fact]
    public void AdditionalState_WithDamageFlag_IsDamagedReturnsTrue()
    {
        var additionalState = new EntityStatePduAdditionalState(Flags: 0x02, AmmoState: 0, LaunchIndicator: 0, EmitterState: 0, ArticulationCount: 0, ArticulatedPartId: 0);

        additionalState.IsDamaged.ShouldBeTrue();
    }

    [Fact]
    public void AdditionalState_WithAmmoFlags_ReturnsCorrectIndicators()
    {
        // Bits 2-5 = Supplied ammo (value 7 = 0b0111), Bits 6-7 = Received ammo (value 3 = 0b11)
        // flags = 11_0111_00 = 0xDC
        var additionalState = new EntityStatePduAdditionalState(Flags: 0xDC, AmmoState: 0, LaunchIndicator: 0, EmitterState: 0, ArticulationCount: 0, ArticulatedPartId: 0);

        additionalState.SuppliedAmmoIndicator.ShouldBe((byte)7);
        additionalState.ReceivedAmmoIndicator.ShouldBe((byte)3);
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var additionalState = new EntityStatePduAdditionalState(
            Flags: 0x0F,
            AmmoState: 50,
            LaunchIndicator: 1234,
            EmitterState: 5678,
            ArticulationCount: 1,
            ArticulatedPartId: 2,
            ArticulationPositions: new short[] { 100 },
            ArticulationDirections: new short[] { -100 },
            ArticulatedPartStates: new byte[] { 255 },
            ArticulationOffsets: new byte[] { 128 }
        );

        var originalPdu = EntityStatePdu.Create()
            .WithEntityId(EntityId.Relative(65535))
            .WithEntityType(EntityType.Mixed)
            .WithLinearPosition(-1000.5, -2000.5, 3000.5)
            .WithAngularOrientation(-Math.PI, Math.PI / 2, -Math.PI / 4)
            .WithLinearVelocity(999.0, 888.0, 777.0)
            .WithAngularVelocity(-1.0, 1.0, 0.5)
            .WithSimulationFederation(255, 1)
            .WithAdditionalState(additionalState)
            .WithNumberOfParts(1)
            .Build();

        var buffer = new byte[originalPdu.ComputedLength()];
        originalPdu.Serialize(buffer);

        var deserializedPdu = EntityStatePdu.Deserialize(buffer.AsSpan());

        deserializedPdu.EntityId.Value.ShouldBe(originalPdu.EntityId.Value);
        deserializedPdu.EntityType.ShouldBe(originalPdu.EntityType);
        deserializedPdu.LinearPosition.X.ShouldBe(originalPdu.LinearPosition.X, 0.001);
        deserializedPdu.AngularOrientation.Y.ShouldBe(originalPdu.AngularOrientation.Y, 0.001);
        deserializedPdu.SimulationReference.ShouldBe(originalPdu.SimulationReference);
        deserializedPdu.FederationReference.ShouldBe(originalPdu.FederationReference);
        deserializedPdu.AdditionalState.Flags.ShouldBe(originalPdu.AdditionalState.Flags);
        deserializedPdu.AdditionalState.AmmoState.ShouldBe(originalPdu.AdditionalState.AmmoState);
        deserializedPdu.AdditionalState.LaunchIndicator.ShouldBe(originalPdu.AdditionalState.LaunchIndicator);
        deserializedPdu.AdditionalState.EmitterState.ShouldBe(originalPdu.AdditionalState.EmitterState);
    }

    private static EntityStatePduAdditionalState CreateTestAdditionalState() => new(0, 0, 0, 0, 0, 0);

    private static EntityStatePdu CreateTestEntityStatePdu() => EntityStatePdu.Create()
        .WithEntityId(EntityId.Relative(42))
        .WithEntityType(EntityType.PhysicalWithLocation)
        .WithLinearPosition(100.0, 200.0, 300.0)
        .WithAngularOrientation(0.1, 0.2, 0.3)
        .WithLinearVelocity(1.0, 2.0, 3.0)
        .WithAngularVelocity(0.01, 0.02, 0.03)
        .WithSimulationFederation(5, 10)
        .WithAdditionalState(CreateTestAdditionalState())
        .WithNumberOfParts(0)
        .Build();
}
