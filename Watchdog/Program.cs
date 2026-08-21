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
    using System.Security.Principal;

    /// <summary>
    /// Companion process for the Make Me Admin service.
    /// </summary>
    /// <remarks>
    /// The service spawns this watchdog at startup, passing its own process
    /// ID on the command line. The watchdog opens a handle to the service
    /// process and waits on it. When the service process terminates - for
    /// ANY reason, including force-quit (taskkill /F), crash, or a Service
    /// Control Manager kill after a stop timeout - the wait handle signals
    /// and the watchdog revokes every user still tracked in the encrypted
    /// user list.
    ///
    /// The watchdog is deliberately a separate process: code running inside
    /// the service process itself cannot run when the process is killed, so
    /// a graceful OnStop() is not guaranteed. A companion process that
    /// outlives the service closes that gap.
    /// </remarks>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">
        /// args[0]: the process ID of the Make Me Admin service process to watch.
        /// </param>
        public static void Main(string[] args)
        {
            int serviceProcessId = 0;
            if ((args != null) && (args.Length > 0))
            {
                int.TryParse(args[0], out serviceProcessId);
            }

            if (serviceProcessId <= 0)
            { // Without a valid service process ID there is nothing to watch.
                return;
            }

            // Ensure the Windows Event Log source exists so that WriteEvent
            // below cannot throw. Idempotent; runs as SYSTEM so it has the
            // required registry access. The service normally creates this in
            // OnStart, but it may be killed before that point.
            try
            {
                ApplicationLog.CreateSource();
            }
            catch (Exception)
            { // Logging is best-effort; never let this abort revocation.
            }

            IntPtr processHandle = IntPtr.Zero;
            try
            {
                // Open the service process with SYNCHRONIZE access so that
                // WaitForSingleObject below blocks until the process exits.
                processHandle = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_SYNCHRONIZE,
                    false,
                    serviceProcessId);

                if (processHandle == IntPtr.Zero)
                { // The service process is already gone (or access was denied).
                  // Treat this as a termination and revoke any tracked users.
                    RevokeAllTrackedUsers();
                    return;
                }

                // Block until the service process terminates. This returns
                // for a graceful stop as well as for a forced termination.
                NativeMethods.WaitForSingleObject(processHandle, NativeMethods.INFINITE);
            }
            catch (Exception ex)
            {
                try
                {
                    ApplicationLog.WriteEvent(string.Format("Watchdog error while monitoring the service process: {0}", ex.Message), EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Error);
                }
                catch (Exception)
                { // Logging is best-effort.
                }
                return;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(processHandle);
                }
            }

            // The service process has terminated. WaitForSingleObject only
            // returns after the process has fully exited, so a graceful
            // OnStop() has already completed and emptied the user list by
            // now. Revoke whatever OnStop() did not get to (forced
            // termination leaves the tracked users in the list).
            RevokeAllTrackedUsers();
        }


        /// <summary>
        /// Revokes administrator rights for every user still tracked in the
        /// encrypted user list.
        /// </summary>
        /// <remarks>
        /// If the service stopped gracefully, its OnStop() already revoked
        /// every user and emptied the list, so there is normally nothing to
        /// do here. If the service was terminated unexpectedly, the list
        /// still contains the affected users and each one is revoked.
        /// </remarks>
        private static void RevokeAllTrackedUsers()
        {
            try
            {
                lock (EncryptedSettings.SyncRoot)
                {
                    EncryptedSettings encryptedSettings = new EncryptedSettings(EncryptedSettings.SettingsFilePath);
                    SecurityIdentifier[] sids = encryptedSettings.AddedUserSIDs;

                    if ((sids == null) || (sids.Length == 0))
                    { // Nothing to do. The service stopped gracefully.
                        return;
                    }

                    // RemoveUser() is a no-op for any user who is not a
                    // member of the local Administrators group (it checks
                    // group membership internally), so this loop is safe for
                    // both graceful stops and forced terminations.
                    int revokedCount = 0;
                    for (int i = 0; i < sids.Length; i++)
                    {
                        if (sids[i] == null)
                        {
                            continue;
                        }

                        try
                        { // Revoke the user. One failure must not
                          // prevent the remaining revocations.
                            LocalAdministratorGroup.RemoveUser(sids[i], RemovalReason.ServiceStopped);
                            revokedCount++;
                        }
                        catch (Exception ex)
                        {
                            ApplicationLog.WriteEvent(string.Format("Watchdog: error removing user {0}: {1}", sids[i], ex.Message), EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Error);
                        }
                    }

                    try
                    {
                        ApplicationLog.WriteEvent(string.Format("Watchdog: service terminated unexpectedly; revoked administrator rights for {0} user(s).", revokedCount), EventID.UserRemovedFromAdminsSuccess, System.Diagnostics.EventLogEntryType.Information);
                    }
                    catch (Exception)
                    { // Logging is best-effort; never let this abort revocation.
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ApplicationLog.WriteEvent(string.Format("Watchdog: unable to load the user list for revocation: {0}", ex.Message), EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Error);
                }
                catch (Exception)
                { // Logging is best-effort.
                }
            }
        }
    }


    /// <summary>
    /// P/Invoke wrappers for the small set of native calls the watchdog needs.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Access right that allows waiting on a process handle.
        /// </summary>
        internal const uint PROCESS_SYNCHRONIZE = 0x00100000;

        /// <summary>
        /// Value meaning "wait indefinitely".
        /// </summary>
        internal const uint INFINITE = 0xFFFFFFFF;

        /// <summary>
        /// Opens an existing process object.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        /// <summary>
        /// Waits until the specified object is in the signaled state.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);
    }
}
