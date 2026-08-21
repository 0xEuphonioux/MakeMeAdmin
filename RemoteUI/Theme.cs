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
    using System.Windows.Forms;

    /// <summary>
    /// Theme palette and system theme detection for the Make Me Admin UI.
    /// Follows the user's Windows app mode (light or dark) and the system accent color.
    /// </summary>
    internal static class Theme
    {
        // UC Davis brand palette.
        private static readonly Color UcdBlue = Color.FromArgb(21, 62, 128);
        private static readonly Color UcdGold = Color.FromArgb(190, 160, 70);

        /// <summary>
        /// Gets the accent color used for interactive elements (primary button, links).
        /// </summary>
        public static Color AccentColor
        {
            get { return UcdBlue; }
        }

        /// <summary>
        /// Gets the gold accent used for branding elements.
        /// </summary>
        public static Color GoldColor
        {
            get { return UcdGold; }
        }

        /// <summary>
        /// Gets whether the user has selected dark mode for Windows apps.
        /// </summary>
        public static bool IsDarkMode
        {
            get
            {
                try
                {
                    // HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
                    using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        if (key != null)
                        {
                            object value = key.GetValue("AppsUseLightTheme");
                            if (value != null)
                            {
                                // 0 = dark, 1 = light
                                return (Convert.ToInt32(value) == 0);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Fall through to light mode on any registry error.
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the window background color for the current theme.
        /// </summary>
        public static Color WindowBackground
        {
            get
            {
                // Slightly lighter than the pure surface color so the Mica
                // backdrop (or the flat fallback) reads as the base layer.
                return IsDarkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
            }
        }

        /// <summary>
        /// Gets the surface color for cards, headers and control backplates.
        /// </summary>
        public static Color SurfaceColor
        {
            get { return IsDarkMode ? Color.FromArgb(44, 44, 44) : Color.White; }
        }

        /// <summary>
        /// Gets the primary text color for the current theme.
        /// </summary>
        public static Color TextColor
        {
            get { return IsDarkMode ? Color.FromArgb(243, 243, 243) : Color.FromArgb(26, 26, 26); }
        }

        /// <summary>
        /// Gets the secondary (muted) text color for the current theme.
        /// </summary>
        public static Color SecondaryTextColor
        {
            get { return IsDarkMode ? Color.FromArgb(154, 154, 154) : Color.FromArgb(96, 96, 96); }
        }

        /// <summary>
        /// Gets the color for control borders (buttons, inputs).
        /// </summary>
        public static Color BorderColor
        {
            get { return IsDarkMode ? Color.FromArgb(86, 86, 86) : Color.FromArgb(204, 204, 204); }
        }

        /// <summary>
        /// Gets the hover color for the primary accent button.
        /// </summary>
        public static Color AccentHoverColor
        {
            get { return IsDarkMode ? Color.FromArgb(41, 96, 176) : Color.FromArgb(31, 82, 158); }
        }

        /// <summary>
        /// Gets the pressed color for the primary accent button.
        /// </summary>
        public static Color AccentPressedColor
        {
            get { return IsDarkMode ? Color.FromArgb(16, 50, 104) : Color.FromArgb(14, 46, 96); }
        }

        /// <summary>
        /// Applies the current theme colors to a form's controls.
        /// </summary>
        /// <param name="form">The form whose controls should be themed.</param>
        public static void ApplyTo(Form form)
        {
            if (null == form)
            {
                return;
            }

            form.BackColor = WindowBackground;
            form.ForeColor = TextColor;
            ApplyToControls(form.Controls);
        }

        /// <summary>
        /// Recursively applies theme colors to a control collection.
        /// </summary>
        private static void ApplyToControls(System.Windows.Forms.Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                // Status strip is a ToolStrip subclass; keep its own rendering.
                if (control is StatusStrip || control is ToolStrip)
                {
                    control.BackColor = WindowBackground;
                    control.ForeColor = TextColor;
                }
                else if (control is Label)
                {
                    control.BackColor = control.Parent != null ? control.Parent.BackColor : WindowBackground;
                    control.ForeColor = TextColor;
                }
                else if (control is TextBox || control is ComboBox)
                {
                    control.BackColor = SurfaceColor;
                    control.ForeColor = TextColor;
                }
                else if (control is Button && !(control is ModernButton))
                {
                    control.BackColor = SurfaceColor;
                    control.ForeColor = TextColor;
                }
                else
                {
                    control.BackColor = control.Parent != null ? control.Parent.BackColor : WindowBackground;
                    control.ForeColor = TextColor;
                }

                if (control.HasChildren)
                {
                    ApplyToControls(control.Controls);
                }
            }
        }
    }
}
