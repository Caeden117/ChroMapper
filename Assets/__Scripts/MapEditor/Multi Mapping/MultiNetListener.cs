using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using TMPro;
using UnityEngine;

public class MultiNetListener : INetEventListener
{
    protected NetManager NetManager;

    protected List<MapperIdentityPacket> Identities = new List<MapperIdentityPacket>();

    private Dictionary<MapperIdentityPacket, GameObject> remotePlayers = new Dictionary<MapperIdentityPacket, GameObject>();

    private Transform cameraTransform;
    private AudioTimeSyncController audioTimeSyncController;

    public MultiNetListener()
    {
        NetManager = new NetManager(this);
    }

    public virtual void OnConnectionRequest(ConnectionRequest request) { }

    // TODO: Gracefully handle a network error
    public virtual void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
    
    // TODO: Maybe ping list in the future?
    public virtual void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    public virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        var mapperId = reader.GetInt();
        var identity = Identities.Find(x => x.ConnectionId == mapperId);

        OnPacketReceived(peer, identity, reader);
    }

    public virtual void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => throw new System.NotImplementedException();

    public virtual void OnPeerConnected(NetPeer peer) { }

    public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { }

    public virtual void OnPacketReceived(NetPeer peer, MapperIdentityPacket identity, NetDataReader reader)
    {
        var packetId = reader.GetByte();

        // Handle packets
        switch (packetId)
        {
            case (byte)Packets.MapperIdentity:
                OnMapperIdentity(peer, reader.Get<MapperIdentityPacket>());
                break;

            case (byte)Packets.MapperPose:
                OnMapperPose(identity, peer, reader.Get<MapperPosePacket>());
                break;

            case (byte)Packets.SendZip:
                OnZipData(peer, reader.Get<MapDataPacket>());
                break;

            case (byte)Packets.BeatmapObjectCreate:
                var creationBeatmapObjectType = (BeatmapObject.ObjectType)reader.GetByte();
                var creationCollection = BeatmapObjectContainerCollection.GetCollectionForType(creationBeatmapObjectType);

                // Yes switch in switch, i know
                var creationObject = creationBeatmapObjectType switch
                {
                    BeatmapObject.ObjectType.Note => reader.Get<BeatmapNote>() as BeatmapObject,
                    BeatmapObject.ObjectType.Event => reader.Get<MapEvent>(),
                    BeatmapObject.ObjectType.Obstacle => reader.Get<BeatmapObstacle>(),
                    BeatmapObject.ObjectType.CustomNote => throw new System.NotImplementedException(), // Custom notes not supported
                    BeatmapObject.ObjectType.CustomEvent => reader.Get<BeatmapCustomEvent>(),
                    BeatmapObject.ObjectType.BpmChange => reader.Get<BeatmapBPMChange>(),
                    _ => throw new InvalidPacketException("Attempting to parse an invalid object type"),
                };

                creationCollection.SpawnObject(creationObject, out _, true, true, false);
                break;

            case (byte)Packets.BeatmapObjectDelete:
                var deletionBeatmapObjectType = (BeatmapObject.ObjectType)reader.GetByte();
                var deletionCollection = BeatmapObjectContainerCollection.GetCollectionForType(deletionBeatmapObjectType);

                var deletionObject = deletionBeatmapObjectType switch
                {
                    BeatmapObject.ObjectType.Note => reader.Get<BeatmapNote>() as BeatmapObject,
                    BeatmapObject.ObjectType.Event => reader.Get<MapEvent>(),
                    BeatmapObject.ObjectType.Obstacle => reader.Get<BeatmapObstacle>(),
                    BeatmapObject.ObjectType.CustomNote => throw new System.NotImplementedException(), // Custom notes not supported
                    BeatmapObject.ObjectType.CustomEvent => reader.Get<BeatmapCustomEvent>(),
                    BeatmapObject.ObjectType.BpmChange => reader.Get<BeatmapBPMChange>(),
                    _ => throw new InvalidPacketException("Attempting to parse an invalid object type"),
                };

                // We abuse the conflict check system to remotely delete an object without having the exact instance.
                deletionCollection.SpawnObject(deletionObject, out _, true, true, false);
                deletionCollection.DeleteObject(deletionObject, false, true, null, false);
                break;
        }
    }

    public virtual void OnZipData(NetPeer peer, MapDataPacket mapData) => throw new System.NotImplementedException();

    public virtual void OnMapperIdentity(NetPeer peer, MapperIdentityPacket identity) => Identities.Add(identity);

    public virtual void OnMapperPose(MapperIdentityPacket identity, NetPeer peer, MapperPosePacket pose)
    {
        if (identity is null) return;

        if (!remotePlayers.TryGetValue(identity, out var remotePlayer) || remotePlayer == null)
        {
            if (remotePlayers.ContainsKey(identity))
            {
                remotePlayers.Remove(identity);
            }

            remotePlayer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            remotePlayers.Add(identity, remotePlayer);
        }

        remotePlayer.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
    }

    public void SendPacketFrom(MapperIdentityPacket fromPeer, NetPeer toPeer, Packets packetId, INetSerializable serializable)
    {
        var writer = new NetDataWriter();

        writer.Put(fromPeer.ConnectionId);
        writer.Put((byte)packetId);
        writer.Put(serializable);

        toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void SendPacketFrom(MapperIdentityPacket fromPeer, NetPeer toPeer, byte[] rawPacketData)
    {
        var writer = new NetDataWriter();

        writer.Put(fromPeer.ConnectionId);
        writer.Put(rawPacketData);

        toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void SendPacketTo(NetPeer toPeer, Packets packetId, INetSerializable serializable)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)packetId);
        writer.Put(serializable);

        toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void SendPacketTo(NetPeer toPeer, Packets packetId, byte[] data)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)packetId);
        writer.Put(data);

        toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void ManualUpdate()
    {
        NetManager?.PollEvents();

        if (audioTimeSyncController != null && cameraTransform != null)
        {
            var poseWriter = new NetDataWriter();

            poseWriter.Put(0);
            poseWriter.Put((byte)Packets.MapperPose);
            poseWriter.Put(new MapperPosePacket()
            {
                Position = cameraTransform.position,
                Rotation = cameraTransform.rotation,
                SongPosition = audioTimeSyncController.CurrentBeat
            });

            NetManager.SendToAll(poseWriter, DeliveryMethod.ReliableOrdered);
        }
    }

    public void SubscribeToCollectionEvents()
    {
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).ObjectSpawnedEvent += MultiNetListener_ObjectSpawnedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle).ObjectSpawnedEvent += MultiNetListener_ObjectSpawnedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).ObjectSpawnedEvent += MultiNetListener_ObjectSpawnedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.CustomEvent).ObjectSpawnedEvent += MultiNetListener_ObjectSpawnedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.BpmChange).ObjectSpawnedEvent += MultiNetListener_ObjectSpawnedEvent;

        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).ObjectDeletedEvent += MultiNetListener_ObjectDeletedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle).ObjectDeletedEvent += MultiNetListener_ObjectDeletedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).ObjectDeletedEvent += MultiNetListener_ObjectDeletedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.CustomEvent).ObjectDeletedEvent += MultiNetListener_ObjectDeletedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.BpmChange).ObjectDeletedEvent += MultiNetListener_ObjectDeletedEvent;

        audioTimeSyncController = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).AudioTimeSyncController;
        cameraTransform = Camera.main.transform;
    }

    public void UnsubscribeFromCollectionEvents()
    {
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).ObjectSpawnedEvent -= MultiNetListener_ObjectSpawnedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle).ObjectSpawnedEvent -= MultiNetListener_ObjectSpawnedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).ObjectSpawnedEvent -= MultiNetListener_ObjectSpawnedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.CustomEvent).ObjectSpawnedEvent -= MultiNetListener_ObjectSpawnedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.BpmChange).ObjectSpawnedEvent -= MultiNetListener_ObjectSpawnedEvent;

        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).ObjectDeletedEvent -= MultiNetListener_ObjectDeletedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle).ObjectDeletedEvent -= MultiNetListener_ObjectDeletedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).ObjectDeletedEvent -= MultiNetListener_ObjectDeletedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.CustomEvent).ObjectDeletedEvent -= MultiNetListener_ObjectDeletedEvent;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.BpmChange).ObjectDeletedEvent -= MultiNetListener_ObjectDeletedEvent;
    }

    private void MultiNetListener_ObjectSpawnedEvent(BeatmapObject obj)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)Packets.BeatmapObjectCreate);
        writer.Put((byte)obj.BeatmapType);
        writer.Put(obj);

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    private void MultiNetListener_ObjectDeletedEvent(BeatmapObject obj)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)Packets.BeatmapObjectDelete);
        writer.Put((byte)obj.BeatmapType);
        writer.Put(obj);

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }
}
