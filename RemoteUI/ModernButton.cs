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
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    /// <summary>
    /// A Windows 11-styled button: 4px rounded corners, flat background,
    /// accent fill for the primary action, and hover/pressed states.
    /// </summary>
    internal class ModernButton : Button
    {
        private const int CornerRadius = 4;

        private bool hovered = false;
        private bool pressed = false;

        /// <summary>
        /// Gets or sets whether this button renders as the primary (accent-filled) action.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Initializes a new instance of the ModernButton class.
        /// </summary>
        public ModernButton()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.TabStop = true;
        }

        /// <summary>
        /// Paints the button with the modern flat appearance.
        /// </summary>
        /// <param name="pe">Paint event data.</param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color background = this.IsPrimary ? Theme.AccentColor : Theme.SurfaceColor;
            Color foreground = this.IsPrimary ? Color.White : Theme.TextColor;
            Color border = this.IsPrimary ? background : Theme.BorderColor;

            if (this.hovered)
            {
                background = this.IsPrimary ? Theme.AccentHoverColor : Theme.SurfaceColor;
                border = this.IsPrimary ? background : Theme.AccentColor;
            }

            if (this.pressed)
            {
                background = this.IsPrimary ? Theme.AccentPressedColor : Theme.BorderColor;
            }

            if (!this.Enabled)
            {
                background = this.IsPrimary ? Color.FromArgb(160, 170, 190) : Color.FromArgb(240, 240, 240);
                foreground = this.IsPrimary ? Color.FromArgb(230, 230, 230) : Color.FromArgb(160, 160, 160);
                border = background;
            }

            using (GraphicsPath path = CreateRoundedRectangle(this.ClientRectangle, CornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(background))
                {
                    pe.Graphics.FillPath(brush, path);
                }

                using (Pen pen = new Pen(border))
                {
                    pe.Graphics.DrawPath(pen, path);
                }
            }

            TextRenderer.DrawText(
                pe.Graphics,
                this.Text,
                this.Font,
                this.ClientRectangle,
                foreground,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// Handles mouse enter to update the hover state.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.hovered = true;
            this.Invalidate();
        }

        /// <summary>
        /// Handles mouse leave to update the hover state.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.hovered = false;
            this.pressed = false;
            this.Invalidate();
        }

        /// <summary>
        /// Handles mouse down to update the pressed state.
        /// </summary>
        /// <param name="mevent">Event data.</param>
        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            this.pressed = true;
            this.Invalidate();
        }

        /// <summary>
        /// Handles mouse up to update the pressed state.
        /// </summary>
        /// <param name="mevent">Event data.</param>
        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            this.pressed = false;
            this.Invalidate();
        }

        /// <summary>
        /// Creates a rounded rectangle path with the specified corner radius.
        /// </summary>
        private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
