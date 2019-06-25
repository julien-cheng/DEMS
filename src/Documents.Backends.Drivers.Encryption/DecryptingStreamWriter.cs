namespace Documents.Backends.Drivers.Encryption
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Security.Cryptography;

    public class DecryptingStreamWriter : Stream
    {
        private readonly Stream PreviousStream;
        private readonly SymmetricAlgorithm Algorithm;
        private ICryptoTransform Decryptor;

        private readonly long From;
        private readonly long To;
        private readonly long StreamLength;
        private readonly int BlockSizeBytes;
        private readonly bool IsSeeking;
        private readonly long TotalLength;
        public readonly long CipherFrom;
        public readonly long CipherTo;
        public readonly long CipherTotalLength;

        private long ClearStreamPosition = 0;
        private int CipherBufferPosition = 0;
        private readonly byte[] CipherBuffer;
        private readonly byte[] BlockBuffer;

        private StreamEncryptionHeader Header = null;
        private MemoryStream HeaderStream = null;

        private int ExpectedHeaderBytes = 0;
        private int SkipBytes = 0;

        public DecryptingStreamWriter(
            Stream previousStream,
            SymmetricAlgorithm algorithm,
            long from,
            long to,
            long totalLength,
            ILogger logger = null
        )
        {
            this.PreviousStream = previousStream;
            this.Algorithm = algorithm;

            Algorithm.Padding = PaddingMode.None;

            this.From = from;
            this.To = to;
            this.TotalLength = totalLength;

            this.BlockSizeBytes = algorithm.BlockSize / 8;

            this.CipherBuffer = new byte[BlockSizeBytes];
            this.BlockBuffer = new byte[BlockSizeBytes];

            this.IsSeeking = this.From != 0;
            int fullHeaderLength = StreamEncryptionHeader.HeaderLength(BlockSizeBytes);

            this.ExpectedHeaderBytes = this.IsSeeking
                ? BlockSizeBytes
                : fullHeaderLength;

            this.HeaderStream = new MemoryStream();

            this.StreamLength = to - from + 1;

            from += fullHeaderLength;
            to += fullHeaderLength;

            var blockNumber = from / BlockSizeBytes;
            var blockStart = blockNumber * BlockSizeBytes;

            SkipBytes = (int)(from - blockStart);

            // backup to either the full header or a prior block
            from = IsSeeking
                ? from - BlockSizeBytes - SkipBytes
                : 0;

            if (from < 0)
                throw new Exception("nonsense output from encryption stream seeking algorithm");

            // if "to" isn't on the edge of a complete block,
            // round it up to the nearest end of block
            if ((to + 1) % BlockSizeBytes != 0)
                to = (((to + 1) / BlockSizeBytes) + 1) * BlockSizeBytes - 1;

            this.CipherFrom = from;

            this.CipherTo = to;

            // note, we are not adding room for padding. EVEN if it's the last block.
            // This class does not use any traditional padding modes, it's length prefixed.
            this.CipherTotalLength = this.ExpectedHeaderBytes
                + (this.TotalLength / BlockSizeBytes) * BlockSizeBytes
                + ((this.TotalLength % BlockSizeBytes == 0) ? 0 : BlockSizeBytes); // if it was not a multiple, add another block
                
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (ExpectedHeaderBytes > 0)
            {
                int headerBytes = Math.Min(count, ExpectedHeaderBytes);
                HeaderStream.Write(buffer, offset, headerBytes);
                ExpectedHeaderBytes -= headerBytes;
                count -= headerBytes;
                offset += headerBytes;

                if (ExpectedHeaderBytes > 0)
                    return;

                var headerBuffer = HeaderStream.ToArray();
                Header = StreamEncryptionHeader.Read(
                    headerBuffer, 0, headerBuffer.Length, BlockSizeBytes, !IsSeeking);

                Decryptor = Algorithm.CreateDecryptor(Algorithm.Key, Header.IV);
            }

            while(count > 0)
            {
                int blockBytes = Math.Min(BlockSizeBytes - CipherBufferPosition, count);

                Array.Copy(buffer, offset, CipherBuffer, CipherBufferPosition, blockBytes);

                count -= blockBytes;
                offset += blockBytes;
                CipherBufferPosition += blockBytes;

                if (CipherBufferPosition == BlockSizeBytes)
                {
                    CipherBufferPosition = 0;
                    int cryptoBytes = Decryptor.TransformBlock(
                            CipherBuffer, 0, BlockSizeBytes, BlockBuffer, 0);

                    if (cryptoBytes < BlockSizeBytes)
                        throw new Exception("CryptoTransform did not return a full block, check padding mode");

                    int clearSize = (int)Math.Min(BlockSizeBytes, TotalLength - ClearStreamPosition);

                    if (ClearStreamPosition + clearSize > StreamLength)
                        clearSize -= (int)((ClearStreamPosition + clearSize) - StreamLength);

                    if (SkipBytes + clearSize > BlockSizeBytes)
                        clearSize -= (int)((SkipBytes + clearSize) - BlockSizeBytes);
                    
                    PreviousStream.Write(BlockBuffer, SkipBytes, clearSize);
                    SkipBytes = 0;

                    ClearStreamPosition += clearSize;
                }
            }
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                return StreamLength;
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
