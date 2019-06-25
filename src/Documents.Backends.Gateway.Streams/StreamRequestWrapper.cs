/*
    Streams that don't implement get_Length() are difficult to
    work with and debug. This lets us declare a length in 
    a thin wrapper around a stream that does not.

    S3 also wants to retry in case of error. This means seeking
    back to zero. That's not going to go over well, and we'll
    do our own retries. But it will reject any stream that doesn't
    claim to support seeking. So we lie, then throw if they try.
*/
namespace Documents.Backends.Gateway.Streams
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class StreamRequestWrapper : Stream
    {
        protected long _Position = 0;

        protected ILogger Logger;

        public Stream UnderlyingStream { get; set; }
        public long DeclaredLength { get; set; }

        public int SkipBytes { get; set; }

        public StreamRequestWrapper(ILogger logger = null)
        {
            this.Logger = logger;
        }

        public StreamRequestWrapper(Stream stream, long declaredLength, ILogger logger = null)
        {
            this.UnderlyingStream = stream;
            this.DeclaredLength = declaredLength;
            this.Logger = logger;
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                int bytes = await UnderlyingStream.ReadAsync(buffer, offset, count);
                _Position += bytes;

                Logger?.LogDebug($"StreamRequestWrapper: Read(buffer, {offset}, {count}) returned {bytes} Position: {this.Position} Length: {this.Length}");

                return bytes;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                int bytes = UnderlyingStream.Read(buffer, offset, count);
                _Position += bytes;

                Logger?.LogDebug($"StreamRequestWrapper: Read(buffer, {offset}, {count}) returned {bytes} Position: {this.Position} Length: {this.Length}");

                return bytes;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override long Length
        {
            get
            {
                //Logger?.LogWarning($"StreamRequestWrapper: get_Length = {DeclaredLength}");

                return DeclaredLength;
            }
        }

        public override long Position
        {
            get
            {
                return _Position;
            }

            set
            {
                Logger?.LogWarning($"StreamRequestWrapper: set_Position({value})");

                throw new NotImplementedException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                if (disposing)
                {
                    try
                    {
                        // todo: work this out when both are disposed
                        //this.UnderlyingStream.Dispose();
                    }
                    catch (Exception) { }
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Logger?.LogWarning($"StreamRequestWrapper: Seek({offset}, {origin})");
            throw new Exception($"StreamRequestWrapper: Seek({offset}, {origin}) Not possible");
        }

        public override void SetLength(long value)
        {
            Logger?.LogWarning($"StreamRequestWrapper: SetLength({value})");
            throw new Exception($"StreamRequestWrapper: SetLength({value}) Not possible");
        }

        private bool DoSkipBytes(byte[] buffer, ref int offset, ref int count)
        {
            if (SkipBytes > 0)
            {
                if (count > SkipBytes)
                {
                    offset += SkipBytes;
                    count -= SkipBytes;

                    SkipBytes = 0;
                    _Position += SkipBytes;
                }
                if (count <= SkipBytes)
                {
                    SkipBytes -= count;
                    _Position += count;
                    return true;
                }
            }
            return false;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (DoSkipBytes(buffer, ref offset, ref count))
                return;
            else
                this.UnderlyingStream.Write(buffer, offset, count);
        }
        public async override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (DoSkipBytes(buffer, ref offset, ref count))
                return;
            else
                await this.UnderlyingStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override void Flush()
        {
        }
    }
}
