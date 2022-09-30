public enum PacketId
{
    MapperIdentity,
    MapperPose,

    SendZip,

    BeatmapObjectCreate,
    BeatmapObjectDelete,

    MapperDisconnect,
    MapperLatency,

    ActionCreated,
    ActionUndo,
    ActionRedo,

    MapColorUpdated,

    CMT_RequestZip,

    CMT_IncomingMapper,
    CMT_AcceptMapper,
    CMT_KickMapper
}
