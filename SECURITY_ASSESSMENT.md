# MakeMeAdmin -- Security Assessment Report

**Target:** `github.com/0xEuphonioux/MakeMeAdmin` (forked from pseymour/MakeMeAdmin)  
**Date:** 2026-07-17  
**Type:** Static code analysis -- C# .NET Windows privilege elevation service

---

## Executive Summary

Make Me Admin is a Windows service that allows standard users to temporarily elevate to Administrator. It uses WCF (named pipes locally, TCP remotely) to receive elevation requests. This assessment identifies 6 security findings ranging from default-allow elevation policies to cryptographic weaknesses.

**Overall risk: HIGH (7.8/10)**

---

## Findings

### VULN-01: Default Allow-All Elevation Policy (CVSS 8.4)

| Field | Value |
|-------|-------|
| **File** | `ProcessCommunication/AdminGroupManipulator.cs:244-253` |
| **CVSS** | **8.4** (AV:L/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H) |
| **Type** | Authorization bypass -- default allow-all |

**Finding:** When no allowed entities list is configured (the default), `UserIsAuthorized()` returns `true` for any user:

```csharp
// AdminGroupManipulator.cs line 244-252
if (allowedSidsList == null)
{ // The allowed list is null, meaning everyone is allowed administrator rights.
    return true;
}
```

An administrator who installs Make Me Admin without explicitly configuring `Allowed Entities` in the registry inadvertently gives every local user the ability to grant themselves Administrator rights on demand.

**Exploit path:**
1. Standard user runs the Make Me Admin client
2. Clicks "Grant Me Admin Rights"
3. User is added to `BUILTIN\Administrators` -- no authorization check fires

**Resolution:** Return `false` when `allowedSidsList` is null. Require explicit configuration. **Fixed in v2.5.0.**

---

### VULN-02: RequireAuthenticationForPrivileges Defaults to FALSE (CVSS 7.8)

| Field | Value |
|-------|-------|
| **File** | `Settings/Settings.cs:497-520` |
| **CVSS** | **7.8** (AV:L/AC:L/PR:L/UI:N/S:C/C:H/I:H/A:H) |
| **Type** | Missing authentication enforcement |

**Finding:** The `RequireAuthenticationForPrivileges` setting defaults to `false`. When false, Windows Hello verification is skipped entirely. A user with access to an unlocked session can elevate without any additional authentication challenge.

```csharp
// Settings.cs line 511-513
else
{ // Neither the policy nor the preference registry entries had a value.
    return false;  // DEFAULT: no additional auth required
}
```

**Resolution:** Default to `true`. Require verification by default. **Fixed in v2.5.0.**

---

### VULN-03: DPAPI LocalMachine Scope -- Any Process Can Decrypt User List (CVSS 6.2)

| Field | Value |
|-------|-------|
| **File** | `UserList/EncryptedSettings.cs:324,394` |
| **CVSS** | **6.2** (AV:L/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:N) |
| **Type** | Cryptographic weakness -- wrong protection scope |

**Finding:** The `users.xml` file is encrypted with `ProtectedData.Protect()` using `DataProtectionScope.LocalMachine`. Any process running on the same machine can decrypt the file.

```csharp
// EncryptedSettings.cs line 324 (encrypt)
byte[] ciphertextBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.LocalMachine);
```

**Resolution:** Use `DataProtectionScope.CurrentUser` with entropy. **Fixed in v2.5.0.**

---

### VULN-04: TCP Remote Endpoint Without Message-Level Authentication (CVSS 7.5)

| Field | Value |
|-------|-------|
| **File** | `Service/MakeMeAdminService.cs:354`, `RemoteUI/SubmitRequestForm.cs:132` |
| **CVSS** | **7.5** (AV:N/AC:H/PR:N/UI:N/S:U/C:H/I:H/A:H) |
| **Type** | Insufficient transport security |

**Finding:** The TCP binding uses `SecurityMode.Transport` which provides TLS encryption but no message-level signing.

**Resolution:** Use `SecurityMode.TransportWithMessageCredential`. **Fixed in v2.5.0.**

---

### VULN-05: BinaryFormatter Import -- Deprecated Deserialization Risk (CVSS 5.5)

| Field | Value |
|-------|-------|
| **File** | `Service/MakeMeAdminService.cs:29` |
| **CVSS** | **5.5** (AV:L/AC:L/PR:L/UI:N/S:U/C:N/I:H/A:N) |
| **Type** | Deprecated API -- potential deserialization vector |

**Finding:** `System.Runtime.Serialization.Formatters.Binary` was imported but unused. BinaryFormatter is deprecated and dangerous due to deserialization gadget chains.

**Resolution:** Remove the unused import. **Fixed in v2.5.0.**

---

### VULN-06: Users.xml Stored in World-Readable CommonApplicationData (CVSS 4.3)

| Field | Value |
|-------|-------|
| **File** | `UserList/EncryptedSettings.cs:86-88` |
| **CVSS** | **4.3** (AV:L/AC:L/PR:L/UI:N/S:U/C:L/I:N/A:N) |
| **Type** | Information disclosure -- file permissions |

**Finding:** The encrypted user list at `C:\ProgramData\Make Me Admin\users.xml` is readable by all authenticated users.

**Resolution:** Set restrictive ACLs on the file (SYSTEM and Administrators only). **Fixed in v2.5.0.**

---

## Attack Chain (Combined Exploit)

The most dangerous scenario combines these findings:

1. Admin installs Make Me Admin with defaults -- no allowed entities list configured
2. **VULN-01**: Any standard user elevates to Administrator on demand
3. **VULN-02**: No Windows Hello re-authentication needed -- walk-by attack possible
4. Attacker now has Administrator on the machine
5. **VULN-03**: Attacker decrypts `users.xml`, finds permanent admin SIDs, or modifies the file

**Total compromise time: approximately 30 seconds from an unlocked desktop.**

---

## Patch Status

All six findings were addressed in v2.5.0. See the [changelog](CHANGELOG.md) for details.

### Additional Hardening Applied

- Registry key ACL check to prevent standard users from modifying `Allowed Entities`
- Every elevation event logged to Windows Event Log and syslog
- SID-to-name resolution for Entra ID identities
- Unified single-line syslog format with structured fields

---

**Assessment:** eupho-RED | 2026-07-17
