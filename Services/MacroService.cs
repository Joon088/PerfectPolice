using System.IO;
using System.Text.Json;
using FiveMPoliceCalculator.Models;
namespace FiveMPoliceCalculator.Services;
public static class MacroService
{
    static readonly string Dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PerfectPolice");
    static readonly string FilePath=Path.Combine(Dir,"macros.json");
    public static List<MacroItem> Load()
    {
        try
        {
            if(File.Exists(FilePath)) return JsonSerializer.Deserialize<List<MacroItem>>(File.ReadAllText(FilePath))??Defaults();
        }catch{}
        var items=Defaults(); Save(items); return items;
    }
    public static void Save(IEnumerable<MacroItem> items)
    {
        try{Directory.CreateDirectory(Dir);File.WriteAllText(FilePath,JsonSerializer.Serialize(items,new JsonSerializerOptions{WriteIndented=true}));}catch{}
    }
    static List<MacroItem> Defaults()=>
    [
      new(){Category="주요 구역 경계령",Name="보석상 RP 경계령",Template="보석상 RP 경계령을 선포합니다."},
      new(){Category="주요 구역 경계령",Name="은행 RP 경계령",Template="은행 RP 경계령을 선포합니다."},
      new(){Category="주요 구역 경계령",Name="편의점 RP 경계령",Template="편의점 RP 경계령을 선포합니다."},
      new(){Category="주요 구역 경계령",Name="청부 RP 경계령",Template="청부 RP 경계령을 선포합니다."},
      new(){Category="개인 대상 출석/수배",Name="출석 요구 공표",Template="[{고번}][{닉네임}]님은 [{5분후}]까지 경찰청으로 출석해주시기 바랍니다."},
      new(){Category="개인 대상 출석/수배",Name="수배 전환 공표",Template="[{고번}][{닉네임}]님은 출석 요구 불응으로 수배 전환합니다."},
      new(){Category="개인 대상 출석/수배",Name="수배 RP 공표",Template="[{고번}][{닉네임}]님에 대한 수배 RP를 진행합니다."},
      new(){Category="영장 RP 절차",Name="1차 영장 발부 시작",Template="[{사유지명}] 영장 집행을 시작합니다."},
      new(){Category="영장 RP 절차",Name="2차 사유지 앞 도착",Template="경찰이 [{사유지명}] 앞에 도착했습니다."},
      new(){Category="영장 RP 절차",Name="3차 대표자 호출",Template="[{사유지명}] 대표자는 [{5분후}]까지 응답해주시기 바랍니다."},
      new(){Category="영장 RP 절차",Name="4차 유예시간 고지",Template="[{사유지명}]에 대한 유예시간을 [{20분후}]까지 부여합니다."},
      new(){Category="영장 RP 절차",Name="5차 전력진압 공지",Template="[{사유지명}]에 대한 전력 진압을 시작합니다."}
    ];
}
