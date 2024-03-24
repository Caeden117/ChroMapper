using NVorbis.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NVorbis
{
    /// <summary>
    /// Implements an easy to use wrapper around <see cref="Contracts.IContainerReader"/> and <see cref="IStreamDecoder"/>.
    /// </summary>
    public sealed class VorbisReader : IVorbisReader
    {
        internal static Func<Stream, bool, Contracts.IContainerReader> CreateContainerReader { get; set; } = (s, cod) => new Ogg.ContainerReader(s, cod);
        internal static Func<Contracts.IPacketProvider, IStreamDecoder> CreateStreamDecoder { get; set; } = pp => new StreamDecoder(pp, new Factory());

        private readonly List<IStreamDecoder> _decoders;
        private readonly Contracts.IContainerReader _containerReader;
        private readonly bool _closeOnDispose;

        private IStreamDecoder _streamDecoder;

        /// <summary>
        /// Raised when a new stream has been encountered in the file or container.
        /// </summary>
        public event EventHandler<NewStreamEventArgs> NewStream;

        /// <summary>
        /// Creates a new instance of <see cref="VorbisReader"/> reading from the specified file.
        /// </summary>
        /// <param name="fileName">The file to read from.</param>
        public VorbisReader(string fileName)
            : this(File.OpenRead(fileName), true)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="VorbisReader"/> reading from the specified stream, optionally taking ownership of it.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <param name="closeOnDispose"><see langword="true"/> to take ownership and clean up the instance when disposed, otherwise <see langword="false"/>.</param>
        public VorbisReader(Stream stream, bool closeOnDispose = true)
        {
            _decoders = new List<IStreamDecoder>();

            var containerReader = CreateContainerReader(stream, closeOnDispose);
            containerReader.NewStreamCallback = ProcessNewStream;

            if (!containerReader.TryInit() || _decoders.Count == 0)
            {
                containerReader.NewStreamCallback = null;
                containerReader.Dispose();

                if (closeOnDispose)
                {
                    stream.Dispose();
                }

                throw new ArgumentException("Could not load the specified container!", nameof(containerReader));
            }
            _closeOnDispose = closeOnDispose;
            _containerReader = containerReader;
            _streamDecoder = _decoders[0];
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        [Obsolete("Use \"new StreamDecoder(Contracts.IPacketProvider)\" and the container's NewStreamCallback or Streams property instead.", true)]
        public VorbisReader(Contracts.IContainerReader containerReader) => throw new NotSupportedException();

        [Obsolete("Use \"new StreamDecoder(Contracts.IPacketProvider)\" instead.", true)]
        public VorbisReader(Contracts.IPacketProvider packetProvider) => throw new NotSupportedException();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private bool ProcessNewStream(Contracts.IPacketProvider packetProvider)
        {
            var decoder = CreateStreamDecoder(packetProvider);
            decoder.ClipSamples = true;

            var ea = new NewStreamEventArgs(decoder);
            NewStream?.Invoke(this, ea);
            if (!ea.IgnoreStream)
            {
                _decoders.Add(decoder);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cleans up this instance.
        /// </summary>
        public void Dispose()
        {
            if (_decoders != null)
            {
                foreach (var decoder in _decoders)
                {
                    decoder.Dispose();
                }
                _decoders.Clear();
            }

            if (_containerReader != null)
            {
                _containerReader.NewStreamCallback = null;
                if (_closeOnDispose)
                {
                    _containerReader.Dispose();
                }
            }
        }

        /// <summary>
        /// Gets the list of <see cref="IStreamDecoder"/> instances associated with the loaded file / container.
        /// </summary>
        public IReadOnlyList<IStreamDecoder> Streams => _decoders;

        #region Convenience Helpers

        // Since most uses of VorbisReader are for single-stream audio files, we can make life simpler for users
        //  by exposing the first stream's properties and methods here.

        /// <summary>
        /// Gets the number of channels in the stream.
        /// </summary>
        public int Channels => _streamDecoder.Channels;

        /// <summary>
        /// Gets the sample rate of the stream.
        /// </summary>
        public int SampleRate => _streamDecoder.SampleRate;

        /// <summary>
        /// Gets the upper bitrate limit for the stream, if specified.
        /// </summary>
        public int UpperBitrate => _streamDecoder.UpperBitrate;

        /// <summary>
        /// Gets the nominal bitrate of the stream, if specified.  May be calculated from <see cref="LowerBitrate"/> and <see cref="UpperBitrate"/>.
        /// </summary>
        public int NominalBitrate => _streamDecoder.NominalBitrate;

        /// <summary>
        /// Gets the lower bitrate limit for the stream, if specified.
        /// </summary>
        public int LowerBitrate => _streamDecoder.LowerBitrate;

        /// <summary>
        /// Gets the tag data from the stream's header.
        /// </summary>
        public ITagData Tags => _streamDecoder.Tags;

        /// <summary>
        /// Gets the encoder's vendor string for the current selected Vorbis stream
        /// </summary>
        [Obsolete("Use .Tags.EncoderVendor instead.")]
        public string Vendor => _streamDecoder.Tags.EncoderVendor;

        /// <summary>
        /// Gets the comments in the current selected Vorbis stream
        /// </summary>
        [Obsolete("Use .Tags.All instead.")]
        public string[] Comments => _streamDecoder.Tags.All.SelectMany(k => k.Value, (kvp, Item) => $"{kvp.Key}={Item}").ToArray();

        /// <summary>
        /// Gets whether the previous short sample count was due to a parameter change in the stream.
        /// </summary>
        [Obsolete("No longer supported.  Will receive a new stream when parameters change.", true)]
        public bool IsParameterChange => throw new NotSupportedException();

        /// <summary>
        /// Gets the number of bits read that are related to framing and transport alone.
        /// </summary>
        public long ContainerOverheadBits => _containerReader?.ContainerBits ?? 0;

        /// <summary>
        /// Gets the number of bits skipped in the container due to framing, ignored streams, or sync loss.
        /// </summary>
        public long ContainerWasteBits => _containerReader?.WasteBits ?? 0;

        /// <summary>
        /// Gets the currently-selected stream's index.
        /// </summary>
        public int StreamIndex => _decoders.IndexOf(_streamDecoder);

        /// <summary>
        /// Returns the number of logical streams found so far in the physical container.
        /// </summary>
        [Obsolete("Use .Streams.Count instead.")]
        public int StreamCount => _decoders.Count;

        /// <summary>
        /// Gets or Sets the current timestamp of the decoder.  Is the timestamp before the next sample to be decoded.
        /// </summary>
        [Obsolete("Use VorbisReader.TimePosition instead.")]
        public TimeSpan DecodedTime
        {
            get => _streamDecoder.TimePosition;
            set => TimePosition = value;
        }

        /// <summary>
        /// Gets or Sets the current position of the next sample to be decoded.
        /// </summary>
        [Obsolete("Use VorbisReader.SamplePosition instead.")]
        public long DecodedPosition
        {
            get => _streamDecoder.SamplePosition;
            set => SamplePosition = value;
        }

        /// <summary>
        /// Gets the total duration of the decoded stream.
        /// </summary>
        public TimeSpan TotalTime => _streamDecoder.TotalTime;

        /// <summary>
        /// Gets the total number of samples in the decoded stream.
        /// </summary>
        public long TotalSamples => _streamDecoder.TotalSamples;

        /// <summary>
        /// Gets or sets the current time position of the stream.
        /// </summary>
        public TimeSpan TimePosition
        {
            get => _streamDecoder.TimePosition;
            set
            {
                _streamDecoder.TimePosition = value;
            }
        }

        /// <summary>
        /// Gets or sets the current sample position of the stream.
        /// </summary>
        public long SamplePosition
        {
            get => _streamDecoder.SamplePosition;
            set
            {
                _streamDecoder.SamplePosition = value;
            }
        }

        /// <summary>
        /// Gets whether the current stream has ended.
        /// </summary>
        public bool IsEndOfStream => _streamDecoder.IsEndOfStream;

        /// <summary>
        /// Gets or sets whether to clip samples returned by <see cref="ReadSamples(float[], int, int)"/>.
        /// </summary>
        public bool ClipSamples
        {
            get => _streamDecoder.ClipSamples;
            set => _streamDecoder.ClipSamples = value;
        }

        /// <summary>
        /// Gets whether <see cref="ReadSamples(float[], int, int)"/> has returned any clipped samples.
        /// </summary>
        public bool HasClipped => _streamDecoder.HasClipped;

        /// <summary>
        /// Gets the <see cref="IStreamStats"/> instance for this stream.
        /// </summary>
        public IStreamStats StreamStats => _streamDecoder.Stats;

        /// <summary>
        /// Gtes stats from each decoder stream available.
        /// </summary>
        [Obsolete("Use Streams[*].Stats instead.", true)]
        public IVorbisStreamStatus[] Stats => throw new NotSupportedException();

        /// <summary>
        /// Searches for the next stream in a concatenated file.  Will raise <see cref="NewStream"/> for the found stream, and will add it to <see cref="Streams"/> if not marked as ignored.
        /// </summary>
        /// <returns><see langword="true"/> if a new stream was found, otherwise <see langword="false"/>.</returns>
        public bool FindNextStream()
        {
            if (_containerReader == null) return false;
            return _containerReader.FindNextStream();
        }

        /// <summary>
        /// Switches to an alternate logical stream.
        /// </summary>
        /// <param name="index">The logical stream index to switch to</param>
        /// <returns><see langword="true"/> if the properties of the logical stream differ from those of the one previously being decoded. Otherwise, <see langword="false"/>.</returns>
        public bool SwitchStreams(int index)
        {
            if (index < 0 || index >= _decoders.Count) throw new ArgumentOutOfRangeException(nameof(index));

            var newDecoder = _decoders[index];
            var oldDecoder = _streamDecoder;
            if (newDecoder == oldDecoder) return false;

            // carry-through the clipping setting
            newDecoder.ClipSamples = oldDecoder.ClipSamples;

            _streamDecoder = newDecoder;

            return newDecoder.Channels != oldDecoder.Channels || newDecoder.SampleRate != oldDecoder.SampleRate;
        }

        /// <summary>
        /// Seeks the stream by the specified duration.
        /// </summary>
        /// <param name="timePosition">The relative time to seek to.</param>
        /// <param name="seekOrigin">The reference point used to obtain the new position.</param>
        public void SeekTo(TimeSpan timePosition, SeekOrigin seekOrigin = SeekOrigin.Begin)
        {
            _streamDecoder.SeekTo(timePosition, seekOrigin);
        }

        /// <summary>
        /// Seeks the stream by the specified sample count.
        /// </summary>
        /// <param name="samplePosition">The relative sample position to seek to.</param>
        /// <param name="seekOrigin">The reference point used to obtain the new position.</param>
        public void SeekTo(long samplePosition, SeekOrigin seekOrigin = SeekOrigin.Begin)
        {
            _streamDecoder.SeekTo(samplePosition, seekOrigin);
        }

        /// <summary>
        /// Reads samples into the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read the samples into.</param>
        /// <param name="offset">The index to start reading samples into the buffer.</param>
        /// <param name="count">The number of samples that should be read into the buffer.</param>
        /// <returns>The number of floats read into the buffer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the buffer is too small or <paramref name="offset"/> is less than zero.</exception>
        /// <remarks>The data populated into <paramref name="buffer"/> is interleaved by channel in normal PCM fashion: Left, Right, Left, Right, Left, Right</remarks>
        public int ReadSamples(float[] buffer, int offset, int count)
        {
            // don't allow non-aligned reads (always on a full sample boundary!)
            count -= count % _streamDecoder.Channels;
            if (count > 0)
            {
                return _streamDecoder.Read(buffer, offset, count);
            }
            return 0;
        }

        /// <summary>
        /// Reads samples into the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read the samples into.</param>
        /// <returns>The number of floats read into the buffer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the buffer is too small.</exception>
        /// <remarks>The data populated into <paramref name="buffer"/> is interleaved by channel in normal PCM fashion: Left, Right, Left, Right, Left, Right</remarks>
        public int ReadSamples(Span<float> buffer)
        {
            // don't allow non-aligned reads (always on a full sample boundary!)
            int count = buffer.Length - buffer.Length % _streamDecoder.Channels;
            if (!buffer.IsEmpty)
            {
                return _streamDecoder.Read(buffer, 0, count);
            }
            return 0;
        }

        /// <summary>
        /// Acknowledges a parameter change as signalled by <see cref="ReadSamples(float[], int, int)"/>.
        /// </summary>
        [Obsolete("No longer needed.", true)]
        public void ClearParameterChange() => throw new NotSupportedException();

        #endregion
    }
}
