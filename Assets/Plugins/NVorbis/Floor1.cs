using NVorbis.Contracts;
using System;
using System.Collections.Generic;

namespace NVorbis
{
    // Linear interpolated values on dB amplitude and linear frequency scale.  Draws a curve between each point to define the low-resolution spectral data.
    class Floor1 : IFloor
    {
        class Data : IFloorData
        {
            internal int[] Posts = new int[64];
            internal int PostCount;

            public bool ExecuteChannel => (ForceEnergy || PostCount > 0) && !ForceNoEnergy;

            public bool ForceEnergy { get; set; }
            public bool ForceNoEnergy { get; set; }
        }

        int[] _partitionClass, _classDimensions, _classSubclasses, _xList, _classMasterBookIndex, _hNeigh, _lNeigh, _sortIdx;
        int _multiplier, _range, _yBits;
        ICodebook[] _classMasterbooks;
        ICodebook[][] _subclassBooks;
        int[][] _subclassBookIndex;

        static readonly int[] _rangeLookup = { 256, 128, 86, 64 };
        static readonly int[] _yBitsLookup = { 8, 7, 7, 6 };

        public void Init(IPacket packet, int channels, int block0Size, int block1Size, ICodebook[] codebooks)
        {
            var maximum_class = -1;
            _partitionClass = new int[(int)packet.ReadBits(5)];
            for (int i = 0; i < _partitionClass.Length; i++)
            {
                _partitionClass[i] = (int)packet.ReadBits(4);
                if (_partitionClass[i] > maximum_class)
                {
                    maximum_class = _partitionClass[i];
                }
            }

            _classDimensions = new int[++maximum_class];
            _classSubclasses = new int[maximum_class];
            _classMasterbooks = new ICodebook[maximum_class];
            _classMasterBookIndex = new int[maximum_class];
            _subclassBooks = new ICodebook[maximum_class][];
            _subclassBookIndex = new int[maximum_class][];
            for (int i = 0; i < maximum_class; i++)
            {
                _classDimensions[i] = (int)packet.ReadBits(3) + 1;
                _classSubclasses[i] = (int)packet.ReadBits(2);
                if (_classSubclasses[i] > 0)
                {
                    _classMasterBookIndex[i] = (int)packet.ReadBits(8);
                    _classMasterbooks[i] = codebooks[_classMasterBookIndex[i]];
                }

                _subclassBooks[i] = new ICodebook[1 << _classSubclasses[i]];
                _subclassBookIndex[i] = new int[_subclassBooks[i].Length];
                for (int j = 0; j < _subclassBooks[i].Length; j++)
                {
                    var bookNum = (int)packet.ReadBits(8) - 1;
                    if (bookNum >= 0) _subclassBooks[i][j] = codebooks[bookNum];
                    _subclassBookIndex[i][j] = bookNum;
                }
            }

            _multiplier = (int)packet.ReadBits(2);

            _range = _rangeLookup[_multiplier];
            _yBits = _yBitsLookup[_multiplier];

            ++_multiplier;

            var rangeBits = (int)packet.ReadBits(4);

            var xList = new List<int>();
            xList.Add(0);
            xList.Add(1 << rangeBits);

            for (int i = 0; i < _partitionClass.Length; i++)
            {
                var classNum = _partitionClass[i];
                for (int j = 0; j < _classDimensions[classNum]; j++)
                {
                    xList.Add((int)packet.ReadBits(rangeBits));
                }
            }
            _xList = xList.ToArray();

            // precalc the low and high neighbors (and init the sort table)
            _lNeigh = new int[xList.Count];
            _hNeigh = new int[xList.Count];
            _sortIdx = new int[xList.Count];
            _sortIdx[0] = 0;
            _sortIdx[1] = 1;
            for (int i = 2; i < _lNeigh.Length; i++)
            {
                _lNeigh[i] = 0;
                _hNeigh[i] = 1;
                _sortIdx[i] = i;
                for (int j = 2; j < i; j++)
                {
                    var temp = _xList[j];
                    if (temp < _xList[i])
                    {
                        if (temp > _xList[_lNeigh[i]]) _lNeigh[i] = j;
                    }
                    else
                    {
                        if (temp < _xList[_hNeigh[i]]) _hNeigh[i] = j;
                    }
                }
            }

            // precalc the sort table
            for (int i = 0; i < _sortIdx.Length - 1; i++)
            {
                for (int j = i + 1; j < _sortIdx.Length; j++)
                {
                    if (_xList[i] == _xList[j]) throw new System.IO.InvalidDataException();

                    if (_xList[_sortIdx[i]] > _xList[_sortIdx[j]])
                    {
                        // swap the sort indexes
                        var temp = _sortIdx[i];
                        _sortIdx[i] = _sortIdx[j];
                        _sortIdx[j] = temp;
                    }
                }
            }
        }

        public IFloorData Unpack(IPacket packet, int blockSize, int channel)
        {
            var data = new Data();

            // hoist ReadPosts to here since that's all we're doing...
            if (packet.ReadBit())
            {
                var postCount = 2;
                data.Posts[0] = (int)packet.ReadBits(_yBits);
                data.Posts[1] = (int)packet.ReadBits(_yBits);

                for (int i = 0; i < _partitionClass.Length; i++)
                {
                    var clsNum = _partitionClass[i];
                    var cdim = _classDimensions[clsNum];
                    var cbits = _classSubclasses[clsNum];
                    var csub = (1 << cbits) - 1;
                    var cval = 0U;
                    if (cbits > 0)
                    {
                        if ((cval = (uint)_classMasterbooks[clsNum].DecodeScalar(packet)) == uint.MaxValue)
                        {
                            // we read a bad value...  bail
                            postCount = 0;
                            break;
                        }
                    }
                    for (int j = 0; j < cdim; j++)
                    {
                        var book = _subclassBooks[clsNum][cval & csub];
                        cval >>= cbits;
                        if (book != null)
                        {
                            if ((data.Posts[postCount] = book.DecodeScalar(packet)) == -1)
                            {
                                // we read a bad value... bail
                                postCount = 0;
                                i = _partitionClass.Length;
                                break;
                            }
                        }
                        ++postCount;
                    }
                }

                data.PostCount = postCount;
            }

            return data;
        }

        public void Apply(IFloorData floorData, int blockSize, float[] residue)
        {
            if (!(floorData is Data data)) throw new ArgumentException("Incorrect packet data!", "packetData");

            var n = blockSize / 2;

            if (data.PostCount > 0)
            {
                var stepFlags = UnwrapPosts(data);

                var lx = 0;
                var ly = data.Posts[0] * _multiplier;
                for (int i = 1; i < data.PostCount; i++)
                {
                    var idx = _sortIdx[i];

                    if (stepFlags[idx])
                    {
                        var hx = _xList[idx];
                        var hy = data.Posts[idx] * _multiplier;
                        if (lx < n) RenderLineMulti(lx, ly, Math.Min(hx, n), hy, residue);
                        lx = hx;
                        ly = hy;
                    }
                    if (lx >= n) break;
                }

                if (lx < n)
                {
                    RenderLineMulti(lx, ly, n, ly, residue);
                }
            }
            else
            {
                Array.Clear(residue, 0, n);
            }
        }

        bool[] UnwrapPosts(Data data)
        {
            var stepFlags = new bool[64];
            stepFlags[0] = true;
            stepFlags[1] = true;

            var finalY = new int[64];
            finalY[0] = data.Posts[0];
            finalY[1] = data.Posts[1];

            for (int i = 2; i < data.PostCount; i++)
            {
                var lowOfs = _lNeigh[i];
                var highOfs = _hNeigh[i];

                var predicted = RenderPoint(_xList[lowOfs], finalY[lowOfs], _xList[highOfs], finalY[highOfs], _xList[i]);

                var val = data.Posts[i];
                var highroom = _range - predicted;
                var lowroom = predicted;
                int room;
                if (highroom < lowroom)
                {
                    room = highroom * 2;
                }
                else
                {
                    room = lowroom * 2;
                }
                if (val != 0)
                {
                    stepFlags[lowOfs] = true;
                    stepFlags[highOfs] = true;
                    stepFlags[i] = true;

                    if (val >= room)
                    {
                        if (highroom > lowroom)
                        {
                            finalY[i] = val - lowroom + predicted;
                        }
                        else
                        {
                            finalY[i] = predicted - val + highroom - 1;
                        }
                    }
                    else
                    {
                        if ((val % 2) == 1)
                        {
                            // odd
                            finalY[i] = predicted - ((val + 1) / 2);
                        }
                        else
                        {
                            // even
                            finalY[i] = predicted + (val / 2);
                        }
                    }
                }
                else
                {
                    stepFlags[i] = false;
                    finalY[i] = predicted;
                }
            }

            for (int i = 0; i < data.PostCount; i++)
            {
                data.Posts[i] = finalY[i];
            }

            return stepFlags;
        }

        int RenderPoint(int x0, int y0, int x1, int y1, int X)
        {
            var dy = y1 - y0;
            var adx = x1 - x0;
            var ady = Math.Abs(dy);
            var err = ady * (X - x0);
            var off = err / adx;
            if (dy < 0)
            {
                return y0 - off;
            }
            else
            {
                return y0 + off;
            }
        }

        void RenderLineMulti(int x0, int y0, int x1, int y1, float[] v)
        {
            var dy = y1 - y0;
            var adx = x1 - x0;
            var ady = Math.Abs(dy);
            var sy = 1 - (((dy >> 31) & 1) * 2);
            var b = dy / adx;
            var x = x0;
            var y = y0;
            var err = -adx;

            v[x0] *= inverse_dB_table[y0];
            ady -= Math.Abs(b) * adx;

            while (++x < x1)
            {
                y += b;
                err += ady;
                if (err >= 0)
                {
                    err -= adx;
                    y += sy;
                }
                v[x] *= inverse_dB_table[y];
            }
        }

        #region dB inversion table

        static readonly float[] inverse_dB_table = {
                                                        1.0649863e-07f, 1.1341951e-07f, 1.2079015e-07f, 1.2863978e-07f,
                                                        1.3699951e-07f, 1.4590251e-07f, 1.5538408e-07f, 1.6548181e-07f,
                                                        1.7623575e-07f, 1.8768855e-07f, 1.9988561e-07f, 2.1287530e-07f,
                                                        2.2670913e-07f, 2.4144197e-07f, 2.5713223e-07f, 2.7384213e-07f,
                                                        2.9163793e-07f, 3.1059021e-07f, 3.3077411e-07f, 3.5226968e-07f,
                                                        3.7516214e-07f, 3.9954229e-07f, 4.2550680e-07f, 4.5315863e-07f,
                                                        4.8260743e-07f, 5.1396998e-07f, 5.4737065e-07f, 5.8294187e-07f,
                                                        6.2082472e-07f, 6.6116941e-07f, 7.0413592e-07f, 7.4989464e-07f,
                                                        7.9862701e-07f, 8.5052630e-07f, 9.0579828e-07f, 9.6466216e-07f,
                                                        1.0273513e-06f, 1.0941144e-06f, 1.1652161e-06f, 1.2409384e-06f,
                                                        1.3215816e-06f, 1.4074654e-06f, 1.4989305e-06f, 1.5963394e-06f,
                                                        1.7000785e-06f, 1.8105592e-06f, 1.9282195e-06f, 2.0535261e-06f,
                                                        2.1869758e-06f, 2.3290978e-06f, 2.4804557e-06f, 2.6416497e-06f,
                                                        2.8133190e-06f, 2.9961443e-06f, 3.1908506e-06f, 3.3982101e-06f,
                                                        3.6190449e-06f, 3.8542308e-06f, 4.1047004e-06f, 4.3714470e-06f,
                                                        4.6555282e-06f, 4.9580707e-06f, 5.2802740e-06f, 5.6234160e-06f,
                                                        5.9888572e-06f, 6.3780469e-06f, 6.7925283e-06f, 7.2339451e-06f,
                                                        7.7040476e-06f, 8.2047000e-06f, 8.7378876e-06f, 9.3057248e-06f,
                                                        9.9104632e-06f, 1.0554501e-05f, 1.1240392e-05f, 1.1970856e-05f,
                                                        1.2748789e-05f, 1.3577278e-05f, 1.4459606e-05f, 1.5399272e-05f,
                                                        1.6400004e-05f, 1.7465768e-05f, 1.8600792e-05f, 1.9809576e-05f,
                                                        2.1096914e-05f, 2.2467911e-05f, 2.3928002e-05f, 2.5482978e-05f,
                                                        2.7139006e-05f, 2.8902651e-05f, 3.0780908e-05f, 3.2781225e-05f,
                                                        3.4911534e-05f, 3.7180282e-05f, 3.9596466e-05f, 4.2169667e-05f,
                                                        4.4910090e-05f, 4.7828601e-05f, 5.0936773e-05f, 5.4246931e-05f,
                                                        5.7772202e-05f, 6.1526565e-05f, 6.5524908e-05f, 6.9783085e-05f,
                                                        7.4317983e-05f, 7.9147585e-05f, 8.4291040e-05f, 8.9768747e-05f,
                                                        9.5602426e-05f, 0.00010181521f, 0.00010843174f, 0.00011547824f,
                                                        0.00012298267f, 0.00013097477f, 0.00013948625f, 0.00014855085f,
                                                        0.00015820453f, 0.00016848555f, 0.00017943469f, 0.00019109536f,
                                                        0.00020351382f, 0.00021673929f, 0.00023082423f, 0.00024582449f,
                                                        0.00026179955f, 0.00027881276f, 0.00029693158f, 0.00031622787f,
                                                        0.00033677814f, 0.00035866388f, 0.00038197188f, 0.00040679456f,
                                                        0.00043323036f, 0.00046138411f, 0.00049136745f, 0.00052329927f,
                                                        0.00055730621f, 0.00059352311f, 0.00063209358f, 0.00067317058f,
                                                        0.00071691700f, 0.00076350630f, 0.00081312324f, 0.00086596457f,
                                                        0.00092223983f, 0.00098217216f, 0.0010459992f,  0.0011139742f,
                                                        0.0011863665f,  0.0012634633f,  0.0013455702f,  0.0014330129f,
                                                        0.0015261382f,  0.0016253153f,  0.0017309374f,  0.0018434235f,
                                                        0.0019632195f,  0.0020908006f,  0.0022266726f,  0.0023713743f,
                                                        0.0025254795f,  0.0026895994f,  0.0028643847f,  0.0030505286f,
                                                        0.0032487691f,  0.0034598925f,  0.0036847358f,  0.0039241906f,
                                                        0.0041792066f,  0.0044507950f,  0.0047400328f,  0.0050480668f,
                                                        0.0053761186f,  0.0057254891f,  0.0060975636f,  0.0064938176f,
                                                        0.0069158225f,  0.0073652516f,  0.0078438871f,  0.0083536271f,
                                                        0.0088964928f,  0.009474637f,   0.010090352f,   0.010746080f,
                                                        0.011444421f,   0.012188144f,   0.012980198f,   0.013823725f,
                                                        0.014722068f,   0.015678791f,   0.016697687f,   0.017782797f,
                                                        0.018938423f,   0.020169149f,   0.021479854f,   0.022875735f,
                                                        0.024362330f,   0.025945531f,   0.027631618f,   0.029427276f,
                                                        0.031339626f,   0.033376252f,   0.035545228f,   0.037855157f,
                                                        0.040315199f,   0.042935108f,   0.045725273f,   0.048696758f,
                                                        0.051861348f,   0.055231591f,   0.058820850f,   0.062643361f,
                                                        0.066714279f,   0.071049749f,   0.075666962f,   0.080584227f,
                                                        0.085821044f,   0.091398179f,   0.097337747f,   0.10366330f,
                                                        0.11039993f,    0.11757434f,    0.12521498f,    0.13335215f,
                                                        0.14201813f,    0.15124727f,    0.16107617f,    0.17154380f,
                                                        0.18269168f,    0.19456402f,    0.20720788f,    0.22067342f,
                                                        0.23501402f,    0.25028656f,    0.26655159f,    0.28387361f,
                                                        0.30232132f,    0.32196786f,    0.34289114f,    0.36517414f,
                                                        0.38890521f,    0.41417847f,    0.44109412f,    0.46975890f,
                                                        0.50028648f,    0.53279791f,    0.56742212f,    0.60429640f,
                                                        0.64356699f,    0.68538959f,    0.72993007f,    0.77736504f,
                                                        0.82788260f,    0.88168307f,    0.9389798f,     1.0f
                                                        };

        #endregion
    }
}
