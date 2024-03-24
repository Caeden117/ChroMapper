using NVorbis.Contracts.Ogg;
using System;
using System.Collections.Generic;
using System.IO;

namespace NVorbis.Ogg
{
    abstract class PageReaderBase : IPageReader
    {
        internal static Func<ICrc> CreateCrc { get; set; } = () => new Crc();

        private readonly ICrc _crc = CreateCrc();
        private readonly HashSet<int> _ignoredSerials = new HashSet<int>();
        private readonly byte[] _headerBuf = new byte[305]; // 27 - 4 + 27 + 255 (found sync at end of first buffer, and found page has full segment count)
        private byte[] _overflowBuf;
        private int _overflowBufIndex;

        private Stream _stream;
        private bool _closeOnDispose;

        protected PageReaderBase(Stream stream, bool closeOnDispose)
        {
            _stream = stream;
            _closeOnDispose = closeOnDispose;
        }

        protected long StreamPosition => _stream?.Position ?? throw new ObjectDisposedException(nameof(PageReaderBase));

        public long ContainerBits { get; private set; }

        public long WasteBits { get; private set; }

        private bool VerifyPage(byte[] headerBuf, int index, int cnt, out byte[] pageBuf, out int bytesRead)
        {
            var segCnt = headerBuf[index + 26];
            if (cnt - index < index + 27 + segCnt)
            {
                pageBuf = null;
                bytesRead = 0;
                return false;
            }

            var dataLen = 0;
            int i;
            for (i = 0; i < segCnt; i++)
            {
                dataLen += headerBuf[index + i + 27];
            }

            pageBuf = new byte[dataLen + segCnt + 27];
            Buffer.BlockCopy(headerBuf, index, pageBuf, 0, segCnt + 27);
            bytesRead = EnsureRead(pageBuf, segCnt + 27, dataLen);
            if (bytesRead != dataLen) return false;
            dataLen = pageBuf.Length;

            _crc.Reset();
            for (i = 0; i < 22; i++)
            {
                _crc.Update(pageBuf[i]);
            }
            _crc.Update(0);
            _crc.Update(0);
            _crc.Update(0);
            _crc.Update(0);
            for (i += 4; i < dataLen; i++)
            {
                _crc.Update(pageBuf[i]);
            }
            return _crc.Test(BitConverter.ToUInt32(pageBuf, 22));
        }

        private bool AddPage(byte[] pageBuf, bool isResync)
        {
            var streamSerial = BitConverter.ToInt32(pageBuf, 14);
            if (!_ignoredSerials.Contains(streamSerial))
            {
                if (AddPage(streamSerial, pageBuf, isResync))
                {
                    ContainerBits += 8 * (27 + pageBuf[26]);
                    return true;
                }
                _ignoredSerials.Add(streamSerial);
            }
            return false;
        }

        private void EnqueueData(byte[] buf, int count)
        {
            if (_overflowBuf != null)
            {
                var newBuf = new byte[_overflowBuf.Length - _overflowBufIndex + count];
                Buffer.BlockCopy(_overflowBuf, _overflowBufIndex, newBuf, 0, newBuf.Length - count);
                var index = buf.Length - count;
                Buffer.BlockCopy(buf, index, newBuf, newBuf.Length - count, count);
                _overflowBufIndex = 0;
            }
            else
            {
                _overflowBuf = buf;
                _overflowBufIndex = buf.Length - count;
            }
        }

        private void ClearEnqueuedData(int count)
        {
            if (_overflowBuf != null && (_overflowBufIndex += count) >= _overflowBuf.Length)
            {
                _overflowBuf = null;
            }
        }

        private int FillHeader(byte[] buf, int index, int count, int maxTries = 10)
        {
            var copyCount = 0;
            if (_overflowBuf != null)
            {
                copyCount = Math.Min(_overflowBuf.Length - _overflowBufIndex, count);
                Buffer.BlockCopy(_overflowBuf, _overflowBufIndex, buf, index, copyCount);
                index += copyCount;
                count -= copyCount;
                if ((_overflowBufIndex += copyCount) == _overflowBuf.Length)
                {
                    _overflowBuf = null;
                }
            }
            if (count > 0)
            {
                copyCount += EnsureRead(buf, index, count, maxTries);
            }
            return copyCount;
        }

        private bool VerifyHeader(byte[] buffer, int index, ref int cnt, bool isFromReadNextPage)
        {
            if (buffer[index] == 0x4f && buffer[index + 1] == 0x67 && buffer[index + 2] == 0x67 && buffer[index + 3] == 0x53)
            {
                if (cnt < 27)
                {
                    if (isFromReadNextPage)
                    {
                        cnt += FillHeader(buffer, 27 - cnt + index, 27 - cnt);
                    }
                    else
                    {
                        cnt += EnsureRead(buffer, 27 - cnt + index, 27 - cnt);
                    }
                }

                if (cnt >= 27)
                {
                    var segCnt = buffer[index + 26];
                    if (isFromReadNextPage)
                    {
                        cnt += FillHeader(buffer, index + 27, segCnt);
                    }
                    else
                    {
                        cnt += EnsureRead(buffer, index + 27, segCnt);
                    }
                    if (cnt == index + 27 + segCnt)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Network streams don't always return the requested size immediately, so this
        // method is used to ensure we fill the buffer if it is possible.
        // Note that it will loop until getting a certain count of zero reads (default: 10).
        // This means in most cases, the network stream probably died by the time we return
        // a short read.
        protected int EnsureRead(byte[] buf, int index, int count, int maxTries = 10)
        {
            var read = 0;
            var tries = 0;
            do
            {
                var cnt = _stream.Read(buf, index + read, count - read);
                if (cnt == 0 && ++tries == maxTries)
                {
                    break;
                }
                read += cnt;
            } while (read < count);
            return read;
        }

        /// <summary>
        /// Verifies the sync sequence and loads the rest of the header.
        /// </summary>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
        protected bool VerifyHeader(byte[] buffer, int index, ref int cnt)
        {
            return VerifyHeader(buffer, index, ref cnt, false);
        }

        /// <summary>
        /// Seeks the underlying stream to the requested position.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <returns>The new position of the stream.</returns>
        /// <exception cref="InvalidOperationException">The stream is not seekable.</exception>
        protected long SeekStream(long offset)
        {
            // make sure we're locked; seeking won't matter if we aren't
            if (!CheckLock()) throw new InvalidOperationException("Must be locked prior to reading!");

            return _stream.Seek(offset, SeekOrigin.Begin);
        }

        virtual protected void PrepareStreamForNextPage() { }

        virtual protected void SaveNextPageSearch() { }

        abstract protected bool AddPage(int streamSerial, byte[] pageBuf, bool isResync);

        abstract protected void SetEndOfStreams();

        virtual public void Lock() { }

        virtual protected bool CheckLock() => true;

        virtual public bool Release() => false;

        public bool ReadNextPage()
        {
            // make sure we're locked; no sense reading if we aren't
            if (!CheckLock()) throw new InvalidOperationException("Must be locked prior to reading!");

            var isResync = false;

            var ofs = 0;
            int cnt;
            PrepareStreamForNextPage();
            while ((cnt = FillHeader(_headerBuf, ofs, 27 - ofs)) > 0)
            {
                cnt += ofs;
                for (var i = 0; i < cnt - 4; i++)
                {
                    if (VerifyHeader(_headerBuf, i, ref cnt, true))
                    {
                        if (VerifyPage(_headerBuf, i, cnt, out var pageBuf, out var bytesRead))
                        {
                            // one way or the other, we have to clear out the page's bytes from the queue (if queued)
                            ClearEnqueuedData(bytesRead);

                            // also, we need to let our inheritors have a chance to save state for next time
                            SaveNextPageSearch();

                            // pass it to our inheritor
                            if (AddPage(pageBuf, isResync))
                            {
                                return true;
                            }

                            // otherwise, the whole page is useless...

                            // save off that we've burned that many bits
                            WasteBits += pageBuf.Length * 8;

                            // set up to load the next page, then loop
                            ofs = 0;
                            cnt = 0;
                            break;
                        }
                        else if (pageBuf != null)
                        {
                            EnqueueData(pageBuf, bytesRead);
                        }
                    }
                    WasteBits += 8;
                    isResync = true;
                }

                if (cnt >= 3)
                {
                    _headerBuf[0] = _headerBuf[cnt - 3];
                    _headerBuf[1] = _headerBuf[cnt - 2];
                    _headerBuf[2] = _headerBuf[cnt - 1];
                    ofs = 3;
                }
            }

            if (cnt == 0)
            {
                SetEndOfStreams();
            }

            return false;
        }

        abstract public bool ReadPageAt(long offset);

        public void Dispose()
        {
            SetEndOfStreams();

            if (_closeOnDispose)
            {
                _stream?.Dispose();
            }
            _stream = null;
        }
    }
}
