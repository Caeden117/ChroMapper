using NVorbis.Contracts.Ogg;
using System;
using System.Collections.Generic;

namespace NVorbis.Ogg
{
    internal class Packet : DataPacket
    {
        // size with 1-2 packet segments (> 2 packet segments should be very uncommon):
        //   x86:  68 bytes
        //   x64: 104 bytes

        // this is the list of pages & packets in packed 24:8 format
        // in theory, this is good for up to 1016 GiB of Ogg file
        // in practice, probably closer to 300 days @ 160k bps
        private IReadOnlyList<int> _dataParts;
        private IPacketReader _packetReader;                    // IntPtr
        int _dataCount;
        Memory<byte> _data;
        int _dataIndex;                                         // 4
        int _dataOfs;                                           // 4

        internal Packet(IReadOnlyList<int> dataParts, IPacketReader packetReader, Memory<byte> initialData)
        {
            _dataParts = dataParts;
            _packetReader = packetReader;
            _data = initialData;
        }

        protected override int TotalBits => (_dataCount + _data.Length) * 8;

        protected override int ReadNextByte()
        {
            if (_dataIndex == _dataParts.Count) return -1;

            var b = _data.Span[_dataOfs];

            if (++_dataOfs == _data.Length)
            {
                _dataOfs = 0;
                _dataCount += _data.Length;
                if (++_dataIndex < _dataParts.Count)
                {
                    _data = _packetReader.GetPacketData(_dataParts[_dataIndex]);
                }
                else
                {
                    _data = Memory<byte>.Empty;
                }
            }

            return b;
        }

        public override void Reset()
        {
            _dataIndex = 0;
            _dataOfs = 0;
            if (_dataParts.Count > 0)
            {
                _data = _packetReader.GetPacketData(_dataParts[0]);
            }

            base.Reset();
        }

        public override void Done()
        {
            _packetReader?.InvalidatePacketCache(this);

            base.Done();
        }
    }
}