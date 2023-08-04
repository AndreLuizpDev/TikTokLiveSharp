using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using TikTokLiveSharp.Models.Protobuf.Objects.DataObjects;

namespace TikTokLiveSharp.Models.Protobuf.Objects
{
    [ProtoContract]
    public partial class User : AProtoBase
    {
        [ProtoMember(1)]
        public ulong UserId { get; set; }

        [ProtoMember(2)]
        public uint Data1 { get; set; }

        /// <summary>
        /// Name set in Profile
        /// </summary>
        [ProtoMember(3)]
        [DefaultValue("")]
        public string NickName { get; set; } = "";

        /// <summary>
        /// Thumbnail? (Used in WebcastLinkMicBattle-Teams)
        /// </summary>
        [ProtoMember(4)]
        public Picture Thumbnail { get; set; }

        /// <summary>
        /// User-Description
        /// </summary>
        [ProtoMember(5)]
        [DefaultValue("")]
        public string Description { get; set; } = "";

        /// <summary>
        /// Avatar
        /// </summary>
        [ProtoMember(9)]
        public Picture ProfilePicture;

        /// <summary>
        /// 720p
        /// </summary>
        [ProtoMember(10)]
        public Picture Picture720;

        /// <summary>
        /// 1080p
        /// </summary>
        [ProtoMember(11)]
        public Picture Picture1080;

        [ProtoMember(15)]
        public uint Data2 { get; set; }

        [ProtoMember(16)]
        public ulong Data3 { get; set; }

        [ProtoMember(21)]
        public List<Picture> AdditionalPictures { get; set; } = new List<Picture>();

        [ProtoMember(22)]
        public FollowerData FollowerData { get; set; }

        [ProtoMember(23)]
        [DefaultValue("")]
        public string UserString1 { get; set; } = "";

        [ProtoMember(25)]
        public UserRanking UserRank1 { get; set; }

        [ProtoMember(32)]
        [DefaultValue("")]
        public string UserString2 { get; set; } = "";

        [ProtoMember(37)]
        public ulong Data4 { get; set; }

        /// <summary>
        /// @-ID for User
        /// </summary>
        [ProtoMember(38)]
        [DefaultValue("")]
        public string UniqueId { get; set; } = "";
        
        [ProtoMember(46)]
        [DefaultValue("")]
        public string Data5 { get; set; } = "";

        [ProtoMember(61)]
        public UserRanking UserRank2 { get; set; }

        [ProtoMember(64)]
        public List<Badge> Badges { get; set; } = new List<Badge>();

        [ProtoMember(1028)]
        [DefaultValue("")]
        public string UserIdString { get; set; } = "";
    }

    [ProtoContract]
    public partial class FollowerData : AProtoBase
    {
        /// <summary>
        /// Amount this user Follows
        /// </summary>
        [ProtoMember(1)]
        public uint Following { get; set; }

        /// <summary>
        /// Amount following this user
        /// </summary>
        [ProtoMember(2)]
        public uint Followers { get; set; }

        /// <summary>
        /// Relative Role of this User to the Host. 
        /// TODO: IS AN ENUM
        /// </summary>
        [ProtoMember(3)]
        public uint FollowsHost { get; set; }
    }

    [ProtoContract]
    public partial class UserRanking : AProtoBase
    {
        [ProtoMember(1)]
        public Picture Img1 { get; set; }

        [ProtoMember(3)]
        [DefaultValue("")]
        public string EventType { get; set; } = "";

        [ProtoMember(4)]
        public Picture Img2 { get; set; }

        [ProtoMember(5)]
        public IdData Id1 { get; set; }

        [ProtoMember(6)]
        public IdData Id2 { get; set; }
    }
}
