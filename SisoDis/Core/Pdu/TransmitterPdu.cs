using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

public record struct TransmitterPdu(
    EntityId EntityId,
    ushort RadioId,
    ushort RadioReference,
    byte TransmitState,
    byte InputSource,
    ushort Padding,
    double Frequency,
    double TransmitBandwidth,
    uint Power,
    ushort ModulationType,
    ushort CryptoSystem,
    Vector3Double AntennaLocation,
    ushort AntennaPatternType,
    ushort AntennaPatternCount,
    ushort NumberOfModulationParameters,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    public const ushort PdTypeValue = 25;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 74;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), RadioId);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), RadioReference);
        offset += 2;

        buffer[offset] = TransmitState;
        offset++;

        buffer[offset] = InputSource;
        offset++;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), Padding);
        offset += 2;

        BinaryPrimitives.WriteDoubleBigEndian(buffer.Slice(offset, 8), Frequency);
        offset += 8;

        BinaryPrimitives.WriteDoubleBigEndian(buffer.Slice(offset, 8), TransmitBandwidth);
        offset += 8;

        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), Power);
        offset += 4;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), ModulationType);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), CryptoSystem);
        offset += 2;

        BinaryPrimitives.WriteDoubleBigEndian(buffer.Slice(offset, 8), AntennaLocation.X);
        offset += 8;
        BinaryPrimitives.WriteDoubleBigEndian(buffer.Slice(offset, 8), AntennaLocation.Y);
        offset += 8;
        BinaryPrimitives.WriteDoubleBigEndian(buffer.Slice(offset, 8), AntennaLocation.Z);
        offset += 8;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), AntennaPatternType);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), AntennaPatternCount);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), NumberOfModulationParameters);
        offset += 2;

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static TransmitterPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
    {
        if (buffer.Length < offset + PduHeader.HeaderLength)
            throw new ArgumentException("Buffer too small for header", nameof(buffer));

        byte magic = buffer[offset];
        if (magic != 1)
            throw new DisValidationException($"Invalid magic: expected 1, got {magic}");

        byte versionMajor = buffer[offset + 1];
        if (versionMajor != 3)
            throw new DisValidationException($"Invalid protocol version: expected 3, got {versionMajor}");

        ushort actualType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 4, 2));
        if (actualType != PdTypeValue)
            throw new DisValidationException($"Invalid PDU type: expected {PdTypeValue}, got {actualType}");

        int pos = offset + PduHeader.HeaderLength;

        ushort entityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId entityId = new EntityId(entityIdValue);
        pos += 2;

        ushort radioId = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort radioReference = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        byte transmitState = buffer[pos];
        pos++;

        byte inputSource = buffer[pos];
        pos++;

        ushort padding = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        double frequency = BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(pos, 8));
        pos += 8;

        double transmitBandwidth = BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(pos, 8));
        pos += 8;

        uint power = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
        pos += 4;

        ushort modulationType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort cryptoSystem = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        Vector3Double antennaLocation = new Vector3Double(
            BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(pos, 8)),
            BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(pos + 8, 8)),
            BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(pos + 16, 8))
        );
        pos += 24;

        ushort antennaPatternType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort antennaPatternCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort numberOfModulationParameters = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new TransmitterPdu(
            entityId,
            radioId,
            radioReference,
            transmitState,
            inputSource,
            padding,
            frequency,
            transmitBandwidth,
            power,
            modulationType,
            cryptoSystem,
            antennaLocation,
            antennaPatternType,
            antennaPatternCount,
            numberOfModulationParameters,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private EntityId _entityId = new(0);
        private ushort _radioId = 0;
        private ushort _radioReference = 0;
        private byte _transmitState = 0;
        private byte _inputSource = 0;
        private ushort _padding = 0;
        private double _frequency = 0;
        private double _transmitBandwidth = 0;
        private uint _power = 0;
        private ushort _modulationType = 0;
        private ushort _cryptoSystem = 0;
        private Vector3Double _antennaLocation = new();
        private ushort _antennaPatternType = 0;
        private ushort _antennaPatternCount = 0;
        private ushort _numberOfModulationParameters = 0;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithEntityId(EntityId id) { _entityId = id; return this; }
        public Builder WithRadioId(ushort id) { _radioId = id; return this; }
        public Builder WithRadioReference(ushort reference) { _radioReference = reference; return this; }
        public Builder WithTransmitState(byte state) { _transmitState = state; return this; }
        public Builder WithInputSource(byte source) { _inputSource = source; return this; }
        public Builder WithPadding(ushort padding) { _padding = padding; return this; }
        public Builder WithFrequency(double freq) { _frequency = freq; return this; }
        public Builder WithTransmitBandwidth(double bw) { _transmitBandwidth = bw; return this; }
        public Builder WithPower(uint pwr) { _power = pwr; return this; }
        public Builder WithModulationType(ushort type) { _modulationType = type; return this; }
        public Builder WithCryptoSystem(ushort system) { _cryptoSystem = system; return this; }
        public Builder WithAntennaLocation(Vector3Double loc) { _antennaLocation = loc; return this; }
        public Builder WithAntennaPatternType(ushort type) { _antennaPatternType = type; return this; }
        public Builder WithAntennaPatternCount(ushort count) { _antennaPatternCount = count; return this; }
        public Builder WithNumberOfModulationParameters(ushort count) { _numberOfModulationParameters = count; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public TransmitterPdu Build() => new(
            _entityId,
            _radioId,
            _radioReference,
            _transmitState,
            _inputSource,
            _padding,
            _frequency,
            _transmitBandwidth,
            _power,
            _modulationType,
            _cryptoSystem,
            _antennaLocation,
            _antennaPatternType,
            _antennaPatternCount,
            _numberOfModulationParameters,
            _simulationRef,
            _federationRef
        );
    }
}