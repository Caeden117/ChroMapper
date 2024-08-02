using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using LiteNetLib;
using LiteNetLib.Utils;
using Beatmap.Base;
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

        if (Settings.Instance.MapVersion == 3)
        {
            BaseObject obj = beatmapObjectType switch
            {
                ObjectType.Note => reader.Get<V2Note>(),
                ObjectType.Event => reader.Get<V2Event>(),
                ObjectType.Obstacle => reader.Get<V3Obstacle>(),
                ObjectType.CustomNote => throw new System.NotImplementedException(), // Custom notes not supported
                ObjectType.CustomEvent => reader.Get<V3CustomEvent>(),
                ObjectType.BpmChange => reader.Get<V3BpmEvent>(),
                ObjectType.Arc => reader.Get<V3Arc>(),
                ObjectType.Chain => reader.Get<V3Chain>(),
                ObjectType.Bookmark => reader.Get<V3Bookmark>(),
                _ => throw new InvalidPacketException("Attempting to parse an invalid object type"),
            };

            if (obj is V2Note note)
            {
                return note.Type switch
                {
                    (int)NoteType.Red => new V3ColorNote(note),
                    (int)NoteType.Blue => new V3ColorNote(note),
                    (int)NoteType.Bomb => new V3BombNote(note),
                    _ => throw new InvalidPacketException("Attempting to parse an invalid note"),
                };
            }

            if (obj is V2Event evt)
            {
                return evt.Type switch
                {
                    (int)EventTypeValue.BpmChange => new V3BpmEvent(evt),
                    (int)EventTypeValue.ColorBoost => new V3ColorBoostEvent(evt),
                    _ => new V3BasicEvent(evt),
                };
            }

            return obj;
        }
        else
        {
            return beatmapObjectType switch
            {
                ObjectType.Note => reader.Get<V2Note>(),
                ObjectType.Event => reader.Get<V2Event>(),
                ObjectType.Obstacle => reader.Get<V2Obstacle>(),
                ObjectType.CustomNote => throw new System.NotImplementedException(), // Custom notes not supported
                ObjectType.CustomEvent => reader.Get<V2CustomEvent>(),
                ObjectType.BpmChange => reader.Get<V2BpmEvent>(),
                ObjectType.Arc => reader.Get<V2Arc>(),
                ObjectType.Chain => throw new InvalidPacketException("Attempting to parse chains in v2"),
                ObjectType.Bookmark => reader.Get<V2Bookmark>(),
                _ => throw new InvalidPacketException("Attempting to parse an invalid object type"),
            };
        }
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
