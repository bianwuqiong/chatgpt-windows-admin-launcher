using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ChatGPTAdminLauncher;

internal static class Program
{
    private const string LauncherTitle = "ChatGPT 管理员启动器";
    private const string PackagePrefix = "OpenAI.Codex_";
    private const string PackageSuffix = "__2p2nqsd0c76g0";
    private const string RequiredArgument = "--do-not-de-elevate";
    private const uint TokenQuery = 0x0008;
    private const int TokenElevation = 20;

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(
        IntPtr processHandle,
        uint desiredAccess,
        out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetTokenInformation(
        IntPtr tokenHandle,
        int tokenInformationClass,
        out int tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var target = ResolveChatGptExecutable();
            WriteLog($"已找到 ChatGPT：{target}");

            if (args.Any(x => string.Equals(x, "--diagnose", StringComparison.OrdinalIgnoreCase)))
            {
                WriteLog("诊断通过：安装路径可解析，启动器已在管理员上下文运行。");
                return IsCurrentProcessElevated() ? 0 : 5;
            }

            var existing = Process.GetProcessesByName("ChatGPT");
            if (existing.Length > 0)
            {
                var visible = existing.FirstOrDefault(HasWindow);
                var elevated = visible is not null && TryIsElevated(visible, out var isElevated)
                    && isElevated;
                var message = elevated
                    ? "ChatGPT 已经以管理员权限运行，无需重复启动。"
                    : "ChatGPT 当前正在运行。为避免中断正在执行的任务，启动器没有强制关闭它。\n\n请先正常退出 ChatGPT，再双击本启动器。";
                WriteLog(message.Replace("\n", " "));
                MessageBox.Show(message, LauncherTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return elevated ? 0 : 6;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = target,
                Arguments = RequiredArgument,
                WorkingDirectory = Path.GetDirectoryName(target)!,
                UseShellExecute = true
            };
            var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Windows 没有返回 ChatGPT 启动进程。");
            WriteLog($"已请求启动 ChatGPT，PID {process.Id}，参数 {RequiredArgument}。");

            // Chromium 未携带该参数时会在约一秒内降权并替换主进程；等待后再验令牌。
            Thread.Sleep(TimeSpan.FromSeconds(4));
            process.Refresh();
            if (!process.HasExited && TryIsElevated(process, out var launchedElevated) && launchedElevated)
            {
                WriteLog($"启动成功：PID {process.Id} 保持管理员令牌。");
                return 0;
            }

            var elevatedReplacement = Process.GetProcessesByName("ChatGPT")
                .FirstOrDefault(candidate => HasWindow(candidate)
                                             && TryIsElevated(candidate, out var value)
                                             && value);
            if (elevatedReplacement is not null)
            {
                WriteLog($"启动成功：管理员主进程 PID {elevatedReplacement.Id}。");
                return 0;
            }

            throw new InvalidOperationException(
                "ChatGPT 已启动，但未能确认管理员主进程。应用更新可能改变了防降权参数。");
        }
        catch (Exception exception)
        {
            var detail = exception.GetBaseException().Message;
            WriteLog("启动失败：" + detail);
            MessageBox.Show(
                $"ChatGPT 管理员启动失败。\n\n{detail}\n\n详细记录：\n{LogPath}",
                LauncherTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static string ResolveChatGptExecutable()
    {
        var windowsApps = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "WindowsApps");
        if (!Directory.Exists(windowsApps))
        {
            throw new DirectoryNotFoundException("找不到 WindowsApps 应用目录。");
        }

        var candidates = Directory.EnumerateDirectories(
                windowsApps,
                PackagePrefix + "*_x64" + PackageSuffix,
                SearchOption.TopDirectoryOnly)
            .Select(directory => new
            {
                Directory = directory,
                Version = ParsePackageVersion(Path.GetFileName(directory)),
                Executable = Path.Combine(directory, "app", "ChatGPT.exe")
            })
            .Where(candidate => File.Exists(candidate.Executable))
            .OrderByDescending(candidate => candidate.Version)
            .ThenByDescending(candidate => Directory.GetLastWriteTimeUtc(candidate.Directory))
            .ToArray();
        if (candidates.Length == 0)
        {
            throw new FileNotFoundException(
                "未找到已安装的 OpenAI ChatGPT Windows 应用。请先从官方渠道安装或更新应用。");
        }

        return Path.GetFullPath(candidates[0].Executable);
    }

    private static Version ParsePackageVersion(string directoryName)
    {
        var match = Regex.Match(
            directoryName,
            "^OpenAI\\.Codex_(?<version>[0-9.]+)_x64__2p2nqsd0c76g0$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        return match.Success && Version.TryParse(match.Groups["version"].Value, out var version)
            ? version
            : new Version(0, 0);
    }

    private static bool HasWindow(Process process)
    {
        try
        {
            process.Refresh();
            return !process.HasExited && process.MainWindowHandle != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCurrentProcessElevated()
    {
        using var current = Process.GetCurrentProcess();
        return TryIsElevated(current, out var elevated) && elevated;
    }

    private static bool TryIsElevated(Process process, out bool elevated)
    {
        elevated = false;
        IntPtr tokenHandle = IntPtr.Zero;
        try
        {
            if (!OpenProcessToken(process.Handle, TokenQuery, out tokenHandle))
            {
                return false;
            }

            if (!GetTokenInformation(
                    tokenHandle,
                    TokenElevation,
                    out var tokenElevation,
                    sizeof(int),
                    out _))
            {
                return false;
            }

            elevated = tokenElevation != 0;
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (tokenHandle != IntPtr.Zero)
            {
                _ = CloseHandle(tokenHandle);
            }
        }
    }

    private static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "ChatGPT-Admin-Launcher.log");

    private static void WriteLog(string message)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}] {message}{Environment.NewLine}";
        File.AppendAllText(LogPath, line, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
