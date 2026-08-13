namespace gud.Server.Models;

public class Repo
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;
}