using System.IO;
using System.Drawing;

namespace SourceAFIS.Tests
{
    static class Settings
    {
        public static string RootFolder = Path.GetFullPath(Path.Combine(".."));
        public static string DataPath = Path.Combine(RootFolder, "Data");
        public static string IsoTemplatePath = Path.Combine(DataPath, "IsoTemplates");
        public static string ImagePath = Path.Combine(DataPath, "Images");

        public static string SomeFingerprintPath = Path.Combine(ImagePath, "101_1.tif");
        public static string MatchingFingerprintPath = Path.Combine(ImagePath, "101_2.tif");
        public static string NonMatchingFingerprintPath = Path.Combine(ImagePath, "102_1.tif");

        public static Bitmap SomeFingerprint = new Bitmap(SomeFingerprintPath);
        public static Bitmap MatchingFingerprint = new Bitmap(MatchingFingerprintPath);
        public static Bitmap NonMatchingFingerprint = new Bitmap(NonMatchingFingerprintPath);
    }
}
