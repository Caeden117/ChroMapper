using System;
using System.Collections.Generic;

namespace NVorbis.Contracts
{
    /// <summary>
    /// Encapsulates a method that initializes a stream reader, optionally ignoring the stream if desired.
    /// </summary>
    /// <param name="packetProvider">The <see cref="IPacketProvider"/> instance for the new stream.</param>
    /// <returns><see langword="true"/> to process the stream, otherwise <see langword="false"/>.</returns>
    public delegate bool NewStreamHandler(IPacketProvider packetProvider);

    /// <summary>
    /// Provides an interface for a Vorbis logical stream container.
    /// </summary>
    public interface IContainerReader : IDisposable
    {
        /// <summary>
        /// Gets or sets the callback to invoke when a new stream is encountered in the container.
        /// </summary>
        NewStreamHandler NewStreamCallback { get; set; }

        /// <summary>
        /// Returns a read-only list of the logical streams discovered in this container.
        /// </summary>
        IReadOnlyList<IPacketProvider> GetStreams();

        /// <summary>
        /// Gets whether the underlying stream can seek.
        /// </summary>
        bool CanSeek { get; }

        /// <summary>
        /// Gets the number of bits dedicated to container framing and overhead.
        /// </summary>
        long ContainerBits { get; }

        /// <summary>
        /// Gets the number of bits that were skipped due to container framing and overhead.
        /// </summary>
        long WasteBits { get; }

        /// <summary>
        /// Attempts to initialize the container.
        /// </summary>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
        bool TryInit();

        /// <summary>
        /// Searches for the next logical stream in the container.
        /// </summary>
        /// <returns><see langword="true"/> if a new stream was found, otherwise <see langword="false"/>.</returns>
        bool FindNextStream();
    }
}
