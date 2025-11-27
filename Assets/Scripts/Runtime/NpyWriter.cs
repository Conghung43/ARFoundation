using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityEngine.XR.ARFoundation.Samples
{
    /// <summary>
    /// Utility class for writing NumPy .npy files.
    /// Supports writing arrays of different data types to the NPY format.
    /// </summary>
    public static class NpyWriter
    {
        private const byte NpyFormatVersion = 1;
        private const uint NpyMagicNumber = 0x9E4E554E; // "\x93NUMPY"

        /// <summary>
        /// Writes a float array to an NPY file.
        /// </summary>
        public static void WriteFloat(string filePath, float[] data, int[] shape)
        {
            byte[] header = CreateHeader("<f4", shape);
            byte[] magic = Encoding.ASCII.GetBytes("\u0093NUMPY");

            using (var stream = File.Create(filePath))
            {
                // Write magic number
                stream.Write(magic, 0, magic.Length);

                // Write format version (major, minor)
                stream.WriteByte(NpyFormatVersion);
                stream.WriteByte(0);

                // Write header length (little-endian uint16)
                ushort headerLength = (ushort)header.Length;
                stream.WriteByte((byte)(headerLength & 0xFF));
                stream.WriteByte((byte)((headerLength >> 8) & 0xFF));

                // Write header
                stream.Write(header, 0, header.Length);

                // Write data
                for (int i = 0; i < data.Length; i++)
                {
                    byte[] bytes = BitConverter.GetBytes(data[i]);
                    stream.Write(bytes, 0, bytes.Length);
                }

                Debug.Log($"Saved float array to {filePath}");
            }
        }

        /// <summary>
        /// Writes a byte array to an NPY file.
        /// </summary>
        public static void WriteByte(string filePath, byte[] data, int[] shape)
        {
            byte[] header = CreateHeader("|u1", shape);
            byte[] magic = Encoding.ASCII.GetBytes("\u0093NUMPY");

            using (var stream = File.Create(filePath))
            {
                // Write magic number
                stream.Write(magic, 0, magic.Length);

                // Write format version (major, minor)
                stream.WriteByte(NpyFormatVersion);
                stream.WriteByte(0);

                // Write header length (little-endian uint16)
                ushort headerLength = (ushort)header.Length;
                stream.WriteByte((byte)(headerLength & 0xFF));
                stream.WriteByte((byte)((headerLength >> 8) & 0xFF));

                // Write header
                stream.Write(header, 0, header.Length);

                // Write data
                stream.Write(data, 0, data.Length);

                Debug.Log($"Saved byte array to {filePath}");
            }
        }

        /// <summary>
        /// Writes a ushort array to an NPY file.
        /// </summary>
        public static void WriteUShort(string filePath, ushort[] data, int[] shape)
        {
            byte[] header = CreateHeader("<u2", shape);
            byte[] magic = Encoding.ASCII.GetBytes("\u0093NUMPY");

            using (var stream = File.Create(filePath))
            {
                // Write magic number
                stream.Write(magic, 0, magic.Length);

                // Write format version (major, minor)
                stream.WriteByte(NpyFormatVersion);
                stream.WriteByte(0);

                // Write header length (little-endian uint16)
                ushort headerLength = (ushort)header.Length;
                stream.WriteByte((byte)(headerLength & 0xFF));
                stream.WriteByte((byte)((headerLength >> 8) & 0xFF));

                // Write header
                stream.Write(header, 0, header.Length);

                // Write data
                for (int i = 0; i < data.Length; i++)
                {
                    byte[] bytes = BitConverter.GetBytes(data[i]);
                    stream.Write(bytes, 0, bytes.Length);
                }

                Debug.Log($"Saved ushort array to {filePath}");
            }
        }

        private static byte[] CreateHeader(string dtype, int[] shape)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{ 'descr': '");
            sb.Append(dtype);
            sb.Append("', 'fortran_order': False, 'shape': (");

            for (int i = 0; i < shape.Length; i++)
            {
                sb.Append(shape[i]);
                if (i < shape.Length - 1)
                    sb.Append(", ");
            }

            if (shape.Length == 1)
                sb.Append(",");

            sb.Append("), }");

            byte[] headerBytes = Encoding.ASCII.GetBytes(sb.ToString());
            
            // Pad to 64-byte boundary
            int totalHeaderSize = headerBytes.Length + 1; // +1 for newline
            int paddedSize = ((totalHeaderSize + 63) / 64) * 64;
            int padding = paddedSize - totalHeaderSize;

            byte[] finalHeader = new byte[paddedSize];
            Array.Copy(headerBytes, finalHeader, headerBytes.Length);
            
            for (int i = 0; i < padding; i++)
            {
                finalHeader[headerBytes.Length + i] = (byte)' ';
            }
            
            finalHeader[paddedSize - 1] = (byte)'\n';

            return finalHeader;
        }
    }
}
