using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp1.Models;
using WindowsFormsApp1.Services;

namespace WindowsFormsApp1
{
    public partial class MainForm : Form
    {
        // ── Colors (Dark VLSI Theme) ──────────────────────────────────────────
        public static readonly Color BgDark       = Color.FromArgb(10, 14, 26);
        public static readonly Color BgPanel      = Color.FromArgb(18, 24, 42);
        public static readonly Color BgCard       = Color.FromArgb(24, 32, 56);
        public static readonly Color AccentBlue   = Color.FromArgb(0, 180, 255);
        public static readonly Color AccentGreen  = Color.FromArgb(0, 230, 150);
        public static readonly Color AccentOrange = Color.FromArgb(255, 165, 0);
        public static readonly Color AccentRed    = Color.FromArgb(255, 70, 70);
        public static readonly Color AccentYellow = Color.FromArgb(255, 220, 50);
        public static readonly Color TextPrimary  = Color.FromArgb(230, 240, 255);
        public static readonly Color TextSecond   = Color.FromArgb(130, 150, 190);
        public static readonly Color BorderColor  = Color.FromArgb(40, 60, 100);

        // ── Services ─────────────────────────────────────────────────────────
        private EthernetPort[] _ports;
        private EthernetMonitorService _monitorService;
        private MessageService _messageService;
        private PingService _pingService;

        // ── UI Controls ──────────────────────────────────────────────────────
        private Panel _headerPanel;
        private Label _lblTitle;
        private Label _lblSubtitle;
        private Label _lblClock;
        private System.Windows.Forms.Timer _clockTimer;

        private Panel _controlBar;
        private Button _btnStartStop;
        private Button _btnPingAll;
        private Button _btnResetAll;
        private Label _lblMonitorStatus;

        private Panel _portsContainer;
        private PortPanel[] _portPanels;

        private Panel _aggregatePanel;
        private Label _lblAggTxPkt;
        private Label _lblAggRxPkt;
        private Label _lblAggTxBytes;
        private Label _lblAggRxBytes;
        private Label _lblAggErrors;

        private Panel _messagePanel;
        private RichTextBox _rtbTxInput;
        private RichTextBox _rtbLog;
        private Button _btnSendAll;
        private ComboBox _cboTargetPort;
        private Button _btnSendSingle;
        private Label _lblDelay;
        private NumericUpDown _nudDelay;

        private StatusStrip _statusBar;
        private ToolStripStatusLabel _tsStatusLabel;
        private ToolStripStatusLabel _tsTimeLabel;

        // ── Main layout table (replaces SplitContainer) ───────────────────────
        private TableLayoutPanel _mainTable;

        public MainForm()
        {
            InitializeComponent();
            InitializePorts();
            InitializeServices();
            BuildUI();
            StartClockTimer();
        }

        // ════════════════════════════════════════════════════════════════════
        //  INITIALIZATION
        // ════════════════════════════════════════════════════════════════════

        private void InitializePorts()
        {
            _ports = new EthernetPort[]
            {
                new EthernetPort(0, MacGroup.MAC0_Direct, "192.168.1.10", "00:0A:35:00:00:01", "MAC0-Direct"),
                new EthernetPort(1, MacGroup.MAC1_Switch, "192.168.2.11", "00:0A:35:00:00:02", "MAC1-SW-P1"),
                new EthernetPort(2, MacGroup.MAC1_Switch, "192.168.2.12", "00:0A:35:00:00:03", "MAC1-SW-P2"),
                new EthernetPort(3, MacGroup.MAC1_Switch, "192.168.2.13", "00:0A:35:00:00:04", "MAC1-SW-P3"),
                new EthernetPort(4, MacGroup.MAC1_Switch, "192.168.2.14", "00:0A:35:00:00:05", "MAC1-SW-P4"),
            };
        }

        private void InitializeServices()
        {
            _monitorService = new EthernetMonitorService(_ports);
            _monitorService.PortStatsUpdated += OnPortStatsUpdated;

            _messageService = new MessageService();
            _messageService.MessageDelivered += OnMessageDelivered;

            _pingService = new PingService();
        }

        // ════════════════════════════════════════════════════════════════════
        //  UI CONSTRUCTION
        // ════════════════════════════════════════════════════════════════════

        private void BuildUI()
        {
            this.Text = "Zynq 7000 DLPU — Ethernet Port Monitor";
            this.BackColor = BgDark;
            this.ForeColor = TextPrimary;
            this.MinimumSize = new Size(1280, 860);
            this.Size = new Size(1440, 960);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

            BuildHeader();
            BuildControlBar();
            BuildPortPanels();
            BuildAggregatePanel();
            BuildMessagePanel();
            BuildStatusBar();
            ArrangeLayout();
        }

        // ── Header ───────────────────────────────────────────────────────────
        private void BuildHeader()
        {
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.FromArgb(8, 12, 22),
                Padding = new Padding(16, 0, 16, 0)
            };

            _lblTitle = new Label
            {
                Text = "⬡  Zynq 7000 DLPU",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = AccentBlue,
                AutoSize = true,
                Location = new Point(16, 12)
            };

            _lblSubtitle = new Label
            {
                Text = "MAC0: Direct Connector (1×)  |  MAC1: Switch (4×)  |  Total: 5 Ethernet Ports",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = TextSecond,
                AutoSize = true,
                Location = new Point(18, 44)
            };

            _lblClock = new Label
            {
                Text = DateTime.Now.ToString("HH:mm:ss.fff"),
                Font = new Font("Consolas", 14f, FontStyle.Regular),
                ForeColor = AccentGreen,
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _lblClock.Location = new Point(_headerPanel.Width - 200, 24);

            _headerPanel.Controls.AddRange(new Control[] { _lblTitle, _lblSubtitle, _lblClock });
            _headerPanel.Resize += (s, e) =>
                _lblClock.Location = new Point(_headerPanel.Width - _lblClock.Width - 20, 24);

            _headerPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(AccentBlue, 1))
                    e.Graphics.DrawLine(pen, 0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
            };
        }

        // ── Control Bar ──────────────────────────────────────────────────────
        private void BuildControlBar()
        {
            _controlBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = BgPanel,
                Padding = new Padding(12, 8, 12, 8)
            };

            _btnStartStop = MakeButton("▶  Start Monitor", AccentGreen, 150);
            _btnStartStop.Click += OnStartStopClicked;

            _btnPingAll = MakeButton("◎  Ping All Ports", AccentBlue, 150);
            _btnPingAll.Click += async (s, e) => await PingAllPorts();

            _btnResetAll = MakeButton("↺  Reset Counters", AccentOrange, 150);
            _btnResetAll.Click += (s, e) =>
            {
                _monitorService.ResetAllStats();
                AppendLog("All port counters reset.", AccentOrange);
            };

            _lblMonitorStatus = new Label
            {
                Text = "● IDLE",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = TextSecond,
                AutoSize = true
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = false,
                Padding = new Padding(0),
                WrapContents = false
            };

            flow.Controls.Add(_btnStartStop);
            flow.Controls.Add(SpacerH(10));
            flow.Controls.Add(_btnPingAll);
            flow.Controls.Add(SpacerH(10));
            flow.Controls.Add(_btnResetAll);
            flow.Controls.Add(SpacerH(20));

            var statusWrapper = new Panel { AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            statusWrapper.Controls.Add(_lblMonitorStatus);
            _lblMonitorStatus.Location = new Point(0, 2);
            flow.Controls.Add(statusWrapper);

            _controlBar.Controls.Add(flow);
        }

        // ── Port Panels ──────────────────────────────────────────────────────
        private void BuildPortPanels()
        {
            _portsContainer = new Panel
            {
                BackColor = BgDark,
                Padding = new Padding(8)
            };

            _portPanels = new PortPanel[_ports.Length];
            for (int i = 0; i < _ports.Length; i++)
            {
                var pp = new PortPanel(_ports[i], _monitorService, _pingService, _messageService);
                pp.MessageLogged += (s, msg) => AppendLog(msg.Text, msg.Color);
                _portPanels[i] = pp;
                _portsContainer.Controls.Add(pp);
            }
        }

        // ── Aggregate Panel ──────────────────────────────────────────────────
        private void BuildAggregatePanel()
        {
            _aggregatePanel = new Panel
            {
                BackColor = Color.FromArgb(12, 18, 35),
                Height = 48,
                Dock = DockStyle.Bottom,
                Padding = new Padding(12, 6, 12, 6)
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var header = MakeStaticLabel("ALL PORTS AGGREGATE  ▶", AccentYellow, 200, bold: true);
            flow.Controls.Add(header);
            flow.Controls.Add(VSep());

            _lblAggTxPkt   = MakeStaticLabel("TX Pkts: 0", AccentGreen, 180);
            _lblAggRxPkt   = MakeStaticLabel("RX Pkts: 0", AccentBlue, 180);
            _lblAggTxBytes = MakeStaticLabel("TX: 0 B", AccentGreen, 160);
            _lblAggRxBytes = MakeStaticLabel("RX: 0 B", AccentBlue, 160);
            _lblAggErrors  = MakeStaticLabel("Errors: 0", AccentRed, 120);

            flow.Controls.AddRange(new Control[]
            {
                _lblAggTxPkt, VSep(), _lblAggRxPkt, VSep(),
                _lblAggTxBytes, VSep(), _lblAggRxBytes, VSep(),
                _lblAggErrors
            });

            _aggregatePanel.Controls.Add(flow);

            _aggregatePanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(AccentYellow, 1))
                    e.Graphics.DrawLine(pen, 0, 0, _aggregatePanel.Width, 0);
            };
        }

        // ── Message Panel ────────────────────────────────────────────────────
        private void BuildMessagePanel()
        {
            _messagePanel = new Panel
            {
                BackColor = BgPanel,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 8)
            };

            // Title bar
            var titleBar = new Panel
            {
                Height = 28,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };
            var titleLabel = new Label
            {
                Text = "📨  TX / RX MESSAGE CONSOLE",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = AccentBlue,
                AutoSize = true,
                Location = new Point(0, 4)
            };
            titleBar.Controls.Add(titleLabel);
            _messagePanel.Controls.Add(titleBar);

            // Input row
            var inputRow = new Panel
            {
                Height = 36,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };

            _rtbTxInput = new RichTextBox
            {
                Width = 380,
                Height = 26,
                BackColor = Color.FromArgb(20, 28, 50),
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                Location = new Point(0, 4),
                ScrollBars = RichTextBoxScrollBars.None,
                Multiline = false
            };
            _rtbTxInput.Text = "Hello Zynq Port!";

            var lblTarget = new Label
            {
                Text = "Target:",
                ForeColor = TextSecond,
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(388, 9)
            };

            _cboTargetPort = new ComboBox
            {
                Width = 130,
                Location = new Point(438, 5),
                BackColor = BgCard,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboTargetPort.Items.Add("All Active Ports");
            foreach (var p in _ports)
                _cboTargetPort.Items.Add($"Port {p.PortId}: {p.PortName}");
            _cboTargetPort.SelectedIndex = 0;

            _lblDelay = new Label
            {
                Text = "Delay (ms):",
                ForeColor = TextSecond,
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(578, 9)
            };

            _nudDelay = new NumericUpDown
            {
                Width = 60,
                Location = new Point(648, 5),
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                BackColor = BgCard,
                ForeColor = AccentGreen,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };

            _btnSendSingle = MakeButton("⬆  Send TX", AccentGreen, 110);
            _btnSendSingle.Location = new Point(716, 3);
            _btnSendSingle.Click += OnSendSingleClicked;

            _btnSendAll = MakeButton("⬆⬆ Broadcast", AccentOrange, 120);
            _btnSendAll.Location = new Point(832, 3);
            _btnSendAll.Click += OnSendAllClicked;

            inputRow.Controls.AddRange(new Control[]
            {
                _rtbTxInput, lblTarget, _cboTargetPort,
                _lblDelay, _nudDelay, _btnSendSingle, _btnSendAll
            });
            _messagePanel.Controls.Add(inputRow);

            // Log box fills the rest
            _rtbLog = new RichTextBox
            {
                BackColor = Color.FromArgb(8, 12, 22),
                ForeColor = TextPrimary,
                Font = new Font("Consolas", 8.5f),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };
            _messagePanel.Controls.Add(_rtbLog);

            AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] Zynq 7000 DLPU Monitor initialized. 5 ports configured.", AccentGreen);
            AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] MAC0 (Direct): Port 0 → {_ports[0].IpAddress}", AccentBlue);
            AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] MAC1 (Switch): Ports 1–4 → {_ports[1].IpAddress} ... {_ports[4].IpAddress}", AccentBlue);
        }

        // ── Status Bar ───────────────────────────────────────────────────────
        private void BuildStatusBar()
        {
            _statusBar = new StatusStrip
            {
                BackColor = Color.FromArgb(6, 10, 20),
                ForeColor = TextSecond,
                SizingGrip = false,
                Dock = DockStyle.Bottom
            };

            _tsStatusLabel = new ToolStripStatusLabel
            {
                Text = "Ready — Monitor IDLE",
                ForeColor = TextSecond,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _tsTimeLabel = new ToolStripStatusLabel
            {
                Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ForeColor = AccentGreen,
                TextAlign = ContentAlignment.MiddleRight
            };

            _statusBar.Items.AddRange(new ToolStripItem[] { _tsStatusLabel, _tsTimeLabel });
        }

        // ── Layout Arrangement ───────────────────────────────────────────────
        // Uses a TableLayoutPanel (2 rows) instead of SplitContainer to avoid
        // the InvalidOperationException caused by SplitterDistance constraints
        // being evaluated before the control has a real size.
        private void ArrangeLayout()
        {
            // Fixed docked controls first (status bar bottom, header top, control bar top)
            this.Controls.Add(_statusBar);
            this.Controls.Add(_headerPanel);
            this.Controls.Add(_controlBar);

            // TableLayoutPanel: row 0 = ports area (60%), row 1 = message console (40%)
            _mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = BgDark,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // Row 0 gets 60% of available height, row 1 gets 40%
            _mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
            _mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            _mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // ── Row 0: port cards + aggregate strip ──────────────────────────
            // Wrapper panel that holds _portsContainer (fill) + _aggregatePanel (bottom)
            var topWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Margin = new Padding(0)
            };

            _portsContainer.Dock   = DockStyle.Fill;
            _aggregatePanel.Dock   = DockStyle.Bottom;

            // Add aggregate first so it anchors to bottom, then ports fill rest
            topWrapper.Controls.Add(_portsContainer);
            topWrapper.Controls.Add(_aggregatePanel);

            _mainTable.Controls.Add(topWrapper, 0, 0);

            // ── Row 1: message console ────────────────────────────────────────
            _messagePanel.Dock = DockStyle.Fill;
            _mainTable.Controls.Add(_messagePanel, 0, 1);

            this.Controls.Add(_mainTable);

            // Trigger initial port-panel layout
            this.Resize += OnFormResize;
            OnFormResize(null, null);
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            if (_portPanels == null) return;

            int n = _portPanels.Length;
            if (n == 0) return;

            int containerW = _portsContainer.ClientSize.Width - 16;
            int containerH = _portsContainer.ClientSize.Height - 16;

            if (containerW <= 0 || containerH <= 0) return;

            int panelW = (containerW - (n - 1) * 8) / n;
            int panelH = containerH;

            for (int i = 0; i < n; i++)
            {
                _portPanels[i].Location = new Point(8 + i * (panelW + 8), 8);
                _portPanels[i].Size = new Size(panelW < 10 ? 10 : panelW, panelH < 10 ? 10 : panelH);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  EVENT HANDLERS
        // ════════════════════════════════════════════════════════════════════

        private void OnStartStopClicked(object sender, EventArgs e)
        {
            if (_monitorService.IsRunning)
            {
                _monitorService.Stop();
                _btnStartStop.Text = "▶  Start Monitor";
                _btnStartStop.BackColor = Color.FromArgb(30, AccentGreen.R, AccentGreen.G, AccentGreen.B);
                _btnStartStop.ForeColor = AccentGreen;
                _lblMonitorStatus.Text = "● IDLE";
                _lblMonitorStatus.ForeColor = TextSecond;
                _tsStatusLabel.Text = "Monitor STOPPED";
                AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] Monitoring stopped.", AccentOrange);
            }
            else
            {
                for (int i = 0; i < _ports.Length; i++)
                    _monitorService.SetPortActive(i, true);

                _monitorService.Start();
                _btnStartStop.Text = "⏹  Stop Monitor";
                _btnStartStop.BackColor = Color.FromArgb(30, AccentRed.R, AccentRed.G, AccentRed.B);
                _btnStartStop.ForeColor = AccentRed;
                _btnStartStop.FlatAppearance.BorderColor = AccentRed;
                _lblMonitorStatus.Text = "● LIVE";
                _lblMonitorStatus.ForeColor = AccentGreen;
                _tsStatusLabel.Text = "Monitor RUNNING — 1 ms poll interval";
                AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] Monitoring started. Poll interval: 1 ms.", AccentGreen);
            }
        }

        private async System.Threading.Tasks.Task PingAllPorts()
        {
            _btnPingAll.Enabled = false;
            _btnPingAll.Text = "Pinging...";
            AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] Pinging all 5 ports...", AccentBlue);

            var results = await _pingService.PingAllPortsAsync(_ports);

            foreach (var r in results)
            {
                _ports[r.PortId].PingSuccess = r.Success;
                _ports[r.PortId].PingLatencyMs = r.Success ? r.LatencyMs : -1;

                var color = r.Success ? AccentGreen : AccentRed;
                var latStr = r.Success ? $"{r.LatencyMs} ms" : $"FAIL ({r.ErrorMessage})";
                AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] Port {r.PortId} ({r.IpAddress}) ping → {latStr}", color);
            }

            if (this.InvokeRequired)
                this.Invoke(new Action(RefreshPortPanels));
            else
                RefreshPortPanels();

            _btnPingAll.Enabled = true;
            _btnPingAll.Text = "◎  Ping All Ports";
        }

        private void OnPortStatsUpdated(object sender, PortStatsEventArgs e)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action(UpdateAggregateStats)); } catch { }
                try { this.Invoke(new Action(RefreshPortPanels)); } catch { }
            }
            else
            {
                UpdateAggregateStats();
                RefreshPortPanels();
            }
        }

        private void OnMessageDelivered(object sender, MessageReceivedEventArgs e)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(() => AppendLog(e.Message.ToString(), AccentBlue)));
            else
                AppendLog(e.Message.ToString(), AccentBlue);
        }

        private void OnSendSingleClicked(object sender, EventArgs e)
        {
            string data = _rtbTxInput.Text.Trim();
            if (string.IsNullOrEmpty(data)) return;

            double delay = (double)_nudDelay.Value;
            int selected = _cboTargetPort.SelectedIndex;

            if (selected == 0)
            {
                _messageService.BroadcastMessage(_ports, data, delay);
                AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] [TX→ALL] \"{data}\" | Delay: {delay}ms", AccentGreen);
            }
            else
            {
                var port = _ports[selected - 1];
                _messageService.SendMessage(port.PortId, port.PortName, data, delay);
                AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] [TX→Port{port.PortId}] \"{data}\" | Delay: {delay}ms", AccentGreen);
            }
        }

        private void OnSendAllClicked(object sender, EventArgs e)
        {
            string data = _rtbTxInput.Text.Trim();
            if (string.IsNullOrEmpty(data)) return;

            double delay = (double)_nudDelay.Value;
            _messageService.BroadcastMessage(_ports, data, delay);
            AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] [BROADCAST TX] \"{data}\" → All Active Ports | Delay: {delay}ms", AccentYellow);
        }

        // ════════════════════════════════════════════════════════════════════
        //  REFRESH HELPERS
        // ════════════════════════════════════════════════════════════════════

        private void UpdateAggregateStats()
        {
            long txP = 0, rxP = 0, txB = 0, rxB = 0, err = 0;
            foreach (var p in _ports)
            {
                txP += p.TxPackets;
                rxP += p.RxPackets;
                txB += p.TxBytes;
                rxB += p.RxBytes;
                err += p.TxErrors + p.RxErrors;
            }

            _lblAggTxPkt.Text   = $"TX Pkts: {txP:N0}";
            _lblAggRxPkt.Text   = $"RX Pkts: {rxP:N0}";
            _lblAggTxBytes.Text = $"TX: {FormatBytes(txB)}";
            _lblAggRxBytes.Text = $"RX: {FormatBytes(rxB)}";
            _lblAggErrors.Text  = $"Errors: {err}";
            _tsTimeLabel.Text   = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private void RefreshPortPanels()
        {
            foreach (var pp in _portPanels)
                pp.RefreshStats();
        }

        // ════════════════════════════════════════════════════════════════════
        //  UTILITIES
        // ════════════════════════════════════════════════════════════════════

        public void AppendLog(string text, Color color)
        {
            if (_rtbLog == null) return;

            if (_rtbLog.InvokeRequired)
            {
                _rtbLog.Invoke(new Action(() => AppendLog(text, color)));
                return;
            }

            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.SelectionLength = 0;
            _rtbLog.SelectionColor = color;
            _rtbLog.AppendText(text + "\n");
            _rtbLog.SelectionColor = _rtbLog.ForeColor;

            // Keep last 500 lines
            while (_rtbLog.Lines.Length > 500)
            {
                _rtbLog.Select(0, _rtbLog.GetFirstCharIndexFromLine(1));
                _rtbLog.SelectedText = "";
            }

            _rtbLog.ScrollToCaret();
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1073741824) return $"{bytes / 1048576.0:F1} MB";
            return $"{bytes / 1073741824.0:F2} GB";
        }

        private void StartClockTimer()
        {
            _clockTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _clockTimer.Tick += (s, e) =>
                _lblClock.Text = DateTime.Now.ToString("HH:mm:ss.fff");
            _clockTimer.Start();
        }

        private static Button MakeButton(string text, Color accent, int width)
        {
            return new Button
            {
                Text = text,
                Width = width,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, accent.R, accent.G, accent.B),
                ForeColor = accent,
                FlatAppearance = { BorderColor = accent, BorderSize = 1 },
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 2),
                UseVisualStyleBackColor = false
            };
        }

        private static Label MakeStaticLabel(string text, Color color, int width, bool bold = false)
        {
            return new Label
            {
                Text = text,
                ForeColor = color,
                Font = new Font("Segoe UI", 8.5f, bold ? FontStyle.Bold : FontStyle.Regular),
                Width = width,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };
        }

        private static Panel SpacerH(int width)
        {
            return new Panel { Width = width, Height = 32, BackColor = Color.Transparent };
        }

        private static Label VSep()
        {
            return new Label
            {
                Width = 1,
                BackColor = Color.FromArgb(50, 70, 120),
                Height = 36,
                Margin = new Padding(4, 0, 4, 0)
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _monitorService?.Stop();
            _monitorService?.Dispose();
            _messageService?.Dispose();
            _clockTimer?.Stop();
            base.OnFormClosing(e);
        }
    }

    // Helper for PortPanel to log to MainForm console
    public class LogEventArgs : EventArgs
    {
        public string Text { get; set; }
        public Color Color { get; set; }
        public LogEventArgs(string text, Color c) { Text = text; Color = c; }
    }
}
