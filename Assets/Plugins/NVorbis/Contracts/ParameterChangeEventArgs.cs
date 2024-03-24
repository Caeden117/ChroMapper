using System;

namespace NVorbis.Contracts
{
    /// <summary>
    /// Contains data for parameter change events.
    /// </summary>
    public class ParameterChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="ParameterChangeEventArgs"/>.
        /// </summary>
        /// <param name="channels">The new number of channels.</param>
        /// <param name="sampleRate">The new sample rate.</param>
        public ParameterChangeEventArgs(int channels, int sampleRate)
        {
            Channels = channels;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Get the new number of channels in the stream.
        /// </summary>
        public int Channels { get; }

        /// <summary>
        /// Gets the new sample rate of the stream.
        /// </summary>
        public int SampleRate { get; }
    }
}
