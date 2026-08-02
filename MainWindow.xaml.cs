using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FiveMPoliceCalculator.Models;
using FiveMPoliceCalculator.Services;
namespace FiveMPoliceCalculator;
public partial class MainWindow : Window
{
 const int HotKeyId=9001;
 readonly ObservableCollection<Offense> selected=[];
 readonly ObservableCollection<FlightPermit> permits=[];
 readonly ObservableCollection<MacroItem> macros=[];
 List<Offense> all=[]; AppSettings settings=new(); bool mini; bool syncing=true; int miniTab;
 List<Offense>? undoItems; int undoPeople=1;
 readonly DispatcherTimer undoTimer=new(){Interval=TimeSpan.FromSeconds(3)};
 readonly DispatcherTimer flightTimer=new(){Interval=TimeSpan.FromSeconds(1)};
 public MainWindow(){InitializeComponent();settings=SettingsService.Load();LoadData();foreach(var m in MacroService.Load())macros.Add(m);BindData();ApplySettings();syncing=false;Recalculate(null,null);RefreshMacroUi();Loaded+=OnLoaded;Closing+=(_,_)=>SaveSettings();undoTimer.Tick+=(_,_)=>{UndoButton.Visibility=Visibility.Collapsed;undoTimer.Stop();};flightTimer.Tick+=FlightTimer_Tick;flightTimer.Start();}
 void LoadData(){var p=Path.Combine(AppContext.BaseDirectory,"Data","offenses.json");all=JsonSerializer.Deserialize<List<Offense>>(File.ReadAllText(p),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??[];}
 void BindData(){SelectedList.ItemsSource=selected;CategoryList.ItemsSource=new[]{"즐겨찾기"}.Concat(all.Select(x=>x.Category).Distinct()).ToList();CategoryList.SelectedIndex=0;FlightPermitList.ItemsSource=permits;MiniFlightPermitList.ItemsSource=permits;MacroGrid.ItemsSource=macros;}
 async void OnLoaded(object s, RoutedEventArgs e)
 {
     var h = new WindowInteropHelper(this).Handle;
     NativeMethods.RegisterHotKey(h, HotKeyId, 0, NativeMethods.VK_PRIOR);
     HwndSource.FromHwnd(h)?.AddHook(WndProc);

     // 창이 열린 뒤 GitHub의 최신 버전을 조용히 확인한다.
     await UpdateService.CheckForUpdatesAsync(this);
 }
 IntPtr WndProc(IntPtr h,int msg,IntPtr wp,IntPtr lp,ref bool handled){if(msg==NativeMethods.WM_HOTKEY&&wp.ToInt32()==HotKeyId){ToggleMini();handled=true;}return IntPtr.Zero;}
 void ApplySettings()
 {
     Left = settings.FullLeft;
     Top = settings.FullTop;
     Width = settings.Width;
     Height = settings.Height;
     OpacitySlider.Value = settings.MiniOpacity;
     FullClickThroughCheck.IsChecked = settings.MiniClickThrough;
     SettingsClickThroughCheck.IsChecked = settings.MiniClickThrough;
     MainTabs.SelectedIndex = Math.Clamp(settings.SelectedMainTab, 0, 4);
     miniTab = Math.Clamp(settings.SelectedMiniTab, 0, 2);
     SetMiniTab(miniTab);
 }
 void SaveSettings()
 {
     if (mini)
     {
         if (double.IsFinite(Left)) settings.MiniLeft = Left;
         if (double.IsFinite(Top)) settings.MiniTop = Top;
     }
     else
     {
         if (double.IsFinite(Left)) settings.FullLeft = Left;
         if (double.IsFinite(Top)) settings.FullTop = Top;
         if (double.IsFinite(Width)) settings.Width = Width;
         if (double.IsFinite(Height)) settings.Height = Height;
     }

     settings.MiniOpacity = OpacitySlider.Value;
     settings.MiniClickThrough = FullClickThroughCheck.IsChecked == true;
     settings.SelectedMainTab = MainTabs.SelectedIndex;
     settings.SelectedMiniTab = miniTab;
     SettingsService.Save(settings);
     MacroService.Save(macros);
 }
 IEnumerable<Offense> Filter(){var q=SearchBox.Text.Trim();var c=CategoryList.SelectedItem?.ToString();return all.Where(o=>(string.IsNullOrWhiteSpace(q)||o.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||o.Category.Contains(q,StringComparison.OrdinalIgnoreCase))&&(c is null||(c=="즐겨찾기"?settings.Favorites.Contains(o.Id):o.Category==c)));}
 void RefreshOffenses()=>OffenseList.ItemsSource=Filter().ToList();
 void CategoryList_SelectionChanged(object s,SelectionChangedEventArgs e)=>RefreshOffenses(); void SearchBox_TextChanged(object s,TextChangedEventArgs e)=>RefreshOffenses();
 void OffenseList_PreviewMouseLeftButtonDown(object s,MouseButtonEventArgs e){DependencyObject? d=e.OriginalSource as DependencyObject;while(d is not null&&d is not ListBoxItem)d=VisualTreeHelper.GetParent(d);if(d is ListBoxItem i&&i.DataContext is Offense o){OffenseList.SelectedItem=o;ToggleOffense(o);e.Handled=true;}}
 void ToggleOffense(Offense o){if(o.AdminOnly){MessageBox.Show(string.IsNullOrWhiteSpace(o.Note)?o.Name:$"{o.Name}\n\n{o.Note}","법률 안내");return;}var found=selected.FirstOrDefault(x=>x.Id==o.Id);if(found is not null)selected.Remove(found);else{if(o.PerPersonRp)MessageBox.Show($"{o.Name}은(는) 인당 벌금 + 인당 구금 항목입니다.\n현재 {GetPeople()}명 기준으로 계산됩니다.","인당 RP 안내",MessageBoxButton.OK,MessageBoxImage.Warning);selected.Add(o);}Recalculate(null,null);}
 void Favorite_Click(object s,RoutedEventArgs e){if(OffenseList.SelectedItem is not Offense o)return;if(!settings.Favorites.Remove(o.Id))settings.Favorites.Add(o.Id);SaveSettings();RefreshOffenses();}
 void RemoveSelected_Click(object s,RoutedEventArgs e){if(SelectedList.SelectedItem is Offense o)selected.Remove(o);Recalculate(null,null);}
 int GetPeople(){return int.TryParse(PeopleBox.Text,out var n)?Math.Max(1,n):1;}
 void SetPeople(int n){n=Math.Max(1,n);syncing=true;PeopleBox.Text=MiniPeopleBox.Text=RpPeopleBox.Text=n.ToString();syncing=false;Recalculate(null,null);}
 void PeopleMinus_Click(object s,RoutedEventArgs e)=>SetPeople(GetPeople()-1); void PeoplePlus_Click(object s,RoutedEventArgs e)=>SetPeople(GetPeople()+1);
 void PeopleBox_TextChanged(object s,TextChangedEventArgs e){if(syncing)return;if(int.TryParse(PeopleBox.Text,out var n))SetPeople(n);} void MiniPeopleBox_TextChanged(object s,TextChangedEventArgs e){if(syncing)return;if(int.TryParse(MiniPeopleBox.Text,out var n))SetPeople(n);} void RpPeopleBox_TextChanged(object s,TextChangedEventArgs e){if(syncing)return;if(int.TryParse(RpPeopleBox.Text,out var n))SetPeople(n);}
 void Recalculate(object? s,RoutedEventArgs? e)
 {
     if(syncing || FineText is null || RpFineText is null) return;
     int people=GetPeople();
     long fine=0; int jail=0;
     foreach(var o in selected)
     {
         int multiplier = o.IsRp ? (o.PerPersonRp ? people : 1) : people;
         fine += o.Fine * multiplier;
         jail += o.JailMinutes * multiplier;
     }

     // 도주 적용은 법전 규칙대로 벌금에만 3배를 적용한다.
     if(EscapeTripleCheck?.IsChecked == true)
         fine *= 3;

     // 감면은 도주 배수까지 적용된 벌금에 계산하고, 보석금에는 적용하지 않는다.
     if(DiscountCheck?.IsChecked == true)
     {
         double discountPercent = 0;
         if(DiscountPercentBox is not null &&
            double.TryParse(DiscountPercentBox.Text, out var enteredPercent))
         {
             discountPercent = Math.Clamp(enteredPercent, 0, 100);
         }

         fine = (long)Math.Round(
             fine * (100 - discountPercent) / 100.0,
             MidpointRounding.AwayFromZero);
     }

     int bailMin=0;
     if(BailCheck.IsChecked==true)
     {
         var t=BailMinutesBox.Text;
         bailMin=string.IsNullOrWhiteSpace(t)?jail:Math.Min(jail,int.TryParse(t,out var b)?Math.Max(0,b):jail);
     }
     long bail=(long)bailMin*1_000_000; int remain=Math.Max(0,jail-bailMin); long total=fine+bail;
     FineText.Text=MiniFine.Text=$"{fine:N0}원"; BailText.Text=MiniBail.Text=$"{bail:N0}원"; JailText.Text=MiniJail.Text=$"{jail}분"; RemainingText.Text=MiniRemaining.Text=$"{remain}분"; TotalFineText.Text=MiniTotalFine.Text=$"{total:N0}원"; PeopleText.Text=$"총 인원 {people}명";

     var rp=selected.LastOrDefault(x=>x.IsRp);
     RpSelectedText.Text=rp?.Name??"선택 없음";
     int rpMultiplier=rp is null?1:(rp.PerPersonRp?people:1);
     long rpFine=rp is null?0:rp.Fine*rpMultiplier;
     int rpJail=rp is null?0:rp.JailMinutes*rpMultiplier;
     int rpBailMin=0;
     if(RpBailCheck.IsChecked==true)
     {
         var t=RpBailMinutesBox.Text;
         rpBailMin=string.IsNullOrWhiteSpace(t)?rpJail:Math.Min(rpJail,int.TryParse(t,out var b)?Math.Max(0,b):rpJail);
     }
     long rpBail=(long)rpBailMin*1_000_000;
     int rpRemain=Math.Max(0,rpJail-rpBailMin);
     RpFineText.Text=$"{rpFine:N0}원"; RpBailText.Text=$"{rpBail:N0}원"; RpJailText.Text=$"{rpJail}분"; RpRemainingText.Text=$"{rpRemain}분"; RpTotalFineText.Text=$"{rpFine+rpBail:N0}원";
 }
 void DiscountCheckChanged(object s, RoutedEventArgs e)
 {
     if(DiscountInputPanel is not null)
         DiscountInputPanel.Visibility = DiscountCheck?.IsChecked == true
             ? Visibility.Visible
             : Visibility.Collapsed;

     Recalculate(null, null);
 }

 void RpQuick_Click(object s,RoutedEventArgs e){if(s is not Button b)return;var o=all.FirstOrDefault(x=>x.Name==b.Content?.ToString());if(o is null)return;foreach(var r in selected.Where(x=>x.IsRp).ToList())selected.Remove(r);ToggleOffense(o);}
 void ResetRp_Click(object s,RoutedEventArgs e){foreach(var r in selected.Where(x=>x.IsRp).ToList())selected.Remove(r);RpBailCheck.IsChecked=false;RpBailMinutesBox.Text="";Recalculate(null,null);}
 void Copy_Click(object s, RoutedEventArgs e)
 {
     var names = selected.Count == 0
         ? "- 선택 없음"
         : string.Join("\n", selected.Select(x => "- " + x.Name));

     var modifiers = new List<string>();
     if(EscapeTripleCheck?.IsChecked == true)
         modifiers.Add("도주 적용: 벌금 3배");

     if(DiscountCheck?.IsChecked == true)
     {
         var percent = double.TryParse(DiscountPercentBox?.Text, out var p)
             ? Math.Clamp(p, 0, 100)
             : 0;
         modifiers.Add($"벌금 감면: {percent:0.##}%");
     }

     var modifierText = modifiers.Count == 0
         ? string.Empty
         : "\n" + string.Join("\n", modifiers);

     Clipboard.SetText(
         $"[처벌 결과]\n{names}{modifierText}\n\n" +
         $"벌금: {FineText.Text}\n" +
         $"보석금: {BailText.Text}\n" +
         $"구금: {JailText.Text}\n" +
         $"남은 구금: {RemainingText.Text}\n" +
         $"총벌금: {TotalFineText.Text}\n" +
         PeopleText.Text);

     Toast("처벌 결과 복사 완료");
 }

 void CopyMiranda_Click(object s, RoutedEventArgs e)
 {
     const string miranda = "당신은 관리자를 선임할 수 있고, 묵비권을 행사하실 수 있으며 지금부터 하시는 말씀은 유치장에서 불리하게 작용할 수 있습니다.";
     Clipboard.SetText(miranda);
     Toast("미란다 고지 복사 완료");
 }

 void Reset_Click(object s,RoutedEventArgs e){undoItems=selected.ToList();undoPeople=GetPeople();selected.Clear();SetPeople(1);BailCheck.IsChecked=false;BailMinutesBox.Text="";EscapeTripleCheck.IsChecked=false;DiscountCheck.IsChecked=false;DiscountPercentBox.Text="0";SearchBox.Text="";UndoButton.Visibility=Visibility.Visible;undoTimer.Stop();undoTimer.Start();Recalculate(null,null);} void Undo_Click(object s,RoutedEventArgs e){if(undoItems is null)return;selected.Clear();foreach(var x in undoItems)selected.Add(x);SetPeople(undoPeople);UndoButton.Visibility=Visibility.Collapsed;undoTimer.Stop();}
 void RegisterFlight_Click(object s,RoutedEventArgs e)=>RegisterFlight(FlightNicknameBox,FlightUniqueBox,FlightAircraftBox); void RegisterMiniFlight_Click(object s,RoutedEventArgs e)=>RegisterFlight(MiniFlightNicknameBox,MiniFlightUniqueBox,MiniFlightAircraftBox);
 void FlightAircraftBox_KeyDown(object s,KeyEventArgs e){if(e.Key==Key.Enter)RegisterFlight_Click(s,e);} void MiniFlightAircraftBox_KeyDown(object s,KeyEventArgs e){if(e.Key==Key.Enter)RegisterMiniFlight_Click(s,e);}
 void RegisterFlight(TextBox nick,TextBox uid,TextBox aircraft){if(string.IsNullOrWhiteSpace(nick.Text)||string.IsNullOrWhiteSpace(uid.Text)||string.IsNullOrWhiteSpace(aircraft.Text)){MessageBox.Show("닉네임, 고유번호, 항공기를 모두 입력하세요.");return;}var p=new FlightPermit{Nickname=nick.Text.Trim(),UniqueId=uid.Text.Trim(),Aircraft=aircraft.Text.Trim(),EndTime=DateTime.Now.AddMinutes(15)};permits.Add(p);Clipboard.SetText($"{p.Nickname} {p.UniqueId} {p.Aircraft} {p.EndTime:HH:mm}까지 허가");nick.Text=uid.Text=aircraft.Text="";SortPermits();Toast("항공 허가문 복사 완료");}
 void ExtendFlight_Click(object s,RoutedEventArgs e){if(s is Button b&&b.Tag is FlightPermit p){p.EndTime=DateTime.Now.AddMinutes(15);p.Refresh();Clipboard.SetText($"{p.Nickname} {p.UniqueId} {p.Aircraft} {p.EndTime:HH:mm}까지 연장 허가");SortPermits();Toast("15분 연장 허가문 복사 완료");}}
 void FlightTimer_Tick(object? s,EventArgs e){foreach(var p in permits.Where(x=>x.EndTime<=DateTime.Now).ToList())permits.Remove(p);foreach(var p in permits)p.Refresh();SortPermits();}
 void SortPermits(){var sorted=permits.OrderBy(x=>x.EndTime).ToList();for(int i=0;i<sorted.Count;i++){var old=permits.IndexOf(sorted[i]);if(old!=i)permits.Move(old,i);}}
 void RefreshMacroUi()
 {
     var boundary=macros.Where(x=>x.Category=="주요 구역 경계령").ToList();
     var person=macros.Where(x=>x.Category=="개인 대상 출석/수배").ToList();
     var warrant=macros.Where(x=>x.Category=="영장 RP 절차").ToList();
     BoundaryMacroButtons.ItemsSource=boundary; PersonMacroButtons.ItemsSource=person; WarrantMacroButtons.ItemsSource=warrant;
     MiniBoundaryButtons.ItemsSource=boundary; MiniPersonButtons.ItemsSource=person; MiniWarrantButtons.ItemsSource=warrant;
 }
 string ExpandMacro(MacroItem m,bool miniMode){string uid=(miniMode?MiniMacroUniqueBox.Text:MacroUniqueBox.Text).Trim();string nick=(miniMode?MiniMacroNicknameBox.Text:MacroNicknameBox.Text).Trim();string prop=(miniMode?MiniMacroPropertyBox.Text:MacroPropertyBox.Text).Trim();return m.Template.Replace("{고번}",uid).Replace("{닉네임}",nick).Replace("{사유지명}",prop).Replace("{5분후}",DateTime.Now.AddMinutes(5).ToString("HH:mm")).Replace("{20분후}",DateTime.Now.AddMinutes(20).ToString("HH:mm"));}
 void CopyMacro_Click(object s,RoutedEventArgs e){if(s is Button b&&b.Tag is MacroItem m){Clipboard.SetText(ExpandMacro(m,false));Toast($"{m.Name} 복사 완료");}} void CopyMiniMacro_Click(object s,RoutedEventArgs e){if(s is Button b&&b.Tag is MacroItem m){Clipboard.SetText(ExpandMacro(m,true));Toast($"{m.Name} 복사 완료");}}
 void AddMacroRow_Click(object s,RoutedEventArgs e){var m=new MacroItem{Category="새 카테고리",Name="새 매크로",Template="복사할 문장"};macros.Add(m);MacroGrid.SelectedItem=m;MacroGrid.ScrollIntoView(m);}
 void DeleteMacroRow_Click(object s,RoutedEventArgs e){if(MacroGrid.SelectedItem is MacroItem m){macros.Remove(m);MacroService.Save(macros);RefreshMacroUi();}}
 void SaveMacros_Click(object s,RoutedEventArgs e){MacroGrid.CommitEdit();MacroService.Save(macros);RefreshMacroUi();Toast("공표문 저장 완료");} void MacroGrid_CellEditEnding(object s,DataGridCellEditEndingEventArgs e){Dispatcher.BeginInvoke(()=>{MacroService.Save(macros);RefreshMacroUi();});}
 void InsertVariable_Click(object s,RoutedEventArgs e){if(MacroGrid.SelectedItem is not MacroItem m||s is not Button b)return;var token=(b.Content?.ToString()??""); if(token.StartsWith("{}")) token=token[2..]; m.Template+=token;MacroGrid.Items.Refresh();}
 void ToggleMini_Click(object s, RoutedEventArgs e) => ToggleMini();
 void ToggleMini()
 {
     // 현재 모드의 위치와 크기를 먼저 저장한다.
     SaveSettings();

     mini = !mini;
     MainTabs.Visibility = mini ? Visibility.Collapsed : Visibility.Visible;
     MiniRoot.Visibility = mini ? Visibility.Visible : Visibility.Collapsed;
     HeaderRoot.Visibility = mini ? Visibility.Collapsed : Visibility.Visible;
     FooterRoot.Visibility = mini ? Visibility.Collapsed : Visibility.Visible;
     FullResizeThumb.Visibility = mini ? Visibility.Collapsed : Visibility.Visible;
     RootGrid.Background = mini ? Brushes.Transparent : (Brush)FindResource("Bg");

     MinWidth = mini ? 390 : 900;
     MinHeight = mini ? 300 : 600;
     Width = mini ? 410 : settings.Width;
     Left = mini ? settings.MiniLeft : settings.FullLeft;
     Top = mini ? settings.MiniTop : settings.FullTop;
     Opacity = mini ? OpacitySlider.Value : 1;
     ResizeMode = mini ? ResizeMode.NoResize : ResizeMode.CanResize;

     if (mini)
     {
         // 미니창의 위쪽 좌표는 유지하고, 선택 탭에 맞춰 아래쪽 높이만 변경한다.
         Height = GetMiniHeight(miniTab);
         EnsureMiniWindowOnScreenKeepingTop();
     }
     else
     {
         Height = settings.Height;
         EnsureWindowOnScreen();
     }

     ApplyClickThrough();
 }

 double GetMiniHeight(int tabIndex) => tabIndex switch
 {
     0 => 560, // 형량
     1 => 430, // 항공
     2 => 650, // 공표문
     _ => 560
 };

 void MiniTab_Click(object s, RoutedEventArgs e)
 {
     if (s is Button b && int.TryParse(b.Tag?.ToString(), out var i))
         SetMiniTab(i);
 }

 void SetMiniTab(int i)
 {
     miniTab = Math.Clamp(i, 0, 2);
     MiniCalculator.Visibility = miniTab == 0 ? Visibility.Visible : Visibility.Collapsed;
     MiniFlight.Visibility = miniTab == 1 ? Visibility.Visible : Visibility.Collapsed;
     MiniMacros.Visibility = miniTab == 2 ? Visibility.Visible : Visibility.Collapsed;

     if (mini)
     {
         // Top 값은 건드리지 않는다. 높이만 바꿔 하단이 늘어나거나 줄어든다.
         Height = GetMiniHeight(miniTab);
         EnsureMiniWindowOnScreenKeepingTop();
     }

     if (!syncing)
         SaveSettings();
 }
 void ClickThroughChanged(object s,RoutedEventArgs e){if(syncing)return;bool v=(s as CheckBox)?.IsChecked==true;syncing=true;FullClickThroughCheck.IsChecked=SettingsClickThroughCheck.IsChecked=v;syncing=false;settings.MiniClickThrough=v;ApplyClickThrough();SaveSettings();}
 void OpacityChanged(object s,RoutedPropertyChangedEventArgs<double> e){if(mini)Opacity=e.NewValue;if(!syncing)SaveSettings();}
 void ApplyClickThrough(){var h=new WindowInteropHelper(this).Handle;if(h==IntPtr.Zero)return;long ex=NativeMethods.GetWindowLongPtr(h,NativeMethods.GWL_EXSTYLE).ToInt64();if(mini&&settings.MiniClickThrough)ex|=NativeMethods.WS_EX_TRANSPARENT|NativeMethods.WS_EX_LAYERED;else ex&=~NativeMethods.WS_EX_TRANSPARENT;NativeMethods.SetWindowLongPtr(h,NativeMethods.GWL_EXSTYLE,new IntPtr(ex));NativeMethods.SetWindowPos(h,IntPtr.Zero,0,0,0,0,NativeMethods.SWP_NOMOVE|NativeMethods.SWP_NOSIZE|NativeMethods.SWP_NOZORDER|NativeMethods.SWP_NOACTIVATE|NativeMethods.SWP_FRAMECHANGED);}
 void MainTabs_SelectionChanged(object s,SelectionChangedEventArgs e){if(!syncing)SaveSettings();}
 void Toast(string text){Title=$"퍼펙트 경찰 · {text}";var t=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1.2)};t.Tick+=(_,_)=>{Title="퍼펙트 경찰";t.Stop();};t.Start();}
 void FullResizeThumb_DragDelta(object s, DragDeltaEventArgs e)
 {
     if (mini) return;

     Width = Math.Max(MinWidth, Width + e.HorizontalChange);
     Height = Math.Max(MinHeight, Height + e.VerticalChange);
     SaveSettings();
 }

 void MiniHeader_MouseLeftButtonDown(object s, MouseButtonEventArgs e)
 {
     if (!mini || settings.MiniClickThrough || e.LeftButton != MouseButtonState.Pressed)
         return;

     try
     {
         DragMove();
         SaveSettings();
     }
     catch (InvalidOperationException)
     {
         // 마우스 상태가 바뀐 경우 이동을 조용히 취소한다.
     }
 }

 void EnsureMiniWindowOnScreenKeepingTop()
 {
     var area = SystemParameters.WorkArea;

     if (Left < area.Left) Left = area.Left;
     if (Left + Width > area.Right) Left = Math.Max(area.Left, area.Right - Width);

     // 위쪽 위치는 사용자가 고정한 값을 유지한다.
     if (Top < area.Top) Top = area.Top;

     // 화면 아래를 벗어나면 Top을 올리지 않고 높이만 줄인다.
     double availableHeight = Math.Max(MinHeight, area.Bottom - Top);
     if (Height > availableHeight)
         Height = availableHeight;
 }

 void EnsureWindowOnScreen()
 {
     var area = SystemParameters.WorkArea;
     if (Left < area.Left) Left = area.Left;
     if (Top < area.Top) Top = area.Top;
     if (Left + Width > area.Right) Left = Math.Max(area.Left, area.Right - Width);
     if (Top + Height > area.Bottom) Top = Math.Max(area.Top, area.Bottom - Height);
 }

 void Header_MouseLeftButtonDown(object s,MouseButtonEventArgs e){if(e.LeftButton==MouseButtonState.Pressed)DragMove();} void Minimize_Click(object s,RoutedEventArgs e)=>WindowState=WindowState.Minimized; void Close_Click(object s,RoutedEventArgs e)=>Close();
 protected override void OnClosed(EventArgs e){NativeMethods.UnregisterHotKey(new WindowInteropHelper(this).Handle,HotKeyId);SaveSettings();base.OnClosed(e);}
}
