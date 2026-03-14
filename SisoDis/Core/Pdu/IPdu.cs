using System.Buffers.Binary;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Core contract for all DIS PDUs per IEEE 1278.1-2012 §5.3.3.1.
/// </summary>
/// <remarks>Adds Serialize, DeserializeBody to the marker; enables factory registration pattern.</remarks>
public interface IPdu
{
    /// <summary>The DIS protocol magic number (must be 1 or 0x01 per specification).</summary>
    byte Magic { get; }
    
    /// <summary>The DIS protocol version major number per IEEE standard.</summary>
    byte ProtocolVersion { get; }
    
    /// <summary>The PDU type code (disambiguates PDU family per §5.3).</summary>
    ushort PdType { get; }

    /// <summary>Gets the total length of this PDU in bytes including header.</summary>
    int ComputedLength();

    void SerializeBody(Span<byte> buffer, int offset);
}
