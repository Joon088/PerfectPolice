using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace FiveMPoliceCalculator.Services;

public static class UpdateService
{
    private const string DefaultVersionUrl =
        "https://raw.githubusercontent.com/Joon088/PerfectPolice/main/update/version.json";

    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public static async Task CheckForUpdatesAsync(Window owner)
    {
        try
        {
            string versionUrl = LoadVersionUrl();
            using var request = new HttpRequestMessage(HttpMethod.Get, versionUrl);
            request.Headers.UserAgent.ParseAdd("PerfectPolice-Updater/2.2.2");

            using HttpResponseMessage response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            UpdateInfo? info = JsonSerializer.Deserialize<UpdateInfo>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (info is null ||
                string.IsNullOrWhiteSpace(info.Version) ||
                string.IsNullOrWhiteSpace(info.DownloadUrl))
            {
                return;
            }

            Version current = GetCurrentVersion();
            if (!TryParseVersion(info.Version, out Version? latest) || latest <= current)
                return;

            string notes = info.Notes is { Count: > 0 }
                ? "\n\n변경 내용\n- " + string.Join("\n- ", info.Notes)
                : string.Empty;

            MessageBoxResult result = MessageBox.Show(
                owner,
                $"새 버전 {latest}이 있습니다.\n현재 버전: {current}{notes}\n\n지금 업데이트하시겠습니까?",
                "퍼펙트 경찰 업데이트",
                info.Required ? MessageBoxButton.OK : MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            bool accepted = info.Required
                ? result == MessageBoxResult.OK
                : result == MessageBoxResult.Yes;

            if (!accepted)
                return;

            await DownloadAndInstallAsync(owner, info.DownloadUrl);
        }
        catch
        {
            // 인터넷 연결 또는 GitHub 장애가 있어도 프로그램 사용을 방해하지 않는다.
        }
    }

    private static async Task DownloadAndInstallAsync(Window owner, string downloadUrl)
    {
        string? currentExe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExe) || !File.Exists(currentExe))
        {
            MessageBox.Show(owner, "현재 실행파일 위치를 확인하지 못했습니다.",
                "업데이트 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string tempDirectory = Path.Combine(Path.GetTempPath(), "PerfectPoliceUpdate");
        Directory.CreateDirectory(tempDirectory);

        string downloadedExe = Path.Combine(tempDirectory, "PerfectPolice_new.exe");
        string updaterBat = Path.Combine(tempDirectory, "apply_update.cmd");

        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        request.Headers.UserAgent.ParseAdd("PerfectPolice-Updater/2.2.2");

        using HttpResponseMessage response = await Client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using (Stream input = await response.Content.ReadAsStreamAsync())
        await using (FileStream output = new(
            downloadedExe,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None))
        {
            await input.CopyToAsync(output);
        }

        if (!File.Exists(downloadedExe) || new FileInfo(downloadedExe).Length < 100_000)
        {
            throw new InvalidDataException("다운로드한 업데이트 파일이 올바르지 않습니다.");
        }

        int processId = Environment.ProcessId;
        string script = BuildUpdaterScript(processId, downloadedExe, currentExe, updaterBat);
        await File.WriteAllTextAsync(updaterBat, script, new UTF8Encoding(false));

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{updaterBat}\"",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });

        Application.Current.Shutdown();
    }

    private static string BuildUpdaterScript(
        int processId,
        string downloadedExe,
        string currentExe,
        string updaterBat)
    {
        static string Q(string value)
            => $"\"{value.Replace("\"", "\"\"")}\"";

        return $$"""
@echo off
setlocal

:wait_for_app
powershell -NoProfile -Command "if (Get-Process -Id {{processId}} -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"

if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto wait_for_app
)

copy /y {{Q(downloadedExe)}} {{Q(currentExe)}} >nul

if errorlevel 1 (
    start "" {{Q(downloadedExe)}}
    exit /b 1
)

start "" {{Q(currentExe)}}

del /q {{Q(downloadedExe)}} >nul 2>nul
del /q {{Q(updaterBat)}} >nul 2>nul

endlocal
""";
    }
    private static string LoadVersionUrl()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "update_config.json");
            if (!File.Exists(path))
                return DefaultVersionUrl;

            string json = File.ReadAllText(path);
            UpdateConfig? config = JsonSerializer.Deserialize<UpdateConfig>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return string.IsNullOrWhiteSpace(config?.VersionUrl)
                ? DefaultVersionUrl
                : config.VersionUrl;
        }
        catch
        {
            return DefaultVersionUrl;
        }
    }

    private static Version GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version
               ?? new Version(0, 0, 0, 0);
    }

    private static bool TryParseVersion(string value, out Version? version)
    {
        string cleaned = value.Trim().TrimStart('v', 'V');
        return Version.TryParse(cleaned, out version);
    }

    private sealed class UpdateConfig
    {
        public string VersionUrl { get; set; } = string.Empty;
    }

    private sealed class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public List<string> Notes { get; set; } = [];
    }
}
