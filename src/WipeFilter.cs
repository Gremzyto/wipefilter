using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace RustFilter
{
    public static class Native
    {
        [DllImport("Magnification.dll", SetLastError = true)] public static extern bool MagInitialize();
        [DllImport("Magnification.dll", SetLastError = true)] public static extern bool MagUninitialize();
        [DllImport("Magnification.dll", SetLastError = true)] public static extern bool MagSetFullscreenColorEffect(float[] effect);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);
        [DllImport("gdi32.dll")] public static extern bool SetDeviceGammaRamp(IntPtr hdc, ushort[] ramp);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    }

    public class Settings
    {
        public int Bright = 0, Contrast = 100, Sat = 100, Gamma = 100, R = 100, G = 100, B = 100;
        public string Ser()
        {
            return Bright + ";" + Contrast + ";" + Sat + ";" + Gamma + ";" + R + ";" + G + ";" + B;
        }
        public static Settings Parse(string s)
        {
            Settings r = new Settings();
            try
            {
                string[] p = s.Split(';');
                r.Bright = int.Parse(p[0]); r.Contrast = int.Parse(p[1]); r.Sat = int.Parse(p[2]);
                r.Gamma = int.Parse(p[3]); r.R = int.Parse(p[4]); r.G = int.Parse(p[5]); r.B = int.Parse(p[6]);
            }
            catch { }
            return r;
        }
        public static Settings Make(int br, int co, int sa, int ga, int r, int g, int b)
        {
            Settings s = new Settings();
            s.Bright = br; s.Contrast = co; s.Sat = sa; s.Gamma = ga; s.R = r; s.G = g; s.B = b;
            return s;
        }
    }

    public static class Filter
    {
        static double lastGamma = -1;

        public static string Apply(Settings s, bool on)
        {
            if (App.TestMode) return "";
            string err = "";
            float[] m = new float[25];
            if (!on)
            {
                m[0] = 1; m[6] = 1; m[12] = 1; m[18] = 1; m[24] = 1;
            }
            else
            {
                float sat = s.Sat / 100f, c = s.Contrast / 100f, b = s.Bright / 200f;
                float[] l = new float[] { 0.2126f, 0.7152f, 0.0722f };
                float[] g = new float[] { s.R / 100f, s.G / 100f, s.B / 100f };
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        m[i * 5 + j] = g[j] * c * ((1 - sat) * l[i] + (i == j ? sat : 0f));
                for (int j = 0; j < 3; j++)
                    m[20 + j] = g[j] * (0.5f * (1 - c) + b);
                m[18] = 1; m[24] = 1;
            }
            if (!Native.MagSetFullscreenColorEffect(m))
                err += "Couche couleur refusee (code " + Marshal.GetLastWin32Error() + "). ";

            double gm = on ? s.Gamma / 100.0 : 1.0;
            if (gm != lastGamma)
            {
                if (SetGamma(gm)) lastGamma = gm;
                else err += "Gamma refuse par Windows (baisse le gamma). ";
            }
            return err;
        }

        public static bool SetGamma(double gm)
        {
            ushort[] ramp = new ushort[768];
            for (int i = 0; i < 256; i++)
            {
                double v = Math.Pow(i / 255.0, 1.0 / gm) * 65535.0;
                if (v < 0) v = 0;
                if (v > 65535) v = 65535;
                ushort u = (ushort)v;
                ramp[i] = u; ramp[256 + i] = u; ramp[512 + i] = u;
            }
            IntPtr dc = Native.GetDC(IntPtr.Zero);
            bool ok = Native.SetDeviceGammaRamp(dc, ramp);
            Native.ReleaseDC(IntPtr.Zero, dc);
            return ok;
        }

        public static void Reset()
        {
            if (App.TestMode) return;
            float[] m = new float[25];
            m[0] = 1; m[6] = 1; m[12] = 1; m[18] = 1; m[24] = 1;
            Native.MagSetFullscreenColorEffect(m);
            SetGamma(1.0);
            lastGamma = 1.0;
        }
    }

    // ---------- Viseur ----------
    public class CrossCfg
    {
        public bool On = false, Outline = true;
        public int Style = 2, Argb = Color.FromArgb(0, 255, 120).ToArgb(), Size = 6, Thick = 2, Gap = 3, OffX = 0, OffY = 0;
        public string Ser()
        {
            return (On ? 1 : 0) + ";" + Style + ";" + Argb + ";" + Size + ";" + Thick + ";" + Gap + ";" + (Outline ? 1 : 0) + ";" + OffX + ";" + OffY;
        }
        public static CrossCfg Parse(string s)
        {
            CrossCfg c = new CrossCfg();
            try
            {
                string[] p = s.Split(';');
                c.On = p[0] == "1"; c.Style = int.Parse(p[1]); c.Argb = int.Parse(p[2]); c.Size = int.Parse(p[3]);
                c.Thick = int.Parse(p[4]); c.Gap = int.Parse(p[5]); c.Outline = p[6] == "1";
                c.OffX = int.Parse(p[7]); c.OffY = int.Parse(p[8]);
            }
            catch { }
            return c;
        }
    }

    public class OverlayForm : Form
    {
        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            DoubleBuffered = true;
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x8000000 | 0x8; // layered, transparent, toolwindow, noactivate, topmost
                return cp;
            }
        }
        public void KeepOnTop()
        {
            if (Visible) Native.SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x13);
        }
    }

    public class CrosshairForm : OverlayForm
    {
        public CrossCfg Cfg = new CrossCfg();
        const int S = 201;

        public CrosshairForm() { Size = new Size(S, S); }

        public void UpdateCfg(CrossCfg c)
        {
            Cfg = c;
            Rectangle sb = Screen.PrimaryScreen.Bounds;
            Location = new Point(sb.Left + sb.Width / 2 - S / 2 + c.OffX, sb.Top + sb.Height / 2 - S / 2 + c.OffY);
            Size = new Size(S, S);
            if (c.On && !Visible && !App.TestMode) Show();
            if (!c.On && Visible) Hide();
            Invalidate();
        }

        void Box(Graphics g, Brush fill, int x, int y, int w, int h)
        {
            if (w <= 0 || h <= 0) return;
            if (Cfg.Outline) g.FillRectangle(Brushes.Black, x - 1, y - 1, w + 2, h + 2);
            g.FillRectangle(fill, x, y, w, h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            Color col = Color.FromArgb(Cfg.Argb);
            if (col.R == 255 && col.G == 0 && col.B == 255) col = Color.FromArgb(254, 0, 255);
            col = Color.FromArgb(255, col);
            int c = S / 2, t = Math.Max(1, Cfg.Thick), len = Cfg.Size, gap = Cfg.Gap, h = t / 2;
            using (SolidBrush br = new SolidBrush(col))
            {
                bool arms = Cfg.Style == 1 || Cfg.Style == 2 || Cfg.Style == 4;
                if (arms)
                {
                    Box(g, br, c - gap - len, c - h, len, t);          // gauche
                    Box(g, br, c + gap + 1 - (t % 2 == 0 ? 0 : 0), c - h, len, t); // droite
                    Box(g, br, c - h, c + gap + 1, t, len);              // bas
                    if (Cfg.Style != 4) Box(g, br, c - h, c - gap - len, t, len); // haut (pas en T)
                }
                if (Cfg.Style == 3)
                {
                    int r = Math.Max(3, len);
                    if (Cfg.Outline)
                        using (Pen po = new Pen(Color.Black, t + 2)) g.DrawEllipse(po, c - r, c - r, r * 2, r * 2);
                    using (Pen p = new Pen(col, t)) g.DrawEllipse(p, c - r, c - r, r * 2, r * 2);
                }
                if (Cfg.Style == 0 || Cfg.Style == 2 || Cfg.Style == 3)
                {
                    int d = Math.Max(2, t);
                    Box(g, br, c - d / 2, c - d / 2, d, d);
                }
            }
        }
    }

    public class OsdForm : OverlayForm
    {
        string text = "";
        System.Windows.Forms.Timer hideTimer = new System.Windows.Forms.Timer();

        public OsdForm()
        {
            Size = new Size(520, 44);
            hideTimer.Interval = 1600;
            hideTimer.Tick += delegate { hideTimer.Stop(); Hide(); };
        }

        public void Flash(string t)
        {
            text = t;
            Rectangle sb = Screen.PrimaryScreen.Bounds;
            Location = new Point(sb.Left + sb.Width / 2 - Width / 2, sb.Top + 60);
            if (!Visible) Show();
            KeepOnTop();
            Invalidate();
            hideTimer.Stop(); hideTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            using (Font f = new Font("Segoe UI", 15f, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(text, f);
                float x = (Width - sz.Width) / 2, y = (Height - sz.Height) / 2;
                for (int dx = -2; dx <= 2; dx += 2)
                    for (int dy = -2; dy <= 2; dy += 2)
                        g.DrawString(text, f, Brushes.Black, x + dx, y + dy);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 235, 230)))
                    g.DrawString(text, f, b, x, y);
            }
        }
    }

    // ---------- Fenetre principale ----------
    // ---------- Resolution ----------
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public int dmFields, dmPositionX, dmPositionY, dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }

    public static class Display
    {
        [DllImport("user32.dll", CharSet = CharSet.Ansi)] static extern bool EnumDisplaySettings(string dev, int mode, ref DEVMODE dm);
        [DllImport("user32.dll", CharSet = CharSet.Ansi)] static extern int ChangeDisplaySettingsEx(string dev, ref DEVMODE dm, IntPtr hwnd, uint flags, IntPtr p);
        [DllImport("user32.dll", CharSet = CharSet.Ansi)] static extern int ChangeDisplaySettingsEx(string dev, IntPtr dm, IntPtr hwnd, uint flags, IntPtr p);

        static DEVMODE New()
        {
            DEVMODE d = new DEVMODE();
            d.dmDeviceName = new string(' ', 32);
            d.dmFormName = new string(' ', 32);
            d.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            return d;
        }

        public static DEVMODE Current()
        {
            DEVMODE d = New();
            EnumDisplaySettings(null, -1, ref d);
            return d;
        }

        public static string Label(DEVMODE d)
        {
            return d.dmPelsWidth + " x " + d.dmPelsHeight + "  @ " + d.dmDisplayFrequency + " Hz";
        }

        public static List<DEVMODE> Modes()
        {
            List<DEVMODE> list = new List<DEVMODE>();
            List<string> seen = new List<string>();
            DEVMODE d = New();
            for (int i = 0; EnumDisplaySettings(null, i, ref d); i++)
            {
                if (d.dmBitsPerPel != 32 || d.dmPelsWidth < 800) continue;
                string k = Label(d);
                if (!seen.Contains(k)) { seen.Add(k); list.Add(d); }
                d = New();
            }
            list.Sort(delegate(DEVMODE a, DEVMODE b)
            {
                int c = (b.dmPelsWidth * b.dmPelsHeight).CompareTo(a.dmPelsWidth * a.dmPelsHeight);
                if (c != 0) return c;
                c = b.dmPelsWidth.CompareTo(a.dmPelsWidth);
                if (c != 0) return c;
                return b.dmDisplayFrequency.CompareTo(a.dmDisplayFrequency);
            });
            return list;
        }

        public static bool Apply(DEVMODE d)
        {
            d.dmFields = 0x80000 | 0x100000 | 0x400000;
            if (ChangeDisplaySettingsEx(null, ref d, IntPtr.Zero, 2, IntPtr.Zero) != 0) return false; // CDS_TEST
            return ChangeDisplaySettingsEx(null, ref d, IntPtr.Zero, 0, IntPtr.Zero) == 0;   // temporaire
        }

        public static void RestoreWindows()
        {
            ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        }
    }

    public class ConfirmForm : Form
    {
        int left = 15;
        Label lbl;
        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();

        public ConfirmForm()
        {
            Text = "WipeFilter";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true; MinimizeBox = false; MaximizeBox = false;
            ClientSize = new Size(360, 120);
            BackColor = Color.FromArgb(18, 18, 22); ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            lbl = new Label();
            lbl.SetBounds(16, 14, 330, 50);
            Controls.Add(lbl);
            Button ok = new Button(); ok.Text = "Garder"; ok.SetBounds(60, 74, 110, 30);
            ok.FlatStyle = FlatStyle.Flat; ok.DialogResult = DialogResult.OK; Controls.Add(ok);
            Button no = new Button(); no.Text = "Revenir"; no.SetBounds(190, 74, 110, 30);
            no.FlatStyle = FlatStyle.Flat; no.DialogResult = DialogResult.Cancel; Controls.Add(no);
            AcceptButton = ok; CancelButton = no;
            t.Interval = 1000;
            t.Tick += delegate
            {
                left--;
                Upd();
                if (left <= 0) { t.Stop(); DialogResult = DialogResult.Cancel; }
            };
            Upd();
            t.Start();
        }

        void Upd() { lbl.Text = "Garder cette resolution ?\nRetour automatique dans " + left + " s."; }
    }

    // ---------- Codes de partage ----------
    public static class Codes
    {
        public static string Enc(string s)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static string Dec(string s)
        {
            s = s.Trim().Replace('-', '+').Replace('_', '/');
            while (s.Length % 4 != 0) s += "=";
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }
    }

    // ---------- Passerelle avec le viseur natif de Rust (RUST-xxxxx-xxx, console F1) ----------
    // Entier de 48 bits en base 62 : bit0=1, bits6-7 style, 8-10 couleur, 11 contour, 12-14 couleur contour,
    // 15-23 point x100, 24-31 (longueur-1)x10, 32-38 (epaisseur-1)x10, 39-47 espacement x100.
    // Conversion approximative (meme regles que le site) : couleur = la plus proche de la palette du jeu.
    public static class NativeCode
    {
        const string AL = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        static readonly int[][] Pal = new int[][] {
            new int[] {255,255,255}, new int[] {0,255,0}, new int[] {0,0,255}, new int[] {255,0,0},
            new int[] {255,233,0}, new int[] {255,0,205}, new int[] {0,255,255}, new int[] {0,0,0} };

        static int CI(double v, int a, int b) { int r = (int)Math.Round(v, MidpointRounding.AwayFromZero); return Math.Max(a, Math.Min(b, r)); }
        static double R2(double v) { return Math.Round(v, 2, MidpointRounding.AwayFromZero); }

        static int Nearest(int argb)
        {
            Color c = Color.FromArgb(argb);
            int best = 0; double bd = double.MaxValue;
            for (int i = 0; i < Pal.Length; i++)
            {
                double d = Math.Pow(Pal[i][0] - c.R, 2) + Math.Pow(Pal[i][1] - c.G, 2) + Math.Pow(Pal[i][2] - c.B, 2);
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        public static string FromCross(CrossCfg c)
        {
            int style = 0; double dot = 0, len = 7, wid = 2, sp = 1;
            if (c.Style == 0) { style = 1; dot = Math.Max(0, Math.Min(5, c.Size / 5.0)); }
            else
            {
                style = c.Style == 4 ? 2 : 0;
                dot = (c.Style == 2 || c.Style == 3) ? 1.7 : 0;
                len = Math.Max(1, Math.Min(20, c.Size / 2.0));
                wid = Math.Max(1, Math.Min(8, c.Thick));
                sp = Math.Max(0, Math.Min(3, c.Gap * 0.15));
            }
            ulong v = 1UL | ((ulong)style << 6) | ((ulong)Nearest(c.Argb) << 8) | ((ulong)(c.Outline ? 1 : 0) << 11)
                | ((ulong)CI(dot * 100, 0, 500) << 15) | ((ulong)CI((len - 1) * 10, 0, 190) << 24)
                | ((ulong)CI((wid - 1) * 10, 0, 70) << 32) | ((ulong)CI(sp * 100, 0, 300) << 39);
            string s = "";
            while (v > 0) { s = AL[(int)(v % 62)] + s; v /= 62; }
            return "RUST-" + s.Substring(0, 5) + "-" + s.Substring(5);
        }

        public static bool LooksNative(string code)
        {
            return code != null && System.Text.RegularExpressions.Regex.IsMatch(code.Trim(), @"^(crosshair\.load_code\s+)?RUST-", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Leve une exception si le code est invalide.
        public static CrossCfg ToCross(string code)
        {
            code = System.Text.RegularExpressions.Regex.Replace(code.Trim(), @"^crosshair\.load_code\s+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(code, @"^RUST-([0-9A-Za-z]{5})-([0-9A-Za-z]{1,6})$");
            if (!m.Success) throw new FormatException("code");
            ulong v = 0;
            foreach (char ch in m.Groups[1].Value + m.Groups[2].Value) v = v * 62 + (ulong)AL.IndexOf(ch);
            if ((v >> 48) != 0 || (v & 1) != 1) throw new FormatException("code");
            Func<int, int, int> f = delegate (int sh, int bits) { return (int)((v >> sh) & ((1UL << bits) - 1)); };
            int ns = f(6, 2);
            if (ns > 2) throw new FormatException("code");
            double dot = f(15, 9) / 100.0, len = f(24, 8) / 10.0 + 1, wid = f(32, 7) / 10.0 + 1, sp = f(39, 9) / 100.0;
            if (dot > 5 || len > 20 || wid > 8 || sp > 3) throw new FormatException("code");
            int[] p = Pal[f(8, 3)];
            CrossCfg c = new CrossCfg();
            c.On = true; c.Outline = f(11, 1) == 1; c.OffX = 0; c.OffY = 0;
            c.Argb = Color.FromArgb(255, p[0], p[1], p[2]).ToArgb();
            if (ns == 1) { c.Style = 0; c.Size = CI(dot * 5, 1, 40); c.Thick = 2; c.Gap = 3; }
            else { c.Style = ns == 2 ? 4 : (dot > 0.3 ? 2 : 1); c.Size = CI(len * 2, 1, 40); c.Thick = CI(wid, 1, 8); c.Gap = CI(sp / 0.15, 0, 20); }
            return c;
        }
    }

    public class MainForm : Form
    {
        static readonly Color Bg = Theme.Bg;
        static readonly Color Panel = Theme.Field;
        static readonly Color Fg = Theme.Fg;
        static readonly Color Dim = Theme.Dim;
        static readonly Color Accent = Theme.Accent;

        string dir, profFile, lastFile, cfgFile;
        Dictionary<string, Settings> profiles = new Dictionary<string, Settings>();
        List<string> names = new List<string>();
        Dictionary<string, string> gameMap = new Dictionary<string, string>();
        Dictionary<string, Keys> binds = new Dictionary<string, Keys>();
        List<string> hotActions = new List<string>();
        List<string> bindRows = new List<string>();
        string captureAction = null;
        bool loading = false, quitting = false, ready = false, autoPaused = false, hasBindCfg = false;
        string autoProfile = null;
        int myPid;

        ModernSlider tBright, tContrast, tSat, tGamma, tR, tG, tB;
        ModernSlider cSize, cThick, cGap, cOffX, cOffY;
        ToggleSwitch chkOn, chkCross, chkOutline, chkAuto, chkOsd;
        ComboBox cbProf, cbStyle, cbGameProf, cbPreset, cbRes;
        TextBox tbExe;
        ListBox lbGames, lbBinds;
        Label status, lblRes, titleLbl, subLbl;
        RoundButton bColor;
        CrossPreview preview;
        NavButton[] navs;
        Panel[] pages;
        ToolTip tips;
        NotifyIcon tray;
        CrosshairForm cross = new CrosshairForm();
        OsdForm osd = new OsdForm();
        System.Windows.Forms.Timer tick = new System.Windows.Forms.Timer();
        int crossArgb;
        List<DEVMODE> modes = new List<DEVMODE>();
        EventHandler displayChanged;

        static readonly string[] PageTitles = new string[] { "Couleurs", "Viseur", "Touches", "Jeux", "Résolution" };
        static readonly string[] PageSubs = new string[] {
            "Règle les filtres de couleur en temps réel.",
            "Crée ton viseur, vois-le en direct et partage-le avec un code.",
            "Choisis tes raccourcis clavier.",
            "Un profil différent selon le jeu.",
            "Change la résolution (4:3 étiré), de façon temporaire." };

        public MainForm()
        {
            myPid = Process.GetCurrentProcess().Id;
            dir = App.DataDir();
            Directory.CreateDirectory(dir);
            profFile = Path.Combine(dir, "profils.txt");
            lastFile = Path.Combine(dir, "dernier.txt");
            cfgFile = Path.Combine(dir, "config.txt");

            Text = "WipeFilter";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Theme.Bg; ForeColor = Theme.Fg;
            Font = new Font("Segoe UI", 9.5f);
            ClientSize = new Size(800, 660);
            StartPosition = FormStartPosition.CenterScreen;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { Icon = SystemIcons.Application; }
            KeyPreview = true;
            tips = new ToolTip();

            // --- barre latérale ---
            Panel side = new Panel();
            side.SetBounds(0, 0, 200, 660);
            side.BackColor = Theme.Side;
            Controls.Add(side);
            LogoLabel logo = new LogoLabel();
            logo.Location = new Point(22, 24);
            side.Controls.Add(logo);
            Label ver = MakeLabel(side, "v4.1  ·  wipefilter.fr", 24, 62, 160, Theme.Dim);
            ver.Font = new Font("Segoe UI", 8.5f);

            navs = new NavButton[5];
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                NavButton nb = new NavButton();
                nb.Text = PageTitles[i];
                nb.SetBounds(0, 108 + i * 46, 200, 42);
                nb.Click += delegate { ShowPage(idx); };
                side.Controls.Add(nb);
                navs[i] = nb;
            }

            chkOn = MakeCheck(side, "Filtre actif", 22, 530, 170);
            chkOn.Checked = true;
            chkOn.CheckedChanged += delegate { ApplyNow(); };
            MakeButton(side, "Réduire dans la barre", 20, 576, 160).Click += delegate { HideToTray(); };

            // --- en-tête ---
            titleLbl = MakeLabel(this, "", 232, 22, 536, Theme.Fg);
            titleLbl.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            titleLbl.Height = 38;
            subLbl = MakeLabel(this, "", 232, 62, 536, Theme.Dim);

            // --- pages ---
            pages = new Panel[5];
            for (int i = 0; i < 5; i++)
            {
                Panel p = new Panel();
                p.SetBounds(232, 100, 536, 520);
                p.BackColor = Theme.Bg;
                Controls.Add(p);
                pages[i] = p;
            }
            status = MakeLabel(this, "", 232, 626, 536, Theme.Dim);
            status.Height = 26;

            // --- Couleurs ---
            Panel pCol = pages[0];
            CardPanel c1 = Card(pCol, 0, 0, 536, 292);
            int y = 14;
            tBright   = AddSlider(c1, "Luminosité", -50, 50, 0, ref y, "{0:+0;-0;0}", ColorChanged);
            tContrast = AddSlider(c1, "Contraste", 50, 200, 100, ref y, "{0}%", ColorChanged);
            tSat      = AddSlider(c1, "Saturation", 0, 300, 100, ref y, "{0}%", ColorChanged);
            tGamma    = AddSlider(c1, "Gamma", 50, 200, 100, ref y, "g", ColorChanged);
            tR        = AddSlider(c1, "Rouge", 50, 150, 100, ref y, "{0}%", ColorChanged);
            tG        = AddSlider(c1, "Vert", 50, 150, 100, ref y, "{0}%", ColorChanged);
            tB        = AddSlider(c1, "Bleu", 50, 150, 100, ref y, "{0}%", ColorChanged);

            CardPanel c2 = Card(pCol, 0, 304, 536, 156);
            int y2 = 16;
            MakeLabel(c2, "Profil", 18, y2 + 5, 60, Theme.Dim);
            cbProf = MakeCombo(c2, 80, y2 + 2, 226);
            cbProf.SelectedIndexChanged += delegate { if (!loading) LoadProfile(cbProf.Text, false); };
            MakeButton(c2, "Sauver", 316, y2, 94, true).Click += delegate { SaveProfile(); };
            MakeButton(c2, "Supprimer", 418, y2, 100).Click += delegate { DeleteProfile(); };
            y2 += 46;
            MakeButton(c2, "Copier le code", 18, y2, 160).Click += delegate { CopyFilterCode(); };
            MakeButton(c2, "Coller un code", 186, y2, 160).Click += delegate { ImportCode(); };
            MakeButton(c2, "Réinitialiser", 354, y2, 164).Click += delegate { SetSliders(new Settings()); ApplyNow(); };
            y2 += 46;
            Label hc = MakeLabel(c2, "Pour créer un filtre : règle les curseurs, écris un nom dans « Profil », clique sur Sauver.\nDouble-clic sur un curseur = valeur par défaut.", 18, y2, 500, Theme.Dim);
            hc.Height = 40; hc.Font = new Font("Segoe UI", 8.75f);

            // --- Viseur ---
            Panel pCross = pages[1];
            CardPanel top = Card(pCross, 0, 0, 536, 258);
            preview = new CrossPreview();
            preview.Location = new Point(10, 8);
            top.Controls.Add(preview);
            tips.SetToolTip(preview, "Clique pour changer le décor de l'aperçu");
            int rx = 270;
            chkCross = MakeCheck(top, "Afficher le viseur", rx, 10, 250);
            chkCross.CheckedChanged += delegate { CrossChanged(); };
            MakeLabel(top, "Preset", rx, 44, 120, Theme.Dim).Font = new Font("Segoe UI", 8.75f);
            cbPreset = MakeCombo(top, rx, 64, 250);
            cbPreset.DropDownStyle = ComboBoxStyle.DropDownList;
            ((DarkCombo)cbPreset).Placeholder = "Choisir un preset…";
            cbPreset.Items.AddRange(new object[] { "Classique vert", "Point rouge", "Croix cyan fine", "Cercle jaune", "T blanc", "Petit point blanc", "Croix rose large" });
            cbPreset.SelectedIndexChanged += delegate { ApplyPreset(cbPreset.SelectedIndex); };
            MakeLabel(top, "Style", rx, 100, 120, Theme.Dim).Font = new Font("Segoe UI", 8.75f);
            cbStyle = MakeCombo(top, rx, 120, 250);
            cbStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            cbStyle.Items.AddRange(new object[] { "Point", "Croix", "Croix + point", "Cercle + point", "T" });
            cbStyle.SelectedIndexChanged += delegate { CrossChanged(); };
            MakeLabel(top, "Couleur", rx, 156, 120, Theme.Dim).Font = new Font("Segoe UI", 8.75f);
            Color[] quick = new Color[] {
                Color.FromArgb(0, 255, 120), Color.FromArgb(255, 40, 40), Color.FromArgb(0, 230, 255), Color.FromArgb(255, 220, 0),
                Color.White, Color.FromArgb(255, 80, 200), Color.FromArgb(255, 140, 0), Color.FromArgb(170, 110, 255) };
            for (int i = 0; i < quick.Length; i++)
            {
                Color qc = quick[i];
                ColorChip chip = new ColorChip();
                chip.Value = qc;
                chip.Location = new Point(rx + i * 32, 178);
                chip.Click += delegate { crossArgb = qc.ToArgb(); CrossChanged(); };
                top.Controls.Add(chip);
            }
            bColor = MakeButton(top, "Couleur perso…", rx, 214, 120);
            bColor.Click += delegate { PickColor(); };
            MakeButton(top, "Code Rust (F1)", rx + 128, 214, 122).Click += delegate { CopyNativeCrossCode(); };
            tips.SetToolTip(top.Controls[top.Controls.Count - 1], "Copie la commande crosshair.load_code à coller dans la console de Rust (F1)");

            CardPanel bot = Card(pCross, 0, 268, 536, 250);
            y = 10;
            cSize  = AddSlider(bot, "Taille", 1, 40, 6, ref y, "{0}px", CrossChanged);
            cThick = AddSlider(bot, "Épaisseur", 1, 8, 2, ref y, "{0}px", CrossChanged);
            cGap   = AddSlider(bot, "Écart", 0, 20, 3, ref y, "{0}px", CrossChanged);
            cOffX  = AddSlider(bot, "Décalage X", -60, 60, 0, ref y, "{0}", CrossChanged);
            cOffY  = AddSlider(bot, "Décalage Y", -60, 60, 0, ref y, "{0}", CrossChanged);
            chkOutline = MakeCheck(bot, "Contour noir", 18, y + 8, 220);
            chkOutline.Checked = true;
            chkOutline.CheckedChanged += delegate { CrossChanged(); };
            MakeButton(bot, "Copier le code", 252, y + 4, 128).Click += delegate { CopyCrossCode(); };
            MakeButton(bot, "Coller un code", 388, y + 4, 130).Click += delegate { ImportCode(); };

            // --- Touches ---
            Panel pKeys = pages[2];
            MakeLabel(pKeys, "Choisis une action, puis clique sur « Changer la touche ».", 0, 0, 536, Theme.Fg);
            CardPanel kc = Card(pKeys, 0, 30, 536, 300);
            lbBinds = new ListBox();
            lbBinds.SetBounds(8, 8, 520, 284);
            StyleList(lbBinds, new Font("Consolas", 10f));
            lbBinds.DoubleClick += delegate { StartCapture(); };
            kc.Controls.Add(lbBinds);
            MakeButton(pKeys, "Changer la touche", 0, 342, 180, true).Click += delegate { StartCapture(); };
            MakeButton(pKeys, "Retirer", 188, 342, 110).Click += delegate { ClearBind(); };
            MakeButton(pKeys, "Touches par défaut", 306, 342, 190).Click += delegate { DefaultBinds(); RefreshBinds(); RegisterAll(); SaveConfig(); };
            Label hk = MakeLabel(pKeys, "Conseil : utilise Ctrl/Shift/Alt + une touche, ou les touches F / pavé numérique,\npour ne pas voler une touche du jeu. Sur AZERTY, évite Ctrl+Alt (= AltGr).", 0, 392, 536, Theme.Dim);
            hk.Height = 40; hk.Font = new Font("Segoe UI", 8.75f);
            chkOsd = MakeCheck(pKeys, "Afficher le nom du profil à l'écran quand il change", 0, 446, 520);
            chkOsd.Checked = true;
            chkOsd.CheckedChanged += delegate { SaveConfig(); };

            // --- Jeux ---
            Panel pGames = pages[3];
            chkAuto = MakeCheck(pGames, "Changer de profil automatiquement selon le jeu", 0, 0, 520);
            chkAuto.CheckedChanged += delegate { autoProfile = null; SaveConfig(); if (!chkAuto.Checked && autoPaused) { autoPaused = false; ApplyNow(); } };
            CardPanel gc = Card(pGames, 0, 40, 536, 68);
            MakeLabel(gc, "Jeu (.exe)", 18, 22, 80, Theme.Dim);
            tbExe = new TextBox();
            tbExe.SetBounds(100, 20, 130, 28);
            tbExe.BackColor = Theme.Field; tbExe.ForeColor = Theme.Fg; tbExe.BorderStyle = BorderStyle.FixedSingle;
            tbExe.Text = "RustClient";
            gc.Controls.Add(tbExe);
            cbGameProf = MakeCombo(gc, 238, 20, 170);
            cbGameProf.DropDownStyle = ComboBoxStyle.DropDownList;
            MakeButton(gc, "Associer", 416, 18, 102, true).Click += delegate { AddGame(); };
            CardPanel lc = Card(pGames, 0, 120, 536, 230);
            lbGames = new ListBox();
            lbGames.SetBounds(8, 8, 520, 214);
            StyleList(lbGames, new Font("Segoe UI", 10f));
            lc.Controls.Add(lbGames);
            MakeButton(pGames, "Retirer", 0, 362, 110).Click += delegate { RemoveGame(); };
            Label hg = MakeLabel(pGames, "Ex : Rust = RustClient, CS2 = cs2, Valorant = VALORANT-Win64-Shipping,\nFortnite = FortniteClient-Win64-Shipping, Apex = r5apex.\nHors d'un jeu associé, le filtre se met en pause.", 0, 410, 536, Theme.Dim);
            hg.Height = 62; hg.Font = new Font("Segoe UI", 8.75f);

            // --- Résolution ---
            Panel pRes = pages[4];
            CardPanel rc = Card(pRes, 0, 0, 536, 146);
            lblRes = MakeLabel(rc, "", 18, 16, 500, Theme.Fg);
            lblRes.Font = new Font("Segoe UI Semibold", 10.5f);
            cbRes = MakeCombo(rc, 18, 52, 340);
            cbRes.DropDownStyle = ComboBoxStyle.DropDownList;
            MakeButton(rc, "Appliquer", 368, 50, 150, true).Click += delegate { ApplyRes(); };
            MakeButton(rc, "Remettre la résolution Windows", 18, 100, 340).Click += delegate { Display.RestoreWindows(); RefreshRes(); };
            Label hr = MakeLabel(pRes, "Après un changement, tu as 15 s pour confirmer, sinon ça revient tout seul.\n\n4:3 étiré (ex : 1440x1080, 1280x960) : mets la mise à l'échelle sur « Plein écran »\ndans le panneau NVIDIA/AMD, sinon tu auras des bandes noires.\n\nLe changement est temporaire : il disparaît au redémarrage du PC.", 0, 162, 536, Theme.Dim);
            hr.Height = 130; hr.Font = new Font("Segoe UI", 9f);

            tray = new NotifyIcon();
            tray.Icon = Icon;
            tray.Text = "WipeFilter";
            tray.Visible = !App.TestMode;
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add("Ouvrir", delegate { ShowFromTray(); });
            menu.MenuItems.Add("Filtre on/off", delegate { chkOn.Checked = !chkOn.Checked; });
            menu.MenuItems.Add("Viseur on/off", delegate { chkCross.Checked = !chkCross.Checked; });
            menu.MenuItems.Add("Quitter", delegate { quitting = true; Close(); });
            tray.ContextMenu = menu;
            tray.DoubleClick += delegate { ShowFromTray(); };

            tick.Interval = 1000;
            tick.Tick += delegate { OnTick(); };

            ShowPage(0);
            LoadProfilesFile();
            LoadConfig();
            if (!hasBindCfg) DefaultBinds();
            if (File.Exists(lastFile)) SetSliders(Settings.Parse(File.ReadAllText(lastFile).Trim()));
            ready = true;
            RefreshBinds();
            RefreshRes();
            ApplyNow();
            CrossChanged();
            displayChanged = delegate { if (ready) { CrossChanged(); RefreshRes(); } };
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += displayChanged;
            tick.Start();
        }

        public void ShowPage(int i)
        {
            for (int j = 0; j < pages.Length; j++)
            {
                pages[j].Visible = (j == i);
                navs[j].Selected = (j == i);
                navs[j].Invalidate();
            }
            titleLbl.Text = PageTitles[i];
            subLbl.Text = PageSubs[i];
        }

        public void Snapshot(string path)
        {
            Bitmap b = new Bitmap(Width, Height);
            if (Visible) { using (Graphics gg = Graphics.FromImage(b)) gg.CopyFromScreen(Left, Top, 0, 0, new Size(Width, Height)); }
            else DrawToBitmap(b, new Rectangle(0, 0, Width, Height));
            b.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            b.Dispose();
        }

        public void Quit() { quitting = true; Close(); }

        // ===== helpers UI =====
        CardPanel Card(Control parent, int x, int y, int w, int h)
        {
            CardPanel c = new CardPanel();
            c.SetBounds(x, y, w, h);
            parent.Controls.Add(c);
            return c;
        }

        Label MakeLabel(Control parent, string text, int x, int y, int w, Color c)
        {
            Label l = new Label();
            l.Text = text; l.ForeColor = c;
            l.AutoSize = false;
            l.SetBounds(x, y, w, 22);
            parent.Controls.Add(l);
            return l;
        }

        ToggleSwitch MakeCheck(Control parent, string text, int x, int y, int w)
        {
            ToggleSwitch c = new ToggleSwitch();
            c.Text = text; c.ForeColor = Theme.Fg;
            c.SetBounds(x, y, w, 26);
            parent.Controls.Add(c);
            return c;
        }

        ComboBox MakeCombo(Control parent, int x, int y, int w)
        {
            ComboBox c = new DarkCombo();
            c.SetBounds(x, y, w, 28);
            c.BackColor = Theme.Field; c.ForeColor = Theme.Fg;
            parent.Controls.Add(c);
            return c;
        }

        RoundButton MakeButton(Control parent, string text, int x, int y, int w) { return MakeButton(parent, text, x, y, w, false); }

        RoundButton MakeButton(Control parent, string text, int x, int y, int w, bool primary)
        {
            RoundButton b = new RoundButton();
            b.Text = text; b.Primary = primary;
            b.ForeColor = Theme.Fg;
            b.SetBounds(x, y, w, 32);
            parent.Controls.Add(b);
            return b;
        }

        void StyleList(ListBox lb, Font f)
        {
            lb.BorderStyle = BorderStyle.None;
            lb.BackColor = Theme.Card; lb.ForeColor = Theme.Fg;
            lb.Font = f;
            lb.DrawMode = DrawMode.OwnerDrawFixed;
            lb.ItemHeight = 30;
            lb.IntegralHeight = false;
            lb.DrawItem += ListDraw;
            lb.HandleCreated += delegate { NativeUi.Dark(lb, "DarkMode_Explorer"); };
        }

        void ListDraw(object sender, DrawItemEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(sel ? Color.FromArgb(52, 34, 32) : Theme.Card)) g.FillRectangle(b, e.Bounds);
            if (sel) using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillRectangle(b, e.Bounds.X, e.Bounds.Y + 4, 3, e.Bounds.Height - 8);
            TextRenderer.DrawText(g, lb.Items[e.Index].ToString(), lb.Font,
                new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height), Theme.Fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        ModernSlider AddSlider(Control parent, string name, int min, int max, int val, ref int y, string fmt, Action onChange)
        {
            MakeLabel(parent, name, 18, y + 4, 100, Theme.Fg);
            ModernSlider t = new ModernSlider();
            t.Minimum = min; t.Maximum = max; t.Value = val; t.DefaultValue = val;
            t.SmallChange = 1; t.LargeChange = 5;
            t.SetBounds(120, y, 330, 28);
            parent.Controls.Add(t);
            Label v = MakeLabel(parent, "", 456, y + 4, 62, Theme.Accent);
            v.TextAlign = ContentAlignment.MiddleRight;
            v.Font = new Font("Segoe UI Semibold", 9.5f);
            EventHandler upd = delegate
            {
                if (fmt == "g") v.Text = (t.Value / 100.0).ToString("0.00", CultureInfo.InvariantCulture);
                else v.Text = string.Format(fmt, t.Value);
                if (!loading) onChange();
            };
            t.ValueChanged += upd;
            upd(null, null);
            y += 38;
            return t;
        }

        static int Clamp(int v, ModernSlider t) { return Math.Max(t.Minimum, Math.Min(t.Maximum, v)); }

        void SetStatus(string s, bool err)
        {
            status.ForeColor = err ? Accent : Dim;
            status.Text = s;
        }

        void Flash(string s) { if (chkOsd.Checked && !Visible) osd.Flash(s); }

        // ===== couleurs =====
        void ColorChanged() { autoPaused = false; ApplyNow(); }

        Settings Read()
        {
            return Settings.Make(tBright.Value, tContrast.Value, tSat.Value, tGamma.Value, tR.Value, tG.Value, tB.Value);
        }

        void SetSliders(Settings s)
        {
            loading = true;
            tBright.Value = Clamp(s.Bright, tBright); tContrast.Value = Clamp(s.Contrast, tContrast);
            tSat.Value = Clamp(s.Sat, tSat); tGamma.Value = Clamp(s.Gamma, tGamma);
            tR.Value = Clamp(s.R, tR); tG.Value = Clamp(s.G, tG); tB.Value = Clamp(s.B, tB);
            loading = false;
        }

        void ApplyNow()
        {
            if (!ready) return;
            Settings cur = Read();
            string err = Filter.Apply(cur, chkOn.Checked && !autoPaused);
            if (err != "") SetStatus(err, true);
            else if (autoPaused) SetStatus("Hors jeu : filtre en pause.", false);
            else SetStatus(chkOn.Checked ? "Filtre applique (jeu en Borderless)." : "Filtre desactive.", false);
            try { File.WriteAllText(lastFile, cur.Ser()); } catch { }
        }

        // ===== profils =====
        void LoadProfilesFile()
        {
            profiles.Clear(); names.Clear();
            if (File.Exists(profFile))
            {
                foreach (string line in File.ReadAllLines(profFile))
                {
                    int k = line.IndexOf('|');
                    if (k <= 0) continue;
                    string n = line.Substring(0, k);
                    if (!profiles.ContainsKey(n)) names.Add(n);
                    profiles[n] = Settings.Parse(line.Substring(k + 1));
                }
            }
            if (names.Count == 0)
            {
                names.Add("Normal"); profiles["Normal"] = new Settings();
                SaveProfilesFile();
            }
            RefreshProfileLists();
        }

        void RefreshProfileLists()
        {
            bool was = loading;
            loading = true;
            string a = cbProf.Text, b = cbGameProf.Text;
            cbProf.Items.Clear(); cbGameProf.Items.Clear();
            foreach (string n in names) { cbProf.Items.Add(n); cbGameProf.Items.Add(n); }
            cbProf.Text = a;
            if (cbGameProf.Items.Contains(b)) cbGameProf.SelectedItem = b;
            else if (cbGameProf.Items.Count > 0) cbGameProf.SelectedIndex = 0;
            loading = was;
            if (ready) { RefreshBinds(); RegisterAll(); }
        }

        void SaveProfilesFile()
        {
            List<string> lines = new List<string>();
            foreach (string n in names) lines.Add(n + "|" + profiles[n].Ser());
            File.WriteAllLines(profFile, lines.ToArray());
        }

        void LoadProfile(string name, bool flash)
        {
            if (!profiles.ContainsKey(name)) return;
            SetSliders(profiles[name]);
            loading = true; cbProf.Text = name; loading = false;
            autoPaused = false;
            ApplyNow();
            if (flash) Flash(name);
        }

        string UniqueName(string n)
        {
            if (!profiles.ContainsKey(n)) return n;
            int i = 2;
            while (profiles.ContainsKey(n + " (" + i + ")")) i++;
            return n + " (" + i + ")";
        }

        void SaveProfile()
        {
            string name = cbProf.Text.Trim().Replace("|", "");
            if (name == "") { SetStatus("Ecris un nom de profil dans la case.", true); return; }
            if (!profiles.ContainsKey(name)) names.Add(name);
            profiles[name] = Read();
            SaveProfilesFile();
            RefreshProfileLists();
            cbProf.Text = name;
            SaveConfig();
            SetStatus("Profil \"" + name + "\" sauvegarde.", false);
        }

        void DeleteProfile()
        {
            string name = cbProf.Text;
            if (!profiles.ContainsKey(name)) return;
            profiles.Remove(name); names.Remove(name);
            binds.Remove("p:" + name);
            SaveProfilesFile();
            RefreshProfileLists();
            cbProf.Text = "";
            SaveConfig();
            SetStatus("Profil supprime.", false);
        }

        void CycleProfile(int dir)
        {
            if (names.Count == 0) return;
            int i = names.IndexOf(cbProf.Text);
            i = (i + dir + names.Count) % names.Count;
            if (i < 0) i = 0;
            LoadProfile(names[i], true);
        }

        // ===== codes de partage =====
        void CopyFilterCode()
        {
            string name = cbProf.Text.Trim().Replace("|", "");
            if (name == "") name = "Mon filtre";
            string code = "RFF-" + Codes.Enc(name + "|" + Read().Ser());
            try { Clipboard.SetText(code); SetStatus("Code du filtre copie !", false); }
            catch { SetStatus("Impossible d'ecrire le presse-papiers.", true); }
        }

        void CopyCrossCode()
        {
            CrossCfg c = cross.Cfg;
            string code = "RFX-" + Codes.Enc(c.Ser());
            try { Clipboard.SetText(code); SetStatus("Code du viseur copie !", false); }
            catch { SetStatus("Impossible d'ecrire le presse-papiers.", true); }
        }

        void CopyNativeCrossCode()
        {
            string cmd = "crosshair.load_code " + NativeCode.FromCross(cross.Cfg);
            try { Clipboard.SetText(cmd); SetStatus("Commande copiee : colle-la dans la console de Rust (F1).", false); }
            catch { SetStatus("Impossible d'ecrire le presse-papiers.", true); }
        }

        void ImportCode()
        {
            string code = "";
            try { code = Clipboard.GetText().Trim(); } catch { }
            try
            {
                if (code.StartsWith("RFF-"))
                {
                    string raw = Codes.Dec(code.Substring(4));
                    int k = raw.IndexOf('|');
                    string name = UniqueName(raw.Substring(0, k).Trim());
                    names.Add(name);
                    profiles[name] = Settings.Parse(raw.Substring(k + 1));
                    SaveProfilesFile();
                    RefreshProfileLists();
                    LoadProfile(name, false);
                    SaveConfig();
                    SetStatus("Filtre \"" + name + "\" importe.", false);
                }
                else if (code.StartsWith("RFX-"))
                {
                    CrossCfg c = CrossCfg.Parse(Codes.Dec(code.Substring(4)));
                    c.On = true;
                    SetCrossUi(c);
                    CrossChanged();
                    SetStatus("Viseur importe.", false);
                }
                else if (NativeCode.LooksNative(code))
                {
                    CrossCfg c = NativeCode.ToCross(code);
                    SetCrossUi(c);
                    CrossChanged();
                    SetStatus("Code Rust importe (conversion approximative).", false);
                }
                else SetStatus("Copie d'abord un code RFF-... (filtre), RFX-... ou RUST-... (viseur).", true);
            }
            catch { SetStatus("Code invalide.", true); }
        }

        // ===== viseur =====
        void SetCrossUi(CrossCfg c)
        {
            loading = true;
            chkCross.Checked = c.On;
            cbStyle.SelectedIndex = Math.Min(4, Math.Max(0, c.Style));
            crossArgb = c.Argb;
            cSize.Value = Clamp(c.Size, cSize); cThick.Value = Clamp(c.Thick, cThick); cGap.Value = Clamp(c.Gap, cGap);
            cOffX.Value = Clamp(c.OffX, cOffX); cOffY.Value = Clamp(c.OffY, cOffY);
            chkOutline.Checked = c.Outline;
            loading = false;
        }

        void CrossChanged()
        {
            if (!ready || loading) return;
            CrossCfg c = new CrossCfg();
            c.On = chkCross.Checked;
            c.Style = Math.Max(0, cbStyle.SelectedIndex);
            c.Argb = crossArgb;
            c.Size = cSize.Value; c.Thick = cThick.Value; c.Gap = cGap.Value;
            c.OffX = cOffX.Value; c.OffY = cOffY.Value;
            c.Outline = chkOutline.Checked;
            cross.UpdateCfg(c);
            bColor.Swatch = Color.FromArgb(255, Color.FromArgb(crossArgb)); bColor.Invalidate(); preview.Cfg = c; preview.Invalidate();
            SaveConfig();
        }

        void ApplyPreset(int i)
        {
            if (i < 0) return;
            //               style, couleur,                  taille, epaisseur, ecart, contour
            int[][] p = new int[][] {
                new int[] { 2, Color.FromArgb(0, 255, 120).ToArgb(), 6, 2, 3, 1 },
                new int[] { 0, Color.FromArgb(255, 40, 40).ToArgb(), 4, 4, 0, 1 },
                new int[] { 1, Color.FromArgb(0, 230, 255).ToArgb(), 8, 1, 4, 1 },
                new int[] { 3, Color.FromArgb(255, 220, 0).ToArgb(), 10, 2, 0, 1 },
                new int[] { 4, Color.White.ToArgb(), 7, 2, 3, 1 },
                new int[] { 0, Color.White.ToArgb(), 2, 2, 0, 1 },
                new int[] { 2, Color.FromArgb(255, 80, 200).ToArgb(), 12, 3, 6, 1 } };
            int[] v = p[i];
            CrossCfg c = new CrossCfg();
            c.On = true; c.Style = v[0]; c.Argb = v[1]; c.Size = v[2]; c.Thick = v[3]; c.Gap = v[4]; c.Outline = v[5] == 1;
            c.OffX = cOffX.Value; c.OffY = cOffY.Value;
            SetCrossUi(c);
            CrossChanged();
        }

        void PickColor()
        {
            ColorDialog d = new ColorDialog();
            d.Color = Color.FromArgb(crossArgb);
            d.FullOpen = true;
            if (d.ShowDialog(this) == DialogResult.OK) { crossArgb = d.Color.ToArgb(); CrossChanged(); }
        }

        // ===== touches =====
        static string KeyText(Keys k)
        {
            if (k == Keys.None) return "(aucune)";
            string s = "";
            if ((k & Keys.Control) != 0) s += "Ctrl+";
            if ((k & Keys.Shift) != 0) s += "Shift+";
            if ((k & Keys.Alt) != 0) s += "Alt+";
            Keys c = k & Keys.KeyCode;
            if (c >= Keys.D0 && c <= Keys.D9) s += (char)('0' + (c - Keys.D0));
            else if (c >= Keys.NumPad0 && c <= Keys.NumPad9) s += "Num" + (c - Keys.NumPad0);
            else if (c == Keys.Left) s += "Gauche";
            else if (c == Keys.Right) s += "Droite";
            else if (c == Keys.Up) s += "Haut";
            else if (c == Keys.Down) s += "Bas";
            else s += c.ToString();
            return s;
        }

        string ActionText(string a)
        {
            if (a == "toggle") return "Filtre on/off";
            if (a == "cross") return "Viseur on/off";
            if (a == "next") return "Profil suivant";
            if (a == "prev") return "Profil précédent";
            if (a.StartsWith("p:")) return "Profil : " + a.Substring(2);
            return a;
        }

        void DefaultBinds()
        {
            binds.Clear();
            binds["toggle"] = Keys.Control | Keys.Shift | Keys.D0;
            binds["cross"] = Keys.Control | Keys.Shift | Keys.X;
            binds["next"] = Keys.Control | Keys.Shift | Keys.Right;
            binds["prev"] = Keys.Control | Keys.Shift | Keys.Left;
            for (int i = 0; i < names.Count && i < 9; i++)
                binds["p:" + names[i]] = Keys.Control | Keys.Shift | (Keys)((int)Keys.D1 + i);
        }

        void RefreshBinds()
        {
            int sel = lbBinds.SelectedIndex;
            bindRows.Clear();
            bindRows.Add("toggle"); bindRows.Add("cross"); bindRows.Add("next"); bindRows.Add("prev");
            foreach (string n in names) bindRows.Add("p:" + n);
            lbBinds.Items.Clear();
            foreach (string a in bindRows)
            {
                Keys k;
                if (!binds.TryGetValue(a, out k)) k = Keys.None;
                string label = ActionText(a);
                if (label.Length > 40) label = label.Substring(0, 39) + "…";
                string key = (captureAction == a) ? ">> appuie sur une touche <<" : KeyText(k);
                lbBinds.Items.Add(label.PadRight(42) + key);
            }
            if (sel >= 0 && sel < lbBinds.Items.Count) lbBinds.SelectedIndex = sel;
        }

        void UnregisterAll()
        {
            for (int i = 1; i <= hotActions.Count; i++) Native.UnregisterHotKey(Handle, i);
            hotActions.Clear();
        }

        void RegisterAll()
        {
            if (!IsHandleCreated || App.TestMode) return;
            UnregisterAll();
            string fails = "";
            foreach (KeyValuePair<string, Keys> kv in binds)
            {
                if (kv.Value == Keys.None) continue;
                if (kv.Key.StartsWith("p:") && !profiles.ContainsKey(kv.Key.Substring(2))) continue;
                uint mods = 0x4000; // NOREPEAT
                if ((kv.Value & Keys.Alt) != 0) mods |= 1;
                if ((kv.Value & Keys.Control) != 0) mods |= 2;
                if ((kv.Value & Keys.Shift) != 0) mods |= 4;
                hotActions.Add(kv.Key);
                if (!Native.RegisterHotKey(Handle, hotActions.Count, mods, (uint)(kv.Value & Keys.KeyCode)))
                    fails += KeyText(kv.Value) + " ";
            }
            if (fails != "") SetStatus("Deja prise par une autre appli : " + fails, true);
        }

        void StartCapture()
        {
            int i = lbBinds.SelectedIndex;
            if (i < 0) { SetStatus("Choisis d'abord une action dans la liste.", true); return; }
            captureAction = bindRows[i];
            UnregisterAll();
            RefreshBinds();
            SetStatus("Appuie sur la touche voulue. Echap = annuler.", false);
            lbBinds.Focus();
        }

        void ClearBind()
        {
            int i = lbBinds.SelectedIndex;
            if (i < 0) return;
            binds.Remove(bindRows[i]);
            RefreshBinds(); RegisterAll(); SaveConfig();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (captureAction != null)
            {
                Keys code = keyData & Keys.KeyCode;
                if (code == Keys.ControlKey || code == Keys.ShiftKey || code == Keys.Menu || code == Keys.LWin || code == Keys.RWin)
                    return true; // on attend la vraie touche
                string a = captureAction;
                captureAction = null;
                if (code == Keys.Escape) SetStatus("Annule.", false);
                else
                {
                    foreach (string other in new List<string>(binds.Keys))
                        if (binds[other] == keyData && other != a) binds.Remove(other); // une touche = une action
                    binds[a] = keyData;
                    bool plain = (keyData & (Keys.Control | Keys.Shift | Keys.Alt)) == 0
                                 && !(code >= Keys.F1 && code <= Keys.F24) && !(code >= Keys.NumPad0 && code <= Keys.NumPad9);
                    if (plain) SetStatus("Attention : " + KeyText(keyData) + " seule sera bloquee pour le jeu.", true);
                    else SetStatus(ActionText(a) + " = " + KeyText(keyData), false);
                    SaveConfig();
                }
                RefreshBinds();
                RegisterAll();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void DoAction(string a)
        {
            if (a == "toggle")
            {
                chkOn.Checked = !chkOn.Checked;
                Flash(chkOn.Checked ? "Filtre ON" : "Filtre OFF");
            }
            else if (a == "cross") chkCross.Checked = !chkCross.Checked;
            else if (a == "next") CycleProfile(1);
            else if (a == "prev") CycleProfile(-1);
            else if (a.StartsWith("p:")) LoadProfile(a.Substring(2), true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int on = 1; Native.DwmSetWindowAttribute(Handle, 20, ref on, 4); } catch { }
            RegisterAll();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                int id = m.WParam.ToInt32();
                if (id >= 1 && id <= hotActions.Count) DoAction(hotActions[id - 1]);
            }
            base.WndProc(ref m);
        }

        // ===== jeux =====
        void AddGame()
        {
            string exe = tbExe.Text.Trim();
            if (exe.ToLower().EndsWith(".exe")) exe = exe.Substring(0, exe.Length - 4);
            if (exe == "" || cbGameProf.SelectedItem == null) { SetStatus("Indique le .exe et le profil.", true); return; }
            gameMap[exe.ToLower()] = (string)cbGameProf.SelectedItem;
            RefreshGames(); SaveConfig();
            SetStatus(exe + " -> " + cbGameProf.SelectedItem, false);
        }

        void RemoveGame()
        {
            if (lbGames.SelectedItem == null) return;
            string line = (string)lbGames.SelectedItem;
            gameMap.Remove(line.Substring(0, line.IndexOf("  ->")));
            RefreshGames(); SaveConfig();
        }

        void RefreshGames()
        {
            lbGames.Items.Clear();
            foreach (KeyValuePair<string, string> kv in gameMap) lbGames.Items.Add(kv.Key + "  ->  " + kv.Value);
        }

        void OnTick()
        {
            cross.KeepOnTop();
            if (!chkAuto.Checked || gameMap.Count == 0) return;
            string proc = null;
            try
            {
                uint pid;
                Native.GetWindowThreadProcessId(Native.GetForegroundWindow(), out pid);
                if ((int)pid == myPid) return;
                proc = Process.GetProcessById((int)pid).ProcessName.ToLower();
            }
            catch { return; }

            string prof;
            if (proc != null && gameMap.TryGetValue(proc, out prof) && profiles.ContainsKey(prof))
            {
                if (autoPaused || autoProfile != prof)
                {
                    autoProfile = prof;
                    LoadProfile(prof, true);
                }
            }
            else if (!autoPaused)
            {
                autoPaused = true;
                autoProfile = null;
                ApplyNow();
            }
        }

        // ===== resolution =====
        void RefreshRes()
        {
            DEVMODE cur = Display.Current();
            lblRes.Text = "Actuelle : " + Display.Label(cur);
            modes = Display.Modes();
            cbRes.Items.Clear();
            int sel = -1;
            for (int i = 0; i < modes.Count; i++)
            {
                cbRes.Items.Add(Display.Label(modes[i]));
                if (modes[i].dmPelsWidth == cur.dmPelsWidth && modes[i].dmPelsHeight == cur.dmPelsHeight && modes[i].dmDisplayFrequency == cur.dmDisplayFrequency) sel = i;
            }
            if (sel >= 0) cbRes.SelectedIndex = sel;
        }

        void ApplyRes()
        {
            int i = cbRes.SelectedIndex;
            if (i < 0) return;
            DEVMODE before = Display.Current();
            if (!Display.Apply(modes[i])) { SetStatus("Resolution refusee par Windows.", true); return; }
            ConfirmForm f = new ConfirmForm();
            if (f.ShowDialog(this) != DialogResult.OK)
            {
                Display.Apply(before);
                SetStatus("Resolution remise comme avant.", false);
            }
            else SetStatus("Resolution : " + Display.Label(modes[i]), false);
            RefreshRes();
            CrossChanged();
        }

        // ===== config =====
        void SaveConfig()
        {
            if (!ready) return;
            List<string> l = new List<string>();
            l.Add("cross=" + cross.Cfg.Ser());
            l.Add("auto=" + (chkAuto.Checked ? 1 : 0));
            l.Add("osd=" + (chkOsd.Checked ? 1 : 0));
            foreach (KeyValuePair<string, string> kv in gameMap) l.Add("game=" + kv.Key + "|" + kv.Value);
            l.Add("binds=1");
            foreach (KeyValuePair<string, Keys> kv in binds) l.Add("bind=" + kv.Key + "|" + (int)kv.Value);
            try { File.WriteAllLines(cfgFile, l.ToArray()); } catch { }
        }

        void LoadConfig()
        {
            CrossCfg c = new CrossCfg();
            bool hasGames = false;
            if (File.Exists(cfgFile))
            {
                foreach (string line in File.ReadAllLines(cfgFile))
                {
                    int k = line.IndexOf('=');
                    if (k <= 0) continue;
                    string key = line.Substring(0, k), val = line.Substring(k + 1);
                    int p = val.LastIndexOf('|');
                    if (key == "cross") c = CrossCfg.Parse(val);
                    else if (key == "auto") chkAuto.Checked = val == "1";
                    else if (key == "osd") chkOsd.Checked = val == "1";
                    else if (key == "binds") hasBindCfg = true;
                    else if (key == "game" && p > 0) { gameMap[val.Substring(0, val.IndexOf('|'))] = val.Substring(val.IndexOf('|') + 1); hasGames = true; }
                    else if (key == "bind" && p > 0)
                    {
                        int kk;
                        if (int.TryParse(val.Substring(p + 1), out kk)) binds[val.Substring(0, p)] = (Keys)kk;
                    }
                }
            }
            if (!hasGames && names.Count > 1) gameMap["rustclient"] = names[1];
            SetCrossUi(c);
            RefreshGames();
        }

        // ===== fenetre =====
        void HideToTray()
        {
            Hide();
            tray.ShowBalloonTip(2000, "WipeFilter", "Toujours actif. Double-clic sur l'icone pour rouvrir.", ToolTipIcon.Info);
        }

        void ShowFromTray() { Show(); WindowState = FormWindowState.Normal; Activate(); }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!quitting && e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult r = MessageBox.Show(
                    "Fermer WipeFilter retire le filtre et le viseur.\n\nOui = quitter\nNon = garder actif en arriere-plan",
                    "WipeFilter", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r == DialogResult.No) { e.Cancel = true; HideToTray(); return; }
                if (r == DialogResult.Cancel) { e.Cancel = true; return; }
            }
            tick.Stop();
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= displayChanged;
            UnregisterAll();
            Filter.Reset();
            cross.Close(); osd.Close();
            tray.Visible = false;
            base.OnFormClosing(e);
        }
    }

    public static class App
    {
        public static bool TestMode = false;

        public static string DataDir()
        {
            if (TestMode) return Path.Combine(Path.GetTempPath(), "wipefilter-test");
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RustFilter");
        }

        public static int SelfTest()
        {
            TestMode = true;
            Application.EnableVisualStyles();
            string dir = DataDir();
            try { if (Directory.Exists(dir)) foreach (string x in Directory.GetFiles(dir)) File.Delete(x); } catch { }
            MainForm f = new MainForm();
            f.Show();
            Application.DoEvents();
            System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            Type t = typeof(MainForm);
            int fails = 0;
            ModernSlider b = (ModernSlider)t.GetField("tBright", bf).GetValue(f);
            b.Value = 17;
            string last = File.ReadAllText(Path.Combine(dir, "dernier.txt")).Trim();
            if (!last.StartsWith("17;")) { Console.Error.WriteLine("ECHEC curseur -> dernier.txt : " + last); fails++; }
            ToggleSwitch cc = (ToggleSwitch)t.GetField("chkCross", bf).GetValue(f);
            ComboBox style = (ComboBox)t.GetField("cbStyle", bf).GetValue(f);
            cc.Checked = true;
            style.SelectedIndex = 3;
            ModernSlider sz = (ModernSlider)t.GetField("cSize", bf).GetValue(f);
            sz.Value = 15;
            string cfg = File.ReadAllText(Path.Combine(dir, "config.txt"));
            if (!cfg.Contains("cross=1;3;") || !cfg.Contains(";15;")) { Console.Error.WriteLine("ECHEC viseur -> config.txt : " + cfg.Split('\n')[0]); fails++; }
            // profil : sauver puis recharger
            ComboBox prof = (ComboBox)t.GetField("cbProf", bf).GetValue(f);
            prof.Text = "TestProfil";
            t.GetMethod("SaveProfile", bf).Invoke(f, null);
            string profs = File.ReadAllText(Path.Combine(dir, "profils.txt"));
            if (!profs.Contains("TestProfil|17;")) { Console.Error.WriteLine("ECHEC profil : " + profs); fails++; }
            // code RFX : copie via le même format que le site
            CrossCfg c2 = new CrossCfg(); c2.On = true; c2.Style = 3; c2.Argb = Color.FromArgb(255, 220, 0).ToArgb(); c2.Size = 10; c2.Thick = 2; c2.Gap = 0; c2.Outline = true;
            string code = "RFX-" + Codes.Enc(c2.Ser());
            if (code != "RFX-MTszOy05MjE2OzEwOzI7MDsxOzA7MA") { Console.Error.WriteLine("ECHEC code RFX : " + code); fails++; }
            // passerelle RUST- : memes vecteurs que le site
            string nat = NativeCode.FromCross(c2);
            if (nat != "RUST-lcRei-H") { Console.Error.WriteLine("ECHEC code RUST : " + nat); fails++; }
            CrossCfg back = NativeCode.ToCross("crosshair.load_code RUST-lcRei-H");
            if (back.Style != 2 || back.Size != 10 || back.Thick != 2 || back.Gap != 0 || !back.Outline) { Console.Error.WriteLine("ECHEC import RUST : " + back.Ser()); fails++; }
            CrossCfg ref1 = NativeCode.ToCross("RUST-FceIG-b9l");
            if (ref1.Style != 4 || ref1.Argb != Color.FromArgb(255, 255, 0, 0).ToArgb() || !ref1.Outline) { Console.Error.WriteLine("ECHEC import RUST ref : " + ref1.Ser()); fails++; }
            f.Quit();
            Console.Out.WriteLine(fails == 0 ? "SELFTEST OK" : ("SELFTEST ECHECS : " + fails));
            return fails;
        }
        public static void Shot(string path, int page)
        {
            TestMode = true;
            Application.EnableVisualStyles();
            MainForm f = new MainForm();
            f.ShowPage(page);
            f.Show();
            for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(100); if (i == 5) { f.Activate(); f.BringToFront(); } }
            f.Snapshot(path);
            f.Quit();
        }

        public static void Run()
        {
            bool created;
            using (Mutex mx = new Mutex(true, "RustFilter_SingleInstance", out created))
            {
                if (!created) { MessageBox.Show("WipeFilter est déjà lancé (regarde près de l'horloge).", "WipeFilter"); return; }
                Application.EnableVisualStyles();
                if (!Native.MagInitialize())
                {
                    MessageBox.Show("Impossible d'initialiser la couche couleur Windows.", "WipeFilter");
                    return;
                }
                try { Application.Run(new MainForm()); }
                finally { Filter.Reset(); Native.MagUninitialize(); }
            }
        }
    }
}