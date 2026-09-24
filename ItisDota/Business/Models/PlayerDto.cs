namespace ItisDota.Business.Models;

public class PlayerDto
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
}
