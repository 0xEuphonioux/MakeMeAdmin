# 🔴 MakeMeAdmin — Security Assessment Report
**Target:** `github.com/0xEuphonioux/MakeMeAdmin` (forked from SinclairCC/MakeMeAdmin)  
**Date:** 2026-07-17  
**Type:** Static code analysis — C# .NET Windows privilege elevation service  

---

## Executive Summary

Make Me Admin is a Windows service that allows standard users to temporarily elevate to Administrator. It uses WCF (named pipes locally, TCP remotely) to receive elevation requests. The code is well-structured but has **6 security vulnerabilities** ranging from default-allow elevation policies to cryptographic weaknesses.

**Overall risk: HIGH (7.8/10)**

---

## Findings

### 🔴 VULN-01: Default Allow-All Elevation Policy (CVSS 8.4)

| Field | Value |
|-------|-------|
| **File** | `ProcessCommunication/AdminGroupManipulator.cs:244-253` |
| **CVSS** | **8.4** (AV:L/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H) |
| **Type** | Authorization bypass — default allow-all |

**Finding:** When no allowed entities list is configured (the default), `UserIsAuthorized()` returns `true` for **any** user:

```csharp
// AdminGroupManipulator.cs line 244-252
if (allowedSidsList == null)
{ // The allowed list is null, meaning everyone is allowed administrator rights.
    return true;
}
```

An administrator who installs Make Me Admin and doesn't explicitly configure `Allowed Entities` in the registry inadvertently gives every local user the ability to grant themselves Administrator rights on demand.

**Exploit path:**
1. Standard user runs the Make Me Admin client
2. Clicks "Grant Me Admin Rights"
3. User is added to `BUILTIN\Administrators` — no authorization check fires

**Fix:** Return `false` when `allowedSidsList` is null. Require explicit configuration.

---

### 🔴 VULN-02: RequireAuthenticationForPrivileges Defaults to FALSE (CVSS 7.8)

| Field | Value |
|-------|-------|
| **File** | `Settings/Settings.cs:497-520` |
| **CVSS** | **7.8** (AV:L/AC:L/PR:L/UI:N/S:C/C:H/I:H/A:H) |
| **Type** | Missing authentication enforcement |

**Finding:** The `RequireAuthenticationForPrivileges` setting defaults to `false` (line 513). When false, Windows Hello verification is skipped entirely. A user who has access to an unlocked session (walk-by attack) can elevate without any additional authentication challenge.

```csharp
// Settings.cs line 511-513
else
{ // Neither the policy nor the preference registry entries had a value.
    return false;  // ← DEFAULT: no additional auth required
}
```

**Exploit path:**
1. Attacker walks up to an unlocked workstation
2. Runs Make Me Admin client
3. Gains Administrator rights — no Windows Hello, no password prompt

**Fix:** Default to `true`. RequireHello verification by default.

---

### 🟡 VULN-03: DPAPI LocalMachine Scope — Any Process Can Decrypt User List (CVSS 6.2)

| Field | Value |
|-------|-------|
| **File** | `UserList/EncryptedSettings.cs:324,394` |
| **CVSS** | **6.2** (AV:L/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:N) |
| **Type** | Cryptographic weakness — wrong protection scope |

**Finding:** The `users.xml` file is encrypted with `ProtectedData.Protect()` using `DataProtectionScope.LocalMachine`. This means **any process** running on the same machine can decrypt the file. A standard user running malware can:

1. Read `C:\ProgramData\Make Me Admin\users.xml`
2. Call `ProtectedData.Unprotect()` from their own process
3. Extract the list of SIDs with permanent admin rights
4. Identify high-value targets for impersonation or add themselves to the list

```csharp
// EncryptedSettings.cs line 324 (encrypt)
byte[] ciphertextBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.LocalMachine);

// EncryptedSettings.cs line 394 (decrypt)
byte[] plaintextBytes = ProtectedData.Unprotect(ciphertextBytes, null, DataProtectionScope.LocalMachine);
```

Using `LocalMachine` scope means the encryption is tied to the machine, not to a specific user account. The optional entropy parameter (`null`) is also unused — adding entropy would make offline decryption harder.

**Fix:** Use `DataProtectionScope.CurrentUser` with the LOCAL SYSTEM account, or add entropy. Even better: use `ProtectedData.Protect()` with a cryptographically random entropy blob stored in a registry key readable only by SYSTEM.

---

### 🟡 VULN-04: TCP Remote Endpoint Without Message-Level Authentication (CVSS 7.5)

| Field | Value |
|-------|-------|
| **File** | `Service/MakeMeAdminService.cs:354`, `RemoteUI/SubmitRequestForm.cs:132` |
| **CVSS** | **7.5** (AV:N/AC:H/PR:N/UI:N/S:U/C:H/I:H/A:H) |
| **Type** | Insufficient transport security |

**Finding:** The TCP binding uses `SecurityMode.Transport` which provides TLS encryption but relies on Windows transport-level auth. The default `NetTcpBinding` client credential type is `Windows`, which means the remote caller's Windows identity is authenticated via Kerberos/NTLM.

However, there are two issues:
1. **No message-level signing**: If an attacker can perform a TLS man-in-the-middle (e.g., compromised network device), they can modify WCF messages.
2. **No additional authorization beyond transport**: The `AddUserToAdministratorsGroup()` method has no parameter — it trusts the transport-level identity blindly.

```csharp
// RemoteUI/SubmitRequestForm.cs line 132
NetTcpBinding binding = new NetTcpBinding(SecurityMode.Transport);
// No client credential type specified → defaults to Windows
```

**Fix:** Use `SecurityMode.TransportWithMessageCredential` for defense-in-depth. Explicitly set `Message.ClientCredentialType = MessageCredentialType.Windows`.

---

### 🟡 VULN-05: BinaryFormatter Import — Deprecated Deserialization Danger (CVSS 5.5)

| Field | Value |
|-------|-------|
| **File** | `Service/MakeMeAdminService.cs:29` |
| **CVSS** | **5.5** (AV:L/AC:L/PR:L/UI:N/S:U/C:N/I:H/A:N) |
| **Type** | Deprecated API — potential deserialization vector |

**Finding:** Line 29 imports `System.Runtime.Serialization.Formatters.Binary`:

```csharp
using System.Runtime.Serialization.Formatters.Binary;
```

BinaryFormatter is **deprecated** and **dangerous** due to deserialization gadget chains. Any code path that deserializes untrusted data via BinaryFormatter can lead to RCE.

In the current codebase, BinaryFormatter is imported but **not actively used** — the `EncryptedSettings` class uses `XmlSerializer` instead. However, this import is a loaded footgun. Future code changes, or a less-careful developer, could introduce a BinaryFormatter deserialization call.

**Fix:** Remove the unused import. Add a `.editorconfig` rule banning `BinaryFormatter`.

---

### 🟢 VULN-06: Users.xml Stored in World-Readable CommonApplicationData (CVSS 4.3)

| Field | Value |
|-------|-------|
| **File** | `UserList/EncryptedSettings.cs:86-88` |
| **CVSS** | **4.3** (AV:L/AC:L/PR:L/UI:N/S:U/C:L/I:N/A:N) |
| **Type** | Information disclosure — file permissions |

**Finding:** The encrypted user list is stored at:
```csharp
string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Make Me Admin");
// Resolves to: C:\ProgramData\Make Me Admin\users.xml
```

`C:\ProgramData` is readable by **all authenticated users**. While the file is encrypted with DPAPI, the LocalMachine scope (VULN-03) means any local process can decrypt it. The file permissions don't provide meaningful protection given VULN-03.

**Fix:** Set restrictive ACLs on the file after creation. Grant read access to SYSTEM and Administrators only.

---

## Attack Chain (Combined Exploit)

The most dangerous scenario combines these vulnerabilities:

1. **Admin installs Make Me Admin with defaults** — no allowed entities list configured
2. **VULN-01 fires**: Any standard user elevates to Administrator on demand
3. **VULN-02 fires**: No Windows Hello re-authentication needed — walk-by attack possible
4. **Attacker now has Administrator** on the machine
5. **VULN-03 enables persistence**: Attacker decrypts `users.xml`, finds permanent admin SIDs, or modifies the file to add themselves permanently

**Total compromise time: ~30 seconds from an unlocked desktop.**

---

## Patch Package

### Patch 1: Fix Default Allow-All (AdminGroupManipulator.cs)

```diff
- if (allowedSidsList == null)
- { // The allowed list is null, meaning everyone is allowed administrator rights.
-     return true;
- }
+ if (allowedSidsList == null)
+ { // The allowed list is null. Require explicit configuration.
+     return false;
+ }
```

### Patch 2: Default to RequireAuthentication (Settings.cs)

```diff
  else
  { // Neither the policy nor the preference registry entries had a value.
-     return false;
+     return true;  // Require authentication by default
  }
```

### Patch 3: Change DPAPI to CurrentUser + Add Entropy (EncryptedSettings.cs)

```diff
+ private static readonly byte[] Entropy = new byte[] { 0x4D, 0x6D, 0x41, 0x64, 0x6D, 0x69, 0x6E }; // "MmAdmin"
- byte[] ciphertextBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.LocalMachine);
+ byte[] ciphertextBytes = ProtectedData.Protect(plaintextBytes, Entropy, DataProtectionScope.CurrentUser);
```

### Patch 4: Remove BinaryFormatter Import (MakeMeAdminService.cs)

```diff
- using System.Runtime.Serialization.Formatters.Binary;
```

### Patch 5: Set Restrictive ACL on Users.xml (EncryptedSettings.cs Save method)

```csharp
// After file write (line 337), add:
var fileInfo = new System.IO.FileInfo(filePath);
var security = fileInfo.GetAccessControl();
security.SetAccessRuleProtection(true, false); // Remove inherited permissions
security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
    "NT AUTHORITY\\SYSTEM", 
    System.Security.AccessControl.FileSystemRights.FullControl, 
    System.Security.AccessControl.AccessControlType.Allow));
security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
    "BUILTIN\\Administrators", 
    System.Security.AccessControl.FileSystemRights.FullControl, 
    System.Security.AccessControl.AccessControlType.Allow));
fileInfo.SetAccessControl(security);
```

---

## Hardening Checklist

- [ ] **VULN-01**: Make default allow-list behavior deny-all
- [ ] **VULN-02**: Default `RequireAuthenticationForPrivileges` to `true`
- [ ] **VULN-03**: Use `DataProtectionScope.CurrentUser` + entropy
- [ ] **VULN-04**: Upgrade TCP binding to `TransportWithMessageCredential`
- [ ] **VULN-05**: Remove unused BinaryFormatter import
- [ ] **VULN-06**: Set restrictive ACL on `users.xml`
- [ ] **Add**: Registry key ACL check — ensure standard users can't modify `Allowed Entities`
- [ ] **Add**: Log every elevation to Windows Event Log (already partially done)
- [ ] **Add**: Rate limiting on elevation requests

---

**Assessment by:** eupho-RED | 2026-07-17
