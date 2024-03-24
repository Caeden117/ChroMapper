using NVorbis.Contracts;
using System;
using System.Collections.Generic;
using System.IO;

namespace NVorbis
{
    // Packed LSP values on dB amplittude and Bark frequency scale.  Virtually unused (libvorbis did not use past beta 4).  Probably untested.
    class Floor0 : IFloor
    {
        class Data : IFloorData
        {
            internal float[] Coeff;
            internal float Amp;

            public bool ExecuteChannel => (ForceEnergy || Amp > 0f) && !ForceNoEnergy;

            public bool ForceEnergy { get; set; }
            public bool ForceNoEnergy { get; set; }
        }

        int _order, _rate, _bark_map_size, _ampBits, _ampOfs, _ampDiv;
        ICodebook[] _books;
        int _bookBits;
        Dictionary<int, float[]> _wMap;
        Dictionary<int, int[]> _barkMaps;

        public void Init(IPacket packet, int channels, int block0Size, int block1Size, ICodebook[] codebooks)
        {
            // this is pretty well stolen directly from libvorbis...  BSD license
            _order = (int)packet.ReadBits(8);
            _rate = (int)packet.ReadBits(16);
            _bark_map_size = (int)packet.ReadBits(16);
            _ampBits = (int)packet.ReadBits(6);
            _ampOfs = (int)packet.ReadBits(8);
            _books = new ICodebook[(int)packet.ReadBits(4) + 1];

            if (_order < 1 || _rate < 1 || _bark_map_size < 1 || _books.Length == 0) throw new InvalidDataException();

            _ampDiv = (1 << _ampBits) - 1;

            for (int i = 0; i < _books.Length; i++)
            {
                var num = (int)packet.ReadBits(8);
                if (num < 0 || num >= codebooks.Length) throw new InvalidDataException();
                var book = codebooks[num];

                if (book.MapType == 0 || book.Dimensions < 1) throw new InvalidDataException();

                _books[i] = book;
            }
            _bookBits = Utils.ilog(_books.Length);

            _barkMaps = new Dictionary<int, int[]>
            {
                [block0Size] = SynthesizeBarkCurve(block0Size / 2),
                [block1Size] = SynthesizeBarkCurve(block1Size / 2)
            };

            _wMap = new Dictionary<int, float[]>
            {
                [block0Size] = SynthesizeWDelMap(block0Size / 2),
                [block1Size] = SynthesizeWDelMap(block1Size / 2)
            };
        }

        int[] SynthesizeBarkCurve(int n)
        {
            var scale = _bark_map_size / toBARK(_rate / 2);

            var map = new int[n + 1];

            for (int i = 0; i < n - 1; i++)
            {
                map[i] = Math.Min(_bark_map_size - 1, (int)Math.Floor(toBARK((_rate / 2f) / n * i) * scale));
            }
            map[n] = -1;
            return map;
        }

        static float toBARK(double lsp)
        {
            return (float)(13.1 * Math.Atan(0.00074 * lsp) + 2.24 * Math.Atan(0.0000000185 * lsp * lsp) + .0001 * lsp);
        }

        float[] SynthesizeWDelMap(int n)
        {
            var wdel = (float)(Math.PI / _bark_map_size);

            var map = new float[n];
            for (int i = 0; i < n; i++)
            {
                map[i] = 2f * (float)Math.Cos(wdel * i);
            }
            return map;
        }

        public IFloorData Unpack(IPacket packet, int blockSize, int channel)
        {
            var data = new Data
            {
                Coeff = new float[_order + 1],
            };

            data.Amp = packet.ReadBits(_ampBits);
            if (data.Amp > 0f)
            {
                // this is pretty well stolen directly from libvorbis...  BSD license
                Array.Clear(data.Coeff, 0, data.Coeff.Length);

                data.Amp = data.Amp / _ampDiv * _ampOfs;

                var bookNum = (uint)packet.ReadBits(_bookBits);
                if (bookNum >= _books.Length)
                {
                    // we ran out of data or the packet is corrupt...  0 the floor and return
                    data.Amp = 0;
                    return data;
                }
                var book = _books[bookNum];

                // first, the book decode...
                for (int i = 0; i < _order;)
                {
                    var entry = book.DecodeScalar(packet);
                    if (entry == -1)
                    {
                        // we ran out of data or the packet is corrupt...  0 the floor and return
                        data.Amp = 0;
                        return data;
                    }
                    for (int j = 0; i < _order && j < book.Dimensions; j++, i++)
                    {
                        data.Coeff[i] = book[entry, j];
                    }
                }

                // then, the "averaging"
                var last = 0f;
                for (int j = 0; j < _order;)
                {
                    for (int k = 0; j < _order && k < book.Dimensions; j++, k++)
                    {
                        data.Coeff[j] += last;
                    }
                    last = data.Coeff[j - 1];
                }
            }
            return data;
        }

        public void Apply(IFloorData floorData, int blockSize, float[] residue)
        {
            if (!(floorData is Data data)) throw new ArgumentException("Incorrect packet data!");

            var n = blockSize / 2;

            if (data.Amp > 0f)
            {
                // this is pretty well stolen directly from libvorbis...  BSD license
                var barkMap = _barkMaps[blockSize];
                var wMap = _wMap[blockSize];

                int i = 0;
                for (i = 0; i < _order; i++)
                {
                    data.Coeff[i] = 2f * (float)Math.Cos(data.Coeff[i]);
                }

                i = 0;
                while (i < n)
                {
                    int j;
                    var k = barkMap[i];
                    var p = .5f;
                    var q = .5f;
                    var w = wMap[k];
                    for (j = 1; j < _order; j += 2)
                    {
                        q *= w - data.Coeff[j - 1];
                        p *= w - data.Coeff[j];
                    }
                    if (j == _order)
                    {
                        // odd order filter; slightly assymetric
                        q *= w - data.Coeff[j - 1];
                        p *= p * (4f - w * w);
                        q *= q;
                    }
                    else
                    {
                        // even order filter; still symetric
                        p *= p * (2f - w);
                        q *= q * (2f + w);
                    }

                    // calc the dB of this bark section
                    q = data.Amp / (float)Math.Sqrt(p + q) - _ampOfs;

                    // now convert to a linear sample multiplier
                    q = (float)Math.Exp(q * 0.11512925f);

                    residue[i] *= q;

                    while (barkMap[++i] == k) residue[i] *= q;
                }
            }
            else
            {
                Array.Clear(residue, 0, n);
            }
        }
    }
}
