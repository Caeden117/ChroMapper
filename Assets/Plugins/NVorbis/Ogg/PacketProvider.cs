using NVorbis.Contracts;
using NVorbis.Contracts.Ogg;
using System;
using System.Collections.Generic;

namespace NVorbis.Ogg
{
    class PacketProvider : Contracts.IPacketProvider, IPacketReader
    {
        private IStreamPageReader _reader;

        private int _pageIndex;
        private int _packetIndex;

        private int _lastPacketPageIndex;
        private int _lastPacketPacketIndex;
        private Packet _lastPacket;
        private int _nextPacketPageIndex;
        private int _nextPacketPacketIndex;

        public bool CanSeek => true;

        public int StreamSerial { get; }

        internal PacketProvider(IStreamPageReader reader, int streamSerial)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            StreamSerial = streamSerial;
        }

        public long GetGranuleCount()
        {
            if (_reader == null) throw new ObjectDisposedException(nameof(PacketProvider));

            if (!_reader.HasAllPages)
            {
                // this will force the reader to attempt to read all pages
                _reader.GetPage(int.MaxValue, out _, out _, out _, out _, out _, out _);
            }
            return _reader.MaxGranulePosition.Value;
        }

        public IPacket GetNextPacket()
        {
            return GetNextPacket(ref _pageIndex, ref _packetIndex);
        }

        public IPacket PeekNextPacket()
        {
            var pageIndex = _pageIndex;
            var packetIndex = _packetIndex;
            return GetNextPacket(ref pageIndex, ref packetIndex);
        }

        public long SeekTo(long granulePos, int preRoll, GetPacketGranuleCount getPacketGranuleCount)
        {
            if (_reader == null) throw new ObjectDisposedException(nameof(PacketProvider));

            int pageIndex = _reader.FindPage(granulePos);
            int packetIndex = FindPacket(pageIndex, preRoll, ref granulePos, getPacketGranuleCount);

            if (!NormalizePacketIndex(ref pageIndex, ref packetIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(granulePos));
            }

            _lastPacket = null;
            _pageIndex = pageIndex;
            _packetIndex = packetIndex;
            return granulePos;
        }

        private (long lastPageGranulePos, int lastPagePacketLength, int firstRealPacket) GetPreviousPageInfo(int pageIndex, GetPacketGranuleCount getPacketGranuleCount)
        {
            if (pageIndex > 0)
            {
                int lastPagePacketLength;
                if (_reader.GetPage(pageIndex - 1, out var lastPageGranulePos, out _, out _, out var isContinued, out var lastPacketCount, out _))
                {
                    if (pageIndex > _reader.FirstDataPageIndex)
                    {
                        --pageIndex;
                        var lastPacketIndex = lastPacketCount - 1;
                        // this will either be a continued packet OR the last packet of the last page
                        // in both cases that's precisely the value we need
                        var lastPacket = CreatePacket(ref pageIndex, ref lastPacketIndex, false, 0, false, isContinued, lastPacketCount, 0);
                        if (lastPacket == null)
                        {
                            throw new System.IO.InvalidDataException("Could not find end of continuation!");
                        }
                        lastPagePacketLength = getPacketGranuleCount(lastPacket);
                    }
                    else
                    {
                        lastPagePacketLength = 0;
                    }
                    return (lastPageGranulePos, lastPagePacketLength, isContinued ? 1 : 0);
                }
                throw new System.IO.InvalidDataException("Could not get preceding page?!");
            }
            else
            {
                return (0, 0, 0);
            }
        }

        private (long[] gps, long endGP) GetTargetPageInfo(int pageIndex, int firstRealPacket, int lastPagePacketLength, GetPacketGranuleCount getPacketGranuleCount)
        {
            if (!_reader.GetPage(pageIndex, out var pageGranulePos, out var isResync, out var isContinuation, out var isContinued, out var packetCount, out _))
            {
                throw new System.IO.InvalidDataException("Could not get found page?!");
            }

            if (isContinued)
            {
                // if continued, the last packet index doesn't apply
                packetCount--;
            }

            // get the granule positions of all packets in the page
            var gps = new long[packetCount];
            var endGP = pageGranulePos;
            for (var i = packetCount - 1; i >= firstRealPacket; i--)
            {
                gps[i] = endGP;

                // it would be nice to pass false instead of isContinued, but (hypothetically) we don't know if getPacketGranuleCount(...) needs the whole thing...
                // Vorbis doesn't, but someone might decide to try to use us for another purpose so we'll be good here.
                var packet = CreatePacket(ref pageIndex, ref i, false, pageGranulePos, i == 0 && isResync, isContinued, packetCount, 0);
                if (packet == null)
                {
                    throw new System.IO.InvalidDataException("Could not find end of continuation!");
                }
                endGP -= getPacketGranuleCount(packet);
            }

            // if we're contnued, the the continued packet ends on our calcualted endGP
            if (firstRealPacket == 1)
            {
                gps[0] = endGP;
                endGP -= lastPagePacketLength;
            }

            return (gps, endGP);
        }

        private int FindPacket(int pageIndex, long[] gps, long endGP, long lastPageGranulePos, int lastPagePacketLength, ref long granulePos)
        {
            // next check for a bugged vorbis encoder...
            if (endGP != lastPageGranulePos)
            {
                var diff = endGP - lastPageGranulePos;
                if (GetIsVorbisBugDiff(diff))
                {
                    if (diff > 0)
                    {
                        // the last packet in the last page is a long block that was mis-counted by libvorbis
                        // if the requested granulePos is <= endGP, it's in that packet
                        // otherwise, the normal logic should be fine
                        // NOTE that this bug does not appear to happen on a continued packet, which makes this safe
                        if (granulePos <= endGP)
                        {
                            granulePos = endGP - lastPagePacketLength;
                            return -1;
                        }
                    }
                    else
                    {
                        // our pageGranulePos is wrong, so adjust everything and let the normal logic apply
                        for (var i = 0; i < gps.Length; i++)
                        {
                            gps[i] -= diff;
                        }
                    }
                }
                // if we're not on the first page, there's a problem...
                // technically there could still be a problem on the first page, but we're ignoring it
                else if (pageIndex > _reader.FirstDataPageIndex)
                {
                    // unknown error...
                    throw new System.IO.InvalidDataException($"GranulePos mismatch: Page {pageIndex}, expected {lastPageGranulePos}, calculated {endGP}");
                }
            }

            // finally, find the packet with the requested granulePos
            for (var i = 0; i < gps.Length; i++)
            {
                if (gps[i] >= granulePos)
                {
                    if (i == 0)
                    {
                        granulePos = endGP;
                    }
                    else
                    {
                        granulePos = gps[i - 1];
                    }
                    return i;
                }
            }

            throw new System.IO.InvalidDataException("Could not find seek packet?!");
        }

        private int FindPacket(int pageIndex, int preRoll, ref long granulePos, GetPacketGranuleCount getPacketGranuleCount)
        {
            // pageIndex is _probably_ the correct page (bugs in libogg mean long->short over page boundary isn't always correct).
            // We check for this by looking for a difference in the previous page's granulePos vs. the calculated value

            // first we look at the page info to see how it is set up
            var (lastPageGranulePos, lastPagePacketLength, firstRealPacket) = GetPreviousPageInfo(pageIndex, getPacketGranuleCount);

            // now get the info on the target page
            var (gps, endGP) = GetTargetPageInfo(pageIndex, firstRealPacket, lastPagePacketLength, getPacketGranuleCount);

            // finally figure out which packet in our known info we need to use
            var packetIndex = FindPacket(pageIndex, gps, endGP, lastPageGranulePos, lastPagePacketLength, ref granulePos);

            // then apply the preRoll (but only if we're not seeking into the first packet, which is its own preRoll)
            if (endGP > 0 || packetIndex > 1)
            {
                packetIndex -= preRoll;
            }
            return packetIndex;
        }

        private bool GetIsVorbisBugDiff(long diff)
        {
            // This requires either breaking abstractions OR doing some fancy bit math...
            // We're gonna use the latter to keep the abstractions clean.
            // For our bug, the bit pattern is x set bits followed by y cleared bits:
            //   x = the number of bits between short & long block sizes
            //   y = the number of bits in the short block size - 2
            // So in binary it looks like 111000000 for 2048/256 block sizes.
            // We pre-adjust the "/ 4" out by starting at 0 for y instead of 2

            // we have to use the absolute value for this to work right
            diff = Math.Abs(diff);

            // find the count for y
            var temp = diff;
            var shortBlockBits = 0;
            while (temp > 0 && (temp & 1) == 0)
            {
                ++shortBlockBits;
                temp >>= 1;
            }

            // find the count for x (shortcut: start from shortBlockBits since this is really an offset from there)
            var longBlockBits = shortBlockBits;
            while ((temp & 1) == 1)
            {
                ++longBlockBits;
                temp >>= 1;
            }

            // if we've encountered the bug, temp will be 0 and diff will equal longBlock / 4 - shortBLock /4
            return temp == 0 && diff == (1 << longBlockBits) - (1 << shortBlockBits);
        }

        // this method calc's the appropriate page and packet prior to the one specified, honoring continuations and handling negative packetIndex values
        // if packet index is larger than the current page allows, we just return it as-is
        private bool NormalizePacketIndex(ref int pageIndex, ref int packetIndex)
        {
            if (!_reader.GetPage(pageIndex, out _, out var isResync, out var isContinuation, out _, out _, out _))
            {
                return false;
            }

            var pgIdx = pageIndex;
            var pktIdx = packetIndex;

            while (pktIdx < (isContinuation ? 1: 0))
            {
                // can't merge across resync
                if (isContinuation && isResync) return false;

                // get the previous packet
                var wasContinuation = isContinuation;
                if (!_reader.GetPage(--pgIdx, out _, out isResync, out isContinuation, out var isContinued, out var packetCount, out _))
                {
                    return false;
                }

                // can't merge if continuation flags don't match
                if (wasContinuation && !isContinued) return false;

                // add the previous packet's packetCount
                pktIdx += packetCount - (wasContinuation ? 1 : 0);
            }

            pageIndex = pgIdx;
            packetIndex = pktIdx;
            return true;
        }

        private Packet GetNextPacket(ref int pageIndex, ref int packetIndex)
        {
            if (_reader == null) throw new ObjectDisposedException(nameof(PacketProvider));

            if (_lastPacketPacketIndex != packetIndex || _lastPacketPageIndex != pageIndex || _lastPacket == null)
            {
                _lastPacket = null;

                while (_reader.GetPage(pageIndex, out var granulePos, out var isResync, out _, out var isContinued, out var packetCount, out var pageOverhead))
                {
                    _lastPacketPageIndex = pageIndex;
                    _lastPacketPacketIndex = packetIndex;
                    _lastPacket = CreatePacket(ref pageIndex, ref packetIndex, true, granulePos, isResync, isContinued, packetCount, pageOverhead);
                    _nextPacketPageIndex = pageIndex;
                    _nextPacketPacketIndex = packetIndex;
                    break;
                }
            }
            else
            {
                pageIndex = _nextPacketPageIndex;
                packetIndex = _nextPacketPacketIndex;
            }
            return _lastPacket;
        }

        private Packet CreatePacket(ref int pageIndex, ref int packetIndex, bool advance, long granulePos, bool isResync, bool isContinued, int packetCount, int pageOverhead)
        {
            // save off the packet data for the initial packet
            var firstPacketData = _reader.GetPagePackets(pageIndex)[packetIndex];

            // create the packet list and add the item to it
            var pktList = new List<int>(2) { (pageIndex << 8) | packetIndex };

            // make sure we handle continuations
            bool isLastPacket;
            bool isFirstPacket;
            var finalPage = pageIndex;
            if (isContinued && packetIndex == packetCount - 1)
            {
                // by definition, it's the first packet in the page it ends on
                isFirstPacket = true;

                // but we don't want to include the current page's overhead if we didn't start the page
                if (packetIndex > 0)
                {
                    pageOverhead = 0;
                }

                // go read the next page(s) that include this packet
                var contPageIdx = pageIndex;
                while (isContinued)
                {
                    if (!_reader.GetPage(++contPageIdx, out granulePos, out isResync, out var isContinuation, out isContinued, out packetCount, out var contPageOverhead))
                    {
                        // no more pages?  In any case, we can't satify the request
                        return null;
                    }
                    pageOverhead += contPageOverhead;

                    // if the next page isn't a continuation or is a resync, the stream is broken so we'll just return what we could get
                    if (!isContinuation || isResync)
                    {
                        break;
                    }

                    // if the next page is continued, only keep reading if there are no more packets in the page
                    if (isContinued && packetCount > 1)
                    {
                        isContinued = false;
                    }

                    // add the packet to the list
                    pktList.Add(contPageIdx << 8);
                }

                // we're now the first packet in the final page, so we'll act like it...
                isLastPacket = packetCount == 1;

                // track the final page read
                finalPage = contPageIdx;
            }
            else
            {
                isFirstPacket = packetIndex == 0;
                isLastPacket = packetIndex == packetCount - 1;
            }

            // create the packet instance and populate it with the appropriate initial data
            var packet = new Packet(pktList, this, firstPacketData)
            {
                IsResync = isResync,
            };

            // if it's the first packet, associate the container overhead with it
            if (isFirstPacket)
            {
                packet.ContainerOverheadBits = pageOverhead * 8;
            }

            // if we're the last packet completed in the page, set the .GranulePosition
            if (isLastPacket)
            {
                packet.GranulePosition = granulePos;

                // if we're the last packet completed in the page, no more pages are available, and _hasAllPages is set, set .IsEndOfStream
                if (_reader.HasAllPages && finalPage == _reader.PageCount - 1)
                {
                    packet.IsEndOfStream = true;
                }
            }

            if (advance)
            {
                // if we've advanced a page, we continued a packet and should pick up with the next page
                if (finalPage != pageIndex)
                {
                    // we're on the final page now
                    pageIndex = finalPage;

                    // the packet index will be modified below, so set it to the end of the continued packet
                    packetIndex = 0;
                }

                // if we're on the last packet in the page, move to the next page
                // we can't use isLast here because the logic is different; last in page granule vs. last in physical page
                if (packetIndex == packetCount - 1)
                {
                    ++pageIndex;
                    packetIndex = 0;
                }
                // otherwise, just move to the next packet
                else
                {
                    ++packetIndex;
                }
            }

            // done!
            return packet;
        }

        Memory<byte> IPacketReader.GetPacketData(int pagePacketIndex)
        {
            var pageIndex = (pagePacketIndex >> 8) & 0xFFFFFF;
            var packetIndex = pagePacketIndex & 0xFF;

            var packets = _reader.GetPagePackets(pageIndex);
            if (packetIndex < packets.Length)
            {
                return packets[packetIndex];
            }
            return Memory<byte>.Empty;
        }

        void IPacketReader.InvalidatePacketCache(IPacket packet)
        {
            if (ReferenceEquals(_lastPacket, packet))
            {
                _lastPacket = null;
            }
        }
    }
}
