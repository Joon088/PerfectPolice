using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace FiveMPoliceCalculator.Models;
public sealed class MacroItem : INotifyPropertyChanged
{
    string category=""; string name=""; string template="";
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Category { get=>category; set{category=value;OnPropertyChanged();} }
    public string Name { get=>name; set{name=value;OnPropertyChanged();} }
    public string Template { get=>template; set{template=value;OnPropertyChanged();} }
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? n=null)=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(n));
}
