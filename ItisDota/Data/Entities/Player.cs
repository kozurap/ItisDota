namespace WebApplication1.Data.Entities;

public class Player
{
    public int Id { get; set; }
    public string RealName { get; set; } = string.Empty;
    public string TgTag { get; set; } = string.Empty;
    public string MmRRange { get; set; } = string.Empty;
    public int RolePriority1 { get; set; }
    public int RolePriority2 { get; set; }
    public int RolePriority3 { get; set; }
    public int RolePriority4 { get; set; }
    public int RolePriority5 { get; set; }
    public int SelectionCount { get; set; }
}
