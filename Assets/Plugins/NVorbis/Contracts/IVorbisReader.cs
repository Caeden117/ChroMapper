using System;
using System.Collections.Generic;
using System.IO;

namespace NVorbis.Contracts
{
    /// <summary>
    /// Describes the interface for <see cref="VorbisReader"/>.
    /// </summary>
    public interface IVorbisReader : IDisposable
    {
        /// <summary>
        /// Raised when a new stream has been encountered in the file or container.
        /// </summary>
        event EventHandler<NewStreamEventArgs> NewStream;

        /// <summary>
        /// Gets the number of bits read that are related to framing and transport alone.
        /// </summary>
        long ContainerOverheadBits { get; }

        /// <summary>
        /// Gets the number of bits skipped in the container due to framing, ignored streams, or sync loss.
        /// </summary>
        long ContainerWasteBits { get; }

        /// <summary>
        /// Gets the list of <see cref="IStreamDecoder"/> instances associated with the loaded file / container.
        /// </summary>
        IReadOnlyList<IStreamDecoder> Streams { get; }

        /// <summary>
        /// Gets the currently-selected stream's index.
        /// </summary>
        int StreamIndex { get; }

        /// <summary>
        /// Gets the number of channels in the stream.
        /// </summary>
        int Channels { get; }

        /// <summary>
        /// Gets the sample rate of the stream.
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// Gets the upper bitrate limit for the stream, if specified.
        /// </summary>
        int UpperBitrate { get; }

        /// <summary>
        /// Gets the nominal bitrate of the stream, if specified.  May be calculated from <see cref="LowerBitrate"/> and <see cref="UpperBitrate"/>.
        /// </summary>
        int NominalBitrate { get; }

        /// <summary>
        /// Gets the lower bitrate limit for the stream, if specified.
        /// </summary>
        int LowerBitrate { get; }

        /// <summary>
        /// Gets the total duration of the decoded stream.
        /// </summary>
        TimeSpan TotalTime { get; }

        /// <summary>
        /// Gets the total number of samples in the decoded stream.
        /// </summary>
        long TotalSamples { get; }

        /// <summary>
        /// Gets or sets whether to clip samples returned by <see cref="ReadSamples(float[], int, int)"/>.
        /// </summary>
        bool ClipSamples { get; set; }

        /// <summary>
        /// Gets or sets the current time position of the stream.
        /// </summary>
        TimeSpan TimePosition { get; set; }

        /// <summary>
        /// Gets or sets the current sample position of the stream.
        /// </summary>
        long SamplePosition { get; set; }

        /// <summary>
        /// Gets whether <see cref="ReadSamples(float[], int, int)"/> has returned any clipped samples.
        /// </summary>
        bool HasClipped { get; }

        /// <summary>
        /// Gets whether the current stream has ended.
        /// </summary>
        bool IsEndOfStream { get; }

        /// <summary>
        /// Gets the <see cref="IStreamStats"/> instance for this stream.
        /// </summary>
        IStreamStats StreamStats { get; }

        /// <summary>
        /// Gets the tag data from the stream's header.
        /// </summary>
        ITagData Tags { get; }

        /// <summary>
        /// Searches for the next stream in a concatenated file.  Will raise <see cref="NewStream"/> for the found stream, and will add it to <see cref="Streams"/> if not marked as ignored.
        /// </summary>
        /// <returns><see langword="true"/> if a new stream was found, otherwise <see langword="false"/>.</returns>
        bool FindNextStream();

        /// <summary>
        /// Switches to an alternate logical stream.
        /// </summary>
        /// <param name="index">The logical stream index to switch to</param>
        /// <returns><see langword="true"/> if the properties of the logical stream differ from those of the one previously being decoded. Otherwise, <see langword="false"/>.</returns>
        bool SwitchStreams(int index);

        /// <summary>
        /// Reads samples into the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read the samples into.</param>
        /// <param name="offset">The index to start reading samples into the buffer.</param>
        /// <param name="count">The number of samples that should be read into the buffer.  Must be a multiple of <see cref="Channels"/>.</param>
        /// <returns>The number of samples read into the buffer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the buffer is too small or <paramref name="offset"/> is less than zero.</exception>
        /// <remarks>The data populated into <paramref name="buffer"/> is interleaved by channel in normal PCM fashion: Left, Right, Left, Right, Left, Right</remarks>
        int ReadSamples(float[] buffer, int offset, int count);

        /// <summary>
        /// Seeks the stream by the specified duration.
        /// </summary>
        /// <param name="timePosition">The relative time to seek to.</param>
        /// <param name="seekOrigin">The reference point used to obtain the new position.</param>
        void SeekTo(TimeSpan timePosition, SeekOrigin seekOrigin = SeekOrigin.Begin);

        /// <summary>
        /// Seeks the stream by the specified sample count.
        /// </summary>
        /// <param name="samplePosition">The relative sample position to seek to.</param>
        /// <param name="seekOrigin">The reference point used to obtain the new position.</param>
        void SeekTo(long samplePosition, SeekOrigin seekOrigin = SeekOrigin.Begin);
    }
}
