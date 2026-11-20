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
        public static readonly Color Bg = Color.FromArgb(13, 19, 31);
        public static readonly Color Side = Color.FromArgb(9, 14, 24);
        public static readonly Color Card = Color.FromArgb(19, 27, 42);
        public static readonly Color Field = Color.FromArgb(27, 37, 57);
        public static readonly Color Track = Color.FromArgb(46, 58, 82);
        public static readonly Color Line = Color.FromArgb(38, 49, 71);
        public static readonly Color Fg = Color.FromArgb(233, 237, 245);
        public static readonly Color Dim = Color.FromArgb(147, 160, 184);
        public static readonly Color Accent = Color.FromArgb(240, 167, 58);
        public static readonly Color AccentHi = Color.FromArgb(255, 191, 94);
        public static readonly Color OnAccent = Color.FromArgb(26, 18, 4);
        public static readonly Color Ok = Color.FromArgb(95, 211, 154);
        static string displayFamily;
        // Police des titres et des gros chiffres : Bahnschrift (livrée avec Windows 10/11), sinon Segoe UI en gras.
        public static Font Display(float size)
        {
            if (displayFamily == null)
            {
                displayFamily = "";
                try
                {
                    foreach (FontFamily ff in new System.Drawing.Text.InstalledFontCollection().Families)
                        if (ff.Name == "Bahnschrift SemiBold Condensed") { displayFamily = ff.Name; break; }
                }
                catch { }
            }
            return displayFamily != "" ? new Font(displayFamily, size, FontStyle.Regular) : new Font("Segoe UI", size, FontStyle.Bold);
        }

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

        // Fond de la fenêtre : dégradé doux, calculé par rapport à la fenêtre pour que tous les panneaux s'y raccordent.
        public static void Backdrop(Control c, Graphics g)
        {
            Control top = c.TopLevelControl;
            Size sz = top != null ? top.ClientSize : c.Size;
            Point o = Point.Empty;
            try { if (top != null && c.IsHandleCreated && top.IsHandleCreated) o = top.PointToClient(c.PointToScreen(Point.Empty)); } catch { }
            Rectangle whole = new Rectangle(-o.X, -o.Y, Math.Max(1, sz.Width), Math.Max(1, sz.Height));
            using (LinearGradientBrush br = new LinearGradientBrush(whole, Color.FromArgb(20, 31, 51), Color.FromArgb(10, 15, 26), 62f))
                g.FillRectangle(br, 0, 0, c.Width, c.Height);
        }

        // Efface avec le bon fond (dégradé si le contrôle est posé directement sur une page).
        public static void ClearBack(Control c, Graphics g)
        {
            if (c.Parent is PagePanel) Backdrop(c, g); else g.Clear(c.BackColor);
        }

        public static Color Shade(Color c, int delta)
        {
            return Color.FromArgb(c.A, Math.Max(0, Math.Min(255, c.R + delta)), Math.Max(0, Math.Min(255, c.G + delta)), Math.Max(0, Math.Min(255, c.B + delta)));
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
            using (GraphicsPath p = Theme.Round(track, 3)) using (SolidBrush b = new SolidBrush(Theme.Track)) g.FillPath(b, p);
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
            Theme.ClearBack(this, g);
            Rectangle sw = new Rectangle(1, (Height - 20) / 2, 38, 20);
            using (GraphicsPath p = Theme.Round(sw, 10))
            {
                using (SolidBrush b = new SolidBrush(chk ? Theme.Ok : Theme.Track)) g.FillPath(b, p);
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
            Theme.ClearBack(this, g);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill, border;
            if (Primary)
            {
                fill = down ? Color.FromArgb(200, 134, 40) : (hover ? Theme.AccentHi : Theme.Accent);
                border = fill;
            }
            else
            {
                fill = down ? Theme.Line : (hover ? Color.FromArgb(38, 52, 78) : Theme.Field);
                border = hover ? Theme.Accent : Theme.Line;
            }
            using (GraphicsPath p = Theme.Round(r, Math.Min(10, Height / 2)))
            {
                // léger dégradé vertical : le haut est un peu plus clair, comme un bouton en relief doux
                using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), Theme.Shade(fill, down ? 0 : 14), fill, 90f)) g.FillPath(b, p);
                using (Pen pen = new Pen(Focused ? Theme.AccentHi : border, 1f)) g.DrawPath(pen, p);
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
            TextRenderer.DrawText(g, Text, Font, tr, Primary ? Theme.OnAccent : ForeColor, Theme.Center);
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
            UpdateRegion();
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); UpdateRegion(); }

        // Coins arrondis : la région découpe aussi le cadre natif et la zone de saisie.
        void UpdateRegion()
        {
            try
            {
                using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, Height), 11))
                {
                    Region old = Region;
                    Region = new Region(p);
                    if (old != null) old.Dispose();
                }
            }
            catch { }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool edit = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool sel = (e.State & DrawItemState.Selected) != 0 && !edit;
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(sel ? Color.FromArgb(52, 44, 28) : Theme.Field)) g.FillRectangle(b, e.Bounds);
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
                        b.Clear(Theme.Card);
                        using (GraphicsPath rp = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 9))
                            using (SolidBrush fb = new SolidBrush(Theme.Field)) b.FillPath(fb, rp);
                        string txt = (SelectedIndex >= 0 && SelectedIndex < Items.Count) ? Items[SelectedIndex].ToString() : Placeholder;
                        Color c = (SelectedIndex >= 0) ? Theme.Fg : Theme.Dim;
                        b.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                        using (SolidBrush tb = new SolidBrush(c))
                        using (StringFormat sf = new StringFormat(StringFormatFlags.NoWrap))
                        {
                            sf.LineAlignment = StringAlignment.Center;
                            sf.Trimming = StringTrimming.EllipsisCharacter;
                            b.DrawString(txt, Font, tb, new RectangleF(8, 0, Width - 34, Height), sf);
                        }
                        Chevron(b, new Rectangle(Width - 24, 1, 23, Height - 2));
                        using (GraphicsPath rp = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 9))
                            using (Pen pen = new Pen(Focused ? Theme.AccentHi : Theme.Line, 1f)) b.DrawPath(pen, rp);
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
                    using (GraphicsPath rp = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 9))
                    {
                        g.SetClip(rp);
                        using (SolidBrush b = new SolidBrush(Theme.Field)) g.FillRectangle(b, Width - 25, 0, 25, Height);
                        Chevron(g, btn);
                        g.SetClip(rp, CombineMode.Exclude);
                        g.Clear(Theme.Card); // coins arrondis : on repeint l'extérieur
                        g.ResetClip();
                        using (Pen pen = new Pen(Focused ? Theme.AccentHi : Theme.Line, 1f)) g.DrawPath(pen, rp);
                    }
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
            g.Clear(Theme.Side);
            // sélection en pastille arrondie (comme une barre latérale macOS)
            Rectangle pill = new Rectangle(10, 3, Width - 21, Height - 7);
            if (Selected || hover || Focused)
                using (GraphicsPath p = Theme.Round(pill, 11))
                {
                    using (SolidBrush b = new SolidBrush(Selected ? Color.FromArgb(36, 50, 78) : Color.FromArgb(20, 30, 48))) g.FillPath(b, p);
                    if (Selected) using (Pen pen = new Pen(Color.FromArgb(70, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                }
            if (Selected) using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillEllipse(b, 24, Height / 2 - 3, 6, 6);
            using (Font f = new Font("Segoe UI Semibold", 10.5f))
                TextRenderer.DrawText(g, Text, f, new Rectangle(40, 0, Width - 40, Height), Selected ? Theme.Fg : Theme.Dim, Theme.Left);
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
            using (Font f = Theme.Display(21f))
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
            Theme.Backdrop(this, g); // les coins laissent voir le dégradé de la page
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, 14))
            {
                // surface en verre sombre : plus claire en haut, bordure fine et liseré de lumière
                using (SolidBrush b = new SolidBrush(Theme.Card)) g.FillPath(b, p); // fond uni : les contrôles posés dessus se raccordent sans cadre
                using (Pen pen = new Pen(Color.FromArgb(150, Theme.Line), 1f)) g.DrawPath(pen, p);
                g.SetClip(p);
                using (Pen hi = new Pen(Color.FromArgb(22, 255, 255, 255), 1f)) g.DrawLine(hi, 14, 1, Width - 15, 1);
                g.ResetClip();
            }
        }
    }

    // ---------- Page : fond en dégradé ----------
    public class PagePanel : Panel
    {
        public PagePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { Theme.Backdrop(this, e.Graphics); }
        protected override void OnPaint(PaintEventArgs e) { Theme.Backdrop(this, e.Graphics); }
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
        const int L = 101; // zone logique (centre en 50), affichée en x2 pour garder des pixels nets
        public CrossCfg Cfg = new CrossCfg();
        int bg = 0;
        static readonly string[] BgNames = new string[] { "Forêt", "Désert", "Neige", "Nuit", "Damier" };
        static readonly Color[][] Bgs = new Color[][] {
            new Color[] { Color.FromArgb(82, 112, 68), Color.FromArgb(32, 50, 28) },
            new Color[] { Color.FromArgb(208, 186, 144), Color.FromArgb(148, 126, 90) },
            new Color[] { Color.FromArgb(238, 243, 248), Color.FromArgb(178, 192, 208) },
            new Color[] { Color.FromArgb(24, 35, 58), Color.FromArgb(8, 12, 22) } };

        public CrossPreview()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.StandardClick, true);
            Size = new Size(202, 202);
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            bg = (bg + 1) % BgNames.Length;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            g.Clear(BackColor);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            bool light = bg == 1 || bg == 2;
            using (GraphicsPath p = Theme.Round(r, 12))
            {
                g.SetClip(p);
                if (bg == 4)
                {
                    // damier de transparence
                    using (SolidBrush a = new SolidBrush(Color.FromArgb(52, 57, 68))) using (SolidBrush b = new SolidBrush(Color.FromArgb(40, 44, 54)))
                        for (int y = 0; y < Height; y += 12)
                            for (int x = 0; x < Width; x += 12)
                                g.FillRectangle(((x / 12 + y / 12) % 2 == 0) ? a : b, x, y, 12, 12);
                }
                else
                {
                    using (LinearGradientBrush lg = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), Bgs[bg][0], Bgs[bg][1], 90f))
                        g.FillRectangle(lg, 0, 0, Width, Height);
                    // grain léger : évite l'aplat et donne un fond « terrain » pour juger le contraste
                    Random rnd = new Random(11 + bg);
                    for (int i = 0; i < 220; i++)
                    {
                        int x = rnd.Next(Width), y = rnd.Next(Height), s = rnd.Next(2, 5);
                        using (SolidBrush sb = new SolidBrush(rnd.Next(2) == 0 ? Color.FromArgb(24, 0, 0, 0) : Color.FromArgb(22, 255, 255, 255)))
                            g.FillRectangle(sb, x, y, s, s);
                    }
                }
                // repères discrets : axes centraux et points tous les 10 px
                Color gc = light ? Color.FromArgb(70, 0, 0, 0) : Color.FromArgb(60, 255, 255, 255);
                int cx = Width / 2, cy = Height / 2;
                using (Pen ax = new Pen(Color.FromArgb(gc.A / 2, gc), 1f))
                {
                    g.DrawLine(ax, 0, cy, Width, cy);
                    g.DrawLine(ax, cx, 0, cx, Height);
                }
                using (SolidBrush dot = new SolidBrush(Color.FromArgb(gc.A / 2, gc)))
                    for (int x = 20; x < Width; x += 20)
                        for (int y = 20; y < Height; y += 20)
                            if (Math.Abs(x - cx) > 1 && Math.Abs(y - cy) > 1) g.FillRectangle(dot, x - 1, y - 1, 2, 2);
                using (Bitmap bmp = new Bitmap(L, L, PixelFormat.Format32bppArgb))
                {
                    using (Graphics gb = Graphics.FromImage(bmp)) CrossRender.Draw(gb, Cfg, L / 2 + Cfg.OffX, L / 2 + Cfg.OffY);
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    using (ImageAttributes ia = new ImageAttributes())
                    {
                        ColorMatrix cm = new ColorMatrix();
                        cm.Matrix33 = Math.Max(0.1f, Cfg.AlphaPct / 100f); // opacité du viseur
                        ia.SetColorMatrix(cm);
                        g.DrawImage(bmp, new Rectangle(0, 0, L * 2, L * 2), 0, 0, L, L, GraphicsUnit.Pixel, ia);
                    }
                }
                g.ResetClip();
                using (Pen pen = new Pen(Theme.Line, 1f)) g.DrawPath(pen, p);
            }
            // nom du décor
            using (Font f = new Font("Segoe UI", 8f))
            {
                Size ts = TextRenderer.MeasureText(BgNames[bg], f);
                Rectangle pill = new Rectangle(8, Height - 26, ts.Width + 14, 18);
                using (GraphicsPath pp = Theme.Round(pill, 9)) using (SolidBrush pb = new SolidBrush(Color.FromArgb(150, 8, 12, 22))) g.FillPath(pb, pp);
                TextRenderer.DrawText(g, BgNames[bg], f, pill, Color.FromArgb(235, 240, 248), Theme.Center);
            }
        }
    }

    // ---------- Vignette de préréglage de viseur ----------
    public class PresetTile : Control
    {
        public CrossCfg Cfg = new CrossCfg();
        public bool Selected;
        bool hover;
        const int W = 34; // zone logique, affichée en x2

        public PresetTile()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            Size = new Size(68, 68);
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
            g.Clear(BackColor);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, 11))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), Color.FromArgb(30, 44, 72), Color.FromArgb(11, 17, 30), 90f)) g.FillPath(b, p);
                g.SetClip(p);
                using (Bitmap bmp = new Bitmap(W, W, PixelFormat.Format32bppArgb))
                {
                    using (Graphics gb = Graphics.FromImage(bmp)) CrossRender.Draw(gb, Cfg, W / 2, W / 2);
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(bmp, new Rectangle(0, 0, Width, Height), 0, 0, W, W, GraphicsUnit.Pixel);
                }
                g.ResetClip();
                using (Pen pen = new Pen(Selected ? Theme.Accent : (hover || Focused ? Theme.Dim : Theme.Line), Selected ? 2f : 1f)) g.DrawPath(pen, p);
            }
        }
    }

    // ---------- Miniature de préréglage : scène de nuit filtrée (aperçu approximatif) ----------
    public class SceneThumb : Control
    {
        public string Caption = "";
        public Settings Preset = new Settings();
        public bool Selected;
        bool hover;
        public string Kind = "night";

        public SceneThumb()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            Size = new Size(118, 86);
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

        // Approximation de l'effet du préréglage : saturation, contraste, luminosité, canaux, gamma.
        ImageAttributes Effect()
        {
            Settings s = Preset;
            float sat = s.Sat / 100f, c = s.Contrast / 100f, br = s.Bright / 50f * 0.12f;
            float gr = s.R / 100f, gg = s.G / 100f, gb = s.B / 100f;
            const float lr = 0.3086f, lg = 0.6094f, lb = 0.0820f;
            float[][] m = new float[][] {
                new float[] { (lr * (1 - sat) + sat) * c * gr, lr * (1 - sat) * c * gg, lr * (1 - sat) * c * gb, 0, 0 },
                new float[] { lg * (1 - sat) * c * gr, (lg * (1 - sat) + sat) * c * gg, lg * (1 - sat) * c * gb, 0, 0 },
                new float[] { lb * (1 - sat) * c * gr, lb * (1 - sat) * c * gg, (lb * (1 - sat) + sat) * c * gb, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0.5f - 0.5f * c + br, 0.5f - 0.5f * c + br, 0.5f - 0.5f * c + br, 0, 1 } };
            ImageAttributes ia = new ImageAttributes();
            ia.SetColorMatrix(new ColorMatrix(m));
            ia.SetGamma(Math.Max(0.3f, (float)Math.Pow(1.0 / Math.Max(0.3, s.Gamma / 100.0), 1.6))); // effet un peu accentué pour rester lisible en miniature
            return ia;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            g.Clear(BackColor);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, 10))
            {
                using (SolidBrush b = new SolidBrush(Selected ? Color.FromArgb(40, 37, 28) : Theme.Field)) g.FillPath(b, p);
                using (Pen pen = new Pen(Selected ? Theme.Accent : (hover ? Theme.Dim : Theme.Line), Selected ? 2f : 1f)) g.DrawPath(pen, p);
            }
            Rectangle img = new Rectangle(6, 6, Width - 13, Height - 36);
            using (GraphicsPath ip = Theme.Round(img, 7))
            {
                g.SetClip(ip);
                using (ImageAttributes ia = Effect())
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(SceneArt.Get(Kind), img, 0, 0, 240, 150, GraphicsUnit.Pixel, ia);
                }
                g.ResetClip();
            }
            TextRenderer.DrawText(g, Caption, Font, new Rectangle(0, Height - 28, Width, 24), Selected ? Theme.Fg : Theme.Dim, Theme.Center);
        }
    }

    // ---------- Scènes illustrées des miniatures de filtres (aucun élément de jeu) ----------
    public static class SceneArt
    {
        static readonly Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>();

        public static Bitmap Get(string kind)
        {
            Bitmap b;
            if (cache.TryGetValue(kind, out b)) return b;
            b = Build(kind);
            cache[kind] = b;
            return b;
        }

        static Color C(int r, int g, int b) { return Color.FromArgb(r, g, b); }

        static void Poly(Graphics g, Color c, params int[] xy)
        {
            Point[] p = new Point[xy.Length / 2];
            for (int i = 0; i < p.Length; i++) p[i] = new Point(xy[i * 2], xy[i * 2 + 1]);
            using (SolidBrush b = new SolidBrush(c)) g.FillPolygon(b, p);
        }

        static void Disc(Graphics g, Color c, int x, int y, int d)
        {
            using (SolidBrush b = new SolidBrush(c)) g.FillEllipse(b, x, y, d, d);
        }

        static void Pine(Graphics g, Color c, int x, int baseY, int h)
        {
            Poly(g, c, x - h / 3, baseY, x, baseY - h, x + h / 3, baseY);
        }

        static void Person(Graphics g, Color c, int x, int y)
        {
            Disc(g, c, x, y, 6);
            using (SolidBrush b = new SolidBrush(c))
            {
                g.FillRectangle(b, x - 2, y + 6, 10, 12);
                g.FillRectangle(b, x - 1, y + 18, 3, 8);
                g.FillRectangle(b, x + 4, y + 18, 3, 8);
            }
        }

        static Bitmap Build(string kind)
        {
            Bitmap bmp = new Bitmap(240, 150);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color top, bot;
                switch (kind)
                {
                    case "night": top = C(7, 10, 22); bot = C(34, 48, 77); break;
                    case "deepnight": top = C(3, 5, 12); bot = C(14, 20, 36); break;
                    case "cave": top = C(14, 16, 20); bot = C(30, 30, 34); break;
                    case "desert": top = C(252, 238, 204); bot = C(255, 248, 228); break;
                    case "snow": top = C(222, 234, 246); bot = C(246, 250, 255); break;
                    case "forest": top = C(122, 168, 122); bot = C(88, 132, 90); break;
                    case "sunset": top = C(58, 38, 92); bot = C(255, 146, 66); break;
                    case "fog": top = C(168, 174, 182); bot = C(204, 208, 213); break;
                    case "pvp": top = C(112, 142, 172); bot = C(176, 192, 202); break;
                    default: top = C(116, 168, 232); bot = C(196, 224, 250); break; // day
                }
                using (LinearGradientBrush sky = new LinearGradientBrush(new Rectangle(0, 0, 240, 150), top, bot, 90f))
                    g.FillRectangle(sky, 0, 0, 240, 150);

                if (kind == "night" || kind == "deepnight")
                {
                    int[][] stars = new int[][] { new int[] { 14, 11 }, new int[] { 41, 24 }, new int[] { 71, 8 }, new int[] { 98, 30 }, new int[] { 124, 13 }, new int[] { 158, 7 }, new int[] { 181, 26 }, new int[] { 210, 15 }, new int[] { 26, 41 }, new int[] { 139, 37 } };
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(kind == "night" ? 170 : 110, 207, 216, 255)))
                        foreach (int[] s in stars) g.FillEllipse(sb, s[0], s[1], 2, 2);
                    if (kind == "night") Disc(g, Color.FromArgb(120, 232, 238, 255), 184, 14, 16);
                    bool deep = kind == "deepnight";
                    Poly(g, deep ? C(10, 14, 26) : C(20, 28, 46), 0, 94, 40, 72, 86, 88, 170, 84, 240, 90, 240, 150, 0, 150);
                    Poly(g, deep ? C(7, 10, 19) : C(15, 22, 37), 0, 108, 52, 92, 105, 105, 195, 101, 240, 106, 240, 150, 0, 150);
                    Color tc = deep ? C(6, 9, 16) : C(11, 17, 29);
                    Pine(g, tc, 30, 112, 26); Pine(g, tc, 43, 114, 20); Pine(g, tc, 219, 110, 28);
                    using (SolidBrush hb = new SolidBrush(deep ? C(15, 12, 10) : C(23, 18, 15))) g.FillRectangle(hb, 148, 92, 36, 24);
                    Poly(g, deep ? C(18, 14, 12) : C(28, 22, 18), 144, 94, 166, 80, 188, 94);
                    Disc(g, Color.FromArgb(deep ? 150 : 205, 255, 179, 90), 153, 98, 5);
                    Poly(g, deep ? C(7, 11, 18) : C(11, 16, 26), 0, 120, 60, 113, 120, 121, 240, 117, 240, 150, 0, 150);
                    Person(g, deep ? C(12, 17, 26) : C(22, 31, 44), 76, 108);
                }
                else if (kind == "cave")
                {
                    Poly(g, C(8, 9, 11), 0, 0, 240, 0, 240, 40, 200, 54, 160, 30, 110, 52, 60, 28, 0, 46);
                    Poly(g, C(22, 22, 26), 0, 150, 0, 96, 40, 84, 90, 100, 150, 88, 210, 98, 240, 90, 240, 150);
                    Poly(g, C(16, 16, 19), 0, 150, 0, 120, 70, 112, 140, 124, 240, 116, 240, 150);
                    Disc(g, Color.FromArgb(70, 255, 170, 70), 160, 84, 34);
                    Disc(g, Color.FromArgb(220, 255, 190, 90), 174, 98, 6);
                    Person(g, C(30, 31, 36), 88, 96);
                }
                else if (kind == "desert")
                {
                    Disc(g, Color.FromArgb(90, 255, 255, 240), 150, 4, 86);
                    Disc(g, C(255, 254, 240), 178, 32, 30);
                    Poly(g, C(232, 204, 150), 0, 96, 50, 76, 110, 92, 190, 80, 240, 94, 240, 150, 0, 150);
                    Poly(g, C(214, 180, 124), 0, 116, 70, 100, 150, 114, 240, 104, 240, 150, 0, 150);
                    Poly(g, C(198, 164, 108), 0, 134, 90, 124, 240, 130, 240, 150, 0, 150);
                    Poly(g, C(166, 134, 92), 30, 118, 44, 104, 62, 118);
                    Poly(g, C(150, 120, 84), 170, 126, 186, 108, 210, 126);
                    Person(g, C(140, 110, 74), 100, 104);
                }
                else if (kind == "snow")
                {
                    Poly(g, C(246, 249, 253), 0, 90, 60, 70, 130, 86, 200, 74, 240, 88, 240, 150, 0, 150);
                    Poly(g, C(236, 242, 249), 0, 112, 80, 98, 170, 110, 240, 100, 240, 150, 0, 150);
                    Poly(g, C(244, 248, 252), 0, 130, 100, 122, 240, 128, 240, 150, 0, 150);
                    Color pc = C(70, 92, 104);
                    Pine(g, pc, 24, 112, 34); Pine(g, pc, 44, 116, 26); Pine(g, pc, 210, 110, 36); Pine(g, pc, 190, 114, 24);
                    Person(g, C(96, 112, 126), 100, 104);
                }
                else if (kind == "forest")
                {
                    Poly(g, C(70, 110, 66), 0, 92, 50, 74, 110, 88, 180, 78, 240, 90, 240, 150, 0, 150);
                    Poly(g, C(44, 84, 46), 0, 112, 70, 98, 150, 112, 240, 104, 240, 150, 0, 150);
                    Color t1 = C(26, 62, 32), t2 = C(34, 76, 40);
                    for (int x = 6; x < 240; x += 24) { Pine(g, (x / 24) % 2 == 0 ? t1 : t2, x, 112, 40 + (x % 3) * 6); }
                    Poly(g, C(38, 68, 34), 0, 124, 90, 116, 170, 124, 240, 118, 240, 150, 0, 150);
                    Person(g, C(30, 58, 34), 104, 106);
                }
                else if (kind == "sunset")
                {
                    Disc(g, Color.FromArgb(120, 255, 210, 130), 92, 64, 70);
                    Disc(g, C(255, 214, 140), 110, 80, 34);
                    Poly(g, C(70, 40, 52), 0, 100, 60, 84, 130, 98, 190, 86, 240, 98, 240, 150, 0, 150);
                    Poly(g, C(44, 28, 38), 0, 120, 90, 108, 170, 120, 240, 112, 240, 150, 0, 150);
                    Pine(g, C(30, 20, 28), 30, 116, 30); Pine(g, C(30, 20, 28), 204, 114, 34);
                    Person(g, C(34, 22, 30), 118, 108);
                }
                else if (kind == "fog")
                {
                    Poly(g, C(158, 166, 172), 0, 96, 50, 80, 120, 92, 190, 82, 240, 94, 240, 150, 0, 150);
                    Poly(g, C(150, 158, 164), 0, 118, 80, 104, 170, 116, 240, 108, 240, 150, 0, 150);
                    Color fc = C(136, 144, 150);
                    Pine(g, fc, 30, 114, 34); Pine(g, fc, 54, 116, 26); Pine(g, fc, 200, 112, 36);
                    Person(g, C(132, 140, 146), 110, 108);
                }
                else if (kind == "pvp")
                {
                    Poly(g, C(104, 112, 100), 0, 94, 50, 80, 120, 90, 190, 78, 240, 92, 240, 150, 0, 150);
                    Poly(g, C(88, 96, 84), 0, 116, 80, 102, 170, 114, 240, 106, 240, 150, 0, 150);
                    Poly(g, C(120, 122, 116), 20, 120, 40, 96, 70, 120);
                    Poly(g, C(110, 112, 106), 170, 122, 196, 98, 224, 122);
                    Person(g, C(72, 80, 72), 96, 106);
                    Person(g, C(74, 82, 74), 148, 108);
                }
                else
                {
                    // jour
                    Disc(g, Color.FromArgb(230, 255, 250, 220), 196, 14, 22);
                    Poly(g, C(86, 134, 74), 0, 94, 50, 76, 110, 90, 180, 80, 240, 92, 240, 150, 0, 150);
                    Poly(g, C(62, 112, 56), 0, 114, 70, 100, 150, 112, 240, 104, 240, 150, 0, 150);
                    Pine(g, C(36, 78, 40), 30, 114, 34); Pine(g, C(36, 78, 40), 212, 110, 36);
                    using (SolidBrush hb = new SolidBrush(C(150, 110, 84))) g.FillRectangle(hb, 148, 94, 36, 24);
                    Poly(g, C(176, 80, 62), 144, 96, 166, 80, 188, 96);
                    Poly(g, C(58, 100, 50), 0, 126, 100, 120, 240, 124, 240, 150, 0, 150);
                    Person(g, C(44, 70, 44), 88, 110);
                }
            }
            return bmp;
        }
    }
}
