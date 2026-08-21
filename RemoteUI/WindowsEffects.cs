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
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    /// <summary>
    /// Applies Windows 11 visual effects (rounded corners, Mica backdrop,
    /// immersive dark mode) to top-level windows via the Desktop Window Manager.
    /// </summary>
    /// <remarks>
    /// All calls are defensive: on Windows 10 or older builds the underlying
    /// DWM attributes do not exist, so every operation is wrapped and failures
    /// degrade silently to the classic appearance.
    /// </remarks>
    internal static class WindowsEffects
    {
        // DWMWINDOWATTRIBUTE values (dwmapi.h)
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        // DWM_WINDOW_CORNER_PREFERENCE values
        private const int DWMWCP_DEFAULT = 0;
        private const int DWMWCP_DONOTROUND = 1;
        private const int DWMWCP_ROUND = 2;
        private const int DWMWCP_ROUNDSMALL = 3;

        // DWM_SYSTEMBACKDROP_TYPE values
        private const int DWMSBT_MAINWINDOW = 2;       // Mica
        private const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylic

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        /// <summary>
        /// Applies the full set of Windows 11 visual effects to a window.
        /// </summary>
        /// <param name="form">The top-level window to style.</param>
        /// <param name="darkMode">Whether the window chrome should use dark mode.</param>
        public static void Apply(Form form, bool darkMode)
        {
            if (null == form)
            {
                return;
            }

            IntPtr hwnd = form.Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            // Rounded corners: 8px radius for top-level windows.
            int cornerPreference = DWMWCP_ROUND;
            SetAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, cornerPreference);

            // Mica backdrop (Windows 11 22H2+). Falls back to the classic
            // solid background on Windows 10, which is the correct behavior.
            int backdrop = DWMSBT_MAINWINDOW;
            SetAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, backdrop);

            // Dark mode title bar / window chrome.
            int dark = darkMode ? 1 : 0;
            SetAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, dark);

            // Accent-colored caption and border to match the primary button.
            int accent = ColorTranslator.ToWin32(Theme.AccentColor);
            SetAttribute(hwnd, DWMWA_CAPTION_COLOR, accent);
            SetAttribute(hwnd, DWMWA_BORDER_COLOR, accent);
            SetAttribute(hwnd, DWMWA_TEXT_COLOR, ColorTranslator.ToWin32(Color.White));
        }

        /// <summary>
        /// Sets a single DWM window attribute, ignoring failures on older Windows builds.
        /// </summary>
        private static void SetAttribute(IntPtr hwnd, int attribute, int value)
        {
            try
            {
                DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));
            }
            catch (DllNotFoundException)
            {
                // dwmapi.dll is present on every supported Windows version; ignore anyway.
            }
            catch (EntryPointNotFoundException)
            {
                // Older Windows builds lack these attributes; keep the classic look.
            }
            catch (Exception)
            {
                // Never let a visual effect crash the elevation UI.
            }
        }
    }
}
