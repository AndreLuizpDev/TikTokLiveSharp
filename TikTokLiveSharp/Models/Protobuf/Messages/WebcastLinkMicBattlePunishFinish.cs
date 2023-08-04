using ProtoBuf;
using TikTokLiveSharp.Models.Protobuf.Messages.Headers;

namespace TikTokLiveSharp.Models.Protobuf.Messages
{
    // Example-data: EKCWh5zih8nmZBiAoLSgnoDznQIgASihltPmre3N5mQyJAihltPmre3N5mQQjPmCjZwxGK0CIKCWh5zih8nmZCgEMAFAAQo/CiBXZWJjYXN0TGlua01pY0JhdHRsZVB1bmlzaEZpbmlzaBChloLU4Z3O5mQYoZaoxsD/weZkINismo2cMSgB
    [ProtoContract]
    public partial class WebcastLinkMicBattlePunishFinish : AProtoBase
    {
        [ProtoMember(1)]
        public MessageHeader Header { get; set; }

        // Winner-Id?
        [ProtoMember(2)]
        public ulong Id1 { get; set; }

        [ProtoMember(3)]
        public ulong Timestamp { get; set; }

        [ProtoMember(4)]
        public uint Data1 { get; set; }

        // Loser-Id?
        [ProtoMember(5)]
        public ulong Id2 { get; set; }

        [ProtoMember(6)]
        public WebcastLinkMicBattlePunishFinishData Data { get; set; }
    }

    [ProtoContract]
    public partial class WebcastLinkMicBattlePunishFinishData : AProtoBase
    {
        // Same as Id2 in outer message
        [ProtoMember(1)]
        public ulong Id2 { get; set; }

        [ProtoMember(2)]
        public ulong Timestamp { get; set; }

        [ProtoMember(3)]
        public uint Data1 { get; set; }

        // Same as Id1 in outer message
        [ProtoMember(4)]
        public ulong Id1 { get; set; }

        [ProtoMember(5)]
        public uint Data2 { get; set; }

        [ProtoMember(6)]
        public uint Data3 { get; set; }

        [ProtoMember(8)]
        public uint Data4 { get; set; }
    }
}
