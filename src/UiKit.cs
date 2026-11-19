using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace RustFilter
{
    // ---------- Thème ----------
    public static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(14, 15, 19);
        public static readonly Color Side = Color.FromArgb(20, 21, 27);
        public static readonly Color Card = Color.FromArgb(24, 26, 33);
        public static readonly Color Field = Color.FromArgb(34, 36, 46);
        public static readonly Color Line = Color.FromArgb(48, 51, 64);
        public static readonly Color Fg = Color.FromArgb(240, 241, 245);
        public static readonly Color Dim = Color.FromArgb(140, 144, 158);
        public static readonly Color Accent = Color.FromArgb(205, 65, 43);
        public static readonly Color AccentHi = Color.FromArgb(224, 79, 56);
        public static readonly Color Ok = Color.FromArgb(34, 197, 94);

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Quality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        public static readonly TextFormatFlags Center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        public static readonly TextFormatFlags Left = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
    }

    // ---------- Curseur ----------
    public class ModernSlider : Control
    {
        int min = 0, max = 100, val = 0;
        bool drag, hover;
        public int SmallChange = 1, LargeChange = 5, DefaultValue = 0;
        public event EventHandler ValueChanged;

        public ModernSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            TabStop = true;
            Height = 28;
            Cursor = Cursors.Hand;
        }

        public int Minimum { get { return min; } set { min = value; if (val < min) val = min; Invalidate(); } }
        public int Maximum { get { return max; } set { max = value; if (val > max) val = max; Invalidate(); } }
        public int Value
        {
            get { return val; }
            set
            {
                int v = Math.Max(min, Math.Min(max, value));
                if (v == val) return;
                val = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        int ValueFromX(int x)
        {
            double t = (x - 9) / (double)Math.Max(1, Width - 18);
            if (t < 0) t = 0; if (t > 1) t = 1;
            return min + (int)Math.Round(t * (max - min));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus(); drag = true; Capture = true;
            Value = ValueFromX(e.X);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (drag) Value = ValueFromX(e.X);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            drag = false; Capture = false; Invalidate();
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        protected override void OnMouseDoubleClick(MouseEventArgs e) { base.OnMouseDoubleClick(e); Value = DefaultValue; }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override bool IsInputKey(Keys k)
        {
            return k == Keys.Left || k == Keys.Right || k == Keys.Up || k == Keys.Down || base.IsInputKey(k);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) Value = val - SmallChange;
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) Value = val + SmallChange;
            else if (e.KeyCode == Keys.PageDown) Value = val - LargeChange;
            else if (e.KeyCode == Keys.PageUp) Value = val + LargeChange;
            else if (e.KeyCode == Keys.Home) Value = min;
            else if (e.KeyCode == Keys.End) Value = max;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            g.Clear(BackColor);
            Rectangle track = new Rectangle(9, Height / 2 - 3, Width - 18, 6);
            double t = (max == min) ? 0 : (val - min) / (double)(max - min);
            int x = 9 + (int)(t * (Width - 18));
            using (GraphicsPath p = Theme.Round(track, 3)) using (SolidBrush b = new SolidBrush(Theme.Field)) g.FillPath(b, p);
            if (x > 9)
            {
                Rectangle fill = new Rectangle(9, track.Y, Math.Max(6, x - 9), track.Height);
                using (GraphicsPath p = Theme.Round(fill, 3)) using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillPath(b, p);
            }
            int r = (hover || drag) ? 9 : 8;
            Rectangle th = new Rectangle(x - r, Height / 2 - r, r * 2, r * 2);
            using (SolidBrush b = new SolidBrush(Color.White)) g.FillEllipse(b, th);
            using (Pen pen = new Pen(Focused ? Theme.AccentHi : Theme.Accent, 2f)) g.DrawEllipse(pen, th);
        }
    }

    // ---------- Interrupteur ----------
    public class ToggleSwitch : Control
    {
        bool chk;
        public event EventHandler CheckedChanged;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            TabStop = true;
            Height = 26;
            Cursor = Cursors.Hand;
        }

        public bool Checked
        {
            get { return chk; }
            set
            {
                if (chk == value) return;
                chk = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e) { base.OnClick(e); Focus(); Checked = !chk; }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) Checked = !chk;
        }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            g.Clear(BackColor);
            Rectangle sw = new Rectangle(1, (Height - 20) / 2, 38, 20);
            using (GraphicsPath p = Theme.Round(sw, 10))
            {
                using (SolidBrush b = new SolidBrush(chk ? Theme.Ok : Theme.Field)) g.FillPath(b, p);
                using (Pen pen = new Pen(Focused ? Theme.AccentHi : (chk ? Theme.Ok : Theme.Line), 1.5f)) g.DrawPath(pen, p);
            }
            int kx = chk ? sw.Right - 18 : sw.X + 4;
            using (SolidBrush b = new SolidBrush(Color.White)) g.FillEllipse(b, kx, sw.Y + 3, 14, 14);
            Rectangle tr = new Rectangle(50, 0, Width - 50, Height);
            TextRenderer.DrawText(g, Text, Font, tr, ForeColor, Theme.Left);
        }
    }

    // ---------- Bouton ----------
    public class RoundButton : Control
    {
        public bool Primary;
        public Color Swatch = Color.Empty;
        bool hover, down;

        public RoundButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            TabStop = true;
            Height = 32;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; down = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); down = true; Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); down = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) OnClick(EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            g.Clear(BackColor);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill, border;
            if (Primary)
            {
                fill = down ? Color.FromArgb(170, 52, 34) : (hover ? Theme.AccentHi : Theme.Accent);
                border = fill;
            }
            else
            {
                fill = down ? Theme.Line : (hover ? Color.FromArgb(44, 46, 58) : Theme.Field);
                border = hover ? Theme.Accent : Theme.Line;
            }
            using (GraphicsPath p = Theme.Round(r, 8))
            {
                using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, p);
                using (Pen pen = new Pen(Focused ? Theme.AccentHi : border, 1.2f)) g.DrawPath(pen, p);
            }
            int tx = 0;
            if (Swatch != Color.Empty)
            {
                Rectangle sr = new Rectangle(12, (Height - 16) / 2, 16, 16);
                using (SolidBrush b = new SolidBrush(Swatch)) g.FillEllipse(b, sr);
                using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f)) g.DrawEllipse(pen, sr);
                tx = 28;
            }
            Rectangle tr = new Rectangle(tx, 0, Width - tx, Height);
            TextRenderer.DrawText(g, Text, Font, tr, Primary ? Color.White : ForeColor, Theme.Center);
        }
    }

    // ---------- Pastille de couleur ----------
    public class ColorChip : Control
    {
        public Color Value = Color.White;
        bool hover;
        public ColorChip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.StandardClick, true);
            Size = new Size(26, 26);
            Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            g.Clear(BackColor);
            Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);
            using (SolidBrush b = new SolidBrush(Value)) g.FillEllipse(b, r);
            using (Pen pen = new Pen(hover ? Theme.Fg : Color.FromArgb(90, 255, 255, 255), hover ? 2f : 1f)) g.DrawEllipse(pen, r);
        }
    }

    public static class NativeUi
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool ValidateRect(IntPtr hWnd, IntPtr lpRect);
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string subApp, string subIdList);
        public static void Dark(Control c, string theme)
        {
            try { SetWindowTheme(c.Handle, theme, null); } catch { }
        }
    }

    // ---------- Liste déroulante sombre ----------
    public class DarkCombo : ComboBox
    {
        public string Placeholder = "";

        public DarkCombo()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 22;
            FlatStyle = FlatStyle.Flat;
            BackColor = Theme.Field;
            ForeColor = Theme.Fg;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeUi.Dark(this, "DarkMode_CFD");
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool edit = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool sel = (e.State & DrawItemState.Selected) != 0 && !edit;
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(sel ? Color.FromArgb(58, 36, 32) : Theme.Field)) g.FillRectangle(b, e.Bounds);
            TextRenderer.DrawText(g, Items[e.Index].ToString(), Font,
                new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height), Theme.Fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        protected override void WndProc(ref Message m)
        {
            if (DropDownStyle == ComboBoxStyle.DropDownList)
            {
                if (m.Msg == 0x14) { m.Result = (IntPtr)1; return; } // pas d'effacement du fond
                if (m.Msg == 0xF) { PaintFull(); NativeUi.ValidateRect(Handle, IntPtr.Zero); return; }
            }
            base.WndProc(ref m);
            if (m.Msg == 0xF) PaintChrome();
        }

        void Chevron(Graphics g, Rectangle btn)
        {
            using (Pen pen = new Pen(Theme.Dim, 1.6f))
            {
                int cx = btn.X + btn.Width / 2, cy = btn.Y + btn.Height / 2;
                g.DrawLine(pen, cx - 4, cy - 2, cx, cy + 2);
                g.DrawLine(pen, cx, cy + 2, cx + 4, cy - 2);
            }
        }

        void PaintFull()
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(Handle))
                using (Bitmap bmp = new Bitmap(Math.Max(1, Width), Math.Max(1, Height)))
                {
                    using (Graphics b = Graphics.FromImage(bmp))
                    {
                        b.SmoothingMode = SmoothingMode.AntiAlias;
                        b.Clear(Theme.Field);
                        string txt = (SelectedIndex >= 0 && SelectedIndex < Items.Count) ? Items[SelectedIndex].ToString() : Placeholder;
                        Color c = (SelectedIndex >= 0) ? Theme.Fg : Theme.Dim;
                        TextRenderer.DrawText(b, txt, Font, new Rectangle(8, 0, Width - 34, Height), c,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                        Chevron(b, new Rectangle(Width - 24, 1, 23, Height - 2));
                        using (Pen pen = new Pen(Focused ? Theme.AccentHi : Theme.Line, 1f)) b.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                    }
                    g.DrawImageUnscaled(bmp, 0, 0);
                }
            }
            catch { }
        }

        void PaintChrome()
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(Handle))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle btn = new Rectangle(Width - 24, 1, 23, Height - 2);
                    using (SolidBrush b = new SolidBrush(Theme.Field)) g.FillRectangle(b, btn);
                    Chevron(g, btn);
                    using (Pen pen = new Pen(Focused ? Theme.AccentHi : Theme.Line, 1f)) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
            catch { }
        }
    }
    // ---------- Navigation latérale ----------
    public class NavButton : Control
    {
        public bool Selected;
        bool hover;
        public NavButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            TabStop = true;
            Height = 42;
            Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) OnClick(EventArgs.Empty);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            Color bg = Selected ? Color.FromArgb(34, 36, 46) : (hover ? Color.FromArgb(28, 30, 38) : Theme.Side);
            g.Clear(bg);
            if (Selected) using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillRectangle(b, 0, 6, 4, Height - 12);
            if (Focused) using (Pen pen = new Pen(Theme.Line)) g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
            using (Font f = new Font("Segoe UI Semibold", 10.5f))
                TextRenderer.DrawText(g, Text, f, new Rectangle(24, 0, Width - 24, Height), Selected ? Theme.Fg : Theme.Dim, Theme.Left);
        }
    }

    // ---------- Logo ----------
    public class LogoLabel : Control
    {
        public LogoLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(180, 34);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            g.Clear(BackColor);
            using (Font f = new Font("Segoe UI", 17f, FontStyle.Bold))
            {
                Size a = TextRenderer.MeasureText(g, "WIPE", f, new Size(400, 40), TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, "WIPE", f, new Point(0, 0), Theme.Fg, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, "FILTER", f, new Point(a.Width - 2, 0), Theme.Accent, TextFormatFlags.NoPadding);
            }
        }
    }

    // ---------- Carte ----------
    public class CardPanel : Panel
    {
        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            g.Clear(Theme.Bg);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, 10))
            {
                using (SolidBrush b = new SolidBrush(Theme.Card)) g.FillPath(b, p);
                using (Pen pen = new Pen(Theme.Line, 1f)) g.DrawPath(pen, p);
            }
        }
    }

    // ---------- Dessin du viseur (identique à la fenêtre transparente) ----------
    public static class CrossRender
    {
        public static void Draw(Graphics g, CrossCfg cfg, int cx, int cy)
        {
            g.SmoothingMode = SmoothingMode.None;
            Color col = Color.FromArgb(255, Color.FromArgb(cfg.Argb));
            int t = Math.Max(1, cfg.Thick), len = cfg.Size, gap = cfg.Gap, h = t / 2;
            bool outline = cfg.Outline;
            using (SolidBrush br = new SolidBrush(col))
            {
                Action<int, int, int, int> box = delegate(int x, int y, int w, int hh)
                {
                    if (w <= 0 || hh <= 0) return;
                    if (outline) g.FillRectangle(Brushes.Black, x - 1, y - 1, w + 2, hh + 2);
                    g.FillRectangle(br, x, y, w, hh);
                };
                bool arms = cfg.Style == 1 || cfg.Style == 2 || cfg.Style == 4;
                if (arms)
                {
                    box(cx - gap - len, cy - h, len, t);
                    box(cx + gap + 1, cy - h, len, t);
                    box(cx - h, cy + gap + 1, t, len);
                    if (cfg.Style != 4) box(cx - h, cy - gap - len, t, len);
                }
                if (cfg.Style == 3)
                {
                    int r = Math.Max(3, len);
                    if (outline) using (Pen po = new Pen(Color.Black, t + 2)) g.DrawEllipse(po, cx - r, cy - r, r * 2, r * 2);
                    using (Pen p = new Pen(col, t)) g.DrawEllipse(p, cx - r, cy - r, r * 2, r * 2);
                }
                if (cfg.Style == 0 || cfg.Style == 2 || cfg.Style == 3)
                {
                    int d = Math.Max(2, cfg.Thick < 2 ? 2 : cfg.Thick);
                    box(cx - d / 2, cy - d / 2, d, d);
                }
            }
        }
    }

    // ---------- Aperçu du viseur ----------
    public class CrossPreview : Control
    {
        const int L = 121; // zone logique : le centre est en 60, les décalages vont de -60 à +60
        public CrossCfg Cfg = new CrossCfg();
        int bg = 3;
        static readonly Color[] Bgs = new Color[] { Color.FromArgb(47, 74, 42), Color.FromArgb(185, 163, 122), Color.FromArgb(226, 233, 238), Color.FromArgb(13, 17, 23) };

        public CrossPreview()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.StandardClick, true);
            Size = new Size(242, 242);
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            bg = (bg + 1) % Bgs.Length;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            g.Clear(BackColor);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, 10))
            {
                using (SolidBrush b = new SolidBrush(Bgs[bg])) g.FillPath(b, p);
                g.SetClip(p);
                Color grid = Color.FromArgb(28, 128, 128, 128);
                using (Pen pen = new Pen(grid))
                    for (int i = 20; i < Width; i += 20) { g.DrawLine(pen, i, 0, i, Height); g.DrawLine(pen, 0, i, Width, i); }
                using (Bitmap bmp = new Bitmap(L, L, PixelFormat.Format32bppArgb))
                {
                    using (Graphics gb = Graphics.FromImage(bmp)) CrossRender.Draw(gb, Cfg, L / 2 + Cfg.OffX, L / 2 + Cfg.OffY);
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(bmp, new Rectangle(0, 0, Width, Height), 0, 0, L, L, GraphicsUnit.Pixel);
                }
                g.ResetClip();
                using (Pen pen = new Pen(Theme.Line, 1f)) g.DrawPath(pen, p);
            }
        }
    }
}
