using ProtoBuf;
using TikTokLiveSharp.Models.Protobuf.Messages.Headers;

namespace TikTokLiveSharp.Models.Protobuf.Messages
{
    [ProtoContract]
    public partial class WebcastLinkmicBattleTaskMessage : AProtoBase
    {
        [ProtoMember(1)]
        public MessageHeader Header { get; set; }

        [ProtoMember(2)]
        public uint Data1 { get; set; }

        [ProtoMember(3)]
        public LinkmicBattleTaskData BattleTaskData { get; set; }

        [ProtoMember(5)] 
        public LinkmicBattleTaskData2 BattleTaskData2 { get; set; }
    }

    /// <summary>
    /// Example-Data (Base-64):
    /// GsQGCsEGCMMBEisIAxInChpwbV9tdF9tYXRjaF9zcGVlZGNoYWxsZW5nZRIJCgNkdXISAjQwEjEIBxItCh9wbV9tdF9saXZlX21hdGNoX2luc3RydWN0aW9uc18xEgoKBW11bHRpEgEyErgCCAUSNAomcG1fbXRfbGl2ZV9tYXRjaF9pbnN0cnVjdGlvbnNfZ2lmdGVyXzESChIBOAoFbXVsdGnKAfwBCmJodHRwczovL3AxNi13ZWJjYXN0LnRpa3Rva2Nkbi5jb20vaW1nL2FsaXNnL3dlYmNhc3Qtc2cvdGlrY2FzdF9iYXR0bGVfdGFza19wZXJzb24ucG5nfnRwbHYtb2JqLnBuZwpiaHR0cHM6Ly9wMTktd2ViY2FzdC50aWt0b2tjZG4uY29tL2ltZy9hbGlzZy93ZWJjYXN0LXNnL3Rpa2Nhc3RfYmF0dGxlX3Rhc2tfcGVyc29uLnBuZ350cGx2LW9iai5wbmcSKXdlYmNhc3Qtc2cvdGlrY2FzdF9iYXR0bGVfdGFza19wZXJzb24ucG5nKgcjQ0VFNUVCGsQCCLQBEChiNAomcG1fbXRfbGl2ZV9tYXRjaF9pbnN0cnVjdGlvbnNfZ2lmdGVyXzISCgoFbXVsdGkSATKoAQG4AQjAAQHKAfwBCmJodHRwczovL3AxNi13ZWJjYXN0LnRpa3Rva2Nkbi5jb20vaW1nL2FsaXNnL3dlYmNhc3Qtc2cvdGlrY2FzdF9iYXR0bGVfdGFza19wZXJzb24ucG5nfnRwbHYtb2JqLnBuZwpiaHR0cHM6Ly9wMTktd2ViY2FzdC50aWt0b2tjZG4uY29tL2ltZy9hbGlzZy93ZWJjYXN0LXNnL3Rpa2Nhc3RfYmF0dGxlX3Rhc2tfcGVyc29uLnBuZ350cGx2LW9iai5wbmcSKXdlYmNhc3Qtc2cvdGlrY2FzdF9iYXR0bGVfdGFza19wZXJzb24ucG5nKgcjQ0VDRUVCIloIhgEQHhgCWioKHHBtX210X21hdGNoX2J1ZmZzdGFydGluZ3Nvb24SCgoFbXVsdGkSATJiJQoXcG1fbXRfbGl2ZV9tYXRjaF9kZXNjXzESCgoFbXVsdGkSATI=
    /// </summary>
    [ProtoContract]
    public partial class LinkmicBattleTaskData : AProtoBase
    {
        [ProtoMember(1)]
        public BattleTaskData Data { get; set; }
    }

    [ProtoContract]
    public partial class BattleTaskData : AProtoBase
    {
        [ProtoMember(1)]
        public uint Data1 { get; set; }

    }

    [ProtoContract]
    public partial class LinkmicBattleTaskData2 : AProtoBase
    {
        [ProtoMember(1)]
        public uint Data1 { get; set; }

        [ProtoMember(2)]
        public uint Data2 { get; set; }
    }
}
