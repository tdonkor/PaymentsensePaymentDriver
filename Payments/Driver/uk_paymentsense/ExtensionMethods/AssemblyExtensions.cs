using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Acrelec.Mockingbird.Payment.ExtensionMethods
{
    public static class AssemblyExtensions
    {
        public struct ImageFileHeader
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        };

        public static DateTime? GetBuildTimestamp(this Assembly assembly)
        {
            var path = assembly.Location;

            var buffer = new byte[Math.Max(Marshal.SizeOf(typeof(ImageFileHeader)), 4)];
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fileStream.Position = 0x3C;
                fileStream.Read(buffer, 0, 4);
                fileStream.Position = BitConverter.ToUInt32(buffer, 0); // COFF header offset
                fileStream.Read(buffer, 0, 4); // "PE\0\0"
                fileStream.Read(buffer, 0, buffer.Length);
            }

            var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                var header =
                    (ImageFileHeader) Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(),
                        typeof(ImageFileHeader));

                var timestamp = new DateTime(1970, 1, 1) +
                                new TimeSpan(header.TimeDateStamp * TimeSpan.TicksPerSecond);
                return TimeZone.CurrentTimeZone.ToLocalTime(timestamp);
            }
            catch
            {
                return null;
            }
            finally
            {
                pinnedBuffer.Free();
            }
        }

        public static string GetFileVersion(this Assembly assembly)
        {
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.FileVersion;
        }

        public static string GetTitle(this Assembly assembly)
        {
            var assemblyTitleAttribute = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
            return assemblyTitleAttribute?.Title;
        }
    }
}
