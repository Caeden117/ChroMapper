using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class MultiServerNetListener : MultiNetListener
{
    private AutoSaveController autoSave;

    public MultiServerNetListener(string displayName, int port, AutoSaveController autoSave) : base()
    {
        this.autoSave = autoSave;

        NetManager.Start(port);

        Identities.Add(new MapperIdentityPacket(displayName, 0));

        SubscribeToCollectionEvents();
    }

    public override void Dispose()
    {
        UnsubscribeFromCollectionEvents();

        base.Dispose();
    }

    public override void OnConnectionRequest(ConnectionRequest request)
    {
        var identity = request.Data.Get<MapperIdentityPacket>();
        identity.ConnectionId = Identities.Count;

        var peer = request.Accept();
        identity.MapperPeer = peer;

        Identities.Add(identity);

        // Send peer identities to new user, and new user identity to other peers
        foreach (var mapper in Identities)
        {
            if (mapper.MapperPeer != null)
            {
                SendPacketFrom(identity, mapper.MapperPeer, Packets.MapperIdentity, identity);
            }

            SendPacketFrom(mapper, peer, Packets.MapperIdentity, mapper);
        }
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

    // This is absolutely NOT a good way to go about this, but I can't think of anything else!
    public override void OnPeerConnected(NetPeer peer)
        => PersistentUI.Instance.StartCoroutine(SaveAndSendMapToPeer(peer));

    // For the host, a peer disconnecting is one of the clients, so we broadcast to everyone else that the client is gone.
    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        var disconnectedIdentity = Identities.Find(x => x.MapperPeer == peer);

        if (disconnectedIdentity != null)
        {
            Identities.Remove(disconnectedIdentity);

            // Send disconnect packet to everyone
            foreach (var mapper in Identities)
            {
                if (mapper.MapperPeer != null && mapper.MapperPeer != peer)
                {
                    SendPacketFrom(disconnectedIdentity, mapper.MapperPeer, Packets.MapperDisconnect, null);
                }
            }

            if (RemotePlayers.TryGetValue(disconnectedIdentity, out var disconnectedPlayer))
            {
                UnityEngine.Object.Destroy(disconnectedPlayer.gameObject);
                RemotePlayers.Remove(disconnectedIdentity);
            }
        }
    }

    private IEnumerator SaveAndSendMapToPeer(NetPeer peer)
    {
        // Save the map
        autoSave.Save(false);

        // Wait until saving operation is completed
        yield return new WaitWhile(() => autoSave.IsSaving);

        // Zip the song in its current state, then send to the player.
        // I'm aware that there's a little bit of time in between saving and zipping where changes made *arent* sent to the client,
        //   but I'm hoping its small enough to not be a big worry.
        var song = BeatSaberSongContainer.Instance.Song;
        var diff = BeatSaberSongContainer.Instance.DifficultyData;
        var characteristic = BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet;

        var zipPath = Path.Combine(song.Directory, song.CleanSongName + ".zip");
        File.Delete(zipPath);

        var exportedFiles = song.GetFilesForArchiving();

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (var pathFileEntryPair in exportedFiles)
            {
                archive.CreateEntryFromFile(pathFileEntryPair.Key, pathFileEntryPair.Value);
            }
        }

        var zipBytes = File.ReadAllBytes(zipPath);

        SendPacketTo(peer, Packets.SendZip, new MapDataPacket(zipBytes, characteristic.BeatmapCharacteristicName, diff.Difficulty));
    }
}
