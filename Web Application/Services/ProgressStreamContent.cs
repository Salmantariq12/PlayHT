using System.Net;

namespace PlayHT.Services
{
    public class ProgressStreamContent : StreamContent
    {
        private readonly Stream _stream;
        private readonly IProgress<int> _progress;

        public ProgressStreamContent(Stream stream, IProgress<int> progress) : base(stream)
        {
            _stream = stream;
            _progress = progress;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var buffer = new byte[8192];
            long totalBytes = 0;
            long totalLength = _stream.Length;
            int bytesRead;

            while ((bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
                totalBytes += bytesRead;
                _progress?.Report((int)((double)totalBytes / totalLength * 100));
            }
        }
    }
}
