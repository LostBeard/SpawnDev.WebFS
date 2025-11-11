
using System;
using System.Net.WebSockets;
using System.Threading;

namespace SpawnDev.WebFS
{
    public class WritableWebSocketStream : Stream, IDisposable
    {
        WebSocket WebSocket;
        public WritableWebSocketStream(WebSocket webSocket)
        {
            WebSocket = webSocket;
        }
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => WebSocket?.State == WebSocketState.Open;

        public override long Length => 0;

        public override long Position { get => 0; set { } }

        public override void Flush()
        {

        }

        int sendCount = 0;
        public async Task EndMessage(CancellationToken cancellationToken = default)
        {
            if (sendCount == 0)
            {
                return;
            }
            if (sendCount > 1)
            {
                var nmt = true;
            }
            sendCount = 0;
            await WebSocket.SendAsync(new byte[]{ }, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            sendCount++;
            await WebSocket.SendAsync(buffer, WebSocketMessageType.Binary, false, cancellationToken).ConfigureAwait(false);
        }
    }
}

