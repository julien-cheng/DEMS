namespace Documents.Backends.Drivers.Encryption
{
    using Microsoft.Extensions.Logging;
    using Org.BouncyCastle.Crypto.Digests;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;

    public class ChunkEncryptorStreamReader : Stream
    {
        private readonly Stream PreviousStream;
        private readonly ICryptoTransform Encryptor;

        private readonly long TotalClearLength;
        private readonly long ClearStreamLength;

        private readonly long StreamLength;

        private readonly int BlockSizeBytes;

        private bool CipherBufferFull = false;
        private readonly byte[] CipherBuffer;
        private readonly byte[] BlockBuffer;

        private long CipherPosition = 0;
        private int CipherBufferPosition = 0;
        private long PreviousStreamPosition = 0;
        private bool InjectHeader = false;
        private StreamEncryptionHeader Header = null;
        private MemoryStream HeaderStream = null;

        private IEnumerable<GeneralDigest> Digests;

        public ChunkEncryptorStreamReader(
            Stream previousStream,
            SymmetricAlgorithm algorithm,
            long from,
            long to,
            long totalFileLength,
            IEnumerable<GeneralDigest> digests = null,
            byte[] iv = null,
            ILogger logger = null
        )
        {
            this.PreviousStream = previousStream;

            this.TotalClearLength = totalFileLength;
            this.ClearStreamLength = to - from + 1;

            this.BlockSizeBytes = algorithm.BlockSize / 8;

            this.CipherBuffer = new byte[BlockSizeBytes];
            this.BlockBuffer = new byte[BlockSizeBytes];

            this.Digests = digests;

            if (from == 0)
            {
                Header = new StreamEncryptionHeader(BlockSizeBytes)
                {
                    From = 0,
                    To = this.TotalClearLength
                };

                algorithm.GenerateIV();
                algorithm.IV.CopyTo(Header.IV, 0);
                // rant: this seems a poor API design because of the race condition.
                // but seeing as it's random data, if it collides with other
                // data, well, that might not be so bad.... but still.

                HeaderStream = new MemoryStream(StreamEncryptionHeader.HeaderLength(BlockSizeBytes));
                Header.WriteTo(HeaderStream);
                HeaderStream.Seek(0, SeekOrigin.Begin);
                if (iv != null)
                    throw new Exception("Encountered non-null IV on first chunk");

                iv = Header.IV;
                InjectHeader = true;
            }
            else
                if (iv == null)
                    throw new Exception("Secondary chunk with null IV encountered");

            long clearLength = to - from + 1;
            long headerLength = (from == 0)
                ? StreamEncryptionHeader.HeaderLength(BlockSizeBytes)
                : 0;

            // note, we are not adding room for padding. EVEN if it's the last block.
            // This class does not use any traditional padding modes, it's length prefixed.
            long cipherLength =
                (clearLength / BlockSizeBytes) * BlockSizeBytes
                + ((clearLength % BlockSizeBytes == 0) ? 0 : BlockSizeBytes); // if it was not a multiple, add another block

            this.StreamLength = headerLength + cipherLength;

            this.Encryptor = algorithm.CreateEncryptor(algorithm.Key, iv);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;

            count = (int)Math.Min((long)count, StreamLength - CipherPosition);

            if (InjectHeader)
            {
                int headerBytesRead = HeaderStream.Read(buffer, offset, count);
                CipherPosition += headerBytesRead;
                bytesRead += headerBytesRead;
                offset += headerBytesRead;
                count -= headerBytesRead;

                if (HeaderStream.Position != HeaderStream.Length)
                    return headerBytesRead;

                InjectHeader = false;
            }

            while (count > 0)
            {
                if (CipherBufferFull) // last read didn't empty the buffer
                {
                    int cipherBytesRead = Math.Min(count, CipherBuffer.Length - CipherBufferPosition);
                    Array.Copy(CipherBuffer, CipherBufferPosition, buffer, offset, cipherBytesRead);

                    count -= cipherBytesRead;
                    bytesRead += cipherBytesRead;
                    CipherBufferPosition += cipherBytesRead;
                    CipherPosition += cipherBytesRead;
                    offset += cipherBytesRead;

                    if (CipherBufferPosition == CipherBuffer.Length) // Is it empty now?
                    {
                        CipherBufferPosition = 0;
                        CipherBufferFull = false;
                    }
                    else
                        // no, return the number of bytes that were read
                        return bytesRead;
                }

                if (!CipherBufferFull && count > 0)
                {
                    int blockLength = (int)Math.Min(ClearStreamLength - PreviousStreamPosition, BlockSizeBytes);

                    int clearBytesRead = 0;
                    while (clearBytesRead < blockLength)
                    {
                        int bytes = PreviousStream.Read(BlockBuffer, clearBytesRead, blockLength - clearBytesRead);
                        clearBytesRead += bytes;
                        PreviousStreamPosition += bytes;
                        if (bytes == 0 && clearBytesRead != blockLength)
                            throw new Exception("Encountered premature EOF on previous stream");
                    }

                    var md5 = MD5.Create();
                    md5.TransformBlock(BlockBuffer, 0, BlockSizeBytes, BlockBuffer, 0);

                    // if we have hash digests, update them
                    if (this.Digests != null)
                        foreach (var digest in this.Digests)
                            digest.BlockUpdate(BlockBuffer, 0, blockLength);

                    int cryptoBytes = Encryptor.TransformBlock(
                            BlockBuffer, 0, BlockSizeBytes, CipherBuffer, 0);
                    if (cryptoBytes < BlockSizeBytes)
                        throw new Exception("CryptoTransform did not return a full block, check padding mode");

                    CipherBufferFull = true;
                }
            }
            return bytesRead;
        }

        public byte[] NextIV()
        {
            return this.CipherBuffer.Clone() as byte[];
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => StreamLength;
        public override long Position 
        {
            get
            {
                return CipherPosition;
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
            => throw new NotImplementedException();

        public override void SetLength(long value)
            => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotImplementedException();
    }
}
