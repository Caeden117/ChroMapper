using System;

namespace NVorbis
{
    /// <summary>
    /// Old interface, current version moved to Contracts.IPacketProvider
    /// </summary>
    [Obsolete("Moved to NVorbis.Contracts.IPacketProvider", true)]
    public interface IPacketProvider : Contracts.IPacketProvider
    {
        /// <summary>
        /// Gets the number of bits of overhead in this stream's container.
        /// </summary>
        [Obsolete("Moved to per-stream IStreamStats instance on IStreamDecoder.Stats or VorbisReader.Stats.", true)]
        long ContainerBits { get; }

        /// <summary>
        /// Retrieves the total number of pages (or frames) this stream uses.
        /// </summary>
        [Obsolete("No longer supported.", true)]
        int GetTotalPageCount();

        /// <summary>
        /// Retrieves the packet specified from the stream.
        /// </summary>
        [Obsolete("Getting a packet by index is no longer supported.", true)]
        DataPacket GetPacket(int packetIndex);

        /// <summary>
        /// Finds the packet index to the granule position specified in the current stream.
        /// </summary>
        [Obsolete("Moved to long SeekTo(long, int, GetPacketGranuleCount)", true)]
        DataPacket FindPacket(long granulePos, Func<DataPacket, DataPacket, int> packetGranuleCountCallback);

        /// <summary>
        /// Sets the next packet to be returned, applying a pre-roll as necessary.
        /// </summary>
        [Obsolete("Seeking to a specified packet is no longer supported.  See SeekTo(...) instead.", true)]
        void SeekToPacket(DataPacket packet, int preRoll);

        /// <summary>
        /// Occurs when the stream is about to change parameters.
        /// </summary>
        [Obsolete("No longer supported.", true)]
        event EventHandler ParameterChange;
    }
}
