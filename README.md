# ChatGPT Windows Admin Launcher / ChatGPT Windows 管理员启动器

[English](#english) | [中文](#中文)

---

## English

### Overview

ChatGPT Windows Admin Launcher is a small, auditable launcher for the Microsoft Store / MSIX ChatGPT desktop app (`OpenAI.Codex`). It starts the original app with an elevated Windows token, passes Chromium's `--do-not-de-elevate` switch, and verifies that the launched main process remains elevated.

### Why this is needed

On some Windows builds, Chromium may restart the ChatGPT main process with a standard token after it was launched from an elevated process. ChatGPT then appears to have been started as administrator while its local Codex command process still lacks administrator access.

OpenAI's [Windows app documentation](https://learn.chatgpt.com/zh-Hans/docs/windows/windows-app) states that Codex inherits administrator rights when the ChatGPT desktop app itself is started as administrator. This launcher preserves that elevated launch on versions where Chromium's automatic de-elevation otherwise intervenes.

`--do-not-de-elevate` is an implementation-level Chromium switch, not a documented OpenAI compatibility contract. The launcher checks the resulting process token and displays a persistent error if a future update changes the behavior.

### Download and use

1. Download `ChatGPT-Admin-Launcher-win-x64.exe` and `SHA256SUMS.txt` from the [latest GitHub Release](https://github.com/bianwuqiong/chatgpt-windows-admin-launcher/releases/latest).
2. Exit every running ChatGPT desktop process normally.
3. Double-click the launcher and approve the Windows UAC prompt.
4. The launcher locates the newest installed x64 `OpenAI.Codex` package and starts its original `ChatGPT.exe`.

The release binary is unsigned, so Windows displays an unknown publisher in UAC. Compare the executable's SHA-256 hash with `SHA256SUMS.txt` from the same Release.

### Security properties

- Requests standard UAC consent on every launch.
- Does not create a scheduled task, service, startup entry, or UAC bypass.
- Does not read credentials, contact the network, or collect telemetry.
- Does not change `WindowsApps` permissions, environment variables, ChatGPT settings, or Codex sandbox settings.
- Does not terminate an existing ChatGPT session. It asks the user to exit first.
- Verifies the launched process token after Chromium has had time to complete its normal startup handoff.
- Writes `ChatGPT-Admin-Launcher.log` to the desktop. The log contains timestamps, the installed package path, process IDs, and launch results; it contains no credentials.

Running ChatGPT with an administrator token increases the impact of local commands that the user authorizes. Use this launcher only for tasks that require administrator access, and use the normal app launcher for ordinary work.

See [SECURITY.md](SECURITY.md) for the detailed trust boundary.

### Verify the result

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

Expected result: `IsAdministrator` is `True`, and the integrity SID is `S-1-16-12288` (High).

### Build from source

Requirements: Windows x64 and the .NET 8 SDK.

```powershell
.\build.ps1
```

The single-file executable and checksum are written to `dist/`.

Run the static security and privacy tests with:

```powershell
python -m unittest discover -s tests -v
```

### Compatibility

The launcher has been tested with Windows 11 x64 and the Microsoft Store `OpenAI.Codex` package. Package versions are discovered dynamically and are not hard-coded. It currently expects the package family suffix `2p2nqsd0c76g0` and an `app\ChatGPT.exe` entry point.

Future ChatGPT, Chromium, MSIX, or Windows updates may change the package layout or de-elevation behavior. The post-launch token check is designed to fail visibly rather than silently report success.

### Related work

- [Fightigertonight/Codex-Admin-Launcher](https://github.com/Fightigertonight/Codex-Admin-Launcher) is a more extensive PowerShell solution that also handles CLI relocation and package-context problems.
- [moligod/Codex-APP-CLI-Administrator](https://github.com/moligod/Codex-APP-CLI-Administrator) documents elevated PowerShell and scheduled-task approaches.
- [notyesbut/codex-desktop-autofix](https://github.com/notyesbut/codex-desktop-autofix) targets broader Codex Desktop MSIX repair scenarios.
- [openai/codex#28107](https://github.com/openai/codex/issues/28107) describes the Windows auto-de-elevation symptom and an external launcher workaround.
- Chromium and WebView2 also document the `do-not-de-elevate` switch in their Windows launch paths.

This project is independent and is not affiliated with or endorsed by OpenAI.

---

## 中文

### 项目简介

ChatGPT Windows 管理员启动器是一个体积很小、便于审计的启动程序，适用于 Microsoft Store / MSIX 版 ChatGPT 桌面应用（软件包名为 `OpenAI.Codex`）。它会使用 Windows 管理员令牌启动原始应用，传入 Chromium 的 `--do-not-de-elevate` 参数，并验证启动后的主进程是否仍保持管理员权限。

### 为什么需要这个启动器

在部分 Windows 版本中，即使从已提权进程启动 ChatGPT，Chromium 仍可能自动使用普通用户令牌重启主进程。结果是 ChatGPT 看起来经过了“以管理员身份运行”，但其内部 Codex 命令进程仍然没有管理员权限。

OpenAI 的 [Windows 应用文档](https://learn.chatgpt.com/zh-Hans/docs/windows/windows-app)说明：当 ChatGPT 桌面应用本身以管理员身份启动时，Codex 会继承该权限。本启动器用于阻止 Chromium 在启动阶段自动降权，从而保留这一管理员启动链。

`--do-not-de-elevate` 是 Chromium 的实现级参数，并非 OpenAI 承诺长期兼容的公开接口。启动器会检查最终进程令牌；如果未来更新改变了该行为，它会显示持续可见的错误，而不会静默报告成功。

### 下载与使用

1. 从[最新 GitHub Release](https://github.com/bianwuqiong/chatgpt-windows-admin-launcher/releases/latest)下载 `ChatGPT-Admin-Launcher-win-x64.exe` 和 `SHA256SUMS.txt`。
2. 正常退出所有正在运行的 ChatGPT 桌面进程。
3. 双击启动器，并在 Windows UAC 窗口中选择“是”。
4. 启动器会自动查找当前已安装的最新版 x64 `OpenAI.Codex` 软件包，并启动其中的原始 `ChatGPT.exe`。

Release 中的程序未进行代码签名，因此 UAC 会显示“未知发布者”。请使用同一 Release 中的 `SHA256SUMS.txt` 核对程序的 SHA-256。

### 安全边界

- 每次启动都通过标准 Windows UAC 请求用户确认。
- 不创建计划任务、系统服务、开机启动项或 UAC 绕过机制。
- 不读取账户凭据，不访问网络，不收集遥测。
- 不修改 `WindowsApps` 权限、环境变量、ChatGPT 设置或 Codex 沙箱设置。
- 不强制结束正在运行的 ChatGPT，而是提示用户先正常退出。
- 在 Chromium 完成正常启动交接后，再检查最终进程令牌。
- 在桌面写入 `ChatGPT-Admin-Launcher.log`。日志只包含时间、软件包路径、进程 ID 和启动结果，不包含凭据。

使用管理员令牌运行 ChatGPT，会提高用户所授权本地命令的系统影响范围。仅在确实需要管理员权限的任务中使用本启动器；普通工作建议使用正常的 ChatGPT 启动方式。

详细信任边界参见 [SECURITY.md](SECURITY.md)。

### 验证权限

可以让 Codex 执行以下命令，也可以在其终端中手动运行：

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

预期结果：`IsAdministrator` 为 `True`，完整性 SID 为 `S-1-16-12288`（High）。

### 从源码构建

环境要求：Windows x64 和 .NET 8 SDK。

```powershell
.\build.ps1
```

生成的单文件程序和校验文件位于 `dist/`。

运行静态安全与隐私测试：

```powershell
python -m unittest discover -s tests -v
```

### 兼容性

本启动器已在 Windows 11 x64 和 Microsoft Store `OpenAI.Codex` 软件包上完成测试。程序会动态查找软件包版本，不写死具体版本号。目前要求软件包族后缀为 `2p2nqsd0c76g0`，入口程序为 `app\ChatGPT.exe`。

未来的 ChatGPT、Chromium、MSIX 或 Windows 更新可能改变软件包结构或自动降权行为。启动后的令牌检查会在不兼容时明确报错，避免产生“看似成功、实际未提权”的结果。

### 同类项目

- [Fightigertonight/Codex-Admin-Launcher](https://github.com/Fightigertonight/Codex-Admin-Launcher)：功能更完整的 PowerShell 方案，还处理 CLI 搬运和软件包上下文问题。
- [moligod/Codex-APP-CLI-Administrator](https://github.com/moligod/Codex-APP-CLI-Administrator)：记录管理员 PowerShell 和最高权限计划任务方案。
- [notyesbut/codex-desktop-autofix](https://github.com/notyesbut/codex-desktop-autofix)：面向更广泛的 Codex Desktop MSIX 修复场景。
- [openai/codex#28107](https://github.com/openai/codex/issues/28107)：记录 Windows 自动降权现象和外部启动器解决思路。
- Chromium 和 WebView2 的 Windows 启动路径中也有 `do-not-de-elevate` 参数的相关说明。

本项目为独立社区项目，与 OpenAI 无隶属关系，也未获得 OpenAI 官方背书。
