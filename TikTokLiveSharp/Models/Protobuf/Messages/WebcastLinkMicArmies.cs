using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using TikTokLiveSharp.Models.Protobuf.Messages.Headers;
using TikTokLiveSharp.Models.Protobuf.Objects;

namespace TikTokLiveSharp.Models.Protobuf.Messages
{
    [ProtoContract]
    public partial class WebcastLinkMicArmies : AProtoBase
    {
        [ProtoMember(1)]
        public MessageHeader Header { get; set; }

        [ProtoMember(2)]
        public ulong Id { get; set; }

        [ProtoMember(3)]
        public List<LinkMicArmiesItems> BattleItems { get; set; }

        [ProtoMember(4)]
        public ulong Id2 { get; set; }

        [ProtoMember(5)]
        public ulong Timestamp1 { get; set; }

        [ProtoMember(6)]
        public ulong Timestamp2 { get; set; }

        /// <summary>
        /// TODO: SHOULD BE AN ENUM
        /// </summary>
        [ProtoMember(7)]
        public int BattleStatus { get; set; }

        [ProtoMember(8)]
        public ulong Data1 { get; set; }

        [ProtoMember(9)]
        public ulong Data2 { get; set; }

        [ProtoMember(10)]
        public uint Data3 { get; set; }

        [ProtoMember(11)]
        public Picture Picture { get; set; }

        [ProtoMember(12)]
        public uint Data4 { get; set; }

        [ProtoMember(13)]
        public uint Data5 { get; set; }

        /// <summary>
        /// Some form of GUID?
        /// <para>
        /// Example: "202308011954135D1C4534EB099D45E64E"
        /// </para>
        /// </summary>
        [ProtoMember(17)]
        [DefaultValue("")]
        public string Data6 { get; set; } = "";
    }
}
