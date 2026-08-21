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
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// A UC Davis-branded header strip used at the top of the elevation form.
    /// Displays the lock icon and the application name on a navy background.
    /// </summary>
    internal class BrandHeader : Panel
    {
        private readonly PictureBox iconBox;
        private readonly Label titleLabel;
        private readonly Label subtitleLabel;

        /// <summary>
        /// Initializes a new instance of the BrandHeader class.
        /// </summary>
        public BrandHeader()
        {
            this.BackColor = Theme.AccentColor;
            this.Height = 64;
            this.Dock = DockStyle.Top;

            this.iconBox = new PictureBox();
            // The resource is an Icon; PictureBox needs an Image, so extract
            // a bitmap from the icon at the desired display size.
            using (System.Drawing.Icon icon = Properties.Resources.UCDavisLock)
            {
                this.iconBox.Image = icon.ToBitmap();
            }
            this.iconBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.iconBox.Size = new Size(44, 44);
            this.iconBox.Location = new Point(12, 10);
            this.iconBox.BackColor = Color.Transparent;

            this.titleLabel = new Label();
            this.titleLabel.Text = Properties.Resources.ApplicationName;
            this.titleLabel.ForeColor = Color.White;
            this.titleLabel.Font = new Font("Segoe UI Variable", 12F, FontStyle.Bold);
            this.titleLabel.AutoSize = true;
            this.titleLabel.Location = new Point(66, 12);
            this.titleLabel.BackColor = Color.Transparent;

            this.subtitleLabel = new Label();
            this.subtitleLabel.Text = "Temporary administrator privileges";
            this.subtitleLabel.ForeColor = Color.FromArgb(210, 220, 235);
            this.subtitleLabel.Font = new Font("Segoe UI Variable", 8.5F);
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.Location = new Point(66, 36);
            this.subtitleLabel.BackColor = Color.Transparent;

            this.Controls.Add(this.iconBox);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.subtitleLabel);
        }

        /// <summary>
        /// Updates the header colors for the current theme.
        /// </summary>
        public void RefreshTheme()
        {
            // The header stays navy in both modes; only the subtitle tint varies.
            this.BackColor = Theme.AccentColor;
            this.titleLabel.ForeColor = Color.White;
            this.subtitleLabel.ForeColor = Theme.IsDarkMode
                ? Color.FromArgb(190, 205, 225)
                : Color.FromArgb(215, 225, 240);
        }
    }
}
