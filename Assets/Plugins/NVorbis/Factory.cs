using NVorbis.Contracts;

namespace NVorbis
{
    class Factory : IFactory
    {
        public IHuffman CreateHuffman()
        {
            return new Huffman();
        }

        public IMdct CreateMdct()
        {
            return new Mdct();
        }

        public ICodebook CreateCodebook()
        {
            return new Codebook();
        }

        public IFloor CreateFloor(IPacket packet)
        {
            var type = (int)packet.ReadBits(16);
            switch (type)
            {
                case 0: return new Floor0();
                case 1: return new Floor1();
                default: throw new System.IO.InvalidDataException("Invalid floor type!");
            }
        }

        public IMapping CreateMapping(IPacket packet)
        {
            if (packet.ReadBits(16) != 0)
            {
                throw new System.IO.InvalidDataException("Invalid mapping type!");
            }

            return new Mapping();
        }

        public IMode CreateMode()
        {
            return new Mode();
        }

        public IResidue CreateResidue(IPacket packet)
        {
            var type = (int)packet.ReadBits(16);
            switch (type)
            {
                case 0: return new Residue0();
                case 1: return new Residue1();
                case 2: return new Residue2();
                default: throw new System.IO.InvalidDataException("Invalid residue type!");
            }
        }
    }
}
