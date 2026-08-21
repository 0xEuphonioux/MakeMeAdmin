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

    /// <summary>
    /// Provides the current display scale factor (96 DPI = 100%).
    /// Used to size fonts explicitly because the forms use
    /// AutoScaleMode.Dpi, which scales control bounds by the DPI
    /// ratio but does not scale fonts automatically.
    /// </summary>
    internal static class DpiHelper
    {
        private const int LOGPIXELSX = 88;

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int index);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        /// <summary>
        /// Gets the scale factor relative to 96 DPI (1.0 = 100%, 1.5 = 150%).
        /// Falls back to 1.0 when the DPI cannot be determined.
        /// </summary>
        public static float ScaleFactor
        {
            get
            {
                try
                {
                    uint dpi = GetDpiForSystem();
                    if (dpi != 0)
                    {
                        return dpi / 96.0f;
                    }

                    IntPtr dc = GetDC(IntPtr.Zero);
                    if (dc != IntPtr.Zero)
                    {
                        try
                        {
                            int deviceDpi = GetDeviceCaps(dc, LOGPIXELSX);
                            if (deviceDpi > 0)
                            {
                                return deviceDpi / 96.0f;
                            }
                        }
                        finally
                        {
                            ReleaseDC(IntPtr.Zero, dc);
                        }
                    }
                }
                catch (Exception)
                {
                    // DPI detection is a rendering nicety; never fail for it.
                }

                return 1.0f;
            }
        }

        /// <summary>
        /// Creates a font scaled for the current display scale factor.
        /// </summary>
        public static Font ScaleFont(string familyName, float baseSize, FontStyle style)
        {
            try
            {
                return new Font(familyName, baseSize * ScaleFactor, style);
            }
            catch (Exception)
            {
                return new Font(FontFamily.GenericSansSerif, baseSize * ScaleFactor, style);
            }
        }
    }
}
