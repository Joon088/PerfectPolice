using System.IO;
using System.Text.Json;
using FiveMPoliceCalculator.Models;
namespace FiveMPoliceCalculator.Services;
public static class SettingsService
{
    static readonly string Dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PerfectPolice");
    static readonly string FilePath=Path.Combine(Dir,"settings.json");
    public static AppSettings Load(){try{if(File.Exists(FilePath)){var s=JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath))??new();Sanitize(s);return s;}}catch{}return new();}
    public static void Save(AppSettings s){try{Sanitize(s);Directory.CreateDirectory(Dir);File.WriteAllText(FilePath,JsonSerializer.Serialize(s,new JsonSerializerOptions{WriteIndented=true}));}catch{}}
    static void Sanitize(AppSettings s){if(!double.IsFinite(s.Left))s.Left=100;if(!double.IsFinite(s.Top))s.Top=100;if(!double.IsFinite(s.Width)||s.Width<900)s.Width=1320;if(!double.IsFinite(s.Height)||s.Height<600)s.Height=820;if(!double.IsFinite(s.MiniOpacity))s.MiniOpacity=.78;s.MiniOpacity=Math.Clamp(s.MiniOpacity,.3,1);s.Favorites??=[];}
}
