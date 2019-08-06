﻿using System.IO;

namespace Configuration_Tool.Configuration
{
    public class Translation
    {
        public string Header { get; set; }

        public string Format { get; set; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Header);
            writer.Write(Format);
        }

        public static Translation Parse(BinaryReader reader)
        {
            Translation translation = new Translation(
                reader.ReadString(),
                reader.ReadString());

            return translation;
        }

        public Translation(string header, string format)
        {
            Header = header;
            Format = format;
        }
    }
}
