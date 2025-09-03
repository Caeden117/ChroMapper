using System;
using System.IO;
using UnityEngine;

public static class FileContentValidationHelper
{
    private const int vorbisIdHeaderOffset = 28;
    private static readonly int oggVorbisSignatureSize = vorbisIdHeaderOffset + "?vorbis".Length;
    private static readonly int wavSignatureSize = "RIFF????WAVE".Length;

    public static AudioType GetAudioType(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open);
        var maxBufferSize = Math.Max(oggVorbisSignatureSize, wavSignatureSize);
        var buffer = new byte[maxBufferSize];
        _ = stream.Read(buffer, 0, maxBufferSize);

        if (IsOggVorbisFileSignature(buffer))
            return AudioType.OGGVORBIS;

        if (IsWavFileSignature(buffer))
            return AudioType.WAV;

        return AudioType.UNKNOWN;
    }

    // Beat Saber and CM only supports ogg vorbis and wav
    public static bool IsSupportedAudioFormat(string filePath) => GetAudioType(filePath) != AudioType.UNKNOWN;

    private static bool IsWavFileSignature(byte[] buffer)
    {
        if (buffer.Length < wavSignatureSize)
            return false;

        return buffer[0] == 'R'
               && buffer[1] == 'I'
               && buffer[2] == 'F'
               && buffer[3] == 'F'
               && buffer[8] == 'W'
               && buffer[9] == 'A'
               && buffer[10] == 'V'
               && buffer[11] == 'E';
    }

    private static bool IsOggVorbisFileSignature(byte[] buffer)
    {
        if (buffer.Length < oggVorbisSignatureSize)
            return false;

        var hasOggContainerFileSignature = buffer[0] == 'O'
                                           && buffer[1] == 'g'
                                           && buffer[2] == 'g'
                                           && buffer[3] == 'S';
        var hasVorbisFileSignature = buffer[vorbisIdHeaderOffset + 1] == 'v'
                                     && buffer[vorbisIdHeaderOffset + 2] == 'o'
                                     && buffer[vorbisIdHeaderOffset + 3] == 'r'
                                     && buffer[vorbisIdHeaderOffset + 4] == 'b'
                                     && buffer[vorbisIdHeaderOffset + 5] == 'i'
                                     && buffer[vorbisIdHeaderOffset + 6] == 's';
        return hasOggContainerFileSignature && hasVorbisFileSignature;
    }
}
