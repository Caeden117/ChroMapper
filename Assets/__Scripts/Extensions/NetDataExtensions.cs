using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using LiteNetLib;
using LiteNetLib.Utils;

public static class NetDataExtensions
{
    public static BeatmapObject GetBeatmapObject(this NetDataReader reader)
    {
        var beatmapObjectType = (BeatmapObject.ObjectType)reader.GetByte();

        return beatmapObjectType switch
        {
            BeatmapObject.ObjectType.Note => reader.Get<BeatmapNote>(),
            BeatmapObject.ObjectType.Event => reader.Get<MapEvent>(),
            BeatmapObject.ObjectType.Obstacle => reader.Get<BeatmapObstacle>(),
            BeatmapObject.ObjectType.CustomNote => throw new System.NotImplementedException(), // Custom notes not supported
            BeatmapObject.ObjectType.CustomEvent => reader.Get<BeatmapCustomEvent>(),
            BeatmapObject.ObjectType.BpmChange => reader.Get<BeatmapBPMChange>(),
            BeatmapObject.ObjectType.Bookmark => reader.Get<BeatmapBookmark>(),
            _ => throw new InvalidPacketException("Attempting to parse an invalid object type"),
        };
    }

    public static void PutBeatmapObject(this NetDataWriter writer, BeatmapObject obj)
    {
        writer.Put((byte)obj.BeatmapType);
        writer.Put(obj);
    }

    private static Dictionary<string, Func<object>> compiledActionCtors = new Dictionary<string, Func<object>>();

    public static BeatmapAction GetBeatmapAction(this NetDataReader reader)
    {
        var actionTypeName = reader.GetString();
        var actionGuid = reader.GetString();

        if (!compiledActionCtors.TryGetValue(actionTypeName, out var compiledCtor))
        {
            var actionType = Type.GetType(actionTypeName);

            if (actionType != null)
            {
                var ctor = actionType.GetConstructor(Type.EmptyTypes);

                var createMethod = new DynamicMethod($"Create{actionTypeName}", actionType, Type.EmptyTypes);

                var il = createMethod.GetILGenerator();
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Ret);

                compiledCtor = createMethod.CreateDelegate(typeof(Func<object>)) as Func<object>;

                compiledActionCtors.Add(actionTypeName, compiledCtor);
            }
        }

        var beatmapAction = compiledCtor() as BeatmapAction;
        beatmapAction.Guid = Guid.Parse(actionGuid);
        beatmapAction.Comment = reader.GetString();
        beatmapAction.Deserialize(reader);

        return beatmapAction;
    }

    public static BeatmapAction GetBeatmapAction(this NetDataReader reader, MapperIdentityPacket identity)
    {
        var action = GetBeatmapAction(reader);
        action.Comment = $"[{identity.Name}] {action.Comment}";
        return action;
    }

    public static void PutBeatmapAction(this NetDataWriter writer, BeatmapAction action)
    {
        writer.Put(action.GetType().Name);
        writer.Put(action.Guid.ToString());
        writer.Put(action.Comment);
        writer.Put(action);
    }
}
