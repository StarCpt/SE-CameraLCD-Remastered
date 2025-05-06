using System.IO;
using System.Xml;
using System.Xml.Serialization;
using VRage.FileSystem;

namespace CameraLCD
{
    public class TargetCameraSettings
    {
        private const string fileName = "CameraLCDSettings.xml";
        private static string FilePath => Path.Combine(MyFileSystem.UserDataPath, "Storage", fileName);

        protected TargetCameraSettings()
        {
        }

        public bool Enabled { get; set; } = true;

        public int X { get; set; } = 25;
        public int Y { get; set; } = 25;
        public int Width { get; set; } = 500;
        public int Height { get; set; } = 500;

        public static TargetCameraSettings Load()
        {
            string file = FilePath;
            if (File.Exists(file))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(TargetCameraSettings));
                    using (XmlReader xml = XmlReader.Create(file))
                    {
                        return (TargetCameraSettings)serializer.Deserialize(xml);
                    }
                }
                catch { }
            }

            return new TargetCameraSettings();
        }

        public void Save()
        {
            try
            {
                string file = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                XmlSerializer serializer = new XmlSerializer(typeof(TargetCameraSettings));
                using (StreamWriter stream = File.CreateText(file))
                {
                    serializer.Serialize(stream, this);
                }
            }
            catch { }
        }
    }
}
