using ProtoBuf;
using System.ComponentModel;
using TikTokLiveSharp.Models.Protobuf.Messages.Headers;
using TikTokLiveSharp.Models.Protobuf.Objects;

namespace TikTokLiveSharp.Models.Protobuf.Messages
{
    [ProtoContract]
    public partial class WebcastMemberMessage : AProtoBase
    {
        [ProtoContract]
        [System.Serializable]
        public enum MemberMessageAction
        {
            Unknown = 0,
            Joined = 1,
            Subscribed = 3
            // ?? = 26
            // ?? = 27 (User2 is set to Host-Info?)
            // ?? = 50 (share?)
        }

        [ProtoMember(1)]
        public MemberMessageHeader Header { get; set; }

        [ProtoMember(2)]
        public User Sender { get; set; }

        /// <summary>
        /// Available in Join-Message
        /// </summary>
        [ProtoMember(3)]
        public ulong TotalViewers { get; set; }

        [ProtoMember(4)]
        public User User2 { get; set; }

        [ProtoMember(10)]
        public int Action { get; set; }

        /// <summary>
        /// Identical to details in Header
        /// </summary>
        [ProtoMember(18)]
        public MemberMessageData EventDetails { get; set; }

        /// <summary>
        /// unknown/homepage_hot-live_cell.live_merge-live_cover
        /// </summary>
        [ProtoMember(19)]
        [DefaultValue("")]
        public string Data1 { get; set; } = "";

        /// <summary>
        /// click/draw
        /// </summary>
        [ProtoMember(20)]
        [DefaultValue("")]
        public string Data2 { get; set; } = "";

        /// <summary>
        /// rec
        /// </summary>
        [ProtoMember(21)]
        [DefaultValue("")]
        public string Data3 { get; set; } = "";

        /// <summary>
        /// other
        /// </summary>
        [ProtoMember(23)]
        [DefaultValue("")]
        public string Data4 { get; set; } = "";

        [ProtoMember(24)]
        public uint Data5 { get; set; }
    }
}
