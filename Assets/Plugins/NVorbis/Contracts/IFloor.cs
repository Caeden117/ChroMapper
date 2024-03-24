namespace NVorbis.Contracts
{
    interface IFloor
    {
        void Init(IPacket packet, int channels, int block0Size, int block1Size, ICodebook[] codebooks);

        IFloorData Unpack(IPacket packet, int blockSize, int channel);

        void Apply(IFloorData floorData, int blockSize, float[] residue);
    }
}
