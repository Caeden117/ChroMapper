namespace NVorbis.Contracts
{
    interface ICodebook
    {
        void Init(IPacket packet, IHuffman huffman);

        int Dimensions { get; }
        int Entries { get; }
        int MapType { get; }

        float this[int entry, int dim] { get; }

        int DecodeScalar(IPacket packet);
    }
}
