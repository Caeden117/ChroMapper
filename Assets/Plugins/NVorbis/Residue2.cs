using NVorbis.Contracts;

namespace NVorbis
{
    // all channels in one pass, interleaved
    class Residue2 : Residue0
    {
        int _channels;

        public override void Init(IPacket packet, int channels, ICodebook[] codebooks)
        {
            _channels = channels;
            base.Init(packet, 1, codebooks);
        }

        public override void Decode(IPacket packet, bool[] doNotDecodeChannel, int blockSize, float[][] buffer)
        {
            // since we're doing all channels in a single pass, the block size has to be multiplied.
            // otherwise this is just a pass-through call
            base.Decode(packet, doNotDecodeChannel, blockSize * _channels, buffer);
        }

        protected override bool WriteVectors(ICodebook codebook, IPacket packet, float[][] residue, int channel, int offset, int partitionSize)
        {
            var chPtr = 0;

            offset /= _channels;
            for (int c = 0; c < partitionSize;)
            {
                var entry = codebook.DecodeScalar(packet);
                if (entry == -1)
                {
                    return true;
                }
                for (var d = 0; d < codebook.Dimensions; d++, c++)
                {
                    residue[chPtr][offset] += codebook[entry, d];
                    if (++chPtr == _channels)
                    {
                        chPtr = 0;
                        offset++;
                    }
                }
            }

            return false;
        }
    }
}
