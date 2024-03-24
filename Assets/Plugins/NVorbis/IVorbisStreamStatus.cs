using System;

namespace NVorbis
{
    /// <summary>
    /// Backwards compatibility type
    /// </summary>
    [Obsolete("Moved to NVorbis.Contracts.IStreamStats", true)]
    public interface IVorbisStreamStatus : Contracts.IStreamStats
    {
        /// <summary>
        /// Gets the calculated latency per page
        /// </summary>
        [Obsolete("No longer supported.", true)]
        TimeSpan PageLatency { get; }

        /// <summary>
        /// Gets the calculated latency per packet
        /// </summary>
        [Obsolete("No longer supported.", true)]
        TimeSpan PacketLatency { get; }

        /// <summary>
        /// Gets the calculated latency per second of output
        /// </summary>
        [Obsolete("No longer supported.", true)]
        TimeSpan SecondLatency { get; }

        /// <summary>
        /// Gets the number of pages read so far in the current stream
        /// </summary>
        [Obsolete("No longer supported.", true)]
        int PagesRead { get; }

        /// <summary>
        /// Gets the total number of pages in the current stream
        /// </summary>
        [Obsolete("No longer supported.", true)]
        int TotalPages { get; }

        /// <summary>
        /// Gets whether the stream has been clipped since the last reset
        /// </summary>
        [Obsolete("Use IStreamDecoder.HasClipped instead.  VorbisReader.HasClipped will return the same value for the stream it is handling.", true)]
        bool Clipped { get; }
    }
}
