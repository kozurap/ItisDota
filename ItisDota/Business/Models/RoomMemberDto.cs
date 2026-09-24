namespace ItisDota.Business.Models;

public class RoomMemberDto
{
    public int Id { get; set; }
    public string RealName { get; set; } = string.Empty;
    public string TgTag { get; set; } = string.Empty;
    public int Mmr { get; set; }
    public int? SteamFriendId { get; set; }
    public int RolePriority1 { get; set; }
    public int RolePriority2 { get; set; }
    public int RolePriority3 { get; set; }
    public int RolePriority4 { get; set; }
    public int RolePriority5 { get; set; }
    public bool IsReady { get; set; }

    public PlayerDto ToPlayerDto()
    {
        return new PlayerDto
        {
            Id = Id,
            RealName = RealName,
            TgTag = TgTag,
            Mmr = Mmr,
            SteamFriendId = SteamFriendId,
            RolePriority1 = RolePriority1,
            RolePriority2 = RolePriority2,
            RolePriority3 = RolePriority3,
            RolePriority4 = RolePriority4,
            RolePriority5 = RolePriority5
        };
    }
}
