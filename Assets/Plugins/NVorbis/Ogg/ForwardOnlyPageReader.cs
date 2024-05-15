using NVorbis.Contracts.Ogg;
using System;
using System.Collections.Generic;
using System.IO;

namespace NVorbis.Ogg
{
    class ForwardOnlyPageReader : PageReaderBase
    {
        internal static Func<IPageReader, int, IForwardOnlyPacketProvider> CreatePacketProvider { get; set; } = (pr, ss) => new ForwardOnlyPacketProvider(pr, ss);

        private readonly Dictionary<int, IForwardOnlyPacketProvider> _packetProviders = new Dictionary<int, IForwardOnlyPacketProvider>();
        private readonly Func<Contracts.IPacketProvider, bool> _newStreamCallback;

        public ForwardOnlyPageReader(Stream stream, bool closeOnDispose, Func<Contracts.IPacketProvider, bool> newStreamCallback)
            : base(stream, closeOnDispose)
        {
            _newStreamCallback = newStreamCallback;
        }

        protected override bool AddPage(int streamSerial, byte[] pageBuf, bool isResync)
        {
            if (_packetProviders.TryGetValue(streamSerial, out var pp))
            {
                // try to add the page...
                if (pp.AddPage(pageBuf, isResync))
                {
                    // ..., then check to see if this is the end of the stream...
                    if (((PageFlags)pageBuf[5] & PageFlags.EndOfStream) != 0)
                    {
                        // ... and if so tell the packet provider the remove it from our list
                        pp.SetEndOfStream();
                        _packetProviders.Remove(streamSerial);
                    }
                    // ..., then let our caller know we're good
                    return true;
                }
                // otherwise, let PageReaderBase.ReadNextPage() know that we can't use the page:
                return false;
            }

            // we don't already have the stream, so try to add it to the list.
            pp = CreatePacketProvider(this, streamSerial);
            if (pp.AddPage(pageBuf, isResync))
            {
                _packetProviders.Add(streamSerial, pp);
                if (_newStreamCallback(pp))
                {
                    return true;
                }
                _packetProviders.Remove(streamSerial);
            }
            return false;
        }

        protected override void SetEndOfStreams()
        {
            foreach (var kvp in _packetProviders)
            {
                kvp.Value.SetEndOfStream();
            }
            _packetProviders.Clear();
        }

        public override bool ReadPageAt(long offset) => throw new NotSupportedException();
    }
}
