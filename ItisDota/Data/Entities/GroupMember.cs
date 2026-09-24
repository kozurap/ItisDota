namespace ItisDota.Data.Entities;

public class GroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int PlayerId { get; set; }
    public int SelectionCount { get; set; }
    public DateTimeOffset JoinedAt { get; set; }

    public PlayerGroup? Group { get; set; }
    public Player? Player { get; set; }
}
