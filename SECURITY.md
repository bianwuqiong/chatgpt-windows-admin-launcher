# Security

## Trust boundary

The launcher itself requests an administrator token through the standard Windows UAC flow. It then starts the installed ChatGPT desktop executable with the same token. It does not bypass UAC or grant privileges to an already running process.

The launched ChatGPT/Codex local execution chain can perform administrator-level actions when permitted by the user's product, workspace, sandbox, and approval settings. Elevation does not replace those controls, but it increases the operating-system impact of commands that are allowed to run.

## Deliberately excluded behavior

The launcher does not:

- accept, store, or transmit passwords or tokens;
- make network requests;
- install a service or scheduled task;
- add startup persistence;
- disable UAC or Chromium sandboxing;
- modify ACLs under `C:\Program Files\WindowsApps`;
- modify ChatGPT, Codex, or Windows security settings;
- terminate an existing ChatGPT session.

## Local log

`ChatGPT-Admin-Launcher.log` is written to the current user's desktop. It records timestamps, the selected local package path, process IDs, and success or failure details. Review the log before sharing it because local paths can reveal an account folder name on some configurations.

## Release verification

Release binaries are unsigned. Verify the SHA-256 value against `SHA256SUMS.txt`, or build from source with the .NET 8 SDK.

## Reporting a vulnerability

Please use GitHub's private vulnerability reporting for this repository when available. Do not include passwords, access tokens, private logs, or other sensitive data in a public issue.
