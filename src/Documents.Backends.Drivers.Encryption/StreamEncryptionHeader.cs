namespace Documents.Backends.Drivers.Encryption
{
    using System;
    using System.IO;

    public class StreamEncryptionHeader
    {
        public long From = -1;
        public long To = -1;
        public byte[] IV = null;


        public StreamEncryptionHeader()
        {}

        public StreamEncryptionHeader(int blockSizeBytes)
        {
            IV = new byte[blockSizeBytes];
        }

        public int WriteTo(byte[] buffer, int offset, int bufferAvailableBytes)
        {
            int bytes = 0;

            if (bufferAvailableBytes < IV.Length + sizeof(long) * 2)
                throw new Exception("Buffer overrun");

            BitConverter.GetBytes(From).CopyTo(buffer, offset + bytes);
            bytes += sizeof(long);

            BitConverter.GetBytes(To).CopyTo(buffer, offset+bytes);
            bytes += sizeof(long);

            IV.CopyTo(buffer, offset + bytes);
            bytes += IV.Length;

            return bytes;
        }

        public void WriteTo(Stream stream)
        {
            byte[] buffer;
            buffer = BitConverter.GetBytes(From);
            stream.Write(buffer, 0, buffer.Length);

            buffer = BitConverter.GetBytes(To);
            stream.Write(buffer, 0, buffer.Length);

            stream.Write(IV, 0, IV.Length);
        }


        public static StreamEncryptionHeader Read(
            byte[] buffer, 
            int offset, 
            int count, 
            int blockSizeBytes,
            bool expectFullHeader = true
            )
        {
            var header = new StreamEncryptionHeader();

            if (expectFullHeader)
            {
                header.From = BitConverter.ToInt64(buffer, offset);
                offset += sizeof(long);

                header.To = BitConverter.ToInt64(buffer, offset);
                offset += sizeof(long);
            }

            header.IV = new byte[blockSizeBytes];
            for (int i = 0; i < header.IV.Length; i++)
                header.IV[i] = buffer[offset + i];
            offset += header.IV.Length;

            return header;
        }

        /*
        public static StreamEncryptionHeader Read(Stream stream, int blockSizeBytes)
        {
            byte[] buffer = new byte[blockSizeBytes + sizeof(long) * 2];
            int bytesRead = 0;

            while (bytesRead < buffer.Length)
            {
                int bytes = 0;
                bytesRead += bytes = stream.Read(buffer, bytes, buffer.Length - bytes);
                if (bytes == 0)
                    throw new Exception("Could not read StreamEncryptionHeader");
            }


            return Read(buffer, 0, buffer.Length, blockSizeBytes);
        }
        */ //useless?

        public long FileLength {
            get
            {
                if (From == -1)
                    return 0;
                else
                    return To - From + 1;
            }
        }

        public static int HeaderLength(int blockSizeBytes)
        {
            return blockSizeBytes + sizeof(long) * 2;
        }
    }
}
