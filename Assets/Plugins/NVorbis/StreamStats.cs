using NVorbis.Contracts;

namespace NVorbis
{
    class StreamStats : IStreamStats
    {
        private int _sampleRate;

        private readonly int[] _packetBits = new int[2];
        private readonly int[] _packetSamples = new int[2];
        private int _packetIndex;

        private long _totalSamples;
        private long _audioBits;
        private long _headerBits;
        private long _containerBits;
        private long _wasteBits;

        private object _lock = new object();
        private int _packetCount;

        public int EffectiveBitRate
        {
            get
            {
                long samples, bits;
                lock (_lock)
                {
                    samples = _totalSamples;
                    bits = _audioBits + _headerBits + _containerBits + _wasteBits;
                }
                if (samples > 0)
                {
                    return (int)(((double)bits / samples) * _sampleRate);
                }
                return 0;
            }
        }

        public int InstantBitRate
        {
            get
            {
                int samples, bits;
                lock (_lock)
                {
                    bits = _packetBits[0] + _packetBits[1];
                    samples = _packetSamples[0] + _packetSamples[1];
                }
                if (samples > 0)
                {
                    return (int)(((double)bits / samples) * _sampleRate);
                }
                return 0;
            }
        }

        public long ContainerBits => _containerBits;

        public long OverheadBits => _headerBits;

        public long AudioBits => _audioBits;

        public long WasteBits => _wasteBits;

        public int PacketCount => _packetCount;

        public void ResetStats()
        {
            lock (_lock)
            {
                _packetBits[0] = _packetBits[1] = 0;
                _packetSamples[0] = _packetSamples[1] = 0;
                _packetIndex = 0;
                _packetCount = 0;
                _audioBits = 0;
                _totalSamples = 0;
                _headerBits = 0;
                _containerBits = 0;
                _wasteBits = 0;
            }
        }

        internal void SetSampleRate(int sampleRate)
        {
            lock (_lock)
            {
                _sampleRate = sampleRate;

                ResetStats();
            }
        }

        internal void AddPacket(int samples, int bits, int waste, int container)
        {
            lock (_lock)
            {
                if (samples >= 0)
                {
                    // audio packet
                    _audioBits += bits;
                    _wasteBits += waste;
                    _containerBits += container;
                    _totalSamples += samples;
                    _packetBits[_packetIndex] = bits + waste;
                    _packetSamples[_packetIndex] = samples;

                    if (++_packetIndex == 2)
                    {
                        _packetIndex = 0;
                    }
                }
                else
                {
                    // header packet
                    _headerBits += bits;
                    _wasteBits += waste;
                    _containerBits += container;
                }
            }
        }
    }
}
