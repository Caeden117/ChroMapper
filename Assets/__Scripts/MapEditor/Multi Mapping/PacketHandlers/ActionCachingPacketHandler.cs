using System;
using System.Collections.Generic;
using LiteNetLib.Utils;

public abstract class ActionCachingPacketHandler
{
    protected static List<Cache> CachedPackets = new List<Cache>();

    public static void FlushCache()
    {
        UnityEngine.Debug.Log("Flushing beatmap action cache...");

        foreach (var obj in CachedPackets)
        {
            switch (obj.CacheType)
            {
                case CacheType.Create:
                    BeatmapActionContainer.AddAction(obj.Object as BeatmapAction, true);
                    break;
                case CacheType.Undo:
                    BeatmapActionContainer.Undo((Guid)obj.Object);
                    break;
                case CacheType.Redo:
                    BeatmapActionContainer.Redo((Guid)obj.Object);
                    break;
            }
        }

        CachedPackets.Clear();
    }

    public class Cache
    {
        public CacheType CacheType;
        public object Object;
    }

    public enum CacheType
    {
        Create,
        Undo,
        Redo
    }
}

public class ActionCreateCachingPacketHandler : ActionCachingPacketHandler, IPacketHandler
{
    public void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader)
        => CachedPackets.Add(new Cache()
        {
            CacheType = CacheType.Create,
            Object = reader.GetBeatmapAction(identity)
        });
}

public class ActionUndoCachingPacketHandler : ActionCachingPacketHandler, IPacketHandler
{
    public void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader)
        => CachedPackets.Add(new Cache()
        {
            CacheType = CacheType.Undo,
            Object = Guid.Parse(reader.GetString())
        });
}

public class ActionRedoCachingPacketHandler : ActionCachingPacketHandler, IPacketHandler
{
    public void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader)
        => CachedPackets.Add(new Cache()
        {
            CacheType = CacheType.Redo,
            Object = Guid.Parse(reader.GetString())
        });
}
