using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using TMPro;
using UnityEngine;

public class MultiNetListener : INetEventListener, IDisposable
{
    protected NetManager NetManager;

    protected List<MapperIdentityPacket> Identities = new List<MapperIdentityPacket>();

    protected Dictionary<MapperIdentityPacket, RemotePlayerContainer> RemotePlayers = new Dictionary<MapperIdentityPacket, RemotePlayerContainer>();

    protected Dictionary<MapperIdentityPacket, MapperPosePacket> CachedPosePackets = new Dictionary<MapperIdentityPacket, MapperPosePacket>();

    private CameraController cameraController;
    private AudioTimeSyncController audioTimeSyncController;
    private TracksManager tracksManager;
    private BookmarkManager bookmarkManager;
    private RemotePlayerContainer remotePlayerPrefab;
    private float previousCursorBeat = 0;
    private List<BeatmapObjectContainerCollection> containerCollections = new List<BeatmapObjectContainerCollection>();

    public MultiNetListener()
    {
        NetManager = new NetManager(this);
        remotePlayerPrefab = Resources.Load<RemotePlayerContainer>("Remote Player");
    }

    public virtual void Dispose() => NetManager.Stop();

    public virtual void OnConnectionRequest(ConnectionRequest request) { }

    public virtual void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        PersistentUI.Instance.ShowDialogBox($"A networking error occured: {socketError}.", null, PersistentUI.DialogBoxPresetType.Ok);
    }
    
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
                var pose = reader.Get<MapperPosePacket>();
                CachedPosePackets[identity] = pose;
                OnMapperPose(identity, peer, pose);
                break;

            case (byte)Packets.MapperDisconnect:
                if (identity != null)
                {
                    Identities.Remove(identity);
                    
                    if (RemotePlayers.TryGetValue(identity, out var disconnectedPlayer))
                    {
                        UnityEngine.Object.Destroy(disconnectedPlayer.gameObject);
                        RemotePlayers.Remove(identity);
                    }
                }
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
                    BeatmapObject.ObjectType.Bookmark => reader.Get<BeatmapBookmark>(),
                    _ => throw new InvalidPacketException("Attempting to parse an invalid object type"),
                };

                if (creationObject is BeatmapBookmark creationBookmark)
                {
                    bookmarkManager.AddBookmark(creationBookmark, false);
                }
                else
                {
                    creationCollection.SpawnObject(creationObject, out _, true, true, false);
                }
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
                    BeatmapObject.ObjectType.Bookmark => reader.Get<BeatmapBookmark>(),
                    _ => throw new InvalidPacketException("Attempting to parse an invalid object type"),
                };

                if (deletionObject is BeatmapBookmark deletionBookmark)
                {
                    bookmarkManager.DeleteBookmarkAtTime(deletionBookmark.Time, false);
                }
                else
                {
                    // We abuse the conflict check system to remotely delete an object without having the exact instance.
                    deletionCollection.SpawnObject(deletionObject, out _, true, true, false);
                    deletionCollection.DeleteObject(deletionObject, false, true, null, false);
                }
                break;
        }
    }

    public virtual void OnZipData(NetPeer peer, MapDataPacket mapData) => throw new System.NotImplementedException();

    public virtual void OnMapperIdentity(NetPeer peer, MapperIdentityPacket identity) => Identities.Add(identity);

    public virtual void OnMapperPose(MapperIdentityPacket identity, NetPeer peer, MapperPosePacket pose)
    {
        if (identity is null || tracksManager == null) return;

        if (!RemotePlayers.TryGetValue(identity, out var remotePlayer) || remotePlayer == null)
        {
            if (RemotePlayers.ContainsKey(identity))
            {
                RemotePlayers.Remove(identity);
            }

            remotePlayer = UnityEngine.Object.Instantiate(remotePlayerPrefab);
            RemotePlayers.Add(identity, remotePlayer);

            var container = remotePlayer.GetComponent<RemotePlayerContainer>();
            container.AssignIdentity(identity);
        }

        var track = tracksManager.GetTrackAtTime(pose.SongPosition).ObjectParentTransform;
        
        if (!remotePlayer.transform.IsChildOf(track))
        {
            remotePlayer.transform.SetParent(track, true);
        }

        remotePlayer.transform.localPosition = EditorScaleController.EditorScale * pose.SongPosition * Vector3.forward;
        remotePlayer.CameraTransform.localPosition = pose.Position;
        remotePlayer.CameraTransform.localRotation = pose.Rotation;
        remotePlayer.GridTransform.localEulerAngles = track.localEulerAngles;
    }

    public void UpdateCachedPoses()
    {
        foreach (var kvp in CachedPosePackets)
        {
            OnMapperPose(kvp.Key, null, kvp.Value);
        }
    }

    public void SendPacketFrom(MapperIdentityPacket fromPeer, NetPeer toPeer, Packets packetId, INetSerializable serializable)
    {
        var writer = new NetDataWriter();

        writer.Put(fromPeer.ConnectionId);
        writer.Put((byte)packetId);
        writer.Put(serializable);

        toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void SendPacketFrom(MapperIdentityPacket fromPeer, NetPeer toPeer, Packets packetId)
    {
        var writer = new NetDataWriter();

        writer.Put(fromPeer.ConnectionId);
        writer.Put((byte)packetId);

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

        if (audioTimeSyncController != null && cameraController != null
            && (cameraController.MovingCamera || audioTimeSyncController.CurrentBeat != previousCursorBeat))
        {
            previousCursorBeat = audioTimeSyncController.CurrentBeat;

            BroadcastPose();
        }
    }

    public void BroadcastPose(NetPeer? targetPeer = null)
    {
        var poseWriter = new NetDataWriter();

        poseWriter.Put(0);
        poseWriter.Put((byte)Packets.MapperPose);
        poseWriter.Put(new MapperPosePacket()
        {
            Position = cameraController.transform.position,
            Rotation = cameraController.transform.rotation,
            SongPosition = previousCursorBeat
        });

        if (targetPeer == null)
        {
            NetManager.SendToAll(poseWriter, DeliveryMethod.ReliableOrdered);
        }
        else
        {
            targetPeer.Send(poseWriter, DeliveryMethod.ReliableOrdered);
        }
    }

    public void SubscribeToCollectionEvents()
    {
        containerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note));
        containerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle));
        containerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event));
        containerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.CustomEvent));
        containerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.BpmChange));

        foreach (var collection in containerCollections)
        {
            collection.ObjectSpawnedEvent += MultiNetListener_ObjectSpawnedEvent;
            collection.ObjectDeletedEvent += MultiNetListener_ObjectDeletedEvent;
        }

        audioTimeSyncController = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).AudioTimeSyncController;
        cameraController = Camera.main.GetComponent<CameraController>();
        tracksManager = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).GetComponent<TracksManager>();

        // sigh
        bookmarkManager = UnityEngine.Object.FindObjectOfType<BookmarkManager>();
        bookmarkManager.BookmarkAdded += MultiNetListener_ObjectSpawnedEvent;
        bookmarkManager.BookmarkDeleted += MultiNetListener_ObjectDeletedEvent;

        EditorScaleController.EditorScaleChangedEvent += OnEditorScaleChanged;
    }

    public void UnsubscribeFromCollectionEvents()
    {
        foreach (var collection in containerCollections)
        {
            collection.ObjectSpawnedEvent -= MultiNetListener_ObjectSpawnedEvent;
            collection.ObjectDeletedEvent -= MultiNetListener_ObjectDeletedEvent;
        }

        containerCollections.Clear();

        bookmarkManager.BookmarkAdded -= MultiNetListener_ObjectSpawnedEvent;
        bookmarkManager.BookmarkDeleted -= MultiNetListener_ObjectDeletedEvent;

        EditorScaleController.EditorScaleChangedEvent -= OnEditorScaleChanged;
    }

    private void OnEditorScaleChanged(float editorScale) => UpdateCachedPoses();

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
