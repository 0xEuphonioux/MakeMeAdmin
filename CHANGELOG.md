# Changelog

All notable changes to this project are documented in this file.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

https://github.com/0xEuphonioux/MakeMeAdmin/commits/master

## [2.5.0] - 2025-06-22

### Added

- Windows Hello support -- PIN, fingerprint, or facial recognition via `UserConsentVerifier` API (TPM-backed).
- Entra ID / hybrid-join detection -- `NetGetAadJoinInformation` for cloud-domain authentication.
- UPN normalization -- works with any Entra ID tenant, including hybrid-joined devices.
- Syslog forwarding for elevated process events (Splunk, Sentinel, etc.).
- Allow Windows Hello Authentication policy setting (DWORD, default: enabled).
- SID-to-name resolution with 5-tier fallback for Entra ID group and role SIDs.
- Unified single-line syslog format with structured `SID=` field for Graylog and SIEM integration.

### Changed

- Default-deny policy -- remote administration now denies by default; explicit allow-list required.
- Authentication required by default -- `RequireAuthentication` defaults to `true`.
- DPAPI hardening -- encrypted settings use `CurrentUser` scope with entropy salt (was `LocalMachine`).
- WCF transport upgrade -- `TransportWithMessageCredential` binding replaces plain TCP.
- MSI `DisplayVersion` now correctly reflects the installed version.

### Removed

- BinaryFormatter -- eliminated unsafe deserialization surface.

### Fixed

- Authentication bypass on Entra ID-joined devices (invalid credentials could grant admin rights).
- UPN suffix mismatch on hybrid-joined devices.
- Credential dialog re-prompt loop on password mismatch.
- Empty username from silent CloudAP auto-auth now properly rejected.
- Cancel on reason prompt now correctly cancels admin request.

### Security

- User data ACL hardening -- restrictive DACL applied to `users.xml` (Authenticated Users read, Administrators full control).
- Credential data excluded from all log output.
- GitHub Actions CI/CD for automated MSI builds.

## [2.4.1] - 2025-11-13

### Added

- German localization.

### Removed

- References to Multilingual App Toolkit (out of support).

### Fixed

- Remote UI now displays German translations correctly.

## [2.4.0] - 2025-10-31

### Added

- Optional dialog prompt for elevation reason.
- User logoff on admin rights expiration with customizable message.
- Elevated process logging.
- Authentication prompt before rights grant.
- Configurable remote feature port.
- Rights renewal after expiration.
- UI close-after-expiry setting.
- Danish translation (Bjorn Kelsen).
- Installer removes added user XML file on uninstall.
- Product version written to registry at `HKEY_LOCAL_MACHINE\SOFTWARE\Sinclair Community College\Make Me Admin\InstalledVersion`.

### Changed

- Significant performance improvement in allowed/denied list checks (Martin Sheppard).
- Event Log source renamed to "Make Me Admin".
- Group policy templates updated to reflect SID or name support (Jakob Dahl).
- Migrated from .NET Framework 4.5.2 to 4.8.

### Removed

- Exit button from UI.
- Automatic logging for service start and stop events.

### Fixed

- Syslog DNS resolution failure.
- Authorization check using wrong principal in automatic-add scenarios (Issue #50).
- Typos in French translation.
- Improved error handling when service is not listening.

## [2.3-fr] - 2019-02-04

### Changed

- French localization changed from fr-CA to fr.

## [2.3] - 2019-01-31

### Added

- French localization (Etienne Croteau).
- Service failure recovery in installer (restart on first/second/third failure).
- Syslog functionality.
- Encrypted added user file.
- GPLv3 license.
- Automatic user add on logon.
- Administrator group membership check in UI.
- Name-based entity support (in addition to SIDs).

### Changed

- Encrypted user list moved from CommonApplicationData to SYSTEM account Application Data.
- Updated non-expiring rights handling.
- Simplified logging API.
- Default admin rights timeout changed to 10 minutes.
- Local Administrators group membership check updated.

### Fixed

- Added explanatory text in ADML for syslog settings.

## [2.2.1] - 2018-01-18

### Added

- WCF security context for user identity in add/remove operations.
- Remote request application with separate allow/deny lists and session management.

### Changed

- Added user SID list now encrypted.

### Removed

- Sinclair-specific user manuals.

## [2.1.3] - 2017-07-17

### Added

- Override setting for external admin rights removal.
- User manuals for Windows 7, 8, and 10.
- Installer selects appropriate user manual by OS.
- Admin rights removal reason logging (timeout, logoff, etc.).

### Deprecated

- User manuals (to be moved to company website).

## [2.1.0] - 2016-02-18

### Added

- Per-user/group timeout overrides.
- Remove admin rights on logout setting.
- Group Policy templates (ADMX and ADML).
- Policy vs. preference distinction for settings.
- User name in add/remove event logs.
- Installer removes added users list and Program Files folder on uninstall.
- External admin rights removal logging (Group Policy, etc.).

## [2.0.0] - 2015-03-17

Initial public commit to GitHub. Previously a private project at Sinclair Community College.

---

## [Unreleased]
