using LiteNetLib;

// When using ChroMapTogether, the session host is effectively another client of the ChroMapTogether server.
public class MultiServerRelayModeNetListener : MultiClientNetListener
{
    private AutoSaveController autoSave;

    public MultiServerRelayModeNetListener(string roomCode, MapperIdentityPacket identity, AutoSaveController autoSave)
        : base(roomCode, identity)
    {
        this.autoSave = autoSave;

        SubscribeToCollectionEvents();
    }

    public override void Dispose()
    {
        UnsubscribeFromCollectionEvents();
        base.Dispose();
    }

    public override void OnMapperIdentity(NetPeer peer, MapperIdentityPacket identity)
    {
        base.OnMapperIdentity(peer, identity);

        BroadcastPose(peer);

        // This is absolutely NOT a good way to go about this, but I can't think of anything else!
        PersistentUI.Instance.StartCoroutine(MultiServerNetListener.SaveAndSendMapToPeer(this, autoSave, peer));
    }

    // No longer doing anything since latency is updated completely via MapperLatencyPackets
    public override void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    
    // The ChroMapTogether server lost connection. As the host, don't return to song select.
    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        PersistentUI.Instance.ShowDialogBox("MultiMapping", "multi.connection.server-lost", null,
            PersistentUI.DialogBoxPresetType.Ok, new object[] { disconnectInfo.Reason });
    }
}
