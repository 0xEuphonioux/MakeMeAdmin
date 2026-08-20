// 
// Copyright © 2010-2025, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//
// Make Me Admin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3.
//
// Make Me Admin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Make Me Admin. If not, see <http://www.gnu.org/licenses/>.
//

namespace SinclairCC.MakeMeAdmin
{
    using System;
    using System.DirectoryServices.AccountManagement;
    using System.Security.Principal;
    using System.Text;

    /// <summary>
    /// This class allows management of the local Administrators group.
    /// </summary>
    public static class LocalAdministratorGroup
    {
        /// <summary>
        /// The context against which all operations are performed by this class.
        /// In this case, the context is the local machine.
        /// </summary>
        //private static PrincipalContext localMachineContext = null;

        /// <summary>
        /// The security identifier (SID) of the local Administrators group.
        /// </summary>
        //private static SecurityIdentifier localAdminsGroupSid = null;

        /// <summary>
        /// Represents the local Administrators group.
        /// </summary>
        //private static GroupPrincipal localAdminGroup = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        static LocalAdministratorGroup()
        {
        }

        /// <summary>
        /// Gets a context representing the local machine.
        /// </summary>
        /*
        private static PrincipalContext LocalMachineContext
        {
            get
            {
                if (localMachineContext == null)
                {
                    try
                    {
                        localMachineContext = new PrincipalContext(ContextType.Machine);
                    }
                    catch (System.ComponentModel.InvalidEnumArgumentException invalidEnumException)
                    {
                        ApplicationLog.WriteEvent(invalidEnumException.Message, EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Error);
                        throw;
                    }
                    catch (ArgumentException argException)
                    {
                        ApplicationLog.WriteEvent(argException.Message, EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Error);
                        throw;
                    }
                }
                return localMachineContext;
            }
        }
        */

        /// <summary>
        /// Gets the security identifier (SID) of the local Administrators group.
        /// </summary>
        private static SecurityIdentifier LocalAdminsGroupSid
        {
            get
            {
                // NOTE: We could memoize this, like below, but that does not seem necessary. It did
                // not seem to make much difference in testing.
                /*
                if (localAdminsGroupSid == null)
                {
                    localAdminsGroupSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                }
                return localAdminsGroupSid;
                */

                return new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            }
        }

        /// <summary>
        /// Gets the name of the local Administrators group.
        /// </summary>
        public static string LocalAdminGroupName
        {
            get
            {
                NTAccount localAdminsGroupAccount = (NTAccount)LocalAdminsGroupSid.Translate(typeof(NTAccount));

                string[] nameParts = localAdminsGroupAccount?.Value.Split('\\');
                if (nameParts.Length > 0)
                {
                    return (nameParts[nameParts.Length - 1]);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets an object representing the local Administrators group.
        /// </summary>
        /*
        private static GroupPrincipal LocalAdminGroup
        {
            get
            {
                if (LocalMachineContext == null)
                {
                    ApplicationLog.WriteEvent(Properties.Resources.LocalMachineContextIsNull, EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Error);
                }
                else
                {
                    if (LocalAdminsGroupSid == null)
                    {
                        ApplicationLog.WriteEvent(Properties.Resources.LocalAdminsGroupSIDIsNull, EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Error);
                    }
                    else
                    {
                        if (localAdminGroup == null)
                        {
                            try
                            {
                                localAdminGroup = GroupPrincipal.FindByIdentity(LocalMachineContext, IdentityType.Sid, LocalAdminsGroupSid.Value);
                            }
                            catch (Exception exception)
                            {
                                ApplicationLog.WriteEvent(string.Format("{0}: {1}", Properties.Resources.Exception, exception.Message), EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Error);
                                throw;
                            }
                        }
                    }
                }
                return localAdminGroup;
            }
        }
        */

        /// <summary>
        /// Adds a user to the local Administrators group.
        /// </summary>
        /// <param name="userIdentity">
        /// The identity of the user to be added to the Administrators group.
        /// </param>
        /// <param name="expirationTime">
        /// The date and time at which the user's administrator rights should expire.
        /// </param>
        /// <param name="remoteAddress">
        /// The address of the remote host from which a request for administrator rights came, if applicable.
        /// </param>
        /// <param name="reason">
        /// The reason the user provided for requesting administrator rights. May be null or empty.
        /// </param>
        public static void AddUser(WindowsIdentity userIdentity, DateTime? expirationTime, string remoteAddress, string reason = null)
        {
            // TODO: Only do this if the user is not a member of the group?

            // Sanitize the reason server-side: it flows into the Event Log and
            // syslog, so control characters would permit log injection, and an
            // oversized reason would bloat log entries.
            reason = SanitizeReason(reason);

            AdminGroupManipulator adminGroupManipulator = new AdminGroupManipulator();
            bool userIsAuthorized = adminGroupManipulator.UserIsAuthorized(userIdentity, Settings.LocalAllowedEntities, Settings.LocalDeniedEntities, "Local allowed");

            if (!string.IsNullOrEmpty(remoteAddress))
            { // Request is from a remote computer. Check the remote authorization list.
                userIsAuthorized &= adminGroupManipulator.UserIsAuthorized(userIdentity, Settings.RemoteAllowedEntities, Settings.RemoteDeniedEntities, "Remote allowed");
            }

            if (
                (!string.IsNullOrEmpty(LocalAdminGroupName)) &&
                (userIdentity.User != null) && 
                (userIdentity.Groups != null) && 
                (userIsAuthorized)
               )
            {
                lock (EncryptedSettings.SyncRoot)
                { // Serialize the whole persist-and-add sequence so the removal
                  // timer cannot observe a user in one state but not the other.

                    // Save the user's information to the list of users.
                    EncryptedSettings encryptedSettings = new EncryptedSettings(EncryptedSettings.SettingsFilePath);
                    encryptedSettings.AddUser(userIdentity, expirationTime, remoteAddress);

                    if (!AddUserToAdministrators(userIdentity.User, reason, userIdentity.Name))
                    { // The group add failed. Do not leave an untracked list entry behind.
                        encryptedSettings.RemoveUser(userIdentity.User);
                    }
                }
            }
            else
            {
                string accountName = userIdentity?.Name ?? "unknown";
                string reasonNote = string.IsNullOrEmpty(reason) ? "" : string.Format(" (reason: \"{0}\")", reason);
                ApplicationLog.WriteEvent(
                    string.Format("Authorization denied for user {0}{1}. Check allowed/denied entity lists.", accountName, reasonNote),
                    EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Warning);
            }
        }

        /// <summary>
        /// Adds the given security identifier (SID) to the local Administrators group.
        /// </summary>
        /// <param name="userSid">
        /// The security identifier (SID) to be added to the local Administrators group.
        /// </param>
        private static bool AddUserToAdministrators(SecurityIdentifier userSid, string reason = null, string identityName = null)
        {
            try
            {
                string displayName = identityName ?? GetAccountNameFromSID(userSid);

                int result = AddLocalGroupMembers(LocalAdminGroupName, userSid);
                if (result == 0)
                {
                    string reasonSuffix = string.IsNullOrEmpty(reason) ? "" : string.Format(" Reason: {0}", reason);
                    string message = string.Format("User {0} added to the Administrators group. SID={1}{2}", displayName, userSid.Value, reasonSuffix);
                    ApplicationLog.WriteEvent(message, EventID.UserAddedToAdminsSuccess, System.Diagnostics.EventLogEntryType.Information);
                    return true;
                }
                else
                {
                    ApplicationLog.WriteEvent(string.Format("Error adding user {0} ({1}) to Administrators group: {2}", displayName, userSid.Value, result), EventID.UserAddedToAdminsFailure, System.Diagnostics.EventLogEntryType.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteEvent(
                    string.Format("Exception in AddUserToAdministrators: {0}", ex.Message),
                    EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// Strips control characters from a user-supplied reason and truncates
        /// it to the configured maximum length, so it is safe to embed in
        /// Event Log and syslog entries.
        /// </summary>
        private static string SanitizeReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return reason;
            }

            int maxLength = Settings.MaximumReasonLength;
            if (maxLength < 1)
            {
                maxLength = 256;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(reason.Length);
            foreach (char c in reason)
            {
                if ((c == '\t') || (!char.IsControl(c)))
                {
                    builder.Append(c);
                    if (builder.Length >= maxLength)
                    {
                        break;
                    }
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Removes the given security identifier (SID) from the local Administrators group.
        /// </summary>
        /// <param name="userSid">
        /// The security identifier (SID) to be removed from the local Administrators group.
        /// </param>
        /// <param name="reason">
        /// The reason for the removal.
        /// </param>
        public static void RemoveUser(SecurityIdentifier userSid, RemovalReason reason)
        {
            // TODO: Only do this if the user is a member of the group?

            if ((!string.IsNullOrEmpty(LocalAdminGroupName)) && (userSid != null))
            {
                SecurityIdentifier[] localAdminSids = GetLocalGroupMembers(LocalAdminGroupName);

                if (localAdminSids == null)
                { // The group could not be enumerated. Do not attempt removal.
                    return;
                }

                foreach (SecurityIdentifier sid in localAdminSids)
                {
                    if (sid == userSid)
                    {
                        string accountName = GetAccountNameFromSID(userSid);
                        int result = RemoveLocalGroupMembers(LocalAdminGroupName, userSid);
                        if (result == 0)
                        {
                            lock (EncryptedSettings.SyncRoot)
                            { // Hold the lock for the list update only; the
                              // message/logoff work below runs outside it.

                                EncryptedSettings encryptedSettings = new EncryptedSettings(EncryptedSettings.SettingsFilePath);
                                encryptedSettings.RemoveUser(userSid);
                            }

                            string reasonString = Properties.Resources.RemovalReasonUnknown;
                            switch (reason)
                            {
                                case RemovalReason.ServiceStopped:
                                    reasonString = Properties.Resources.RemovalReasonServiceStopped;
                                    break;
                                case RemovalReason.Timeout:
                                    reasonString = Properties.Resources.RemovalReasonTimeout;
                                    break;
                                case RemovalReason.UserLogoff:
                                    reasonString = Properties.Resources.RemovalReasonUserLogoff;
                                    break;
                                case RemovalReason.UserRequest:
                                    reasonString = Properties.Resources.RemovalReasonUserRequest;
                                    break;
                            }
                            string message = string.Format(Properties.Resources.UserRemoved, userSid, accountName, reasonString);
                            ApplicationLog.WriteEvent(message, EventID.UserRemovedFromAdminsSuccess, System.Diagnostics.EventLogEntryType.Information);



                            if (Settings.LogOffAfterExpiration > 0)
                            {
                                int MB_OK = 0x0;
                                int MB_ICONWARNING = 0x00000030;

                                // Get a WindowsIdentity object for the user matching the added user SID.
                                WindowsIdentity sessionIdentity = null;
                                WindowsIdentity userIdentity = null;
                                int sendToSessionId = 0;
                                /*bool performRemoval = true;*/
                                int[] currentSessionIds = LsaLogonSessions.LogonSessions.GetLoggedOnUserSessionIds();
                                foreach (int sessionId in currentSessionIds)
                                {
                                    if (LsaLogonSessions.LogonSessions.GetSidForSessionId(sessionId) == userSid)
                                    {
                                        sendToSessionId = sessionId;
                                    }
                                    sessionIdentity = LsaLogonSessions.LogonSessions.GetWindowsIdentityForSessionId(sessionId);
                                    if ((sessionIdentity != null) && (sessionIdentity.User == userSid))
                                    {
                                        userIdentity = sessionIdentity;
                                    }
                                }
                                int response = 0;

                                System.Text.StringBuilder messageBuilder = new System.Text.StringBuilder();
                                for (int i = 0; i < Settings.LogOffMessage.Length; i++)
                                {
                                    if (i == (Settings.LogOffMessage.Length - 1))
                                    {
                                        messageBuilder.Append(Settings.LogOffMessage[i]);
                                    }
                                    else
                                    {
                                        messageBuilder.AppendLine(Settings.LogOffMessage[i]);
                                    }
                                }
                                if (sendToSessionId != 0)
                                { // Only message and log off the user's session
                                  // when it was actually found; session 0 is the
                                  // non-interactive services session and cannot
                                  // be logged off.
                                    int returnValue = LsaLogonSessions.LogonSessions.SendMessageToSession(sendToSessionId, messageBuilder.ToString(), (MB_OK + MB_ICONWARNING), Settings.LogOffAfterExpiration, out response);
                                    returnValue = LsaLogonSessions.LogonSessions.LogoffSession(sendToSessionId);
                                }
                            }

                        }
                        else
                        {
                            ApplicationLog.WriteEvent(string.Format(Properties.Resources.RemovingUserReturnedError, userSid, accountName, result), EventID.UserRemovedFromAdminsFailure, System.Diagnostics.EventLogEntryType.Warning);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates that all of the users stored in the on-disk user list
        /// are in the local Administrators group if they're supposed to be, and vice-versa.
        /// </summary>
        public static void ValidateAllAddedUsers()
        {
            lock (EncryptedSettings.SyncRoot)
            { // Hold the user-list lock for the entire read-modify-write cycle
              // so that concurrent AddUser/RemoveUser calls cannot be lost.

            // Get a list of the users stored in the on-disk list.
            EncryptedSettings encryptedSettings = new EncryptedSettings(EncryptedSettings.SettingsFilePath);
            SecurityIdentifier[] addedUserList = encryptedSettings.AddedUserSIDs;

            // Get a list of the current members of the Administrators group.
            SecurityIdentifier[] localAdminSids = null;
            if ((addedUserList.Length > 0) && (!string.IsNullOrEmpty(LocalAdminGroupName)))
            {
                localAdminSids = GetLocalGroupMembers(LocalAdminGroupName);
            }

            for (int i = 0; i < addedUserList.Length; i++)
            {
                bool sidFoundInAdminsGroup = false;
                if ((addedUserList[i] != null) && (localAdminSids != null))
                {
                    foreach (SecurityIdentifier sid in localAdminSids)
                    {
                        if (sid == addedUserList[i])
                        {
                            sidFoundInAdminsGroup = true;
                            break;
                        }
                    }

                    AdminGroupManipulator adminGroup = new AdminGroupManipulator();

                    if (sidFoundInAdminsGroup)
                    {  // User's SID was found in the local administrators group.

                        DateTime? expirationTime = encryptedSettings.GetExpirationTime(addedUserList[i]);

                        if (expirationTime.HasValue)
                        { // The user's rights expire at some point.
                            if (expirationTime.Value > DateTime.Now)
                            { // The user's administrator rights expire in the future.

                                // Nothing to do here, since the user is already in the administrators group.

                            }
                            else
                            { // The user's administrator rights have expired.
                                LocalAdministratorGroup.RemoveUser(addedUserList[i], RemovalReason.Timeout);
                            }
                        }

                        else
                        { // The user's rights never expire.

                            // Get a WindowsIdentity object for the user matching the added user SID.
                            WindowsIdentity sessionIdentity = null;
                            WindowsIdentity userIdentity = null;
                            int[] loggedOnSessionIDs = LsaLogonSessions.LogonSessions.GetLoggedOnUserSessionIds();
                            foreach (int sessionId in loggedOnSessionIDs)
                            {
                                sessionIdentity = LsaLogonSessions.LogonSessions.GetWindowsIdentityForSessionId(sessionId);
                                if ((sessionIdentity != null) && (sessionIdentity.User == addedUserList[i]))
                                {
                                    userIdentity = sessionIdentity;
                                    break;
                                }
                            }

                            if (
                                (Settings.AutomaticAddAllowed != null) &&
                                (Settings.AutomaticAddAllowed.Length > 0) &&
                                (adminGroup.UserIsAuthorized(userIdentity, Settings.AutomaticAddAllowed, Settings.AutomaticAddDenied, "Automatic add allowed"))
                               )
                            { // The user is an automatically-added user.

                                // Nothing to do here. The user is an automatically-added one, and their rights don't expire.
                            }
                            else
                            { // The user is not an automatically-added user.

                                // Users who are not automatically added should not have non-expiring rights. Remove this user.
                                LocalAdministratorGroup.RemoveUser(addedUserList[i], RemovalReason.Timeout);
                            }
                        }
                    }
                    else
                    { // User's SID was not found in the local administrators group.

                        DateTime? expirationTime = encryptedSettings.GetExpirationTime(addedUserList[i]);

                        if (expirationTime.HasValue)
                        { // The user's rights expire at some point.
                            if (expirationTime.Value > DateTime.Now)
                            { // The user's administrator rights expire in the future.
                                string accountName = GetAccountNameFromSID(addedUserList[i]);
                                if (Settings.OverrideRemovalByOutsideProcess)
                                {
                                    ApplicationLog.WriteEvent(string.Format(Properties.Resources.UserRemovedByOutsideProcess + " " + Properties.Resources.AddingUserBackToAdministrators, addedUserList[i], string.IsNullOrEmpty(accountName) ? Properties.Resources.UnknownAccount : accountName), EventID.UserRemovedByExternalProcess, System.Diagnostics.EventLogEntryType.Information);
                                    AddUserToAdministrators(addedUserList[i]);
                                }
                                else
                                {
                                    ApplicationLog.WriteEvent(string.Format(Properties.Resources.UserRemovedByOutsideProcess + " " + Properties.Resources.RemovingUserFromList, addedUserList[i], string.IsNullOrEmpty(accountName) ? Properties.Resources.UnknownAccount : accountName), EventID.UserRemovedByExternalProcess, System.Diagnostics.EventLogEntryType.Information);
                                    encryptedSettings.RemoveUser(addedUserList[i]);
                                }
                            }
                            else
                            { // The user's administrator rights have expired.
                              // No need to remove from the administrators group, as we already know the SID
                              // is not present in the group.

                                encryptedSettings.RemoveUser(addedUserList[i]);

                            }
                        }
                        else
                        { // The user's rights never expire.

                            // Get a WindowsIdentity object for the user matching the added user SID.
                            WindowsIdentity sessionIdentity = null;
                            WindowsIdentity userIdentity = null;
                            int[] loggedOnSessionIDs = LsaLogonSessions.LogonSessions.GetLoggedOnUserSessionIds();
                            foreach (int sessionId in loggedOnSessionIDs)
                            {
                                sessionIdentity = LsaLogonSessions.LogonSessions.GetWindowsIdentityForSessionId(sessionId);
                                if ((sessionIdentity != null) && (sessionIdentity.User == addedUserList[i]))
                                {
                                    userIdentity = sessionIdentity;
                                    break;
                                }
                            }

                            if (
                                (Settings.AutomaticAddAllowed != null) &&
                                (Settings.AutomaticAddAllowed.Length > 0) &&
                                (adminGroup.UserIsAuthorized(userIdentity, Settings.AutomaticAddAllowed, Settings.AutomaticAddDenied, "Automatic add allowed"))
                               )
                            { // The user is an automatically-added user.

                                // The users rights do not expire, but they are an automatically-added user and are missing
                                // from the Administrators group. Add the user back in.

                                AddUserToAdministrators(addedUserList[i]);
                            }
                            else
                            { // The user is not an automatically-added user.

                                // The user is not in the Administrators group now, but they are
                                // listed as having non-expiring rights, even though they are not
                                // automatically added. This should never really happen, but
                                // just in case, we'll remove them from the on-disk user list.
                                encryptedSettings.RemoveUser(addedUserList[i]);

                            }

                        }
                    }
                }
            }
            } // end lock (EncryptedSettings.SyncRoot)
        }


        /// <summary>
        /// Gets the human-friendly account name corresponding to a given
        /// security identifier (SID).
        /// 
        /// Resolution order (5-tier fallback):
        ///   1. .NET Translate(typeof(NTAccount)) – normal user/group SIDs
        ///   2. IdentityStore cache – Entra ID groups cached locally
        ///   3. Win32 LookupAccountSid – capability SIDs, domain SIDs
        ///   4. [AppContainer] prefix for S-1-12-* SIDs
        ///   5. Raw SID string – never returns null
        /// </summary>
        /// <param name="sid">
        /// The security identifier (SID) for which the account name should be retrieved.
        /// </param>
        /// <returns>
        /// Returns the account name or a fallback string. Never returns null.
        /// </returns>
        internal static string GetAccountNameFromSID(SecurityIdentifier sid)
        {
            // 1. Try .NET Translate (works for most account SIDs)
            try
            {
                NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                return account.Value;
            }
            catch (IdentityNotMappedException) { }
            catch (System.ArgumentNullException) { }
            catch (System.ArgumentException) { }
            catch (System.SystemException) { }

            // 2. Try Windows IdentityStore cache (Entra ID / Azure AD groups/roles)
            //    HKLM\SOFTWARE\Microsoft\IdentityStore\Cache\<SID>\IdentityCache
            try
            {
                string cacheKeyPath = string.Format(
                    @"SOFTWARE\Microsoft\IdentityStore\Cache\{0}\IdentityCache",
                    sid.Value);

                using (var cacheKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(cacheKeyPath))
                {
                    if (cacheKey != null)
                    {
                        string displayName = cacheKey.GetValue("DisplayName") as string;
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            return displayName;
                        }
                    }
                }
            }
            catch { }

            // 3. Try Win32 LookupAccountSid (handles capability SIDs, domain SIDs)
            try
            {
                byte[] sidBytes = new byte[sid.BinaryLength];
                sid.GetBinaryForm(sidBytes, 0);

                uint nameLen = 256;
                uint domainLen = 256;
                StringBuilder name = new StringBuilder((int)nameLen);
                StringBuilder domain = new StringBuilder((int)domainLen);

                if (NativeMethods.LookupAccountSid(
                    null,
                    sidBytes,
                    name,
                    ref nameLen,
                    domain,
                    ref domainLen,
                    out NativeMethods.SID_NAME_USE peUse))
                {
                    if (domain.Length > 0)
                    {
                        return domain.ToString() + "\\" + name.ToString();
                    }
                    return name.ToString();
                }
            }
            catch { }

            // 4. For capability/AppContainer SIDs, prefix with label
            if (sid.Value.StartsWith("S-1-12-"))
            {
                return "[AppContainer] " + sid.Value;
            }

            // 5. Last resort: return the raw SID (never null)
            return sid.Value;
        }

        /// <summary>
        /// Gets the security identifier (SID) corresponding to a given sid string or account name.
        /// </summary>
        /// <param name="accountName">
        /// The SID string or account name that the SID should be retrieved for.
        /// Environment Variables will be expanded out. The following formats are accepted:
        ///  * S-1-5-32-545 (local, domain or well-known SID)
        ///  * DOMAIN\xxxxx (domain user or group)
        ///  * .\\xxxx or %COMPUTERNAME%\\xxxx (local user or group - also accepts explicitly using the computer name)
        ///  * NT AUTHORITY\INTERACTIVE, BUILTIN\Users, etc (well-known names)
        ///  * xxxxx (search for local, domain or well-known user or group)
        /// </param>
        /// <returns>
        /// Returns a security identifier (SID) corresponding to a given sid string or account name.
        /// If the account name doesn't exist, null is returned.
        /// </returns>
        internal static SecurityIdentifier GetSIDFromAccountName(string accountName)
        {
            try
            {
                /*string sidPattern = @"^S-\d-\d+-(\d+-){1,14}\d+$";*/ // Does not match SIDs such as S-1-5-113
                string sidPattern = @"^S-\d-\d+(-\d+){1,15}$";
                bool isSid = System.Text.RegularExpressions.Regex.IsMatch(accountName, sidPattern);

                if (isSid)
                {
                    return new SecurityIdentifier(accountName);
                }
                else
                {
                    string resolvedAccountName = Environment.ExpandEnvironmentVariables(accountName.Replace(".\\", "%COMPUTERNAME%\\"));
                    NTAccount account = new NTAccount(resolvedAccountName);
                    SecurityIdentifier sid = (SecurityIdentifier)account?.Translate(typeof(SecurityIdentifier));
                    return sid;
                }
            }
            catch (IdentityNotMappedException)
            { // Some or all identity references could not be translated.
                return null;
            }
            catch (System.ArgumentNullException)
            { // The target translation type is null.
                return null;
            }
            catch (System.ArgumentException)
            { // The target translation type is not an IdentityReference type.
                return null;
            }
            catch (System.SystemException)
            { // A Win32 error code was returned.
                return null;
            }
        }

        /*
        public static bool CurrentUserIsMember()
        {
            bool isMember = false;

            List<SecurityIdentifier> userSids = Shared.GetAuthorizationGroups(WindowsIdentity.GetCurrent(TokenAccessLevels.Read));

            foreach (SecurityIdentifier sid in userSids)
            {
                isMember = sid.Equals(localAdminsGroupSid);
                if (isMember) { break; }
            }

            return isMember;
        }
        */

        /// <summary>
        /// Adds a security identifier (SID) to a local security group.
        /// </summary>
        /// <param name="GroupName">
        /// The name of the group to which the SID is to be added.
        /// </param>
        /// <param name="memberSid">
        /// The security identifier (SID) to be added to the local group.
        /// </param>
        /// <returns>
        /// If the addition of the group member is successful, this function returns
        /// zero (0). Otherwise, a non-zero value is returned.
        /// </returns>
        private static int AddLocalGroupMembers(string GroupName, SecurityIdentifier memberSid)
        {
            int returnValue = -1;
            NativeMethods.LOCALGROUP_MEMBERS_INFO_0 newMemberInfo = new NativeMethods.LOCALGROUP_MEMBERS_INFO_0();
            byte[] binarySid = new byte[memberSid.BinaryLength];
            memberSid.GetBinaryForm(binarySid, 0);

            IntPtr unmanagedPointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(binarySid.Length);
            System.Runtime.InteropServices.Marshal.Copy(binarySid, 0, unmanagedPointer, binarySid.Length);
            newMemberInfo.lgrmi0_sid = unmanagedPointer;
            returnValue = NativeMethods.NetLocalGroupAddMembers(null, GroupName, 0, ref newMemberInfo, 1);
            System.Runtime.InteropServices.Marshal.FreeHGlobal(unmanagedPointer);
            return returnValue;
        }

        /// <summary>
        /// Removes a security identifier (SID) from a local security group.
        /// </summary>
        /// <param name="GroupName">
        /// The name of the group from which the SID is to be removed.
        /// </param>
        /// <param name="memberSid">
        /// The security identifier (SID) to be removed from the local group.
        /// </param>
        /// <returns>
        /// If the removal of the group member is successful, this function returns
        /// zero (0). Otherwise, a non-zero value is returned.
        /// </returns>
        private static int RemoveLocalGroupMembers(string GroupName, SecurityIdentifier memberSid)
        {
            int returnValue = -1;
            NativeMethods.LOCALGROUP_MEMBERS_INFO_0 memberInfo = new NativeMethods.LOCALGROUP_MEMBERS_INFO_0();
            byte[] binarySid = new byte[memberSid.BinaryLength];
            memberSid.GetBinaryForm(binarySid, 0);

            IntPtr unmanagedPointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(binarySid.Length);
            System.Runtime.InteropServices.Marshal.Copy(binarySid, 0, unmanagedPointer, binarySid.Length);
            memberInfo.lgrmi0_sid = unmanagedPointer;
            returnValue = NativeMethods.NetLocalGroupDelMembers(null, GroupName, 0, ref memberInfo, 1);
            System.Runtime.InteropServices.Marshal.FreeHGlobal(unmanagedPointer);
            return returnValue;
        }

        /// <summary>
        /// Retrieves a list of the members of a particular local group in the security database.
        /// </summary>
        /// <param name="GroupName">
        /// The name of the local group whose members are to be listed.
        /// </param>
        /// <returns>
        /// Returns an array of security identifiers (SIDs), each representing a group member.
        /// </returns>
        private static SecurityIdentifier[] GetLocalGroupMembers(string GroupName)
        {
            SecurityIdentifier[] returnValue = null;
            IntPtr buffer = IntPtr.Zero;
            IntPtr Resume = IntPtr.Zero;

            int val = NativeMethods.NetLocalGroupGetMembers(null, GroupName, 0, out buffer, -1, out int EntriesRead, out int TotalEntries, Resume);
            if (EntriesRead > 0)
            {
                returnValue = new SecurityIdentifier[EntriesRead];
                NativeMethods.LOCALGROUP_MEMBERS_INFO_0[] Members = new NativeMethods.LOCALGROUP_MEMBERS_INFO_0[EntriesRead];
                IntPtr iter = buffer;
                for (int i = 0; i < EntriesRead; i++)
                {
                    Members[i] = (NativeMethods.LOCALGROUP_MEMBERS_INFO_0)System.Runtime.InteropServices.Marshal.PtrToStructure(iter, typeof(NativeMethods.LOCALGROUP_MEMBERS_INFO_0));
                    iter = (IntPtr)((long)iter + System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.LOCALGROUP_MEMBERS_INFO_0)));
                    if (Members[i].lgrmi0_sid != IntPtr.Zero)
                    {
                        SecurityIdentifier sid = new SecurityIdentifier(Members[i].lgrmi0_sid);
                        returnValue[i] = sid;
                    }
                    else
                    {
                        returnValue[i] = null;
                    }
                }
                NativeMethods.NetApiBufferFree(buffer);
            }
            return returnValue;
        }

        /// <summary>
        /// Determines whether the given WindowsIdentity is directly a member
        /// of the local Administrators group.
        /// </summary>
        /// <param name="userIdentity">
        /// The WindowsIdentity whose group membership is to be checked.
        /// </param>
        /// <returns>
        /// Returns true if the given WindowsIdentity is specifically listed in
        /// the group membership of the local Administrators group.
        /// </returns>
        /// <remarks>
        /// A given user is considered a direct member of the
        /// Administrators group if they are specifically listed in the group's
        /// membership.
        /// </remarks>
        public static bool IsMemberOfAdministratorsDirectly(WindowsIdentity userIdentity)
        {
            SecurityIdentifier[] localAdminSids = GetLocalGroupMembers(LocalAdminGroupName);

            bool isMember = false;

            if (localAdminSids != null)
            {
                foreach (SecurityIdentifier sid in localAdminSids)
                {
                    if ((sid != null) && sid.Equals(userIdentity.User))
                    {
                        isMember = true;
                        break;
                    }
                }
            }

            return isMember;
        }

        /// <summary>
        /// Determines whether the given WindowsIdentity is a member of the local
        /// Administrators group, either directly or via nested group memberships.
        /// </summary>
        /// <param name="userIdentity">
        /// The WindowsIdentity whose group membership is to be checked.
        /// </param>
        /// <returns>
        /// Returns true if the given WindowsIdentity is a member of the local
        /// Administrators group in any way (specifically listed or via nested groups).
        /// </returns>
        public static bool IsMemberOfAdministrators(WindowsIdentity userIdentity)
        {
            bool isMember = false;

            if (userIdentity != null)
            {
                isMember = IsMemberOfAdministratorsDirectly(userIdentity);

                if (!isMember)
                {
                    SecurityIdentifier[] localAdminSids = GetLocalGroupMembers(LocalAdminGroupName);

                    // We can't use userIdentity.Groups here, because it does not include group memberships
                    // that are used for deny-only.
                    SecurityIdentifier[] groupSids = LsaLogonSessions.LogonSessions.GetGroupMemberships(userIdentity.Token);

                    if ((localAdminSids != null) && (groupSids != null))
                    {
                        foreach (SecurityIdentifier userGroupSid in groupSids)
                        {
                            foreach (SecurityIdentifier adminSid in localAdminSids)
                            {
                                if ((adminSid != null) && adminSid.Equals(userGroupSid))
                                {
                                    isMember = true;
                                    break;
                                }
                            }
                            if (isMember) { break; }
                        }
                    }
                }
            }

            return isMember;
        }


        // TODO: This function doesn't really belong here.
        /// <summary>
        /// Ends a network session between this computer and a remote host.
        /// </summary>
        /// <param name="remoteHost">
        /// The name of the computer of the client to disconnect.
        /// </param>
        /// <param name="userName">
        /// The name of the user whose session is to be terminated.
        /// </param>
        /// <returns>
        /// If this function succeeds, the return value is zero (0).
        /// </returns>
        public static int EndNetworkSession(string remoteHost, string userName)
        {
            return NativeMethods.NetSessionDel(null, remoteHost, userName);
        }
    }
}
