using System;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace SinclairCC.MakeMeAdmin
{
    /// <summary>
    /// Wraps the Windows.Security.Credentials.UI.UserConsentVerifier API
    /// for use in a .NET Framework 4.8 WinForms desktop application.
    /// </summary>
    internal static class HelloVerificationHelper
    {
        /// <summary>
        /// Checks whether Windows Hello is available and configured for
        /// the currently signed-in user.
        /// </summary>
        public static async Task<bool> IsAvailableAsync()
        {
            try
            {
                var availability = await UserConsentVerifier.CheckAvailabilityAsync();
                return availability == UserConsentVerifierAvailability.Available;
            }
            catch
            {
                // If we can't even check, treat as unavailable.
                return false;
            }
        }

        /// <summary>
        /// Requests a Windows Hello verification. The user must provide
        /// their PIN, fingerprint, or facial recognition. Returns the
        /// verification result.
        /// </summary>
        /// <param name="message">
        /// The message shown in the verification prompt.
        /// </param>
        public static async Task<UserConsentVerificationResult> VerifyAsync(string message)
        {
            return await UserConsentVerifier.RequestVerificationAsync(message);
        }

        /// <summary>
        /// Returns a human-readable description of the verification result.
        /// </summary>
        public static string GetResultDescription(UserConsentVerificationResult result)
        {
            switch (result)
            {
                case UserConsentVerificationResult.Verified:
                    return "Identity verified.";
                case UserConsentVerificationResult.DeviceNotPresent:
                    return "No authentication device is available.";
                case UserConsentVerificationResult.NotConfiguredForUser:
                    return "Windows Hello is not configured for this user.";
                case UserConsentVerificationResult.DisabledByPolicy:
                    return "Windows Hello is disabled by Group Policy.";
                case UserConsentVerificationResult.DeviceBusy:
                    return "The authentication device is busy. Please try again.";
                case UserConsentVerificationResult.RetriesExhausted:
                    return "Too many failed authentication attempts.";
                case UserConsentVerificationResult.Canceled:
                    return "Verification was canceled.";
                default:
                    return $"Unknown verification result: {result}";
            }
        }
    }
}
