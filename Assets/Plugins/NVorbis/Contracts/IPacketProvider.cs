namespace NVorbis.Contracts
{
    /// <summary>
    /// Encapsulates a method that calculates the number of granules decodable from the specified packet.
    /// </summary>
    /// <param name="packet">The <see cref="IPacket"/> to calculate.</param>
    /// <returns>The calculated number of granules.</returns>
    public delegate int GetPacketGranuleCount(IPacket packet);

    /// <summary>
    /// Describes an interface for a packet stream reader.
    /// </summary>
    public interface IPacketProvider
    {
        /// <summary>
        /// Gets whether the provider supports seeking.
        /// </summary>
        bool CanSeek { get; }

        /// <summary>
        /// Gets the serial number of this provider's data stream.
        /// </summary>
        int StreamSerial { get; }

        /// <summary>
        /// Gets the next packet in the stream and advances to the next packet position.
        /// </summary>
        /// <returns>The <see cref="IPacket"/> instance for the next packet if available, otherwise <see langword="null"/>.</returns>
        IPacket GetNextPacket();

        /// <summary>
        /// Gets the next packet in the stream without advancing to the next packet position.
        /// </summary>
        /// <returns>The <see cref="IPacket"/> instance for the next packet if available, otherwise <see langword="null"/>.</returns>
        IPacket PeekNextPacket();

        /// <summary>
        /// Seeks the stream to the packet that is prior to the requested granule position by the specified preroll number of packets.
        /// </summary>
        /// <param name="granulePos">The granule position to seek to.</param>
        /// <param name="preRoll">The number of packets to seek backward prior to the granule position.</param>
        /// <param name="getPacketGranuleCount">A <see cref="GetPacketGranuleCount"/> delegate that returns the number of granules in the specified packet.</param>
        /// <returns>The granule position at the start of the packet containing the requested position.</returns>
        long SeekTo(long granulePos, int preRoll, GetPacketGranuleCount getPacketGranuleCount);

        /// <summary>
        /// Gets the total number of granule available in the stream.
        /// </summary>
        long GetGranuleCount();
    }
}
