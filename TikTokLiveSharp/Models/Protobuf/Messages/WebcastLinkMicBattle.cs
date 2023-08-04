using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using TikTokLiveSharp.Models.Protobuf.Messages.Headers;
using TikTokLiveSharp.Models.Protobuf.Objects;
using TikTokLiveSharp.Models.Protobuf.Objects.DataObjects;

namespace TikTokLiveSharp.Models.Protobuf.Messages
{
    [ProtoContract]
    public partial class WebcastLinkMicBattle : AProtoBase
    {
        [ProtoMember(1)]
        public MessageHeader Header { get; set; }

        [ProtoMember(2)]
        public ulong Id { get; set; }

        [ProtoMember(3)]
        public LinkMicBattleConfig BattleConfig { get; set; }

        [ProtoMember(4)]
        public uint Data1 { get; set; }

        [ProtoMember(5)]
        public List<LinkMicBattleDetails> Details { get; set; } = new List<LinkMicBattleDetails>();

        [ProtoMember(9)]
        public List<LinkMicBattleTeam> Teams1 { get; set; } = new List<LinkMicBattleTeam>();

        [ProtoMember(10)]
        public List<LinkMicBattleTeam> Teams2 { get; set; } = new List<LinkMicBattleTeam>();

        [ProtoMember(13)]
        public List<LinkMicBattleTeamData> TeamData { get; set; } = new List<LinkMicBattleTeamData>();

        // Winner-Id?
        [ProtoMember(16)]
        public ulong Id2 { get; set; }
    }

    [ProtoContract]
    public partial class LinkMicBattleConfig : AProtoBase
    {
        [ProtoMember(1)]
        public ulong Id { get; set; }

        [ProtoMember(2)]
        public ulong Timestamp { get; set; }

        [ProtoMember(3)]
        public uint Data1 { get; set; }

        [ProtoMember(4)]
        public ulong Id2 { get; set; }

        [ProtoMember(5)]
        public uint Data2 { get; set; }

        [ProtoMember(6)]
        public uint Data3 { get; set; }

        [ProtoMember(7)]
        public LinkMicBattleGift BattleGift { get; set; }

        [ProtoMember(8)]
        public uint Data4 { get; set; }
    }

    [ProtoContract]
    public partial class LinkMicBattleData : AProtoBase
    {
        [ProtoMember(1)]
        public ulong Id { get; set; }

        [ProtoMember(2)]
        public uint Data1 { get; set; }

        [ProtoMember(3)]
        public uint Data2 { get; set; }

        [ProtoMember(5)]
        public uint Data3 { get; set; }

        [ProtoMember(6)]
        [DefaultValue("")]
        public string Url { get; set; } = "";
    }

    [ProtoContract]
    public partial class LinkMicBattleDetails : AProtoBase
    {
        [ProtoMember(1)]
        public ulong Id { get; set; }

        [ProtoMember(2)]
        public LinkMicBattleData Details { get; set; }
    }

    [ProtoContract]
    public partial class LinkMicBattleTeam : AProtoBase
    {
        [ProtoMember(1)]
        public ulong Id { get; set; }

        [ProtoMember(2)]
        public LinkMicBattleTeamPlayers TeamPlayers { get; set; }
    }

    [ProtoContract]
    public partial class LinkMicBattleTeamPlayers : AProtoBase
    {
     //   [ProtoMember(1)] 
     //   public List<User> Players { get; set; } = new List<User>();

        [ProtoMember(2)]
        public uint Data1 { get; set; }
    }


    [ProtoContract]
    public partial class LinkMicBattleTeamData : AProtoBase
    {
        [ProtoMember(1)]
        public ulong TeamId { get; set; }

        [ProtoMember(2)]
        public LinkMicBattleData Data { get; set; }
    }

    [ProtoContract]
    public partial class LinkMicBattleGift : AProtoBase
    {
        // Found with Value: pm_mt_match_gift_option_select_page_all_option
        [ProtoMember(2)]
        [DefaultValue("")]
        public string Type { get; set; } = "";

        [ProtoMember(3)]
        public Picture Picture { get; set; }
    }
}
