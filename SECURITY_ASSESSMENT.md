# MakeMeAdmin -- Security Hardening Report

**Target:** `github.com/0xEuphonioux/MakeMeAdmin` (forked from pseymour/MakeMeAdmin)  
**Date:** 2026-07-17  
**Type:** Security hardening review — C# .NET Windows privilege elevation service

---

## Executive Summary

Make Me Admin is a Windows service that allows standard users to temporarily elevate to Administrator. It uses WCF (named pipes locally, TCP remotely) to receive elevation requests. This report documents six security hardening measures applied to the fork to strengthen default configurations and reduce attack surface. All changes were incorporated in v2.5.0.

---

## Security Hardening Measures

### Default-Deny Elevation Policy

| Field | Value |
|-------|-------|
| **File** | `ProcessCommunication/AdminGroupManipulator.cs` |
| **Area** | Authorization |

**Before:** When no allowed entities list was configured, `UserIsAuthorized()` returned `true` for any user — an implicit allow-all default.

**Fix:** Changed to a default-deny posture. `UserIsAuthorized()` now returns `false` when no allowed entities are explicitly configured. Remote administration requires an explicit allow-list.

---

### Authentication Required by Default

| Field | Value |
|-------|-------|
| **File** | `Settings/Settings.cs` |
| **Area** | Authentication enforcement |

**Before:** The `RequireAuthenticationForPrivileges` setting defaulted to `false`. Windows Hello verification was skipped unless explicitly enabled by the administrator.

**Fix:** The setting now defaults to `true`. Windows Hello (PIN, fingerprint, facial recognition) or password verification is required for every elevation request by default.

---

### DPAPI Hardening — CurrentUser Scope with Entropy

| Field | Value |
|-------|-------|
| **File** | `UserList/EncryptedSettings.cs` |
| **Area** | Cryptographic storage |

**Before:** The `users.xml` file was encrypted with `ProtectedData.Protect()` using `DataProtectionScope.LocalMachine`, allowing any process on the machine to decrypt it.

**Fix:** Changed to `DataProtectionScope.CurrentUser` with an entropy salt, binding decryption to the specific user account that performed the encryption.

---

### WCF Transport Upgrade

| Field | Value |
|-------|-------|
| **File** | `Service/MakeMeAdminService.cs`, `RemoteUI/SubmitRequestForm.cs` |
| **Area** | Transport security |

**Before:** The TCP endpoint used `SecurityMode.Transport` providing TLS encryption without message-level signing.

**Fix:** Upgraded to `SecurityMode.TransportWithMessageCredential`, adding message-level authentication on top of transport encryption.

---

### BinaryFormatter Import Removed

| Field | Value |
|-------|-------|
| **File** | `Service/MakeMeAdminService.cs` |
| **Area** | Code hygiene |

**Before:** `System.Runtime.Serialization.Formatters.Binary` was imported. This API is deprecated and carries deserialization risks.

**Fix:** The unused import was removed.

---

### Restrictive ACL on User Data File

| Field | Value |
|-------|-------|
| **File** | `UserList/EncryptedSettings.cs` |
| **Area** | File system permissions |

**Before:** The encrypted user list at `C:\ProgramData\Make Me Admin\users.xml` was readable by all authenticated users.

**Fix:** Restrictive ACLs applied — only SYSTEM and Administrators have access.

---

## Additional Hardening Applied

- Registry key ACL check to prevent standard users from modifying `Allowed Entities`
- Every elevation event logged to Windows Event Log and syslog
- SID-to-name resolution for Entra ID identities
- Unified single-line syslog format with structured fields for SIEM integration

---

All measures were incorporated in v2.5.0. See the [changelog](CHANGELOG.md) for the full version history.

**Review:** eupho-RED | 2026-07-17