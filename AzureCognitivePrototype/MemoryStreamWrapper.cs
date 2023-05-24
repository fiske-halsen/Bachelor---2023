using Microsoft.CognitiveServices.Speech;

namespace AzureCognitivePrototype
{
    public class MemoryStreamWrapper : Stream
    {
        private readonly MemoryStream _memoryStream;

        public MemoryStreamWrapper() { }
        

        public MemoryStreamWrapper(AudioDataStream audioDataStream)
        {
            var bytes = ReadAllBytes(audioDataStream);
            _memoryStream = new MemoryStream(bytes);
        }

        private byte[] ReadAllBytes(AudioDataStream audioDataStream)
        {
            List<byte> byteList = new List<byte>();
            byte[] buffer = new byte[4096];
            uint bytesRead;
            uint position = 0;
            while ((bytesRead = audioDataStream.ReadData(position, buffer)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    byteList.Add(buffer[i]);
                }
                position += bytesRead;
            }
            return byteList.ToArray();
        }

        public override bool CanRead => _memoryStream.CanRead;
        public override bool CanSeek => _memoryStream.CanSeek;
        public override bool CanWrite => _memoryStream.CanWrite;

        public override long Length => _memoryStream.Length;

        public override long Position
        {
            get => _memoryStream.Position;
            set => _memoryStream.Position = value;
        }

        public override void Flush() => _memoryStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _memoryStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _memoryStream.Seek(offset, origin);

        public override void SetLength(long value) => _memoryStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _memoryStream.Write(buffer, offset, count);
    }
}
