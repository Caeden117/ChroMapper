namespace NVorbis.Contracts
{
    interface IMapping
    {
        void Init(IPacket packet, int channels, IFloor[] floors, IResidue[] residues, IMdct mdct);

        void DecodePacket(IPacket packet, int blockSize, int channels, float[][] buffer);
    }
}
