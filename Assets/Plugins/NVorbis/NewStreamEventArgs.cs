using NVorbis.Contracts;
using System;

namespace NVorbis
{
    /// <summary>
    /// Event data for when a new logical stream is found in a container.
    /// </summary>
    [Serializable]
    public class NewStreamEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="NewStreamEventArgs"/> with the specified <see cref="IStreamDecoder"/>.
        /// </summary>
        /// <param name="streamDecoder">An <see cref="IStreamDecoder"/> instance.</param>
        public NewStreamEventArgs(IStreamDecoder streamDecoder)
        {
            StreamDecoder = streamDecoder ?? throw new ArgumentNullException(nameof(streamDecoder));
        }

        /// <summary>
        /// Gets new the <see cref="IStreamDecoder"/> instance.
        /// </summary>
        public IStreamDecoder StreamDecoder { get; }

        /// <summary>
        /// Gets or sets whether to ignore the logical stream associated with the packet provider.
        /// </summary>
        public bool IgnoreStream { get; set; }
    }
}
