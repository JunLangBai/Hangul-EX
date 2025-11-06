using System;
using System.IO;
using UnityEngine;

// (和上次一样，保持不变)
namespace MemoryGameTools 
{
    public static class SavWav
    {
        private const int HEADER_SIZE = 44;
        
        public static bool Save(string filepath, float[] samples, int frequency, int channels)
        {
            if (!filepath.EndsWith(".wav"))
            {
                filepath += ".wav";
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            using (var fileStream = new FileStream(filepath, FileMode.Create))
            using (var writer = new BinaryWriter(fileStream))
            {
                // WAV
                writer.Write(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                writer.Write(0); 
                writer.Write(new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                writer.Write(0);

                // 写入
                for (int i = 0; i < samples.Length; i++)
                {
                    short intSample = (short)(samples[i] * short.MaxValue);
                    writer.Write(intSample);
                }

                // 更新
                long fileSize = writer.BaseStream.Length;
                writer.Seek(4, SeekOrigin.Begin);
                writer.Write((int)(fileSize - 8));
                writer.Seek(40, SeekOrigin.Begin);
                writer.Write((int)(fileSize - HEADER_SIZE));
            }

            return true;
        }
    }
}