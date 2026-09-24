namespace ItisDota.Business.Models;

public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InviteToken { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public bool IsOwner { get; set; }
}
