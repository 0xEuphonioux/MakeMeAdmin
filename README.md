# Make Me Admin

> **⚠️ This is a fork of [pseymour/MakeMeAdmin](https://github.com/pseymour/MakeMeAdmin) maintained by [0xEuphonioux](https://github.com/0xEuphonioux).**
>
> This fork focuses on **syslog logging improvements** and **Entra ID (Azure AD) authentication fixes** for modern enterprise environments.

Make Me Admin is a simple application for Windows that allows standard user accounts to be elevated to administrator-level, on a temporary basis.

You can find documentation in the [wiki](https://github.com/pseymour/MakeMeAdmin/wiki).

---

## Changes in this fork

### Syslog forwarding (v2.4.2+)
- Elevated process detection events are now forwarded to syslog via `ApplicationLog.WriteEvent()`
- Events flow through `Applications and Services Logs/MakeMeAdmin/Operational` to your SIEM
- Same structured format as SAP Privileges for macOS

### Entra ID authentication fixes (v2.4.3–v2.4.13)
- **Entra-joined devices**: Detects Entra ID / hybrid join via `NetGetAadJoinInformation` and skips `LogonUser` when appropriate
- **CloudAP tokens**: Handles biometric (Windows Hello) authentication by detecting `@@`-prefixed CloudAP token credentials
- **UPN normalization**: Strips `DOMAIN\` prefix and `@company.com` suffix from credential usernames generically — works for any Entra ID tenant, not just a single organization
- Fixes the authentication loop where Entra-joined users with biometric/UPN were repeatedly prompted for credentials

### Build improvements
- GitHub Actions CI/CD workflow for MSI builds (`build-msi.yml`)
- WiX v3 + WiX v4 toolchain coexistence
- Version bumped to 2.4.13

---

## Original README

The original upstream project is at [pseymour/MakeMeAdmin](https://github.com/pseymour/MakeMeAdmin).
