using NVorbis.Contracts;
using System;

namespace NVorbis
{
    class Mode : IMode
    {
        private struct OverlapInfo
        {
            public int PacketStartIndex;
            public int PacketTotalLength;
            public int PacketValidLength;
        }

        const float M_PI2 = 3.1415926539f / 2;

        int _channels;
        bool _blockFlag;
        int _blockSize;
        IMapping _mapping;
        float[][] _windows;
        OverlapInfo[] _overlapInfo;

        public void Init(IPacket packet, int channels, int block0Size, int block1Size, IMapping[] mappings)
        {
            _channels = channels;

            _blockFlag = packet.ReadBit();
            if (0 != packet.ReadBits(32))
            {
                throw new System.IO.InvalidDataException("Mode header had invalid window or transform type!");
            }

            var mappingIdx = (int)packet.ReadBits(8);
            if (mappingIdx >= mappings.Length)
            {
                throw new System.IO.InvalidDataException("Mode header had invalid mapping index!");
            }
            _mapping = mappings[mappingIdx];

            if (_blockFlag)
            {
                _blockSize = block1Size;
                _windows = new float[][]
                {
                    CalcWindow(block0Size, block1Size, block0Size),
                    CalcWindow(block1Size, block1Size, block0Size),
                    CalcWindow(block0Size, block1Size, block1Size),
                    CalcWindow(block1Size, block1Size, block1Size),
                };
                _overlapInfo = new OverlapInfo[]
                {
                    CalcOverlap(block0Size, block1Size, block0Size),
                    CalcOverlap(block1Size, block1Size, block0Size),
                    CalcOverlap(block0Size, block1Size, block1Size),
                    CalcOverlap(block1Size, block1Size, block1Size),
                };
            }
            else
            {
                _blockSize = block0Size;
                _windows = new float[][]
                {
                    CalcWindow(block0Size, block0Size, block0Size),
                };
            }
        }

        private static float[] CalcWindow(int prevBlockSize, int blockSize, int nextBlockSize)
        {
            var array = new float[blockSize];

            var left = prevBlockSize / 2;
            var wnd = blockSize;
            var right = nextBlockSize / 2;

            var leftbegin = wnd / 4 - left / 2;
            var rightbegin = wnd - wnd / 4 - right / 2;

            for (int i = 0; i < left; i++)
            {
                var x = (float)Math.Sin((i + .5) / left * M_PI2);
                x *= x;
                array[leftbegin + i] = (float)Math.Sin(x * M_PI2);
            }

            for (int i = leftbegin + left; i < rightbegin; i++)
            {
                array[i] = 1.0f;
            }

            for (int i = 0; i < right; i++)
            {
                var x = (float)Math.Sin((right - i - .5) / right * M_PI2);
                x *= x;
                array[rightbegin + i] = (float)Math.Sin(x * M_PI2);
            }
        
            return array;
        }

        private static OverlapInfo CalcOverlap(int prevBlockSize, int blockSize, int nextBlockSize)
        {
            var leftOverlapHalfSize = prevBlockSize / 4;
            var rightOverlapHalfSize = nextBlockSize / 4;

            var packetStartIndex = blockSize / 4 - leftOverlapHalfSize;
            var packetTotalLength = blockSize / 4 * 3 + rightOverlapHalfSize;
            var packetValidLength = packetTotalLength - rightOverlapHalfSize * 2;
        
            return new OverlapInfo
            {
                PacketStartIndex = packetStartIndex,
                PacketValidLength = packetValidLength,
                PacketTotalLength = packetTotalLength,
            };
        }

        private bool GetPacketInfo(IPacket packet, out int windowIndex, out int packetStartIndex, out int packetValidLength, out int packetTotalLength)
        {
            if (packet.IsShort)
            {
                windowIndex = 0;
                packetStartIndex = 0;
                packetValidLength = 0;
                packetTotalLength = 0;
                return false;
            }

            if (_blockFlag)
            {
                var prevFlag = packet.ReadBit();
                var nextFlag = packet.ReadBit();

                windowIndex = (prevFlag ? 1 : 0) + (nextFlag ? 2 : 0);

                var overlapInfo = _overlapInfo[windowIndex];
                packetStartIndex = overlapInfo.PacketStartIndex;
                packetValidLength = overlapInfo.PacketValidLength;
                packetTotalLength = overlapInfo.PacketTotalLength;
            }
            else
            {
                windowIndex = 0;
                packetStartIndex = 0;
                packetValidLength = _blockSize / 2;
                packetTotalLength = _blockSize;
            }

            return true;
        }

        public bool Decode(IPacket packet, float[][] buffer, out int packetStartindex, out int packetValidLength, out int packetTotalLength)
        {
            if (GetPacketInfo(packet, out var windowIndex, out packetStartindex, out packetValidLength, out packetTotalLength))
            {
                _mapping.DecodePacket(packet, _blockSize, _channels, buffer);

                var window = _windows[windowIndex];
                for (var i = 0; i < _blockSize; i++)
                {
                    for (var ch = 0; ch < _channels; ch++)
                    {
                        buffer[ch][i] *= window[i];
                    }
                }
                return true;
            }
            return false;
        }

        public int GetPacketSampleCount(IPacket packet)
        {
            GetPacketInfo(packet, out _, out var packetStartIndex, out var packetValidLength, out _);
            return packetValidLength - packetStartIndex;
        }
    }
}
