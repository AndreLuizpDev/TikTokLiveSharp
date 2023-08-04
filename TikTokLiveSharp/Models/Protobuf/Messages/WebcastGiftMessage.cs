using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using TikTokLiveSharp.Models.Protobuf.Messages.Headers;
using TikTokLiveSharp.Models.Protobuf.Objects;
using TikTokLiveSharp.Models.Protobuf.Objects.DataObjects;

namespace TikTokLiveSharp.Models.Protobuf.Messages
{
    /// <summary>
    /// Gift sent by user
    /// </summary>
    [ProtoContract]
    public partial class WebcastGiftMessage : AProtoBase
    {
        [ProtoMember(1)]
        public GiftMessageHeader Header { get; set; }

        /// <summary>
        /// Gift-ID
        /// </summary>
        [ProtoMember(2)]
        public ulong GiftId { get; set; }

        [ProtoMember(3)]
        public int Data1 { get; set; }

        [ProtoMember(4)]
        public int Data2 { get; set; }

        /// <summary>
        /// Index of Message in Streak
        /// </summary>
        [ProtoMember(5)]
        public uint RepeatCount { get; set; }

        /// <summary>
        /// Number of Gifts sent in Streak
        /// </summary>
        [ProtoMember(6)]
        public uint Amount { get; set; }

        /// <summary>
        /// User who sent the Gift
        /// </summary>
        [ProtoMember(7)]
        public User Sender { get; set; }

        /// <summary>
        /// Receiver? Found with text "live_gift_send_message_to_anyuser" (Host sent a gift to a viewer?)
        /// </summary>
        [ProtoMember(8)]
        public User User2 { get; set; }

        /// <summary>
        /// Whether this is the final message in the GiftStreak
        /// </summary>
        [ProtoMember(9)]
        public bool RepeatEnd { get; set; }

        [ProtoMember(11)]
        public ulong ServerTime { get; set; }

        [ProtoMember(13)]
        public int Data3 { get; set; }

        [ProtoMember(14)]
        public GiftData1 Data4 { get; set; }

        /// <summary>
        /// Details for Gift that was sent
        /// </summary>
        [ProtoMember(15)]
        public Gift GiftDetails { get; set; }

        /// <summary>
        /// "202301050654360101042172152C002035"
        /// </summary>
        [ProtoMember(16)]
        [DefaultValue("")]
        public string LogId { get; set; } = "";

        [ProtoMember(17)]
        public int Data5 { get; set; }

        [ProtoMember(19)]
        [DefaultValue("")]
        public string Data6 { get; set; } = "";

        /// <summary>
        /// GiftReceipt
        /// </summary>
        [ProtoMember(22)]
        [DefaultValue("")]
        public string ReceiptJson { get; set; } = "";

        /// <summary>
        /// Same as JSON, but deserialized
        /// </summary>
        [ProtoMember(23)]
        public GiftReceipt Receipt { get; set; }

        [ProtoMember(24)]
        public int Data7 { get; set; }

        [ProtoMember(25)]
        public uint Data8 { get; set; }

        [ProtoMember(26)]
        public GiftDetails1 FirstGift1 { get; set; }

        [ProtoMember(27)]
        public GiftDetails1 FirstGift2 { get; set; }

        [ProtoMember(31)]
        public GiftDetails2 Data9 { get; set; }

        [ProtoMember(32)]
        public DataContainer Data10 { get; set; }
        
        [ProtoMember(33)]
        [DefaultValue("")]
        public string Data11 { get; set; } = "";

        [ProtoMember(34)]
        public uint Data12 { get; set; }

        [ProtoMember(36)]
        public uint Data13 { get; set; }
    }

    [ProtoContract]
    public partial class GiftData1 : AProtoBase
    {
        [ProtoMember(1)]
        [DefaultValue("")]
        public string Data1 { get; set; } = "";

        [ProtoMember(2)]
        public uint Data2 { get; set; }

        [ProtoMember(3)]
        public uint Data3 { get; set; }
    }

    [ProtoContract]
    public partial class GiftDetails1 : AProtoBase
    {
        [ProtoMember(1)]
        [DefaultValue("")]
        public string EventType { get; set; } = "";

        [ProtoMember(2)]
        [DefaultValue("")]
        public string Label { get; set; } = "";

        [ProtoMember(3)]
        public TikTokColor Color { get; set; }

        [ProtoMember(4)]
        public GiftDetailsData data { get; set; }
    }

    [ProtoContract]
    public partial class GiftDetails2 : AProtoBase
    {
        [ProtoMember(1)]
        public uint Data1 { get; set; }

        [ProtoMember(3)]
        public uint Data2 { get; set; }

        [ProtoMember(4)]
        public List<string> Urls { get; set; } = new List<string>();
    }
}
