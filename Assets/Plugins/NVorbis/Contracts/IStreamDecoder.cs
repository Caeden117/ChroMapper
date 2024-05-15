using System;
using System.IO;

namespace NVorbis.Contracts
{
    /// <summary>
    /// Describes a stream decoder instance for Vorbis data.
    /// </summary>
    public interface IStreamDecoder : IDisposable
    {
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
        /// Gets the tag data from the stream's header.
        /// </summary>
        ITagData Tags { get; }

        /// <summary>
        /// Gets the total duration of the decoded stream.
        /// </summary>
        TimeSpan TotalTime { get; }

        /// <summary>
        /// Gets the total number of samples in the decoded stream.
        /// </summary>
        long TotalSamples { get; }

        /// <summary>
        /// Gets or sets the current time position of the stream.
        /// </summary>
        TimeSpan TimePosition { get; set; }

        /// <summary>
        /// Gets or sets the current sample position of the stream.
        /// </summary>
        long SamplePosition { get; set; }

        /// <summary>
        /// Gets or sets whether to clip samples returned by <see cref="Read(Span&lt;float&gt;, int, int)"/>.
        /// </summary>
        bool ClipSamples { get; set; }

        /// <summary>
        /// Gets whether <see cref="Read(Span&lt;float&gt;, int, int)"/> has returned any clipped samples.
        /// </summary>
        bool HasClipped { get; }

        /// <summary>
        /// Gets whether the decoder has reached the end of the stream.
        /// </summary>
        bool IsEndOfStream { get; }

        /// <summary>
        /// Gets the <see cref="IStreamStats"/> instance for this stream.
        /// </summary>
        IStreamStats Stats { get; }

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

        /// <summary>
        /// Reads samples into the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read the samples into.</param>
        /// <param name="offset">The index to start reading samples into the buffer.</param>
        /// <param name="count">The number of samples that should be read into the buffer.  Must be a multiple of <see cref="Channels"/>.</param>
        /// <returns>The number of samples read into the buffer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the buffer is too small or <paramref name="offset"/> is less than zero.</exception>
        /// <remarks>The data populated into <paramref name="buffer"/> is interleaved by channel in normal PCM fashion: Left, Right, Left, Right, Left, Right</remarks>
        int Read(Span<float> buffer, int offset, int count);
    }
}
