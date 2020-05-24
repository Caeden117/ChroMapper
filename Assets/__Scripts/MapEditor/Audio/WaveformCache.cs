using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

public class WaveformCache {
    public class WaveformData
    {
        public int chunks;
        public int chunksLoaded { get; internal set; }
        public float[][] bandVolumes { get; internal set; }

        public void initBandVolumes(int len)
        {
            if (bandVolumes == null)
            {
                bandVolumes = new float[len * chunks][];
            }
        }

        public void getChunk(int chunkId, int len, ref float[][] toRender)
        {
            Array.Copy(bandVolumes, chunkId * len, toRender, 0, len);
            Array.Reverse(toRender);
        }
    }

    private Thread saveThread;
    private string fileName;
    private int ColumnsPerChunk;
    private WaveformGenerator waveformGenerator;

    public bool fileHadAllChunks { get; private set; } = false;
    public WaveformData waveformData { get; private set; } = new WaveformData();

    public WaveformCache(WaveformGenerator waveformGenerator, int ColumnsPerChunk) {
        this.ColumnsPerChunk = ColumnsPerChunk;
        this.waveformGenerator = waveformGenerator;
        fileName = Path.Combine(BeatSaberSongContainer.Instance.song.directory, ".waveform");
    }

    public void ReadWaveformFromFile()
    {
        int idx = 0;
        float[] bandTemp = new float[AudioManager._bands.Length - 1];
        Debug.Log("WaveformCache: Reading file");

        if (!File.Exists(fileName))
        {
            waveformData.chunksLoaded = 0;
            return;
        }

        using (FileStream file = File.OpenRead(fileName))
        {
            using (GZipStream decompressionStream = new GZipStream(file, CompressionMode.Decompress))
            {
                using (var resultStream = new MemoryStream())
                {
                    // Decompress first and then read
                    decompressionStream.CopyTo(resultStream);
                    resultStream.Seek(0, SeekOrigin.Begin);

                    using (BinaryReader reader = new BinaryReader(resultStream))
                    {
                        waveformData.chunksLoaded = reader.ReadInt32();
                        waveformData.chunks = reader.ReadInt32();
                        waveformData.initBandVolumes(ColumnsPerChunk);
                        while (resultStream.Position < resultStream.Length)
                        {
                            int chunksGen = idx / ColumnsPerChunk;
                            if (chunksGen >= waveformData.chunksLoaded)
                            {
                                break;
                            }
                            for (int i = 0; i < AudioManager._bands.Length - 1; i++)
                            {
                                bandTemp[i] = reader.ReadUInt16() / 600f;
                            }
                            // Repopulate this as it's used for future saves
                            waveformData.bandVolumes[idx] = (float[])bandTemp.Clone();
                            if (++idx % ColumnsPerChunk == 0)
                            {
                                waveformGenerator.ChunkComplete(chunksGen);
                            }
                        }
                    }
                }
            }
        }

        if (waveformData.chunks <= waveformData.chunksLoaded)
        {
            Debug.Log("WaveformCache: Loaded all chunks from file");
            fileHadAllChunks = true;
        }
    }

    private void SaveWaveformToFile()
    {
        using (FileStream file = File.Create(fileName))
        {
            using (GZipStream compressionStream = new GZipStream(file, CompressionMode.Compress))
            {
                using (BinaryWriter writer = new BinaryWriter(compressionStream))
                {
                    // This data helps us know when we load the file again what chunks can be
                    // rendered (as some will be saved partially complete)
                    // And if we need to resume processing
                    writer.Write(waveformData.chunksLoaded);
                    writer.Write(waveformData.chunks);
                    foreach (float[] band in waveformData.bandVolumes)
                    {
                        if (band == null || band.Length == 0)
                        {
                            // Save unprocessed data as 0s, the gzip wrapper compresses these very well
                            for (int i = 0; i < AudioManager._bands.Length - 1; i++)
                            {
                                writer.Write(0f);
                            }
                        }
                        else
                        {
                            foreach (float value in band)
                            {
                                // 16-bit is plenty for the spectogram
                                writer.Write((ushort)(value*600));
                            }
                        }
                    }
                }
            }
        }
    }

    public void ScheduleSave(HashSet<int> renderedChunks)
    {
        if (OngoingSave())
        {
            return;
        }

        Debug.Log("WaveformCache: Saving state");

        while (renderedChunks.Contains(waveformData.chunksLoaded))
            waveformData.chunksLoaded++;

        saveThread = new Thread(SaveWaveformToFile);
        saveThread.Start();
    }

    public bool OngoingSave()
    {
        return saveThread != null && saveThread.IsAlive;
    }
}
