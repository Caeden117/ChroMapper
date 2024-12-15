using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class MultiServerNetListener : MultiNetListener, INetAdmin
{
    private AutoSaveController autoSave;
    private List<string> tempBannedIps = new List<string>();

    public MultiServerNetListener(MapperIdentityPacket hostIdentity, int port, AutoSaveController autoSave) : base()
    {
        this.autoSave = autoSave;

        NetManager.Start(port);

        Identities.Add(hostIdentity);

        SubscribeToCollectionEvents();
    }

    public override void Dispose()
    {
        UnsubscribeFromCollectionEvents();

        base.Dispose();
    }

    public override void OnConnectionRequest(ConnectionRequest request)
    {
        if (tempBannedIps.Contains(request.RemoteEndPoint.Address.MapToIPv4().ToString()))
        {
            var writer = new NetDataWriter();
            writer.Put("You have been banned by the host.");
            request.Reject(writer);
            return;
        }

        var identity = request.Data.Get<MapperIdentityPacket>();

        var requestVersion = identity.ApplicationVersion;
        var hostVersion = Application.version;
        Debug.Log($"Request Version {requestVersion} | Host version {hostVersion}");

        if (requestVersion != hostVersion)
        {
            var writer = new NetDataWriter();
            writer.Put($"You are not using the same CM version as host ({hostVersion}).");
            request.Reject(writer);
            return;
        }

        identity.ConnectionId = Identities.Count;

        var peer = request.Accept();
        identity.MapperPeer = peer;

        Identities.Add(identity);

        // Send peer identities to new user, and new user identity to other peers
        foreach (var mapper in Identities)
        {
            if (mapper.MapperPeer != null)
            {
                SendPacketFrom(identity, mapper.MapperPeer, PacketId.MapperIdentity, identity);
            }

            SendPacketFrom(mapper, peer, PacketId.MapperIdentity, mapper);

            if (CachedPosePackets.TryGetValue(mapper, out var lastKnownPose))
            {
                SendPacketFrom(mapper, peer, PacketId.MapperPose, lastKnownPose);
            }
        }

        // Provide host pose to new user
        BroadcastPose(peer);

        // This is absolutely NOT a good way to go about this, but I can't think of anything else!
        PersistentUI.Instance.StartCoroutine(SaveAndSendMapToPeer(this, autoSave, peer));
    }

    public override void OnPacketReceived(NetPeer peer, MapperIdentityPacket identity, NetDataReader reader)
    {
        // We need to correct identity with the connection ID from the peer
        identity = Identities.Find(x => x.MapperPeer == peer);

        var packetBytes = new byte[reader.AvailableBytes];
        Array.Copy(reader.RawData, reader.Position, packetBytes, 0, reader.AvailableBytes);

        // Resend packet to all other mappers
        foreach (var mapper in Identities)
        {
            if (mapper.MapperPeer != null && mapper.MapperPeer != peer)
            {
                SendPacketFrom(identity, mapper.MapperPeer, packetBytes);
            }
        }

        base.OnPacketReceived(peer, identity, reader);
    }

    // For the server, we broadcast the clients latency to everyone. Just something fun.
    public override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        var identity = Identities.Find(x => x.MapperPeer == peer);

        if (identity == null) return;

        // Update client latency for the host
        if (RemotePlayers.TryGetValue(identity, out var remotePeer))
        {
            remotePeer.UpdateLatency(latency);
        }

        // Rebroadcast latency to everyone else
        foreach (var mapper in Identities)
        {
            if (mapper.MapperPeer != null && mapper.MapperPeer != peer)
            {
                SendPacketFrom(identity, mapper.MapperPeer, PacketId.MapperLatency, new MapperLatencyPacket()
                {
                    Latency = latency
                });
            }
        }
    }

    // For the host, a peer disconnecting is one of the clients, so we broadcast to everyone else that the client is gone.
    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        var identity = Identities.Find(x => x.MapperPeer == peer);

        if (identity == null) return;

        // Send disconnect packet to everyone
        foreach (var mapper in Identities)
        {
            if (mapper.MapperPeer != null && mapper.MapperPeer != peer)
            {
                SendPacketFrom(identity, mapper.MapperPeer, PacketId.MapperDisconnect);
            }
        }

        OnMapperDisconnected(this, identity, null);
    }

    public void Kick(MapperIdentityPacket identity)
        => PersistentUI.Instance.ShowDialogBox("MultiMapping", "multi.kick",
            res => HandleKick(res, identity), PersistentUI.DialogBoxPresetType.YesNo, new[] { identity.Name });

    public void Ban(MapperIdentityPacket identity)
        => PersistentUI.Instance.ShowDialogBox("MultiMapping", "multi.ban",
            res => HandleBan(res, identity), PersistentUI.DialogBoxPresetType.YesNo, new[] { identity.Name });

    private void HandleKick(int res, MapperIdentityPacket identity)
    {
        if (res == 0 && identity.MapperPeer != null)
        {
            var writer = new NetDataWriter();
            writer.Put("You have been kicked by the host.");
            identity.MapperPeer.Disconnect(writer);
        }
    }

    private void HandleBan(int res, MapperIdentityPacket identity)
    {
        if (res == 0 && identity.MapperPeer != null)
        {
            tempBannedIps.Add(identity.MapperPeer.EndPoint.Address.MapToIPv4().ToString());
            var writer = new NetDataWriter();
            writer.Put("You have been banned by the host.");
            identity.MapperPeer.Disconnect(writer);
        }
    }

    internal static IEnumerator SaveAndSendMapToPeer(MultiNetListener listener, AutoSaveController autoSave, NetPeer peer)
    {
        // Save the map
        autoSave.Save(false);

        // Wait until saving operation is completed
        yield return new WaitWhile(() => !autoSave.IsSaving);

        // Zip the song in its current state, then send to the player.
        // I'm aware that there's a little bit of time in between saving and zipping where changes made *arent* sent to the client,
        //   but I'm hoping its small enough to not be a big worry.
        var mapInfo = BeatSaberSongContainer.Instance.Info;
        var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;

        var zipPath = Path.Combine(mapInfo.Directory, mapInfo.CleanSongName + ".zip");
        File.Delete(zipPath);

        var exportedFiles = BeatSaberSongExtensions.GetFilesForArchiving(mapInfo);

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (var pathFileEntryPair in exportedFiles)
            {
                archive.CreateEntryFromFile(pathFileEntryPair.Key, pathFileEntryPair.Value);
            }
        }

        var zipBytes = File.ReadAllBytes(zipPath);

        listener.SendPacketTo(peer, PacketId.SendZip, new MapDataPacket(zipBytes, infoDifficulty.Characteristic, infoDifficulty.Difficulty));
    }
}
