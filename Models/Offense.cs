namespace FiveMPoliceCalculator.Models;
public sealed class Offense
{
    public string Id { get; set; } = "";
    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public long Fine { get; set; }
    public int JailMinutes { get; set; }
    public bool IsRp { get; set; }
    public bool PerPersonRp { get; set; }
    public bool AdminOnly { get; set; }
    public string Note { get; set; } = "";
    public override string ToString()
    {
        if (AdminOnly)
            return $"[안내] {Name}";

        return $"{Name}  |  {Fine:N0}원 / {JailMinutes}분";
    }
}
