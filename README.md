# Make Me Admin

<p align="center">
  <img src="SecurityLock.png" width="80" alt="Make Me Admin"><br>
  <strong>Temporary Administrator Privileges for Windows</strong><br>
  <em>Entra ID &bull; Syslog &bull; Biometric &bull; Enterprise-ready</em>
</p>

<p align="center">
  <a href="https://github.com/0xEuphonioux/MakeMeAdmin/releases/latest/download/Make.Me.Admin.2.5.7.x64.msi">
    <img src="https://img.shields.io/badge/⬇️_Download_Installer-v2.5.7-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Download Installer">
  </a>
</p>

<p align="center">
  <a href="https://github.com/0xEuphonioux/MakeMeAdmin/releases/latest">All releases</a> &bull;
  <a href="https://github.com/pseymour/MakeMeAdmin/wiki">Documentation</a> &bull;
  <a href="https://github.com/pseymour/MakeMeAdmin">Upstream</a>
</p>

---

## What is it?

Make Me Admin lets **standard users** temporarily elevate to administrator — without IT intervention. Think `sudo` for Windows.

- 🕐 **Auto-expires** — rights are removed after a configurable timeout
- 🔐 **Authenticated** — requires Windows password or biometric (Windows Hello)
- 📋 **Reason tracking** — optional/required justification with canned reasons
- 📡 **SIEM-ready** — forwards all events to syslog (Splunk, Sentinel, etc.)
- ☁️ **Entra ID / hybrid-join** — works with on-prem AD, Azure AD, and hybrid deployments

> **⚠️ This is a fork** of [pseymour/MakeMeAdmin](https://github.com/pseymour/MakeMeAdmin) with security hardening, Entra ID auth fixes, and syslog improvements.

---

## Quick Install

1. **[Download the MSI](https://github.com/0xEuphonioux/MakeMeAdmin/releases/latest/download/Make.Me.Admin.2.5.7.x64.msi)**
2. Run it **as Administrator**
3. That's it — the service starts automatically

### Upgrade

Install the new MSI over your existing version. All settings are preserved.

### Uninstall

From **Settings → Apps → Make Me Admin**, or silently:

```cmd
msiexec /x {GUID} /qn
```

---

## Configuration

Settings are managed via **Group Policy** or **registry**:

```
HKLM\SOFTWARE\Policies\Sinclair Community College\Make Me Admin\  (GPO)
HKLM\SOFTWARE\Sinclair Community College\Make Me Admin\           (Local)
```

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Admin Rights Timeout` | DWORD | 30 | Minutes before auto-revoke |
| `Prompt For Reason` | DWORD | 0 (None) | 0=None, 1=Optional, 2=Required |
| `Allow Free Text Reason` | DWORD | 1 (Yes) | Allow custom reason text |
| `Canned Reasons` | MULTI_SZ | — | Predefined reason dropdown options |
| `Require Authentication For Privileges` | DWORD | 1 (Yes) | Require password/biometric |
| `Remove Admin Rights On Logout` | DWORD | 1 (Yes) | Auto-revoke on sign-out |
| `Log Elevated Processes` | DWORD | 0 (No) | Log when admin processes launch |
| `Syslog Servers` | — | — | Configure syslog forwarding |

See the [upstream wiki](https://github.com/pseymour/MakeMeAdmin/wiki) for full documentation.

---

## Version History

### v2.5.4 (latest)
- 🔐 **CRITICAL**: Fixed auth bypass on Entra ID-joined devices (wrong passwords could grant admin)
- 🛡️ Credential blob sanitization — CloudAP tokens never leak to logs
- 🛠️ Cancel on reason prompt now properly cancels admin request
- 📦 MSI `DisplayVersion` fixed (was stuck at 2.5.0)

### v2.5.0
- Entra ID / hybrid-join detection (`NetGetAadJoinInformation`)
- CloudAP token handling for Windows Hello / biometric auth
- UPN normalization — works with any Entra ID tenant
- Syslog forwarding for elevated process events
- GitHub Actions CI/CD for MSI builds

### v2.4.2
- Syslog forwarding infrastructure

---

## Building from Source

```cmd
git clone https://github.com/0xEuphonioux/MakeMeAdmin.git
cd MakeMeAdmin
# Open in Visual Studio, restore NuGet packages, build
# Or trigger the GitHub Actions workflow
```

Requires: Visual Studio 2022+, WiX Toolset v4, .NET Framework 4.8.

---

<p align="center">
  <sub>Forked from <a href="https://github.com/pseymour/MakeMeAdmin">pseymour/MakeMeAdmin</a> &bull; Licensed under GPL v3</sub>
</p>
