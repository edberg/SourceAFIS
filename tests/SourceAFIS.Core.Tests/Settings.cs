using System.IO;
using System.Drawing;
using System.Reflection;
using System.Resources;

namespace SourceAFIS.Tests
{
    static class Settings
    {
        private static Stream GetResourceStream(string name)
        {
            var assembly = typeof(Settings).GetTypeInfo().Assembly;
            var stream = assembly.GetManifestResourceStream(name);
            return stream;
        }

        private static byte[] GetResourceBytes(string name)
        {
            using (var stream = GetResourceStream(name))
            {
                if (stream == null) return null;
                byte[] ba = new byte[stream.Length];
                stream.Read(ba, 0, ba.Length);
                return ba;
            }
        }

        public static Bitmap SomeFingerprint = new Bitmap(GetResourceStream("SourceAFIS.Core.Tests.Data.Images.101_1.tif"));
        public static Bitmap MatchingFingerprint = new Bitmap(GetResourceStream("SourceAFIS.Core.Tests.Data.Images.101_2.tif"));
        public static Bitmap NonMatchingFingerprint = new Bitmap(GetResourceStream("SourceAFIS.Core.Tests.Data.Images.102_1.tif"));

        public static byte[] Template1_1 = GetResourceBytes("SourceAFIS.Core.Tests.Data.IsoTemplates.1_1.ist");
        public static byte[] Template1_2 = GetResourceBytes("SourceAFIS.Core.Tests.Data.IsoTemplates.1_2.ist");
        public static byte[] Template2_1 = GetResourceBytes("SourceAFIS.Core.Tests.Data.IsoTemplates.2_1.ist");
        public static byte[] Template2_2 = GetResourceBytes("SourceAFIS.Core.Tests.Data.IsoTemplates.2_2.ist");

    }
}
