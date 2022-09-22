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
    protected internal NetManager NetManager;

    protected List<MapperIdentityPacket> Identities = new List<MapperIdentityPacket>();

    protected Dictionary<MapperIdentityPacket, RemotePlayerContainer> RemotePlayers = new Dictionary<MapperIdentityPacket, RemotePlayerContainer>();

    protected Dictionary<MapperIdentityPacket, MapperPosePacket> CachedPosePackets = new Dictionary<MapperIdentityPacket, MapperPosePacket>();

    private CameraController cameraController;
    private AudioTimeSyncController audioTimeSyncController;
    private TracksManager tracksManager;
    private BookmarkManager bookmarkManager;
    private RemotePlayerContainer remotePlayerPrefab;
    private float previousCursorBeat = 0;
    private float localSongSpeed = 1;

    public MultiNetListener()
    {
        NetManager = new NetManager(this);
        remotePlayerPrefab = Resources.Load<RemotePlayerContainer>("Remote Player");
    }

    public virtual void Dispose()
    {
        var disconnectPacketWriter = new NetDataWriter();
        disconnectPacketWriter.Put(0);
        disconnectPacketWriter.Put((byte)Packets.MapperDisconnect);
        NetManager.SendToAll(disconnectPacketWriter, DeliveryMethod.ReliableOrdered);

        NetManager.Stop();
    }

    public virtual void OnConnectionRequest(ConnectionRequest request) { }

    public virtual void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        PersistentUI.Instance.ShowDialogBox("MultiMapping", "multi.connection.network-error", null,
            PersistentUI.DialogBoxPresetType.Ok, new object[] { socketError });
    }

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

                // We apply cached song position to an incoming pose packet if the mapper is playing through the song.
                //    This eliminates jittering when clients move the camera while playing the song.
                if (pose.IsPlayingSong && CachedPosePackets.TryGetValue(identity, out var remotePose))
                {
                    pose.SongPosition = remotePose.SongPosition;
                }

                CachedPosePackets[identity] = pose;

                OnMapperPose(identity, peer, pose);
                break;

            case (byte)Packets.MapperLatency:
                var latencyPacket = reader.Get<MapperLatencyPacket>();

                if (RemotePlayers.TryGetValue(identity, out var remotePlayer))
                {
                    remotePlayer.UpdateLatency(latencyPacket.Latency);
                }
                break;

            case (byte)Packets.MapperDisconnect:
                OnMapperDisconnected(identity);
                break;

            case (byte)Packets.SendZip:
                OnZipData(peer, reader.Get<MapDataPacket>());
                break;

            case (byte)Packets.BeatmapObjectCreate:
                var creationObject = reader.GetBeatmapObject();

                if (creationObject is BeatmapBookmark creationBookmark)
                {
                    bookmarkManager.AddBookmark(creationBookmark, false);
                }
                break;

            case (byte)Packets.BeatmapObjectDelete:
                var deletionObject = reader.GetBeatmapObject();

                if (deletionObject is BeatmapBookmark deletionBookmark)
                {
                    bookmarkManager.DeleteBookmarkAtTime(deletionBookmark.Time, false);
                }
                break;
            case (byte)Packets.ActionCreated:
                var action = reader.GetBeatmapAction(identity);
                BeatmapActionContainer.AddAction(action, true);
                break;
            case (byte)Packets.ActionUndo:
                var undoGuid = Guid.Parse(reader.GetString());
                BeatmapActionContainer.Undo(undoGuid);
                break;
            case (byte)Packets.ActionRedo:
                var redoGuid = Guid.Parse(reader.GetString());
                BeatmapActionContainer.Redo(redoGuid);
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

    public void OnMapperDisconnected(MapperIdentityPacket identity)
    {
        if (identity != null)
        {
            // Null out the mapper peer; we can't remove Identity entirely, or
            //    else some people can share the same connection id
            identity.MapperPeer = null;

            if (RemotePlayers.TryGetValue(identity, out var disconnectedPlayer))
            {
                UnityEngine.Object.Destroy(disconnectedPlayer.gameObject);
                RemotePlayers.Remove(identity);
            }

            CachedPosePackets.Remove(identity);
        }
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

        if (audioTimeSyncController != null && cameraController != null &&
            (cameraController.MovingCamera || (!audioTimeSyncController.IsPlaying && audioTimeSyncController.CurrentBeat != previousCursorBeat)))
        {
            previousCursorBeat = audioTimeSyncController.CurrentBeat;

            BroadcastPose();
        }

        foreach (var kvp in CachedPosePackets)
        {
            var mapper = kvp.Key;
            var pose = kvp.Value;

            // If a client is playing through the song, we calculate their new song position locally.
            // This eliminates sending Pose packets every frame, while also smoothly moving clients.
            // This *will* desync over time due to floating point precision, but song position will
            //    be corrected when playback stops.
            if (pose.IsPlayingSong)
            {
                pose.SongPosition += BeatSaberSongContainer.Instance.Song.BeatsPerMinute / 60 * Time.deltaTime * pose.PlayingSongSpeed;

                OnMapperPose(mapper, null, pose);
            }
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
            SongPosition = audioTimeSyncController.CurrentBeat,
            IsPlayingSong = audioTimeSyncController.IsPlaying,
            PlayingSongSpeed = localSongSpeed
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
        audioTimeSyncController = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).AudioTimeSyncController;
        audioTimeSyncController.PlayToggle += OnTogglePlaying;
        cameraController = Camera.main.GetComponent<CameraController>();
        tracksManager = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).GetComponent<TracksManager>();

        // sigh
        bookmarkManager = UnityEngine.Object.FindObjectOfType<BookmarkManager>();
        bookmarkManager.BookmarkAdded += MultiNetListener_ObjectSpawnedEvent;
        bookmarkManager.BookmarkDeleted += MultiNetListener_ObjectDeletedEvent;

        Settings.NotifyBySettingName("SongSpeed", UpdateLocalSongSpeed);

        EditorScaleController.EditorScaleChangedEvent += OnEditorScaleChanged;

        BeatmapActionContainer.ActionCreatedEvent += BeatmapActionContainer_ActionCreatedEvent;
        BeatmapActionContainer.ActionUndoEvent += BeatmapActionContainer_ActionUndoEvent;
        BeatmapActionContainer.ActionRedoEvent += BeatmapActionContainer_ActionRedoEvent;
    }

    public void UnsubscribeFromCollectionEvents()
    {
        audioTimeSyncController.PlayToggle -= OnTogglePlaying;

        bookmarkManager.BookmarkAdded -= MultiNetListener_ObjectSpawnedEvent;
        bookmarkManager.BookmarkDeleted -= MultiNetListener_ObjectDeletedEvent;

        Settings.ClearSettingNotifications("SongSpeed");

        EditorScaleController.EditorScaleChangedEvent -= OnEditorScaleChanged;

        BeatmapActionContainer.ActionCreatedEvent -= BeatmapActionContainer_ActionCreatedEvent;
        BeatmapActionContainer.ActionUndoEvent -= BeatmapActionContainer_ActionUndoEvent;
        BeatmapActionContainer.ActionRedoEvent -= BeatmapActionContainer_ActionRedoEvent;
    }

    private void OnEditorScaleChanged(float editorScale) => UpdateCachedPoses();

    private void OnTogglePlaying(bool isPlaying) => BroadcastPose();

    private void UpdateLocalSongSpeed(object obj)
    {
        localSongSpeed = (float)obj / 10f;

        BroadcastPose();
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

    private void BeatmapActionContainer_ActionCreatedEvent(BeatmapAction obj)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)Packets.ActionCreated);
        writer.PutBeatmapAction(obj);

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    private void BeatmapActionContainer_ActionUndoEvent(BeatmapAction obj)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)Packets.ActionUndo);
        writer.Put(obj.Guid.ToString());

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    private void BeatmapActionContainer_ActionRedoEvent(BeatmapAction obj)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)Packets.ActionRedo);
        writer.Put(obj.Guid.ToString());

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }
}
