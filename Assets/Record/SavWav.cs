
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public static class SavWav
{
    private const int HEADER_SIZE = 44;

    public static bool Save(string filepath, AudioClip clip)
    {
        if (!filepath.EndsWith(".wav"))
        {
            filepath += ".wav";
        }

        Debug.Log("Saving to: " + filepath);

        // 确保目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

        using (var fileStream = new FileStream(filepath, FileMode.Create))
        using (var writer = new BinaryWriter(fileStream))
        {
            // WAV header
            writer.Write(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            writer.Write(0); // Placeholder for file size
            writer.Write(new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
            writer.Write(new byte[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            writer.Write(16); // Sub-chunk size
            writer.Write((ushort)1); // Audio format (1 for PCM)
            writer.Write((ushort)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2); // Byte rate
            writer.Write((ushort)(clip.channels * 2)); // Block align
            writer.Write((ushort)16); // Bits per sample

            // Data sub-chunk
            writer.Write(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            writer.Write(0); // Placeholder for data size

            // Audio data
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Convert float samples to 16-bit PCM
            for (int i = 0; i < samples.Length; i++)
            {
                short intSample = (short)(samples[i] * short.MaxValue);
                writer.Write(intSample);
            }

            // Go back and fill in sizes
            long fileSize = writer.BaseStream.Length;
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((int)(fileSize - 8));
            writer.Seek(40, SeekOrigin.Begin);
            writer.Write((int)(fileSize - HEADER_SIZE));
        }

        return true;
    }
}
