using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class MultiNetListener : INetEventListener, IDisposable
{
    public const int PoseUpdateFramerate = 24;

    protected internal NetManager NetManager;

    protected List<MapperIdentityPacket> Identities = new List<MapperIdentityPacket>();

    protected Dictionary<MapperIdentityPacket, RemotePlayerContainer> RemotePlayers = new Dictionary<MapperIdentityPacket, RemotePlayerContainer>();

    protected Dictionary<MapperIdentityPacket, MapperPosePacket> CachedPosePackets = new Dictionary<MapperIdentityPacket, MapperPosePacket>();

    private Dictionary<PacketId, IPacketHandler> registeredPacketHandlers = new Dictionary<PacketId, IPacketHandler>();
    private CameraController cameraController;
    private AudioTimeSyncController audioTimeSyncController;
    private TracksManager tracksManager;
    private BookmarkManager bookmarkManager;
    private CustomColorsUIController customColors;
    private RemotePlayerContainer remotePlayerPrefab;
    private float previousCursorBeat = 0;
    private float localSongSpeed = 1;
    private float lastPoseUpdateTime = 0;

    public MultiNetListener()
    {
        NetManager = new NetManager(this);
        remotePlayerPrefab = Resources.Load<RemotePlayerContainer>("Remote Player");
        RegisterPacketHandler(PacketId.MapperIdentity, OnMapperIdentity);
        RegisterPacketHandler(PacketId.MapperPose, OnMapperPose);
        RegisterPacketHandler(PacketId.MapperDisconnect, OnMapperDisconnected);
        RegisterPacketHandler(PacketId.MapperLatency, OnMapperLatency);
    }

    public virtual void Dispose()
    {
        var disconnectPacketWriter = new NetDataWriter();
        disconnectPacketWriter.Put(0);
        disconnectPacketWriter.Put((byte)PacketId.MapperDisconnect);
        NetManager.SendToAll(disconnectPacketWriter, DeliveryMethod.ReliableOrdered);

        NetManager.Stop();
    }

    public void RegisterPacketHandler<THandler>(PacketId packetId) where THandler : IPacketHandler, new()
        => registeredPacketHandlers[packetId] = new THandler();

    public void RegisterPacketHandler<THandler>(PacketId packetId, THandler instance) where THandler : IPacketHandler
        => registeredPacketHandlers[packetId] = instance;

    public void RegisterPacketHandler(PacketId packetId, Action<MultiNetListener, MapperIdentityPacket, NetDataReader> onHandlePacket)
        => RegisterPacketHandler(packetId, new DelegatePacketHandler(onHandlePacket));

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

    public virtual void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

    public virtual void OnPeerConnected(NetPeer peer) { }

    public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { }

    public virtual void OnPacketReceived(NetPeer peer, MapperIdentityPacket identity, NetDataReader reader)
    {
        var packetId = reader.GetByte();

        if (registeredPacketHandlers.TryGetValue((PacketId)packetId, out var handler))
        {
            handler.HandlePacket(this, identity, reader);
        }
        else
        {
            Debug.LogWarning($"No handler for packet {packetId}");
        }
    }

    public virtual void OnMapperIdentity(MultiNetListener _, MapperIdentityPacket identity, NetDataReader reader)
        => Identities.Add(reader.Get<MapperIdentityPacket>());

    public void OnMapperPose(MultiNetListener _, MapperIdentityPacket identity, NetDataReader reader)
    {
        var pose = reader.Get<MapperPosePacket>();

        // We apply cached song position to an incoming pose packet if the mapper is playing through the song.
        //    This eliminates jittering when clients move the camera while playing the song.
        if (pose.IsPlayingSong && CachedPosePackets.TryGetValue(identity, out var remotePose))
        {
            pose.SongPosition = remotePose.SongPosition;
        }

        CachedPosePackets[identity] = pose;

        UpdateMapperPose(identity, pose);
    }

    public void UpdateMapperPose(MapperIdentityPacket identity, MapperPosePacket pose)
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

    public void OnMapperDisconnected(MultiNetListener _, MapperIdentityPacket identity, NetDataReader __)
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

    public void OnMapperLatency(MultiNetListener _, MapperIdentityPacket identity, NetDataReader reader)
    {
        var latencyPacket = reader.Get<MapperLatencyPacket>();

        if (RemotePlayers.TryGetValue(identity, out var remotePlayer))
        {
            remotePlayer.UpdateLatency(latencyPacket.Latency);
        }
    }

    public void UpdateCachedPoses()
    {
        foreach (var kvp in CachedPosePackets)
        {
            UpdateMapperPose(kvp.Key, kvp.Value);
        }
    }

    public void SendPacketFrom(MapperIdentityPacket fromPeer, NetPeer toPeer, PacketId packetId, INetSerializable serializable)
    {
        var writer = new NetDataWriter();

        writer.Put(fromPeer.ConnectionId);
        writer.Put((byte)packetId);
        writer.Put(serializable);

        toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void SendPacketFrom(MapperIdentityPacket fromPeer, NetPeer toPeer, PacketId packetId)
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

    public void SendPacketTo(NetPeer toPeer, PacketId packetId, INetSerializable serializable)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)packetId);
        writer.Put(serializable);

        toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void SendPacketTo(NetPeer toPeer, PacketId packetId, byte[] data)
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
            (cameraController.MovingCamera || (!audioTimeSyncController.IsPlaying && audioTimeSyncController.CurrentBeat != previousCursorBeat))
            && Time.time - lastPoseUpdateTime >= 1f / PoseUpdateFramerate)
        {
            lastPoseUpdateTime = Time.time;
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

                UpdateMapperPose(mapper, pose);
            }
        }
    }

    public void BroadcastPose(NetPeer? targetPeer = null)
    {
        var poseWriter = new NetDataWriter();

        poseWriter.Put(0);
        poseWriter.Put((byte)PacketId.MapperPose);
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
        RegisterPacketHandler(PacketId.BeatmapObjectCreate, new BookmarkCreatePacketHandler(bookmarkManager));
        RegisterPacketHandler(PacketId.BeatmapObjectDelete, new BookmarkDeletePacketHandler(bookmarkManager));

        // double sigh
        customColors = UnityEngine.Object.FindObjectOfType<CustomColorsUIController>();
        customColors.CustomColorsUpdatedEvent += CustomColors_CustomColorsUpdatedEvent;
        RegisterPacketHandler(PacketId.MapColorUpdated, new MapColorUpdatePacketHandler(customColors));

        Settings.NotifyBySettingName("SongSpeed", UpdateLocalSongSpeed);

        EditorScaleController.EditorScaleChangedEvent += OnEditorScaleChanged;

        BeatmapActionContainer.ActionCreatedEvent += BeatmapActionContainer_ActionCreatedEvent;
        RegisterPacketHandler<ActionCreatedPacketHandler>(PacketId.ActionCreated);
        BeatmapActionContainer.ActionUndoEvent += BeatmapActionContainer_ActionUndoEvent;
        RegisterPacketHandler<ActionUndoPacketHandler>(PacketId.ActionUndo);
        BeatmapActionContainer.ActionRedoEvent += BeatmapActionContainer_ActionRedoEvent;
        RegisterPacketHandler<ActionRedoPacketHandler>(PacketId.ActionRedo);
    }

    public void UnsubscribeFromCollectionEvents()
    {
        audioTimeSyncController.PlayToggle -= OnTogglePlaying;

        bookmarkManager.BookmarkAdded -= MultiNetListener_ObjectSpawnedEvent;
        bookmarkManager.BookmarkDeleted -= MultiNetListener_ObjectDeletedEvent;
        
        customColors.CustomColorsUpdatedEvent -= CustomColors_CustomColorsUpdatedEvent;

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
        writer.Put((byte)PacketId.BeatmapObjectCreate);
        writer.Put((byte)obj.BeatmapType);
        writer.Put(obj);

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    private void MultiNetListener_ObjectDeletedEvent(BeatmapObject obj)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)PacketId.BeatmapObjectDelete);
        writer.Put((byte)obj.BeatmapType);
        writer.Put(obj);

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    private void BeatmapActionContainer_ActionCreatedEvent(BeatmapAction obj)
    {
        obj.Identity = Settings.Instance.MultiSettings.LocalIdentity;

        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)PacketId.ActionCreated);
        writer.PutBeatmapAction(obj);

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    private void BeatmapActionContainer_ActionUndoEvent(BeatmapAction obj)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)PacketId.ActionUndo);
        writer.Put(obj.Guid.ToString());

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    private void BeatmapActionContainer_ActionRedoEvent(BeatmapAction obj)
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)PacketId.ActionRedo);
        writer.Put(obj.Guid.ToString());

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    private void CustomColors_CustomColorsUpdatedEvent()
    {
        var writer = new NetDataWriter();

        writer.Put(0);
        writer.Put((byte)PacketId.MapColorUpdated);
        writer.Put(customColors.CreatePacketFromColors());

        NetManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }
}
