using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WindowsFormsApp1.Models;
using WindowsFormsApp1.Services;

namespace WindowsFormsApp1
{
    public class PortPanel : UserControl
    {
        // ── References ───────────────────────────────────────────────────────
        private readonly EthernetPort _port;
        private readonly EthernetMonitorService _monitor;
        private readonly PingService _pingSvc;
        private readonly MessageService _msgSvc;

        // ── Events ───────────────────────────────────────────────────────────
        public event EventHandler<LogEventArgs> MessageLogged;

        // ── LED state ────────────────────────────────────────────────────────
        private Color _ledColor = Color.FromArgb(80, 80, 80);
        private System.Windows.Forms.Timer _blinkTimer;
        private bool _blinkState = false;

        // ── Controls ─────────────────────────────────────────────────────────
        private Panel _ledPanel;
        private Label _lblPortName;
        private Label _lblMacGroup;
        private Label _lblIp;
        private Label _lblMacAddr;
        private Label _lblStatus;
        private Label _lblLinkSpeed;

        private Label _lblTxPkts;
        private Label _lblRxPkts;
        private Label _lblTxBytes;
        private Label _lblRxBytes;
        private Label _lblErrors;
        private Label _lblPingLatency;

        private Button _btnToggle;
        private Button _btnPing;
        private Button _btnReset;

        private RichTextBox _rtbPortLog;
        private TextBox _txtMessage;
        private Button _btnSendMsg;

        // ── Color palette (mirrored from MainForm) ────────────────────────────
        private static Color BgCard     = Color.FromArgb(20, 28, 52);
        private static Color BgInner    = Color.FromArgb(14, 20, 38);
        private static Color AccBlue    = Color.FromArgb(0, 180, 255);
        private static Color AccGreen   = Color.FromArgb(0, 230, 150);
        private static Color AccRed     = Color.FromArgb(255, 70, 70);
        private static Color AccOrange  = Color.FromArgb(255, 165, 0);
        private static Color AccYellow  = Color.FromArgb(255, 220, 50);
        private static Color TxtPri     = Color.FromArgb(220, 235, 255);
        private static Color TxtSec     = Color.FromArgb(120, 145, 190);
        private static Color AccPurple  = Color.FromArgb(180, 100, 255);

        public PortPanel(EthernetPort port, EthernetMonitorService monitor, PingService ping, MessageService msg)
        {
            _port    = port;
            _monitor = monitor;
            _pingSvc = ping;
            _msgSvc  = msg;

            InitUI();
            SetupBlinkTimer();
        }

        // ════════════════════════════════════════════════════════════════════
        //  INIT
        // ════════════════════════════════════════════════════════════════════
        private void InitUI()
        {
            this.BackColor   = BgCard;
            this.BorderStyle = BorderStyle.None;
            this.Padding     = new Padding(0);
            this.DoubleBuffered = true;

            // Custom border painting
            this.Paint += OnPanelPaint;

            BuildHeader();
            BuildInfoSection();
            BuildStatsSection();
            BuildControlSection();
            BuildMessageSection();
            BuildPortLog();
        }

        // ── Header (port title + LED) ─────────────────────────────────────
        private void BuildHeader()
        {
            var hdr = new Panel
            {
                Height = 52,
                Dock = DockStyle.Top,
                BackColor = _port.MacGroup == MacGroup.MAC0_Direct
                    ? Color.FromArgb(0, 50, 80)
                    : Color.FromArgb(30, 0, 60)
            };

            // LED
            _ledPanel = new Panel
            {
                Width = 14, Height = 14,
                Location = new Point(10, 19),
                BackColor = Color.FromArgb(80, 80, 80)
            };
            _ledPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(_ledColor))
                    e.Graphics.FillEllipse(br, 0, 0, 13, 13);
                using (var pen = new Pen(Color.FromArgb(180, _ledColor), 1))
                    e.Graphics.DrawEllipse(pen, 0, 0, 13, 13);
                // Glow
                using (var glow = new SolidBrush(Color.FromArgb(60, _ledColor)))
                    e.Graphics.FillEllipse(glow, -3, -3, 20, 20);
            };

            // Port name
            _lblPortName = new Label
            {
                Text = $"PORT {_port.PortId}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = _port.MacGroup == MacGroup.MAC0_Direct ? AccBlue : AccPurple,
                AutoSize = true,
                Location = new Point(30, 8)
            };

            // MAC Group badge
            _lblMacGroup = new Label
            {
                Text = _port.MacGroupLabel,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = _port.MacGroup == MacGroup.MAC0_Direct ? AccBlue : AccPurple,
                AutoSize = true,
                Location = new Point(30, 32)
            };

            // Status
            _lblStatus = new Label
            {
                Text = "DISCONNECTED",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = TxtSec,
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _lblStatus.Location = new Point(hdr.Width - 120, 8);

            hdr.Controls.AddRange(new Control[] { _ledPanel, _lblPortName, _lblMacGroup, _lblStatus });
            hdr.Resize += (s, e) => _lblStatus.Location = new Point(hdr.Width - _lblStatus.Width - 8, 8);

            this.Controls.Add(hdr);
        }

        // ── Info section ─────────────────────────────────────────────────────
        private void BuildInfoSection()
        {
            var infoPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 3,
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = BgInner,
                Padding = new Padding(6, 4, 6, 4),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _lblIp = MakeInfoLabel($"IP: {_port.IpAddress}", AccBlue);
            _lblMacAddr = MakeInfoLabel($"MAC: {_port.MacAddress}", TxtSec);
            _lblLinkSpeed = MakeInfoLabel($"⚡ {_port.LinkSpeedMbps} Mbps", AccGreen);
            _lblPingLatency = MakeInfoLabel("Ping: —", AccYellow);

            infoPanel.Controls.Add(_lblIp, 0, 0);
            infoPanel.Controls.Add(_lblMacAddr, 1, 0);
            infoPanel.Controls.Add(_lblLinkSpeed, 0, 1);
            infoPanel.Controls.Add(_lblPingLatency, 1, 1);

            this.Controls.Add(infoPanel);
        }

        // ── Stats section ────────────────────────────────────────────────────
        private void BuildStatsSection()
        {
            var statsPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 3,
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = BgCard,
                Padding = new Padding(6, 4, 6, 4),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var lblTxHdr = MakeInfoLabel("▲ TRANSMIT", AccGreen, bold: true);
            var lblRxHdr = MakeInfoLabel("▼ RECEIVE",  AccBlue,  bold: true);
            _lblTxPkts   = MakeInfoLabel("Packets: 0",  AccGreen);
            _lblRxPkts   = MakeInfoLabel("Packets: 0",  AccBlue);
            _lblTxBytes  = MakeInfoLabel("Bytes: 0 B",  AccGreen);
            _lblRxBytes  = MakeInfoLabel("Bytes: 0 B",  AccBlue);

            statsPanel.Controls.Add(lblTxHdr, 0, 0);
            statsPanel.Controls.Add(lblRxHdr, 1, 0);
            statsPanel.Controls.Add(_lblTxPkts, 0, 1);
            statsPanel.Controls.Add(_lblRxPkts, 1, 1);
            statsPanel.Controls.Add(_lblTxBytes, 0, 2);
            statsPanel.Controls.Add(_lblRxBytes, 1, 2);

            // Errors row
            var errPanel = new Panel { Dock = DockStyle.Top, Height = 20, BackColor = BgInner };
            _lblErrors = new Label
            {
                Text = "TX Errors: 0  |  RX Errors: 0",
                ForeColor = AccRed,
                Font = new Font("Consolas", 7.5f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            errPanel.Controls.Add(_lblErrors);

            this.Controls.Add(statsPanel);
            this.Controls.Add(errPanel);
        }

        // ── Control buttons ──────────────────────────────────────────────────
        private void BuildControlSection()
        {
            var ctrlPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = BgCard,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(4, 4, 4, 4),
                WrapContents = false
            };

            _btnToggle = MakeSmallButton("Connect", AccGreen);
            _btnToggle.Click += OnTogglePort;

            _btnPing = MakeSmallButton("Ping", AccYellow);
            _btnPing.Click += async (s, e) =>
            {
                _btnPing.Enabled = false;
                _btnPing.Text = "...";
                var result = await _pingSvc.PingPortAsync(_port.PortId, _port.IpAddress);
                _port.PingSuccess = result.Success;
                _port.PingLatencyMs = result.Success ? result.LatencyMs : -1;
                RefreshStats();
                _btnPing.Enabled = true;
                _btnPing.Text = "Ping";

                var color = result.Success ? AccGreen : AccRed;
                var latStr = result.Success ? $"{result.LatencyMs} ms" : $"FAIL";
                MessageLogged?.Invoke(this, new LogEventArgs(
                    $"[{DateTime.Now:HH:mm:ss.fff}] Ping Port {_port.PortId} ({_port.IpAddress}) → {latStr}", color));
                AppendPortLog($"Ping → {latStr}", color);
            };

            _btnReset = MakeSmallButton("Reset", AccOrange);
            _btnReset.Click += (s, e) =>
            {
                _monitor.ResetPortStats(_port.PortId);
                RefreshStats();
                AppendPortLog("Counters reset.", AccOrange);
            };

            ctrlPanel.Controls.AddRange(new Control[] { _btnToggle, _btnPing, _btnReset });
            this.Controls.Add(ctrlPanel);
        }

        // ── Per-port message box ─────────────────────────────────────────────
        private void BuildMessageSection()
        {
            var msgPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = BgInner,
                Padding = new Padding(4, 4, 4, 4)
            };

            _txtMessage = new TextBox
            {
                Width = 120,
                Height = 24,
                BackColor = Color.FromArgb(16, 24, 44),
                ForeColor = TxtPri,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 8f),
                Location = new Point(4, 5),
                Text = $"Msg to Port {_port.PortId}"
            };

            _btnSendMsg = MakeSmallButton("⬆TX", AccGreen);
            _btnSendMsg.Location = new Point(130, 4);
            _btnSendMsg.Width = 50;
            _btnSendMsg.Click += (s, e) =>
            {
                string data = _txtMessage.Text.Trim();
                if (string.IsNullOrEmpty(data)) return;
                _msgSvc.SendMessage(_port.PortId, _port.PortName, data, 1.0);
                AppendPortLog($"TX: \"{data}\"", AccGreen);
                MessageLogged?.Invoke(this, new LogEventArgs(
                    $"[{DateTime.Now:HH:mm:ss.fff}] [TX→Port{_port.PortId}] \"{data}\"", AccGreen));
            };

            msgPanel.Controls.AddRange(new Control[] { _txtMessage, _btnSendMsg });
            this.Controls.Add(msgPanel);
        }

        // ── Per-port log ─────────────────────────────────────────────────────
        private void BuildPortLog()
        {
            _rtbPortLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = BgInner,
                ForeColor = TxtPri,
                Font = new Font("Consolas", 7.5f),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(4)
            };

            AppendPortLog($"Port {_port.PortId} ({_port.PortName}) initialized.", AccBlue);
            AppendPortLog($"IP: {_port.IpAddress}  MAC: {_port.MacAddress}", TxtSec);

            this.Controls.Add(_rtbPortLog);
        }

        // ════════════════════════════════════════════════════════════════════
        //  PUBLIC REFRESH
        // ════════════════════════════════════════════════════════════════════
        public void RefreshStats()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(RefreshStats)); return; }

            _lblTxPkts.Text  = $"Packets: {_port.TxPackets:N0}";
            _lblRxPkts.Text  = $"Packets: {_port.RxPackets:N0}";
            _lblTxBytes.Text = $"Bytes: {MainForm.FormatBytes(_port.TxBytes)}";
            _lblRxBytes.Text = $"Bytes: {MainForm.FormatBytes(_port.RxBytes)}";
            _lblErrors.Text  = $"TX Err: {_port.TxErrors}  |  RX Err: {_port.RxErrors}";

            // Ping latency
            if (_port.PingLatencyMs >= 0)
                _lblPingLatency.Text = $"Ping: {_port.PingLatencyMs} ms";
            else
                _lblPingLatency.Text = "Ping: —";

            // LED & status
            UpdateLED();
            _lblStatus.Text = _port.Status.ToString().ToUpper();
            _lblStatus.ForeColor = StatusColor(_port.Status);
        }

        // ════════════════════════════════════════════════════════════════════
        //  LED ANIMATION
        // ════════════════════════════════════════════════════════════════════
        private void SetupBlinkTimer()
        {
            _blinkTimer = new System.Windows.Forms.Timer { Interval = 120 };
            _blinkTimer.Tick += (s, e) =>
            {
                _blinkState = !_blinkState;
                UpdateLED();
            };
            _blinkTimer.Start();
        }

        private void UpdateLED()
        {
            Color target;
            switch (_port.Status)
            {
                case PortStatus.Connected:
                    target = AccGreen;
                    break;
                case PortStatus.Transmitting:
                    target = _blinkState ? AccGreen : Color.FromArgb(0, 100, 60);
                    break;
                case PortStatus.Receiving:
                    target = _blinkState ? AccBlue : Color.FromArgb(0, 60, 100);
                    break;
                case PortStatus.Error:
                    target = _blinkState ? AccRed : Color.FromArgb(100, 0, 0);
                    break;
                default:
                    target = Color.FromArgb(60, 60, 60);
                    break;
            }

            _ledColor = target;
            _ledPanel?.Invalidate();
        }

        // ════════════════════════════════════════════════════════════════════
        //  TOGGLE PORT
        // ════════════════════════════════════════════════════════════════════
        private void OnTogglePort(object sender, EventArgs e)
        {
            if (!_port.IsActive)
            {
                _monitor.SetPortActive(_port.PortId, true);
                _btnToggle.Text = "Disconnect";
                _btnToggle.BackColor = Color.FromArgb(30, AccRed.R, AccRed.G, AccRed.B);
                _btnToggle.ForeColor = AccRed;
                _btnToggle.FlatAppearance.BorderColor = AccRed;
                AppendPortLog("Port connected.", AccGreen);
                MessageLogged?.Invoke(this, new LogEventArgs(
                    $"[{DateTime.Now:HH:mm:ss.fff}] Port {_port.PortId} ({_port.PortName}) CONNECTED.", AccGreen));
            }
            else
            {
                _monitor.SetPortActive(_port.PortId, false);
                _btnToggle.Text = "Connect";
                _btnToggle.BackColor = Color.FromArgb(30, AccGreen.R, AccGreen.G, AccGreen.B);
                _btnToggle.ForeColor = AccGreen;
                _btnToggle.FlatAppearance.BorderColor = AccGreen;
                AppendPortLog("Port disconnected.", AccRed);
                MessageLogged?.Invoke(this, new LogEventArgs(
                    $"[{DateTime.Now:HH:mm:ss.fff}] Port {_port.PortId} ({_port.PortName}) DISCONNECTED.", AccRed));
            }

            RefreshStats();
        }

        // ════════════════════════════════════════════════════════════════════
        //  BORDER PAINT
        // ════════════════════════════════════════════════════════════════════
        private void OnPanelPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color borderCol = _port.MacGroup == MacGroup.MAC0_Direct
                ? Color.FromArgb(0, 100, 160)
                : Color.FromArgb(80, 0, 140);

            using (var pen = new Pen(borderCol, 1.5f))
            {
                var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                g.DrawRectangle(pen, rect);
            }

            // Top accent line
            using (var br = new LinearGradientBrush(
                new Point(0, 0), new Point(this.Width, 0),
                _port.MacGroup == MacGroup.MAC0_Direct ? AccBlue : AccPurple,
                Color.Transparent))
            {
                g.FillRectangle(br, 0, 0, this.Width, 3);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════════
        private void AppendPortLog(string text, Color color)
        {
            if (_rtbPortLog == null) return;
            if (_rtbPortLog.InvokeRequired) { _rtbPortLog.Invoke(new Action(() => AppendPortLog(text, color))); return; }

            _rtbPortLog.SelectionStart = _rtbPortLog.TextLength;
            _rtbPortLog.SelectionColor = color;
            _rtbPortLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {text}\n");
            _rtbPortLog.ScrollToCaret();
        }

        private static Label MakeInfoLabel(string text, Color color, bool bold = false)
        {
            return new Label
            {
                Text = text,
                ForeColor = color,
                Font = new Font("Consolas", 7.8f, bold ? FontStyle.Bold : FontStyle.Regular),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(2, 0, 0, 0)
            };
        }

        private static Button MakeSmallButton(string text, Color accent)
        {
            return new Button
            {
                Text = text,
                Width = 80,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(25, accent.R, accent.G, accent.B),
                ForeColor = accent,
                FlatAppearance = { BorderColor = accent, BorderSize = 1 },
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 2, 2, 2),
                UseVisualStyleBackColor = false
            };
        }

        private static Color StatusColor(PortStatus s)
        {
            switch (s)
            {
                case PortStatus.Connected:     return AccGreen;
                case PortStatus.Transmitting:  return AccGreen;
                case PortStatus.Receiving:     return AccBlue;
                case PortStatus.Error:         return AccRed;
                default:                       return Color.FromArgb(80, 80, 80);
            }
        }
    }
}
