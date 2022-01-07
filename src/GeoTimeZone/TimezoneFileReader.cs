using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace GeoTimeZone
{
    internal class TimezoneFileReader
    {
        private const int LineEndLength = 1;

#if !NETSTANDARD2_1
        private static readonly object Locker = new object();
#endif

        private readonly int _lineLength;
        private readonly Lazy<MemoryStream> LazyData;
        private readonly Lazy<int> LazyCount;

        internal static TimezoneFileReader Default { get; } = new TimezoneFileReader(5, new Lazy<MemoryStream>(LoadData));

        private TimezoneFileReader(int precision, Lazy<MemoryStream> loadData)
        {
            _lineLength = precision + 3;
            LazyData = loadData;
            LazyCount = new Lazy<int>(GetCount);
        }

        internal static TimezoneFileReader Create(int precision, Stream timezoneFileStream)
        {
            if (!(timezoneFileStream is MemoryStream ms))
            {
                ms = new MemoryStream();
                timezoneFileStream.CopyTo(ms);
            }

#if NETSTANDARD2_1
            return new TimezoneFileReader(precision, new Lazy<MemoryStream>(ms));
#else
            return new TimezoneFileReader(precision, new Lazy<MemoryStream>(() => ms));
#endif
        }

        private static MemoryStream LoadData()
        {
            var ms = new MemoryStream();

#if NETSTANDARD1_1
            Assembly assembly = typeof(TimezoneFileReader).GetTypeInfo().Assembly;
#else
            Assembly assembly = typeof(TimezoneFileReader).Assembly;
#endif

            using Stream compressedStream = assembly.GetManifestResourceStream("GeoTimeZone.TZ.dat.gz");
            using var stream = new GZipStream(compressedStream!, CompressionMode.Decompress);
            if (stream == null)
                throw new InvalidOperationException();

            stream.CopyTo(ms);

            return ms;
        }

        private int GetCount()
        {
            MemoryStream ms = LazyData.Value;
            return (int) (ms.Length / (_lineLength + LineEndLength));
        }

        public int Count => LazyCount.Value;

        public string GetLine(int line)
        {
            int index = (_lineLength + LineEndLength) * (line - 1);

            MemoryStream stream = LazyData.Value;

#if NETSTANDARD2_1
            var span = new ReadOnlySpan<byte>(stream.GetBuffer(), index, _lineLength);
            return Encoding.UTF8.GetString(span);
#else
            var buffer = new byte[_lineLength];

            lock (Locker)
            {
                stream.Position = index;
                stream.Read(buffer, 0, _lineLength);
            }

            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
#endif
        }
    }
}