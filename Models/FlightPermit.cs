using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
namespace FiveMPoliceCalculator.Models;
public sealed class FlightPermit : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nickname { get; set; } = "";
    public string UniqueId { get; set; } = "";
    public string Aircraft { get; set; } = "";
    public DateTime EndTime { get; set; }
    public string Header => $"{Nickname} · {UniqueId}  {Aircraft}";
    public TimeSpan Remaining => EndTime - DateTime.Now;
    public string RemainingText
    {
        get
        {
            var r = Remaining;
            if (r <= TimeSpan.Zero) return "만료";
            return $"남은 시간 {(int)r.TotalMinutes:00}:{r.Seconds:00}";
        }
    }
    public string EndText => $"{EndTime:HH:mm}까지";
    public Brush StatusBrush => Remaining.TotalSeconds <= 60 ? new SolidColorBrush(Color.FromRgb(251,191,36)) : new SolidColorBrush(Color.FromRgb(59,130,246));
    public void Refresh(){OnPropertyChanged(nameof(RemainingText));OnPropertyChanged(nameof(EndText));OnPropertyChanged(nameof(StatusBrush));}
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name=null)=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
}
