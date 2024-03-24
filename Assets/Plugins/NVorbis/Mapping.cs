using NVorbis.Contracts;
using System;

namespace NVorbis
{
    class Mapping : IMapping
    {
        IMdct _mdct;
        int[] _couplingAngle;
        int[] _couplingMangitude;
        IFloor[] _submapFloor;
        IResidue[] _submapResidue;
        IFloor[] _channelFloor;
        IResidue[] _channelResidue;

        public void Init(IPacket packet, int channels, IFloor[] floors, IResidue[] residues, IMdct mdct)
        {
            var submapCount = 1;
            if (packet.ReadBit())
            {
                submapCount += (int)packet.ReadBits(4);
            }

            // square polar mapping
            var couplingSteps = 0;
            if (packet.ReadBit())
            {
                couplingSteps = (int)packet.ReadBits(8) + 1;
            }

            var couplingBits = Utils.ilog(channels - 1);
            _couplingAngle = new int[couplingSteps];
            _couplingMangitude = new int[couplingSteps];
            for (var j = 0; j < couplingSteps; j++)
            {
                var magnitude = (int)packet.ReadBits(couplingBits);
                var angle = (int)packet.ReadBits(couplingBits);
                if (magnitude == angle || magnitude > channels - 1 || angle > channels - 1)
                {
                    throw new System.IO.InvalidDataException("Invalid magnitude or angle in mapping header!");
                }
                _couplingAngle[j] = angle;
                _couplingMangitude[j] = magnitude;
            }

            if (0 != packet.ReadBits(2))
            {
                throw new System.IO.InvalidDataException("Reserved bits not 0 in mapping header.");
            }

            var mux = new int[channels];
            if (submapCount > 1)
            {
                for (var c = 0; c < channels; c++)
                {
                    mux[c] = (int)packet.ReadBits(4);
                    if (mux[c] > submapCount)
                    {
                        throw new System.IO.InvalidDataException("Invalid channel mux submap index in mapping header!");
                    }
                }
            }

            _submapFloor = new IFloor[submapCount];
            _submapResidue = new IResidue[submapCount];
            for (var j = 0; j < submapCount; j++)
            {
                packet.SkipBits(8); // unused placeholder
                var floorNum = (int)packet.ReadBits(8);
                if (floorNum >= floors.Length)
                {
                    throw new System.IO.InvalidDataException("Invalid floor number in mapping header!");
                }
                var residueNum = (int)packet.ReadBits(8);
                if (residueNum >= residues.Length)
                {
                    throw new System.IO.InvalidDataException("Invalid residue number in mapping header!");
                }

                _submapFloor[j] = floors[floorNum];
                _submapResidue[j] = residues[residueNum];
            }

            _channelFloor = new IFloor[channels];
            _channelResidue = new IResidue[channels];
            for (var c = 0; c < channels; c++)
            {
                _channelFloor[c] = _submapFloor[mux[c]];
                _channelResidue[c] = _submapResidue[mux[c]];
            }

            _mdct = mdct;
        }

        public void DecodePacket(IPacket packet, int blockSize, int channels, float[][] buffer)
        {
            var halfBlockSize = blockSize >> 1;

            // read the noise floor data
            var floorData = new IFloorData[_channelFloor.Length];
            var noExecuteChannel = new bool[_channelFloor.Length];
            for (var i = 0; i < _channelFloor.Length; i++)
            {
                floorData[i] = _channelFloor[i].Unpack(packet, blockSize, i);
                noExecuteChannel[i] = !floorData[i].ExecuteChannel;

                // pre-clear the residue buffers
                Array.Clear(buffer[i], 0, halfBlockSize);
            }

            // make sure we handle no-energy channels correctly given the couplings..
            for (var i = 0; i < _couplingAngle.Length; i++)
            {
                if (floorData[_couplingAngle[i]].ExecuteChannel || floorData[_couplingMangitude[i]].ExecuteChannel)
                {
                    floorData[_couplingAngle[i]].ForceEnergy = true;
                    floorData[_couplingMangitude[i]].ForceEnergy = true;
                }
            }

            // decode the submaps into the residue buffer
            for (var i = 0; i < _submapFloor.Length; i++)
            {
                for (var j = 0; j < _channelFloor.Length; j++)
                {
                    if (_submapFloor[i] != _channelFloor[j] || _submapResidue[i] != _channelResidue[j])
                    {
                        // the submap doesn't match, so this floor doesn't contribute
                        floorData[j].ForceNoEnergy = true;
                    }
                }

                _submapResidue[i].Decode(packet, noExecuteChannel, blockSize, buffer);
            }

            // inverse coupling
            for (var i = _couplingAngle.Length - 1; i >= 0; i--)
            {
                if (floorData[_couplingAngle[i]].ExecuteChannel || floorData[_couplingMangitude[i]].ExecuteChannel)
                {
                    var magnitude = buffer[_couplingMangitude[i]];
                    var angle = buffer[_couplingAngle[i]];

                    // we only have to do the first half; MDCT ignores the last half
                    for (var j = 0; j < halfBlockSize; j++)
                    {
                        float newM, newA;

                        var oldM = magnitude[j];
                        var oldA = angle[j];
                        if (oldM > 0)
                        {
                            if (oldA > 0)
                            {
                                newM = oldM;
                                newA = oldM - oldA;
                            }
                            else
                            {
                                newA = oldM;
                                newM = oldM + oldA;
                            }
                        }
                        else
                        {
                            if (oldA > 0)
                            {
                                newM = oldM;
                                newA = oldM + oldA;
                            }
                            else
                            {
                                newA = oldM;
                                newM = oldM - oldA;
                            }
                        }

                        magnitude[j] = newM;
                        angle[j] = newA;
                    }
                }
            }

            // apply floor / dot product / MDCT (only run if we have sound energy in that channel)
            for (var c = 0; c < _channelFloor.Length; c++)
            {
                if (floorData[c].ExecuteChannel)
                {
                    _channelFloor[c].Apply(floorData[c], blockSize, buffer[c]);
                    _mdct.Reverse(buffer[c], blockSize);
                }
                else
                {
                    // since we aren't doing the IMDCT, we have to explicitly clear the back half of the block
                    Array.Clear(buffer[c], halfBlockSize, halfBlockSize);
                }
            }
        }
    }
}
