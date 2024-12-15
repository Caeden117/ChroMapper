using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using LiteNetLib;
using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V2.Customs;
using Beatmap.V3;
using Beatmap.V3.Customs;

public static class NetDataExtensions
{
    public static BaseObject GetBeatmapObject(this NetDataReader reader)
    {
        var beatmapObjectType = (ObjectType)reader.GetByte();
        
        BaseObject obj = beatmapObjectType switch
        {
            ObjectType.Note => reader.Get<BaseNote>(),
            ObjectType.Event => reader.Get<BaseEvent>(),
            ObjectType.Obstacle => reader.Get<BaseObstacle>(),
            ObjectType.CustomNote => throw new System.NotImplementedException(), // Custom notes not supported
            ObjectType.CustomEvent => reader.Get<BaseCustomEvent>(),
            ObjectType.BpmChange => reader.Get<BaseBpmEvent>(),
            ObjectType.Arc => reader.Get<BaseArc>(),
            ObjectType.Chain => reader.Get<BaseChain>(),
            ObjectType.Bookmark => reader.Get<BaseBookmark>(),
            ObjectType.Waypoint => reader.Get<BaseWaypoint>(),
            _ => throw new InvalidPacketException("Attempting to parse an invalid object type"),
        };

        return obj;
    }

    public static void PutBeatmapObject(this NetDataWriter writer, BaseObject obj)
    {
        writer.Put((byte)obj.ObjectType);
        writer.Put(obj);
    }

    private static Dictionary<string, Func<object>> compiledActionCtors = new Dictionary<string, Func<object>>();

    public static BeatmapAction GetBeatmapAction(this NetDataReader reader, MapperIdentityPacket identity)
    {
        var actionTypeName = reader.GetString();
        var actionGuid = reader.GetString();

        if (!compiledActionCtors.TryGetValue(actionTypeName, out var compiledCtor))
        {
            var actionType = Type.GetType(actionTypeName);

            if (actionType != null && typeof(BeatmapAction).IsAssignableFrom(actionType))
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
        beatmapAction.Identity = identity;
        beatmapAction.Guid = Guid.Parse(actionGuid);
        beatmapAction.Comment = $"[{identity.Name}] {reader.GetString()}";
        beatmapAction.Deserialize(reader);
        return beatmapAction;
    }

    public static void PutBeatmapAction(this NetDataWriter writer, BeatmapAction action)
    {
        writer.Put(action.GetType().Name);
        writer.Put(action.Guid.ToString());
        writer.Put(action.Comment);
        writer.Put(action);
    }
}
