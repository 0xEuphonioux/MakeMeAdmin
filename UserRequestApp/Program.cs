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
    using System.Windows.Forms;

    /// <summary>
    /// This class contains the main entry point for the application.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        internal static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            EnablePerMonitorV2DpiAwareness();
            Application.Run(new SubmitRequestForm());
        }

        /// <summary>
        /// Enables PerMonitorV2 DPI awareness where supported (.NET Framework 4.7+).
        /// Uses reflection so the call is a no-op on runtimes without the API.
        /// HighDpiMode.PerMonitorV2 has the numeric value 4.
        /// </summary>
        private static void EnablePerMonitorV2DpiAwareness()
        {
            try
            {
                System.Reflection.MethodInfo setHighDpi =
                    typeof(Application).GetMethod("SetHighDpiMode");
                if (setHighDpi != null)
                {
                    object perMonitorV2 = Enum.Parse(
                        setHighDpi.GetParameters()[0].ParameterType, "PerMonitorV2");
                    setHighDpi.Invoke(null, new object[] { perMonitorV2 });
                }
            }
            catch (Exception)
            {
                // DPI awareness is a rendering nicety; never fail startup for it.
            }
        }
    }
}
