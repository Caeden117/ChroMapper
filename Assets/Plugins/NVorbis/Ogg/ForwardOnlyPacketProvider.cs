using NVorbis.Contracts;
using NVorbis.Contracts.Ogg;
using System;
using System.Collections.Generic;

namespace NVorbis.Ogg
{
    class ForwardOnlyPacketProvider : DataPacket, IForwardOnlyPacketProvider
    {
        private int _lastSeqNo;
        private readonly Queue<(byte[] buf, bool isResync)> _pageQueue = new Queue<(byte[] buf, bool isResync)>();

        private readonly IPageReader _reader;
        private byte[] _pageBuf;
        private int _packetIndex;
        private bool _isEndOfStream;
        private int _dataStart;
        private bool _lastWasPeek;

        private Memory<byte> _packetBuf;

        private int _dataIndex;

        public ForwardOnlyPacketProvider(IPageReader reader, int streamSerial)
        {
            _reader = reader;
            StreamSerial = streamSerial;

            // force the first page to read
            _packetIndex = int.MaxValue;
        }

        public bool CanSeek => false;

        public int StreamSerial { get; }

        public bool AddPage(byte[] buf, bool isResync)
        {
            if (((PageFlags)buf[5] & PageFlags.BeginningOfStream) != 0)
            {
                if (_isEndOfStream)
                {
                    return false;
                }
                isResync = true;
                _lastSeqNo = BitConverter.ToInt32(buf, 18);
            }
            else
            {
                // check the sequence number
                var seqNo = BitConverter.ToInt32(buf, 18);
                isResync |= seqNo != _lastSeqNo + 1;
                _lastSeqNo = seqNo;
            }

            // there must be at least one packet with data
            var ttl = 0;
            for (var i = 0; i < buf[26]; i++)
            {
                ttl += buf[27 + i];
            }
            if (ttl == 0)
            {
                return false;
            }

            _pageQueue.Enqueue((buf, isResync));
            return true;
        }

        public void SetEndOfStream()
        {
            _isEndOfStream = true;
        }

        public IPacket GetNextPacket()
        {
            // if not done...
            if (_packetBuf.Length > 0)
            {
                // only allow if last call was for peek
                if (!_lastWasPeek) throw new InvalidOperationException("Must call Done() on previous packet first.");

                // then return ourself, noting that we didn't peek the packet
                _lastWasPeek = false;
                return this;
            }

            // always advance to the next packet
            _lastWasPeek = false;
            if (GetPacket())
            {
                return this;
            }
            return null;
        }

        public IPacket PeekNextPacket()
        {
            // if not done...
            if (_packetBuf.Length > 0)
            {
                // only allow if last call was for peek
                if (!_lastWasPeek) throw new InvalidOperationException("Must call Done() on previous packet first.");

                // then just return ourself
                return this;
            }

            // use a local variable to throw away the updated position
            _lastWasPeek = true;
            if (GetPacket())
            {
                return this;
            }
            return null;
        }

        private bool GetPacket()
        {
            // if we don't already have a page, grab it
            byte[] pageBuf;
            bool isResync;
            int dataStart;
            int packetIndex;
            bool isCont, isCntd;
            if (_pageBuf != null && _packetIndex < 27 + _pageBuf[26])
            {
                pageBuf = _pageBuf;
                isResync = false;
                dataStart = _dataStart;
                packetIndex = _packetIndex;
                isCont = false;
                isCntd = pageBuf[26 + pageBuf[26]] == 255;
            }
            else
            {
                if (!ReadNextPage(out pageBuf, out isResync, out dataStart, out packetIndex, out isCont, out isCntd))
                {
                    // couldn't read the next page...
                    return false;
                }
            }

            // first, set flags from the start page
            var contOverhead = dataStart;
            var isFirst = packetIndex == 27;
            if (isCont)
            {
                if (isFirst)
                {
                    // if it's a continuation, we just read it for a new packet and there's a continuity problem
                    isResync = true;

                    // skip the first packet; it's a partial
                    contOverhead += GetPacketLength(pageBuf, ref packetIndex);

                    // if we moved to the end of the page, we can't satisfy the request from here...
                    if (packetIndex == 27 + pageBuf[26])
                    {
                        // ... so we'll just recurse and try again
                        return GetPacket();
                    }
                }
            }
            if (!isFirst)
            {
                contOverhead = 0;
            }

            // second, determine how long the packet is
            var dataLen = GetPacketLength(pageBuf, ref packetIndex);
            var packetBuf = new Memory<byte>(pageBuf, dataStart, dataLen);
            dataStart += dataLen;

            // third, determine if the packet is the last one in the page
            var isLast = packetIndex == 27 + pageBuf[26];
            if (isCntd)
            {
                if (isLast)
                {
                    // we're on the continued packet, so it really counts with the next page
                    isLast = false;
                }
                else
                {
                    // whelp, not quite...  gotta account for the continued packet
                    var pi = packetIndex;
                    GetPacketLength(pageBuf, ref pi);
                    isLast = pi == 27 + pageBuf[26];
                }
            }

            // forth, if it is the last one, process continuations or flags & granulePos
            var isEos = false;
            long? granulePos = null;
            if (isLast)
            {
                granulePos = BitConverter.ToInt64(pageBuf, 6);

                // fifth, set flags from the end page
                if (((PageFlags)pageBuf[5] & PageFlags.EndOfStream) != 0 || (_isEndOfStream && _pageQueue.Count == 0))
                {
                    isEos = true;
                }
            }
            else
            {
                while (isCntd && packetIndex == 27 + pageBuf[26])
                {
                    if (ReadNextPage(out pageBuf, out isResync, out dataStart, out packetIndex, out isCont, out isCntd) && !isResync && isCont)
                    {
                        // we're in the right spot!

                        // update the overhead count
                        contOverhead += 27 + pageBuf[26];

                        // save off the previous buffer data
                        var prevBuf = packetBuf;

                        // get the size of this page's portion
                        var contSz = GetPacketLength(pageBuf, ref packetIndex);

                        // set up the new buffer and fill it
                        packetBuf = new Memory<byte>(new byte[prevBuf.Length + contSz]);
                        prevBuf.CopyTo(packetBuf);
                        (new Memory<byte>(pageBuf, dataStart, contSz)).CopyTo(packetBuf.Slice(prevBuf.Length));

                        // now that we've read, update our start position
                        dataStart += contSz;
                    }
                    else
                    {
                        // just use what data we can...
                        break;
                    }
                }
            }

            // last, save off our state and return true
            IsResync = isResync;
            GranulePosition = granulePos;
            IsEndOfStream = isEos;
            ContainerOverheadBits = contOverhead * 8;
            _pageBuf = pageBuf;
            _dataStart = dataStart;
            _packetIndex = packetIndex;
            _packetBuf = packetBuf;
            _isEndOfStream |= isEos;
            Reset();
            return true;
        }

        private bool ReadNextPage(out byte[] pageBuf, out bool isResync, out int dataStart, out int packetIndex, out bool isContinuation, out bool isContinued)
        {
            while (_pageQueue.Count == 0)
            {
                if (_isEndOfStream || !_reader.ReadNextPage())
                {
                    // we must be done
                    pageBuf = null;
                    isResync = false;
                    dataStart = 0;
                    packetIndex = 0;
                    isContinuation = false;
                    isContinued = false;
                    return false;
                }
            }

            var temp = _pageQueue.Dequeue();
            pageBuf = temp.buf;
            isResync = temp.isResync;

            dataStart = pageBuf[26] + 27;
            packetIndex = 27;
            isContinuation = ((PageFlags)pageBuf[5] & PageFlags.ContinuesPacket) != 0;
            isContinued = pageBuf[26 + pageBuf[26]] == 255;
            return true;
        }

        private int GetPacketLength(byte[] pageBuf, ref int packetIndex)
        {
            var len = 0;
            while (pageBuf[packetIndex] == 255 && packetIndex < pageBuf[26] + 27)
            {
                len += pageBuf[packetIndex];
                ++packetIndex;
            }
            if (packetIndex < pageBuf[26] + 27)
            {
                len += pageBuf[packetIndex];
                ++packetIndex;
            }
            return len;
        }

        protected override int TotalBits => _packetBuf.Length * 8;

        protected override int ReadNextByte()
        {
            if (_dataIndex < _packetBuf.Length)
            {
                return _packetBuf.Span[_dataIndex++];
            }
            return -1;
        }

        public override void Reset()
        {
            _dataIndex = 0;
            base.Reset();
        }

        public override void Done()
        {
            _packetBuf = Memory<byte>.Empty;
            base.Done();
        }

        long Contracts.IPacketProvider.GetGranuleCount() => throw new NotSupportedException();
        long Contracts.IPacketProvider.SeekTo(long granulePos, int preRoll, GetPacketGranuleCount getPacketGranuleCount) => throw new NotSupportedException();
    }
}
