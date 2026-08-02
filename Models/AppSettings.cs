namespace FiveMPoliceCalculator.Models;
public sealed class AppSettings
{
    public List<string> Favorites { get; set; } = [];
    public double MiniOpacity { get; set; } = 0.78;
    public bool MiniClickThrough { get; set; }
    public double Left { get; set; } = 100; // 이전 설정 호환용
    public double Top { get; set; } = 100;  // 이전 설정 호환용
    public double FullLeft { get; set; } = 100;
    public double FullTop { get; set; } = 100;
    public double MiniLeft { get; set; } = 80;
    public double MiniTop { get; set; } = 80;
    public double Width { get; set; } = 1320;
    public double Height { get; set; } = 820;
    public int SelectedMainTab { get; set; }
    public int SelectedMiniTab { get; set; }
}
