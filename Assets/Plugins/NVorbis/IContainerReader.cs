using System;

namespace NVorbis
{
    /// <summary>
    ///  Old interface, current version moved to Contracts.IContainerReader
    /// </summary>
    [Obsolete("Moved to NVorbis.Contracts.IContainerReader", true)]
    public interface IContainerReader : Contracts.IContainerReader
    {
        /// <summary>
        /// Gets the list of stream serials found in the container so far.
        /// </summary>
        [Obsolete("Use Streams.Select(s => s.StreamSerial).ToArray() instead.", true)]
        int[] StreamSerials { get; }

        /// <summary>
        /// Gets the number of pages that have been read in the container.
        /// </summary>
        [Obsolete("No longer supported.", true)]
        int PagesRead { get; }

        /// <summary>
        /// Event raised when a new logical stream is found in the container.
        /// </summary>
        [Obsolete("Moved to NewStreamCallback.", true)]
        event EventHandler<NewStreamEventArgs> NewStream;

        /// <summary>
        /// Initializes the container and finds the first stream.
        /// </summary>
        [Obsolete("Renamed to TryInit().", true)]
        bool Init();

        /// <summary>
        /// Retrieves the total number of pages in the container.
        /// </summary>
        [Obsolete("No longer supported.", true)]
        int GetTotalPageCount();
    }
}
