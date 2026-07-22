# Make Me Admin

**Temporary Administrator Privileges for Windows**

*Entra ID | Syslog | Biometric | Enterprise-ready*

[Download Installer (v2.5.0)](https://github.com/0xEuphonioux/MakeMeAdmin/releases/latest/download/Make.Me.Admin.2.5.0.x64.msi)

[All releases](https://github.com/0xEuphonioux/MakeMeAdmin/releases/latest) | [Documentation](https://github.com/pseymour/MakeMeAdmin/wiki) | [Upstream](https://github.com/pseymour/MakeMeAdmin)

---

## Overview

Make Me Admin allows standard users to temporarily elevate to administrator without IT intervention. Similar to `sudo` on Unix systems.

- **Auto-expiring** -- rights are revoked after a configurable timeout
- **Authenticated** -- Windows Hello (PIN, fingerprint, face) or password verification
- **Reason tracking** -- optional or required justification with predefined and free-text reasons
- **SIEM integration** -- all elevation events forwarded to syslog (Splunk, Microsoft Sentinel, etc.)
- **Entra ID and hybrid-join** -- compatible with on-premises AD, Entra ID, and hybrid deployments

> This is a fork of [pseymour/MakeMeAdmin](https://github.com/pseymour/MakeMeAdmin) with security hardening, Entra ID authentication fixes, Windows Hello support, SID resolution improvements, and unified syslog event logging.

---

## Quick Install

1. [Download the MSI](https://github.com/0xEuphonioux/MakeMeAdmin/releases/latest/download/Make.Me.Admin.2.5.0.x64.msi)
2. Run the installer as Administrator
3. The service starts automatically on install

### Upgrade

Install the new MSI over your existing version. All registry settings and configuration are preserved.

### Uninstall

From **Settings > Apps > Make Me Admin**, or silently:

```cmd
msiexec /x {PRODUCT-GUID} /qn
```

---

## Configuration

Settings are applied via **Group Policy** or **registry**:

```
HKLM\SOFTWARE\Policies\Sinclair Community College\Make Me Admin\  (GPO)
HKLM\SOFTWARE\Sinclair Community College\Make Me Admin\           (Local)
```

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Admin Rights Timeout` | DWORD | 30 | Minutes before automatic revocation |
| `Prompt For Reason` | DWORD | 0 (None) | 0=None, 1=Optional, 2=Required |
| `Allow Free Text Reason` | DWORD | 1 (Yes) | Allow custom reason text |
| `Canned Reasons` | MULTI_SZ | -- | Predefined reason dropdown values |
| `Require Authentication For Privileges` | DWORD | 1 (Yes) | Require password or biometric verification |
| `Allow Windows Hello Authentication` | DWORD | 1 (Yes) | Enable PIN, fingerprint, or facial recognition |
| `Remove Admin Rights On Logout` | DWORD | 1 (Yes) | Revoke rights on user sign-out |
| `Log Elevated Processes` | DWORD | 0 (No) | Log elevation events for launched processes |

Full documentation is available in the [upstream wiki](https://github.com/pseymour/MakeMeAdmin/wiki).

---

## Version History

### v2.5.0

**Authentication and Identity**
- Windows Hello support -- PIN, fingerprint, or facial recognition via `UserConsentVerifier` API
- Entra ID / hybrid-join detection via `NetGetAadJoinInformation`
- UPN normalization for hybrid-joined devices
- SID-to-name resolution with 5-tier fallback, including Entra ID IdentityStore cache lookup
- Unified single-line syslog format with structured `SID=` field

**Security Fixes**
- Default-deny policy: remote administration requires explicit allow-list
- Authentication required by default (`RequireAuthentication` defaults to `true`)
- DPAPI hardening: encrypted settings use `CurrentUser` scope with entropy salt
- WCF transport upgrade: `TransportWithMessageCredential` replaces plain TCP binding
- BinaryFormatter import removed
- Restrictive ACL applied to user data file

**Operations and CI**
- Syslog forwarding for all elevation and process events
- Credential data excluded from all log output
- GitHub Actions CI/CD for automated MSI builds

### v2.4.2
- Syslog forwarding infrastructure

---

## Building from Source

```cmd
git clone https://github.com/0xEuphonioux/MakeMeAdmin.git
cd MakeMeAdmin
```

Open `MakeMeAdmin.sln` in Visual Studio 2022 or later. Restore NuGet packages and build the solution.

Requirements: Visual Studio 2022+, WiX Toolset v3 and v4, .NET Framework 4.8.

---

Forked from [pseymour/MakeMeAdmin](https://github.com/pseymour/MakeMeAdmin) | Licensed under GPL v3
