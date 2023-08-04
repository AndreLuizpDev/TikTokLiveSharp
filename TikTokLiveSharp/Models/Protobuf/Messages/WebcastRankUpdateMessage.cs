using ProtoBuf;
using System.ComponentModel;
using TikTokLiveSharp.Models.Protobuf.Messages.Headers;
using TikTokLiveSharp.Models.Protobuf.Objects;

namespace TikTokLiveSharp.Models.Protobuf.Messages
{
    [ProtoContract]
    public partial class WebcastRankUpdateMessage : AProtoBase
    {
        [ProtoMember(1)]
        public MessageHeader Header { get; set; }

        [ProtoMember(2)]
        public RankUpdateData Data { get; set; }

        [ProtoMember(3)]
        public uint Data1 { get; set; }

        [ProtoMember(5)]
        public uint Data2 { get; set; }
    }


    [ProtoContract]
    public partial class RankUpdateData : AProtoBase
    {
        [ProtoMember(1)]
        public uint Data1 { get; set; }

        [ProtoMember(2)]
        public uint Data2 { get; set; }

        [ProtoMember(3)]
        public Ranking RankData { get; set; }

        [ProtoMember(4)]
        public RankColor Color { get; set; }

        [ProtoMember(6)]
        public ulong Data3 { get; set; }

        [ProtoMember(7)]
        [DefaultValue("")]
        public string Data4 { get; set; } = "";

        [ProtoMember(8)]
        public uint Data5 { get; set; }

        [ProtoMember(9)]
        public uint Data6 { get; set; }

        [ProtoMember(10)]
        public ulong Data7 { get; set; }

        [ProtoMember(11)]
        public uint Data8 { get; set; }
    }

    [ProtoContract]
    public partial class RankColor : AProtoBase
    {
        [ProtoMember(4)]
        [DefaultValue("")]
        public string Color { get; set; } = "";
    }


    [ProtoContract]
    public partial class RankColor2 : AProtoBase
    {
        [ProtoMember(1)]
        public uint Data1 { get; set; }

        [ProtoMember(2)]
        [DefaultValue("")]
        public string Color { get; set; } = "";

        [ProtoMember(4)]
        public uint Data2 { get; set; }
    }
}
