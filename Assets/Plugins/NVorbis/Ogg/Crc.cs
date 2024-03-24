namespace NVorbis.Ogg
{
    class Crc : Contracts.Ogg.ICrc
    {
        const uint CRC32_POLY = 0x04c11db7;
        static readonly uint[] s_crcTable;

        static Crc()
        {
            s_crcTable = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint s = i << 24;
                for (int j = 0; j < 8; ++j)
                {
                    s = (s << 1) ^ (s >= (1U << 31) ? CRC32_POLY : 0);
                }
                s_crcTable[i] = s;
            }
        }

        uint _crc;

        public Crc()
        {
            Reset();
        }

        public void Reset()
        {
            _crc = 0U;
        }

        public void Update(int nextVal)
        {
            _crc = (_crc << 8) ^ s_crcTable[nextVal ^ (_crc >> 24)];
        }

        public bool Test(uint checkCrc)
        {
            return _crc == checkCrc;
        }
    }
}
