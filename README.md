# ChatGPT Windows Admin Launcher

A small, auditable Windows launcher for the Microsoft Store / MSIX ChatGPT desktop app (`OpenAI.Codex`). It starts the app with an elevated token and passes Chromium's `--do-not-de-elevate` switch, then verifies that the launched main process remains elevated.

中文说明见下方。

## Why

On some Windows builds, launching the ChatGPT desktop executable from an elevated process can be followed by Chromium automatically restarting the main process with a standard token. The desktop app then looks elevated while its local command process still lacks administrator access.

OpenAI's [Windows app documentation](https://learn.chatgpt.com/zh-Hans/docs/windows/windows-app) says that Codex inherits administrator rights when the ChatGPT desktop app itself is started as administrator. This launcher preserves that elevated launch on the versions where Chromium's automatic de-elevation otherwise intervenes.

`--do-not-de-elevate` is an implementation-level Chromium switch, not a documented OpenAI compatibility contract. The launcher verifies the resulting token and fails visibly if a future update changes the behavior.

## Download and use

1. Download `ChatGPT-Admin-Launcher-win-x64.exe` from the latest GitHub Release.
2. Exit every running ChatGPT desktop process normally.
3. Double-click the launcher and approve the Windows UAC prompt.
4. The launcher locates the newest installed x64 `OpenAI.Codex` package and starts its original `ChatGPT.exe`.

The release is unsigned, so Windows displays an unknown publisher in UAC. Compare the file's SHA-256 hash with `SHA256SUMS.txt` from the same Release.

## Security properties

- Requests normal UAC consent on every launch.
- Does not create a scheduled task, service, startup entry, or UAC bypass.
- Does not read credentials, contact the network, or collect telemetry.
- Does not change `WindowsApps` permissions, environment variables, ChatGPT settings, or Codex sandbox settings.
- Does not terminate an existing ChatGPT session. It asks the user to exit first.
- Writes a local diagnostic log named `ChatGPT-Admin-Launcher.log` on the desktop. The log contains timestamps, the installed package path, process IDs, and launch results; it contains no credentials.

Running ChatGPT with an administrator token increases the impact of local commands that the user authorizes. Use the elevated launcher only for tasks that require administrator access, and use the normal app launcher for ordinary work.

See [SECURITY.md](SECURITY.md) for the detailed trust boundary.

## Verify the result

Ask Codex to run, or run in its terminal:

```powershell
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
[pscustomobject]@{
    IsAdministrator = $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
    Integrity = (& "$env:SystemRoot\System32\whoami.exe" /groups |
        Select-String 'S-1-16-12288').Line
}
```

Expected: `IsAdministrator` is `True`, and the integrity SID is `S-1-16-12288` (High).

## Build from source

Requirements: Windows x64 and the .NET 8 SDK.

```powershell
.\build.ps1
```

The single-file executable and checksum are written to `dist/`.

## Compatibility

Tested with Windows 11 x64 and the Microsoft Store `OpenAI.Codex` package. Package versions are discovered dynamically and are not hard-coded. The launcher currently expects the package family suffix `2p2nqsd0c76g0` and an `app\ChatGPT.exe` entry point.

## Related work

- [Fightigertonight/Codex-Admin-Launcher](https://github.com/Fightigertonight/Codex-Admin-Launcher) is a more extensive PowerShell solution that also handles CLI relocation and package-context problems.
- [moligod/Codex-APP-CLI-Administrator](https://github.com/moligod/Codex-APP-CLI-Administrator) documents elevated PowerShell and scheduled-task approaches.
- [notyesbut/codex-desktop-autofix](https://github.com/notyesbut/codex-desktop-autofix) targets broader Codex Desktop MSIX repair scenarios.
- [openai/codex#28107](https://github.com/openai/codex/issues/28107) describes the Windows auto-de-elevation symptom and an external launcher workaround.
- Chromium and WebView2 also document the `do-not-de-elevate` launch switch in their Windows launch paths.

This project is independent and is not affiliated with or endorsed by OpenAI.

---

## 中文说明

这是一个体积很小、可审计的 Windows 单文件启动器，用于以管理员令牌启动 Microsoft Store / MSIX 版 ChatGPT 桌面应用。它会：

1. 每次正常请求 UAC；
2. 动态查找当前安装的 `OpenAI.Codex` x64 软件包；
3. 使用 `--do-not-de-elevate` 启动原始 `ChatGPT.exe`；
4. 等待 Chromium 完成启动后验证主进程仍是管理员令牌；
5. 在失败时显示错误并写入桌面日志。

它不会创建计划任务、服务或开机启动项，不会关闭正在运行的 ChatGPT，不会修改系统权限、软件包、环境变量或 Codex 配置。使用前请正常退出 ChatGPT，双击 Release 中的 EXE 并同意 UAC。
