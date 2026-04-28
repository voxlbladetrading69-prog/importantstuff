using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using Npgsql;
using SiticoneNetCoreUI;
using SkiaSharp;
using Supabase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Opus
{
    public partial class Homepage : Form
    {
        private readonly string _welcomeUsername;
        private enum AnalyticsRange
        {
            Last24Hours,
            LastWeek,
            LastMonth
        }

        // state variables
        string activePanel = "DevicesPanel";
        private Device? _selectedDevice;
        private string? _selectedAccountUsername;
        private string? _selectedPackageName;
        private string activeDeviceDetailTab = "DeviceOverview";
        private readonly List<SiticoneDashboardButtonAdvanced> _dynamicAccountButtons = new();
        private readonly List<TableLayoutPanel> _dynamicPackageRows = new();
        private AnalyticsRange _selectedAnalyticsRange = AnalyticsRange.Last24Hours;
        private readonly string _conn =
    "Host=aws-1-ap-southeast-2.pooler.supabase.com;" +
    "Port=6543;" +
    "Database=postgres;" +
    "Username=postgres.pozhzivlssyhcynpctiz;" +
    "Password=plshelpmedead123;" +
    "SSL Mode=Require;" +
    "Trust Server Certificate=true;" +
    "Timeout=5;" +
    "Command Timeout=10;" +
    "Pooling=false;";
        private readonly DeviceCacheService _cacheService;
        private readonly bool _cachePreloaded;
        private readonly bool _initialConnectivityIssue;
        private readonly System.Windows.Forms.Timer _cachePollTimer;
        private readonly AccessToken? _accessToken;
        private readonly DbService _db;
        private readonly List<FeedbackEntry> _feedbackEntries = new();
        private readonly ContextMenuStrip _feedbackContextMenu = new();
        private static bool _welcomePopupShown;
        //
        public Homepage(AccessToken? accessToken = null, DeviceCacheService? preloadedCacheService = null, bool initialConnectivityIssue = false)
        {
            InitializeComponent();
            HideSubDashboardHorizontalScrollBar();
            _accessToken = accessToken;
            _welcomeUsername = string.IsNullOrWhiteSpace(accessToken?.Username) ? "User" : accessToken!.Username.Trim();
            HomeLabel1.Text = $"Welcome back, {_welcomeUsername} !";
            _cacheService = preloadedCacheService ?? new DeviceCacheService(_conn);
            _db = new DbService(_conn);
            _cachePreloaded = preloadedCacheService != null;
            _initialConnectivityIssue = initialConnectivityIssue;
            _cachePollTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
            _cachePollTimer.Tick += async (_, __) => await PollCacheFromServerAsync();
            Device.BindStatsLabels( // set to the text labels 
                TotalText,
                ActiveText,
                InactiveText
            );
            TotalLabel.Text = "Total Accounts";
            ActiveLabel.Text = "Active Accounts";
            InactiveLabel.Text = "Inactive Accounts";
            Device.BindOverviewPanelLabels(
                ActiveDevicesText,      // the big "1"
                ActiveDevicesLastDay         // "+0 in the last 24 Hours"
            );
            this.Load += InitiateDbConnection;
            this.FormClosing += (_, __) => _cachePollTimer.Stop();
        }
        private async void InitiateDbConnection(object? sender, EventArgs e)
        {
            try
            {
                if (!_cachePreloaded)
                {
                    await _cacheService.InitializeAsync();
                }
                RenderDevicesFromCache();
                _cachePollTimer.Start();

                if (_initialConnectivityIssue)
                {
                    MessageBox.Show("Slow internet connection.", "Connection issue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch
            {
                MessageBox.Show("Slow internet connection.", "Connection issue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            //for (int i = 0; i < 10; i++) // add some fake devices for demo purposes
            //{
            //    Device newDevice = new Device("Samphone", 1, 5, $"{i + 1} hours ago", Color.FromArgb(255, 128, 128));
            //    DevicesFlowLayoutPanel.Controls.Add(newDevice.DeviceButton);
            //}
        }
        private async Task PollCacheFromServerAsync(bool refreshUi = false)
        {
            try
            {
                await _cacheService.RefreshAsync();
                if (refreshUi)
                {
                    RenderDevicesFromCache();
                }
            }
            catch
            {
                // Keep UI responsive if a background poll fails.
            }
        }

        private void RenderDevicesFromCache()
        {
            Device.ResetStats();
            var devices = _cacheService.BuildDashboardDevices();
            DevicesFlowLayoutPanel.Controls.Clear();

            foreach (var d in devices)
            {
                d.CardClicked += Device_CardClicked;
                d.DeviceButton.Cursor = Cursors.Hand;
                DevicesFlowLayoutPanel.Controls.Add(d.DeviceButton);
            }
            CenterDeviceCards();
            Device.UpdateStats();

            if (_selectedDevice != null)
            {
                var refreshed = devices.FirstOrDefault(d => d.DeviceId == _selectedDevice.DeviceId);
                if (refreshed != null)
                {
                    _selectedDevice = refreshed;
                    RefreshSelectedDeviceAnalytics();
                }
            }
        }
        private void Device_CardClicked(object? sender, EventArgs e)
        {
            if (sender is not Device device) return;

            _selectedDevice = device;
            _selectedAccountUsername = null;
            _selectedPackageName = null;
            SubDashboard.Visible = true;
            DeviceDetailsOverlay.Visible = true;
            SubDashboard.BringToFront();
            DeviceDetailsOverlay.BringToFront();
            RefreshSelectedDeviceAnalytics();
            ShowDeviceOverviewTab();
            SubDashboardGradientToggler(DeviceOverview, EventArgs.Empty);
        }

        private void RefreshSelectedDeviceAnalytics()
        {
            if (_selectedDevice == null) return;
            var snapshot = _cacheService.GetDeviceSnapshot(_selectedDevice.DeviceId);
            if (snapshot == null) return;

            PopulateDeviceOverview(snapshot);
            BuildAccountTabs(snapshot);
            BuildPackageStateTable(snapshot);
            if (!string.IsNullOrWhiteSpace(_selectedAccountUsername))
            {
                PopulateAccountAnalytics(snapshot, _selectedAccountUsername);
            }
        }

        private void ShowDeviceOverviewTab()
        {
            activeDeviceDetailTab = "DeviceOverview";
            _selectedPackageName = null;
            DeviceOverviewContentPanel.Visible = true;
            AccountDetailsContentPanel.Visible = false;
        }

        private void ShowAccountTab(string packageName)
        {
            activeDeviceDetailTab = "AccountDetails";
            _selectedPackageName = packageName;
            DeviceOverviewContentPanel.Visible = false;
            AccountDetailsContentPanel.Visible = true;
            PopulateAccountAnalyticsForPackage(_cacheService.GetDeviceSnapshot(_selectedDevice!.DeviceId), packageName);
        }

        private void BuildAccountTabs(Opus.Cachers.DeviceState snapshot)
        {
            foreach (var btn in _dynamicAccountButtons)
            {
                sContainer.Controls.Remove(btn);
                btn.Dispose();
            }
            _dynamicAccountButtons.Clear();

            var packages = snapshot.AccountsByUsername.Values
                 .GroupBy(a => CanonicalPackageName(a), StringComparer.OrdinalIgnoreCase)
                 .Select(g =>
                 {
                     var latest = g
                         .OrderByDescending(a => a.LastEventUtc)
                         .ThenBy(a => a.Username, StringComparer.OrdinalIgnoreCase)
                         .First();
                     return new
                     {
                         Package = g.Key,
                         LastEventUtc = latest.LastEventUtc,
                         Active = g.Any(a => a.IsActive()),
                         ButtonText = string.IsNullOrWhiteSpace(latest.Username)
                             ? ShortPackageName(g.Key)
                             : latest.Username
                     };
                 })
                 .OrderByDescending(a => a.LastEventUtc)
                .ThenBy(a => a.Package, StringComparer.OrdinalIgnoreCase)
                .ToList();
            
            foreach (var pkg in packages)
            {
                var btn = CreateAccountTabButton(ShortPackageName(pkg.ButtonText), pkg.Active);
                btn.Tag = pkg.Package;
                sContainer.Controls.Add(btn);
                _dynamicAccountButtons.Add(btn);
                btn.Click += (_, __) =>
                {
                    ShowAccountTab(pkg.Package);
                    SubDashboardGradientToggler(btn, EventArgs.Empty);
                };
            }
        }

        private SiticoneDashboardButtonAdvanced CreateAccountTabButton(string title, bool active)
        {
            var btn = new SiticoneDashboardButtonAdvanced
            {
                BackColor = PlaceholderAccount.BackColor,
                BorderColor = PlaceholderAccount.BorderColor,
                Font = PlaceholderAccount.Font,
                ForeColor = PlaceholderAccount.ForeColor,
                GradientAngle = PlaceholderAccount.GradientAngle,
                GradientColor1 = PlaceholderAccount.GradientColor1,
                GradientColor2 = PlaceholderAccount.GradientColor2,
                GroupName = PlaceholderAccount.GroupName,
                HoverAnimationDurationMs = PlaceholderAccount.HoverAnimationDurationMs,
                HoverBorderColor = PlaceholderAccount.HoverBorderColor,
                HoverColor = PlaceholderAccount.HoverColor,
                HoverTextColor = PlaceholderAccount.HoverTextColor,
                IndicatorColor = PlaceholderAccount.IndicatorColor,
                IndicatorSide = PlaceholderAccount.IndicatorSide,
                IndicatorWidth = PlaceholderAccount.IndicatorWidth,
                Margin = PlaceholderAccount.Margin,
                PressedColor = PlaceholderAccount.PressedColor,
                ReadOnlyColor = PlaceholderAccount.ReadOnlyColor,
                ReadOnlyTextColor = PlaceholderAccount.ReadOnlyTextColor,
                RippleColor = PlaceholderAccount.RippleColor,
                SelectedBackColor = PlaceholderAccount.SelectedBackColor,
                SelectedBorderColor = PlaceholderAccount.SelectedBorderColor,
                SelectedTextColor = PlaceholderAccount.SelectedTextColor,
                Size = PlaceholderAccount.Size,
                TextLeftPadding = PlaceholderAccount.TextLeftPadding,
                Text = title
            };
            btn.Image = Properties.Resources.plric;
            btn.IndicatorColor = active ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128);
            return btn;
        }

        private void PopulateDeviceOverview(Opus.Cachers.DeviceState device)
        {
            OverviewSubtitleLabel.Text = $"{device.Name}   {(device.IsOnline() ? "● Online" : "● Offline")}";
            OverviewSubtitleLabel.ForeColor = device.IsOnline() ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128);
            OnlineStatusValue.Text = device.IsOnline() ? "ONLINE" : "OFFLINE";
            OnlineStatusValue.ForeColor = device.IsOnline() ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128);
            ActivePackagesValue.Text = $"{device.ActiveAccounts} / {Math.Max(device.MaxAccounts, 1)}";

            var restartCount = device.AccountsByUsername.Values
                .Select(a => TryGetInt(a.Values, "restart_count"))
                .DefaultIfEmpty(0)
                .Max();
            RestartCountValue.Text = restartCount.ToString();

            InfoValue1.Text = device.DeviceId;
            InfoValue2.Text = ToAgoText(device.LastSeenUtc);
            InfoValue3.Text = FormatUptime(device.UptimeSec);
            InfoValue4.Text = $"{device.BatteryPct}%";
            InfoValue5.Text = device.Charging;
            InfoValue6.Text = device.Network;
            InfoValue7.Text = $"{device.PingMs} ms";
            InfoValue10.Text = $"{device.RamUsedMb} MB";
            InfoValue11.Text = $"{device.RamFreeMb} MB";
            InfoValue12.Text = $"{device.StorageFreeMb} MB";
            InfoValue13.Text = $"{device.BatteryTempC:0.#} C";
            InfoValue14.Text = device.DeviceId;
        }

        private void BuildPackageStateTable(Opus.Cachers.DeviceState snapshot)
        {
            foreach (var row in _dynamicPackageRows)
            {
                PackageContainer.Controls.Remove(row);
                row.Dispose();
            }
            _dynamicPackageRows.Clear();

            var packages = snapshot.AccountsByUsername.Values
                .GroupBy(a => CanonicalPackageName(a), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var latest = g.OrderByDescending(a => a.LastEventUtc).First();
                    return new
                    {
                        Package = g.Key,
                        LastSeen = latest.LastEventUtc,
                        Active = g.Any(a => a.IsActive()),
                        Reason = GetStringValue(latest.Values, "launch_reason", "reason", "source", "trigger", "manual")
                    };
                })
                .OrderByDescending(x => x.LastSeen)
                .ToList();

            ActivePackagesSub1.Text = packages.Count > 0 ? $"Recent: {ShortPackageName(packages[0].Package)}" : "Recent: -";
            PlaceholderPackage.Visible = packages.Count == 0;

            foreach (var pkg in packages)
            {
                var row = CreatePackageRow(pkg.Package, pkg.Active, pkg.LastSeen, pkg.Reason);
                PackageContainer.Controls.Add(row);
                _dynamicPackageRows.Add(row);
            }
        }

        private TableLayoutPanel CreatePackageRow(string packageName, bool active, DateTime lastSeenUtc, string reason)
        {
            var row = new TableLayoutPanel
            {
                BackColor = PlaceholderPackage.BackColor,
                ColumnCount = PlaceholderPackage.ColumnCount,
                Margin = PlaceholderPackage.Margin,
                Size = PlaceholderPackage.Size
            };
            row.ColumnStyles.Clear();
            foreach (ColumnStyle style in PlaceholderPackage.ColumnStyles)
            {
                row.ColumnStyles.Add(new ColumnStyle(style.SizeType, style.Width));
            }
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            row.Controls.Add(ClonePackageLabel(Package1Name, ShortPackageName(packageName), Color.White), 0, 0);
            row.Controls.Add(ClonePackageLabel(Package1State, active ? "Online" : "Offline", active ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128)), 1, 0);
            row.Controls.Add(ClonePackageLabel(Package1Heartbeat, ToShortAgoText(lastSeenUtc), Color.White), 2, 0);
            row.Controls.Add(ClonePackageLabel(Package1Reason, reason, Color.DarkGray), 3, 0);
            return row;
        }

        private SiticoneLabel ClonePackageLabel(SiticoneLabel template, string text, Color? foreColor = null)
            => new SiticoneLabel
            {
                BackColor = template.BackColor,
                Dock = template.Dock,
                Font = template.Font,
                ForeColor = foreColor ?? template.ForeColor,
                TextAlign = template.TextAlign,
                Text = text
            };

        private static string CanonicalPackageName(Opus.Cachers.AccountState account)
        {
            if (!string.IsNullOrWhiteSpace(account.PackageName)) return account.PackageName.Trim();
            return string.IsNullOrWhiteSpace(account.Username) ? "unknown" : account.Username.Trim();
        }

        private static string ShortPackageName(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName)) return "unknown";
            var idx = packageName.LastIndexOf('.');
            return idx >= 0 && idx < packageName.Length - 1 ? packageName[(idx + 1)..] : packageName;
        }

        private static string GetStringValue(Dictionary<string, object?> values, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!values.TryGetValue(key, out var value) || value == null) continue;
                var text = value.ToString();
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            return "manual";
        }

        private void PopulateAccountAnalyticsForPackage(Opus.Cachers.DeviceState? device, string packageName)
        {
            if (device == null) return;
            var latest = device.AccountsByUsername.Values
                .Where(a => string.Equals(CanonicalPackageName(a), packageName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.LastEventUtc)
                .FirstOrDefault();
            if (latest == null) return;
            _selectedAccountUsername = latest.Username;
            PopulateAccountAnalytics(device, latest.Username);
        }
        private void PopulateAccountAnalytics(Opus.Cachers.DeviceState? device, string username)
        {
            if (device == null || !device.AccountsByUsername.TryGetValue(username, out var account)) return;

            AccountDetailsTitle.Text = username;
            var active = account.IsActive();
            AccountDetailsStatus.Text = active ? "● Active" : "● Inactive";
            AccountDetailsStatus.ForeColor = active ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128);

            var orderedTimeline = account.MetricTimeline
                .OrderBy(p => p.EventTimeUtc)
                .ToList();

            var latestPoint = orderedTimeline.LastOrDefault()
                ?? new Opus.Cachers.AccountMetricPoint { EventTimeUtc = DateTime.UtcNow, Honey = 0m, HiveSize = 0m };

            CurrentHoneyValue.Text = $"{latestPoint.Honey:0}";
            CurrentHiveValue.Text = $"{latestPoint.HiveSize:0}";

            var cutoffUtc = DateTime.UtcNow - GetLookbackWindow(_selectedAnalyticsRange);
            var points = orderedTimeline
                .Where(p => p.EventTimeUtc >= cutoffUtc)
                .TakeLast(300)
                .ToList();

            var eventCount = points.Count;
            if (eventCount == 0)
            {
                points = new List<Opus.Cachers.AccountMetricPoint>
                {
                    new Opus.Cachers.AccountMetricPoint { EventTimeUtc = DateTime.UtcNow.AddSeconds(-30), Honey = latestPoint.Honey, HiveSize = latestPoint.HiveSize },
                    new Opus.Cachers.AccountMetricPoint { EventTimeUtc = DateTime.UtcNow, Honey = latestPoint.Honey, HiveSize = latestPoint.HiveSize },
                    new Opus.Cachers.AccountMetricPoint { EventTimeUtc = DateTime.UtcNow.AddSeconds(30), Honey = latestPoint.Honey, HiveSize = latestPoint.HiveSize }
                };
            }
            else if (eventCount == 1)
            {
                var only = points[0];
                points = new List<Opus.Cachers.AccountMetricPoint>
                {
                    new Opus.Cachers.AccountMetricPoint { EventTimeUtc = only.EventTimeUtc.AddSeconds(-30), Honey = only.Honey, HiveSize = only.HiveSize },
                    only,
                    new Opus.Cachers.AccountMetricPoint { EventTimeUtc = only.EventTimeUtc.AddSeconds(30), Honey = only.Honey, HiveSize = only.HiveSize }
                };
            }

            var honey = points.Select(p => (double)p.Honey).ToList();
            var hive = points.Select(p => (double)p.HiveSize).ToList();
            var xAxisLabels = BuildTimelineLabels(points.Select(p => p.EventTimeUtc).ToList(), eventCount);

            var minHoney = honey.Min();
            var maxHoney = honey.Max();
            var range = maxHoney - minHoney;
            if (Math.Abs(range) < 0.1)
            {
                maxHoney += 1;
                minHoney -= 1;
                range = maxHoney - minHoney;
            }


            HoneyChart.Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = honey,
                    GeometrySize = 0,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.Gold, 4)
                }
            };
            HoneyChart.YAxes = new LiveChartsCore.SkiaSharpView.Axis[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    MinLimit = minHoney - range * 0.1,
                    MaxLimit = maxHoney + range * 0.1,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(255, 255, 255, 40))
                }
            };
            HoneyChart.XAxes = new LiveChartsCore.SkiaSharpView.Axis[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    Labels = xAxisLabels,
                    LabelsRotation = 0,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(255, 255, 255, 20)),
                    MinStep = 1,
                    ForceStepToMin = true
                }
            }; 
            cartesianChart1.Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = hive,
                    GeometrySize = 0,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.DeepSkyBlue, 4)
                }
            };
            var (minHive, maxHive) = GetExpandedAxisLimits(hive.Min(), hive.Max());
            cartesianChart1.YAxes = new LiveChartsCore.SkiaSharpView.Axis[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    MinLimit = minHive,
                    MaxLimit = maxHive,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(255, 255, 255, 40))
                }
            };
            cartesianChart1.XAxes = new LiveChartsCore.SkiaSharpView.Axis[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    Labels = xAxisLabels,
                    LabelsRotation = 0,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(255, 255, 255, 20)),
                    MinStep = 1,
                    ForceStepToMin = true
                }
            };
        }

        private static (double minLimit, double maxLimit) GetExpandedAxisLimits(double min, double max)
        {
            var minLimit = min < 0 ? min * 1.10 : min * 0.90;
            var maxLimit = max < 0 ? max * 0.90 : max * 1.10;

            if (Math.Abs(maxLimit - minLimit) < 0.1)
            {
                maxLimit += 1;
                minLimit -= 1;
            }

            return (minLimit, maxLimit);
        }

        private string[] BuildTimelineLabels(List<DateTime> timestampsUtc, int originalEventCount)
        {
            if (timestampsUtc.Count == 0)
                return Array.Empty<string>();

            var timeFormat = _selectedAnalyticsRange switch
            {
                AnalyticsRange.Last24Hours => "HH:mm",
                AnalyticsRange.LastWeek => "MM-dd HH:mm",
                AnalyticsRange.LastMonth => "MM-dd",
                _ => "HH:mm"
            };

            RefreshAnalyticsRangeButtonStates();

            // For a single-event visualization with 3 padded points, show only the center label.
            if (originalEventCount == 1 && timestampsUtc.Count >= 3)
            {
                return new[] { "", timestampsUtc[1].ToLocalTime().ToString(timeFormat), "" };
            }

            var labels = Enumerable.Repeat("", timestampsUtc.Count).ToArray();

            // For small collections, avoid indexing [1].
            if (timestampsUtc.Count == 1)
            {
                labels[0] = timestampsUtc[0].ToLocalTime().ToString(timeFormat);
                return labels;
            }

            // General case: place a label at the middle point.
            var mid = timestampsUtc.Count / 2;
            labels[mid] = timestampsUtc[mid].ToLocalTime().ToString(timeFormat);

            return labels;
        }
        private void RefreshAnalyticsRangeButtonStates()
        {
            SetRangeButtonSelectedState(AnalyticsRange24HButton, _selectedAnalyticsRange == AnalyticsRange.Last24Hours);
            SetRangeButtonSelectedState(AnalyticsRange7DButton, _selectedAnalyticsRange == AnalyticsRange.LastWeek);
            SetRangeButtonSelectedState(AnalyticsRange30DButton, _selectedAnalyticsRange == AnalyticsRange.LastMonth);
        }

        private static void SetRangeButtonSelectedState(SiticoneTextButtonAdvanced button, bool isSelected)
        {
            button.TextColor = isSelected ? Color.White : Color.DarkGray;
            button.HoverShowUnderline = isSelected;
        }
        private void HideSubDashboardHorizontalScrollBar()
        {
            sContainer.HorizontalScroll.Maximum = 0;
            sContainer.HorizontalScroll.Enabled = false;
            sContainer.HorizontalScroll.Visible = false;
            sContainer.AutoScroll = true;
        }

        private void RefreshSubDashboardGradientState()
        {
            if (!SubDashboard.Visible) return;

            if (activeDeviceDetailTab == "DeviceOverview")
            {
                SubDashboardGradientToggler(DeviceOverview, EventArgs.Empty);
            }
            else
            {
                var selectedAccountButton = _dynamicAccountButtons
                    .FirstOrDefault(btn => string.Equals(btn.Tag?.ToString(), _selectedPackageName, StringComparison.OrdinalIgnoreCase));

                if (selectedAccountButton != null)
                {
                    SubDashboardGradientToggler(selectedAccountButton, EventArgs.Empty);
                }
            }

            HideSubDashboardHorizontalScrollBar();
        }
        public void SetGraphRangeToLast24Hours() => SetAnalyticsRange(AnalyticsRange.Last24Hours);
        public void SetGraphRangeToLastWeek() => SetAnalyticsRange(AnalyticsRange.LastWeek);
        public void SetGraphRangeToLastMonth() => SetAnalyticsRange(AnalyticsRange.LastMonth);

        private void SetAnalyticsRange(AnalyticsRange range)
        {
            _selectedAnalyticsRange = range;
            RefreshSelectedDeviceAnalytics();
            RefreshSubDashboardGradientState();
        }

        private static TimeSpan GetLookbackWindow(AnalyticsRange range) => range switch
        {
            AnalyticsRange.Last24Hours => TimeSpan.FromHours(24),
            AnalyticsRange.LastWeek => TimeSpan.FromDays(7),
            AnalyticsRange.LastMonth => TimeSpan.FromDays(30),
            _ => TimeSpan.FromHours(24)
        };
        private static int TryGetInt(Dictionary<string, object?> values, string key)
        {
            if (!values.TryGetValue(key, out var value) || value == null) return 0;
            return int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
        }

        private static string ToAgoText(DateTime utc)
        {
            if (utc == DateTime.MinValue) return "Never";
            var span = DateTime.UtcNow - utc;
            if (span.TotalSeconds < 60) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
            return $"{(int)span.TotalDays} day(s) ago";
        }

        private static string ToShortAgoText(DateTime utc)
        {
            if (utc == DateTime.MinValue) return "-";
            var span = DateTime.UtcNow - utc;
            if (span.TotalSeconds < 60) return $"{Math.Max(1, (int)span.TotalSeconds)} sec";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr";
            return $"{(int)span.TotalDays} day";
        }
        private static string FormatUptime(long uptimeSec)
        {
            if (uptimeSec <= 0) return "0m";
            var ts = TimeSpan.FromSeconds(uptimeSec);
            return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
        }
        private void PositionStatsCards()
        {
            Control parent = DevicesPanel;

            int parentWidth = parent.ClientSize.Width;

            // ratio-based side padding
            // change these until it matches your design nicely
            int sidePadding = (int)(parentWidth * 0.05f); // 3% from each side

            // vertically keep current top, or set one explicitly
            int top = InactiveAccounts.Top;

            // left card
            TotalDevices.Left = sidePadding;
            TotalDevices.Top = top;

            // center card
            ActiveAccounts.Left = (parentWidth - ActiveAccounts.Width) / 2;
            ActiveAccounts.Top = top;

            // right card
            InactiveAccounts.Left = parentWidth - sidePadding - InactiveAccounts.Width;
            InactiveAccounts.Top = top;
        }
        private void CenterDeviceCards()
        {
            foreach (Control ctrl in DevicesFlowLayoutPanel.Controls)
            {
                int left = Math.Max(0, (DevicesFlowLayoutPanel.ClientSize.Width - ctrl.Width) / 2);
                ctrl.Margin = new Padding(left, ctrl.Margin.Top, 0, ctrl.Margin.Bottom);
            }
            DevicesFlowLayoutPanel.Visible = false;
            DevicesFlowLayoutPanel.Visible = true;
        }

        private void GradientToggler(object sender, EventArgs e) //turn on EnableGradient for this button and turn it off for the others, also set IsSelected to true for this button and false for the others
        {
            GradientTogglerForContainer(sender, Container);
        }

        private void SubDashboardGradientToggler(object sender, EventArgs e)
        {
            GradientTogglerForContainer(sender, sContainer);
        }

        private static void GradientTogglerForContainer(object sender, Control container)
        {
            if (sender is not SiticoneDashboardButtonAdvanced btn) return;

            foreach (Control control in container.Controls)
            {
                if (control is SiticoneDashboardButtonAdvanced child)
                {
                    child.EnableGradient = false;
                    child.IsSelected = false;
                }
            }
            btn.EnableGradient = true;
            btn.IsSelected = true;
        }
        private void DashboardButton_Click(object sender, EventArgs e)
        {
            SiticoneNetCoreUI.SiticoneDashboardButtonAdvanced btn = sender as SiticoneNetCoreUI.SiticoneDashboardButtonAdvanced;
            string targetname = btn.Text + "Panel";
            this.Controls.Find(activePanel, true)[0].Hide();
            activePanel = targetname;
            this.Controls.Find(activePanel, true)[0].Show();
        }

        private void Homepage_Load(object sender, EventArgs e)
        {
            foreach (Control control in Container.Controls)
            {
                if (control is SiticoneNetCoreUI.SiticoneDashboardButtonAdvanced child)
                {
                    child.Click += GradientToggler;
                    child.Click += DashboardButton_Click;
                }
                AnalyticsRange24HButton.Click += (_, __) => SetGraphRangeToLast24Hours();
                AnalyticsRange7DButton.Click += (_, __) => SetGraphRangeToLastWeek();
                AnalyticsRange30DButton.Click += (_, __) => SetGraphRangeToLastMonth();
                RefreshAnalyticsRangeButtonStates();
            }
            DashboardButton_Click(HomeButton, EventArgs.Empty);
            DeviceOverview.Click += (_, __) =>
            {
                ShowDeviceOverviewTab();
                SubDashboardGradientToggler(DeviceOverview, EventArgs.Empty);
            };
            PlaceholderAccount.Visible = false;
            PlaceholderPackage.Visible = false;
            GradientToggler(HomeButton, EventArgs.Empty);
            BackToDevicesButton.Click += BackToDevicesButton_Click;
            PositionStatsCards();
            siticoneButtonAdvanced1.Click += AddDeviceButton_Click;
            FeedbackSubmitButton.Click += FeedbackSubmitButton_Click;
            FeedbackRefreshButton.Click += async (_, __) => await LoadFeedbackEntriesAsync();
            ConfigureFeedbackContextMenu();
            _ = InitializeFeedbackPanelAsync();
            ShowWelcomePopupIfNeeded();
            //Device newDevice2 = new Device("linging phone", 0, 5, "2 hours ago", Color.FromArgb(255, 128, 128));
            //DevicesFlowLayoutPanel.Controls.Add(newDevice2.DeviceButton);
        }
        private void ShowWelcomePopupIfNeeded()
        {
            if (_welcomePopupShown)
            {
                return;
            }

            _welcomePopupShown = true;
            var popup = new Popup(
                image: null,
                title: "Hello",
                text: $"Hello, {_welcomeUsername}! Welcome to Opus.",
                focusedControl: HomePanel,
                focusEnabled: false);
            popup.WithProceedAction(() => popup.ClosePopup());
            popup.ShowOn(this);
        }
        private async Task InitializeFeedbackPanelAsync()
        {
            try
            {
                await _db.EnsureAccessTokensTableAsync();
                await _db.EnsureAccessTokenPermissionsAsync();
                await _db.EnsureFeedbackTableAsync();
                ApplyLicenseStatusFromToken();
                ApplyFeedbackModeUi();
                await LoadFeedbackEntriesAsync();
            }
            catch
            {
                FeedbackModeLabel.Text = "Feedback service unavailable.";
            }
        }

        private void ApplyLicenseStatusFromToken()
        {
            if (_accessToken == null)
            {
                LicenseStatus.Text = "UNKNOWN";
                LicenseDuration.Text = "No access token information.";
                return;
            }

            if (_accessToken.ExpirationDateUtc == null)
            {
                LicenseStatus.Text = _accessToken.HasElevatedFeedbackAccess ? "ADMIN" : "ACTIVE";
                LicenseDuration.Text = _accessToken.HasElevatedFeedbackAccess
                    ? "No expiry (elevated token)"
                    : "No expiry";
                return;
            }

            LicenseStatus.Text = "ACTIVE";
            LicenseDuration.Text = $"Expires: {_accessToken.ExpirationDateUtc.Value:yyyy-MM-dd HH:mm} UTC";
        }

        private void ApplyFeedbackModeUi()
        {
            var isAdminToken = IsAdminToken();
            FeedbackInput.Enabled = !isAdminToken;
            FeedbackSubmitButton.Enabled = !isAdminToken;
            FeedbackInput.Visible = !isAdminToken;
            FeedbackSubmitButton.Visible = !isAdminToken;
            FeedbackModeLabel.Text = isAdminToken
                ? "Elevated access token detected: viewing all submitted feedback."
                : "Submit product feedback below.";
        }

        private async Task LoadFeedbackEntriesAsync()
        {
            var isAdminToken = IsAdminToken();
            var entries = isAdminToken
                ? await _db.GetFeedbackEntriesAsync()
                : await _db.GetFeedbackEntriesByUsernameAsync(_welcomeUsername);

            _feedbackEntries.Clear();
            _feedbackEntries.AddRange(entries);
            FeedbackList.Items.Clear();
            foreach (var entry in entries)
            {
                var favoriteMarker = entry.IsFavorite ? "★ " : "";
                FeedbackList.Items.Add($"{favoriteMarker}[{entry.CreatedAtUtc:yyyy-MM-dd HH:mm}] {entry.Username}: {entry.Content}");
            }
        }

        private async void FeedbackSubmitButton_Click(object? sender, EventArgs e)
        {
            if (IsAdminToken())
            {
                return;
            }

            var message = FeedbackInput.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show("Please enter feedback before submitting.", "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                FeedbackSubmitButton.Enabled = false;
                await _db.SubmitFeedbackAsync(_welcomeUsername, message);
                FeedbackInput.TextContent = string.Empty;
                await LoadFeedbackEntriesAsync();
            }
            catch
            {
                MessageBox.Show("Could not submit feedback right now.", "Connection issue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                FeedbackSubmitButton.Enabled = true;
            }
        }

        private bool IsAdminToken() => _accessToken != null && _accessToken.ExpirationDateUtc == null;

        private void ConfigureFeedbackContextMenu()
        {
            var favoriteItem = new ToolStripMenuItem("Toggle Favorite");
            favoriteItem.Click += async (_, __) => await ToggleSelectedFeedbackFavoriteAsync();
            var deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.Click += async (_, __) => await DeleteSelectedFeedbackAsync();

            _feedbackContextMenu.Items.Add(favoriteItem);
            _feedbackContextMenu.Items.Add(deleteItem);
            FeedbackList.ContextMenuStrip = _feedbackContextMenu;
            FeedbackList.MouseDown += FeedbackList_MouseDown;
        }

        private void FeedbackList_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var index = FeedbackList.IndexFromPoint(e.Location);
            if (index >= 0)
            {
                FeedbackList.SelectedIndex = index;
            }
        }

        private async Task ToggleSelectedFeedbackFavoriteAsync()
        {
            var selected = GetSelectedFeedbackEntry();
            if (selected == null)
            {
                return;
            }

            try
            {
                await _db.SetFeedbackFavoriteAsync(selected.Id, !selected.IsFavorite);
                await LoadFeedbackEntriesAsync();
            }
            catch
            {
                MessageBox.Show("Could not update favorite status right now.", "Connection issue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task DeleteSelectedFeedbackAsync()
        {
            var selected = GetSelectedFeedbackEntry();
            if (selected == null)
            {
                return;
            }

            var confirm = MessageBox.Show(
                "Are you sure you want to delete this feedback?",
                "Delete feedback",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                await _db.DeleteFeedbackAsync(selected.Id);
                await LoadFeedbackEntriesAsync();
            }
            catch
            {
                MessageBox.Show("Could not delete feedback right now.", "Connection issue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private FeedbackEntry? GetSelectedFeedbackEntry()
        {
            var index = FeedbackList.SelectedIndex;
            if (index < 0 || index >= _feedbackEntries.Count)
            {
                return null;
            }

            return _feedbackEntries[index];
        }
        private void BackToDevicesButton_Click(object? sender, EventArgs e)
        {
            _selectedAccountUsername = null;
            _selectedPackageName = null;
            ShowDeviceOverviewTab();
            SubDashboardGradientToggler(DeviceOverview, EventArgs.Empty);
            DeviceDetailsOverlay.Visible = false;
            SubDashboard.Visible = false;
        }

        private void DevicesFlowLayoutPanel_Resize(object sender, EventArgs e)
        {
            CenterDeviceCards();
        }

        private void DevicesPanel_Resize(object sender, EventArgs e)
        {
            PositionStatsCards();
        }

        private void DevicesPanel_ControlAdded(object sender, ControlEventArgs e)
        {
            CenterDeviceCards();
        }

        private async void AddDeviceButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new AddDeviceDialog
            {
                StartPosition = FormStartPosition.CenterParent
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                await _cacheService.AddAllowedDeviceAsync(dialog.EnteredHwid);
                RenderDevicesFromCache();
                await PollCacheFromServerAsync(refreshUi: true);
            }
            catch
            {
                MessageBox.Show("Slow internet connection.", "Connection issue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InfoLabel6_Click(object sender, EventArgs e)
        {

        }
    }

    public class Device
    {
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public int ActiveAccounts { get; set; }
        public int MaxAccounts { get; set; }
        public string LastSyncText { get; set; }
        public Color StatusColor { get; set; }
        public DateTime LastSyncUtc { get; set; } = DateTime.MinValue;
        public DateTime FirstSeenUtc { get; set; } = DateTime.MinValue;

        // this is the actual UI card you add into your FlowLayoutPanel
        public SiticoneAdvancedPanel DeviceButton { get; private set; }

        // optional exposed controls in case you want to hook events later
        public SiticoneButtonAdvanced QuickRebootButton { get; private set; }
        public SiticoneTextButton OptionsButton { get; private set; }

        private static List<Device> _devices = new List<Device>();

        private static SiticoneLabel _totalDevicesLabel;
        private static SiticoneLabel _activeAccountsLabel;
        private static SiticoneLabel _inactiveAccountsLabel;

        private static SiticoneLabel _panelTotalDevicesLabel;    
        private static SiticoneLabel _panelLast24hDeltaLabel;

        public event EventHandler? CardClicked;

        private bool _isCardHovered = false;

        public Device(
            string name,
            int activeAccounts = 0,
            int maxAccounts = 5,
            string lastSyncText = "Never",
            Color? statusColor = null,
            string deviceId = null)
        {
            DeviceId = deviceId ?? Guid.NewGuid().ToString();
            Name = name;
            ActiveAccounts = activeAccounts;
            MaxAccounts = maxAccounts;
            LastSyncText = lastSyncText;
            StatusColor = statusColor ?? Color.Red;

            BuildDeviceButton();
            _devices.Add(this);
            UpdateStats();
        }
        public static void BindStatsLabels(
    SiticoneLabel totalDevices,
    SiticoneLabel activeAccounts,
    SiticoneLabel inactiveAccounts)
        {
            _totalDevicesLabel = totalDevices;
            _activeAccountsLabel = activeAccounts;
            _inactiveAccountsLabel = inactiveAccounts;

            UpdateStats();
        }

        public static void BindOverviewPanelLabels(
            SiticoneLabel totalDevicesValueLabel,
            SiticoneLabel last24hDeltaLabel)
        {
            _panelTotalDevicesLabel = totalDevicesValueLabel;
            _panelLast24hDeltaLabel = last24hDeltaLabel;
            UpdateStats();
        }

        private void SetCardHover(bool on)
        {
            _isCardHovered = on;

            var highlighter = DeviceButton.Controls["Highlighter"] as SiticoneDashboardButtonAdvanced;
            if (highlighter == null) return;

            // normal vs hover visual
            highlighter.BorderColor = on ? Color.FromArgb(180, 180, 192) : Color.FromArgb(45, 45, 48);
            highlighter.HoverBorderColor = Color.FromArgb(180, 180, 192);

            // optional slight brighten
            highlighter.GradientColor1 = on ? Color.FromArgb(52, 52, 56) : Color.FromArgb(45, 45, 48);
            highlighter.GradientColor2 = on ? Color.FromArgb(52, 52, 56) : Color.FromArgb(45, 45, 48);
            highlighter.IsSelected = false;
        }

        private void ClearStickyCardSelection()
        {
            if (DeviceButton.Controls["Highlighter"] is not SiticoneDashboardButtonAdvanced highlighter) return;

            highlighter.IsSelected = false;
            highlighter.BorderColor = Color.FromArgb(45, 45, 48);
            highlighter.HoverBorderColor = Color.FromArgb(180, 180, 192);
        }

        private void HookHoverRecursive(Control root)
        {
            root.MouseEnter += (_, __) => SetCardHover(true);

            root.MouseLeave += (_, __) =>
            {
                // only remove hover when cursor truly leaves card area
                var p = DeviceButton.PointToClient(Cursor.Position);
                if (!DeviceButton.ClientRectangle.Contains(p))
                    SetCardHover(false);
            };

            foreach (Control c in root.Controls)
            {
                if (c.Name == "QRebootButton") continue;
                HookHoverRecursive(c);
            }
        }
        private void HookCardClickRecursive(Control root)
        {
            if (root.Name != "QRebootButton" && root.Name != "Options")
            {
                root.Click += (_, __) =>
                {
                    ClearStickyCardSelection();
                    CardClicked?.Invoke(this, EventArgs.Empty);
                };
            }

            foreach (Control c in root.Controls)
            {
                if (c.Name == "QRebootButton" || c.Name == "Options") continue;
                HookCardClickRecursive(c);
            }
        }
        public static void UpdateStats()
        {
            if (_totalDevicesLabel != null)
                _totalDevicesLabel.Text = _devices.Sum(d => d.MaxAccounts).ToString();

            if (_activeAccountsLabel != null)
                _activeAccountsLabel.Text = _devices.Sum(d => d.ActiveAccounts).ToString();

            if (_inactiveAccountsLabel != null)
                _inactiveAccountsLabel.Text = _devices.Sum(d => d.MaxAccounts - d.ActiveAccounts).ToString();

            if (_panelTotalDevicesLabel != null)
                _panelTotalDevicesLabel.Text = _devices.Count.ToString();

            int active24h = _devices.Count(d => d.LastSyncUtc >= DateTime.UtcNow.AddHours(-24));
            if (_panelLast24hDeltaLabel != null)
                _panelLast24hDeltaLabel.Text = $"+{active24h} active in the last 24 hours";
        }
        public static void ResetStats()
        {
            _devices.Clear();
            UpdateStats();
        }

        public void SetDeviceName(string newName)
        {
            Name = newName;
            var control = DeviceButton.Controls["DeviceName"] as SiticoneLabel;
            if (control != null)
                control.Text = newName;
        }

        public void SetLastSync(string newSyncText)
        {
            LastSyncText = newSyncText;
            var control = DeviceButton.Controls["LastSyncTxt"] as SiticoneLabel;
            if (control != null)
                control.Text = newSyncText;
        }

        public void SetStatusColor(Color color)
        {
            StatusColor = color;
            var control = DeviceButton.Controls["StatusIndicator"] as Panel;
            if (control != null)
                control.BackColor = color;
        }

        public void SetAccounts(int activeAccounts, int maxAccounts = 5)
        {
            ActiveAccounts = activeAccounts;
            MaxAccounts = maxAccounts;

            // remove old dots
            for (int i = DeviceButton.Controls.Count - 1; i >= 0; i--)
            {
                if (DeviceButton.Controls[i].Name.StartsWith("Acc"))
                    DeviceButton.Controls.RemoveAt(i);
            }

            int startX = 75;
            int dotY = 45;

            for (int i = 0; i < MaxAccounts; i++)
            {
                var accDot = new SiticoneRadialButton();
                accDot.Name = $"Acc{i + 1}";
                accDot.Location = new Point(startX + (i * 15), dotY);
                accDot.Size = new Size(10, 10);
                accDot.Margin = new Padding(0);
                accDot.Text = " ";
                accDot.IsReadOnly = true;
                accDot.EnableRippleEffect = false;
                accDot.EnableShadow = false;
                accDot.CanBeep = false;
                accDot.CanGlow = false;
                accDot.CanShake = false;
                accDot.BorderWidth = 2;

                bool active = i < ActiveAccounts;
                accDot.BaseColor = active ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128);
                accDot.BorderColor = active ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128);
                accDot.HoverColor = accDot.BaseColor;

                DeviceButton.Controls.Add(accDot);
                accDot.BringToFront();
                UpdateStats();
            }
        }

        private void BuildDeviceButton()
        {
            DeviceButton = new SiticoneAdvancedPanel();
            DeviceButton.Name = $"DeviceButton_{DeviceId}";
            DeviceButton.Size = new Size(920, 70);
            DeviceButton.Margin = new Padding(0, 0, 0, 10);
            DeviceButton.Padding = new Padding(10);
            DeviceButton.BackColor = Color.Transparent;
            DeviceButton.BorderWidth = 0F;
            DeviceButton.BorderColor = Color.Black;
            DeviceButton.EnableGradient = true;
            DeviceButton.GradientStartColor = Color.FromArgb(45, 45, 48);
            DeviceButton.GradientEndColor = Color.FromArgb(45, 45, 48);
            DeviceButton.TopLeftRadius = 10;
            DeviceButton.TopRightRadius = 10;
            DeviceButton.BottomLeftRadius = 10;
            DeviceButton.BottomRightRadius = 10;
            DeviceButton.EnableShadow = false;
            DeviceButton.EnableBorderGlow = false;
            DeviceButton.EnableAnimation = false;
            DeviceButton.CornerPadding = new Padding(0);

            // background highlighter
            var highlighter = new SiticoneDashboardButtonAdvanced();

            highlighter.BackColor = Color.Transparent;
            highlighter.BadgeColor = Color.Red;
            highlighter.BadgeFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            highlighter.BadgeTextColor = Color.White;
            highlighter.BorderColor = Color.FromArgb(45, 45, 48);
            highlighter.BorderThickness = 1;
            highlighter.BottomLeftRadius = 1;
            highlighter.BottomRightRadius = 5;
            highlighter.Font = new Font("Segoe UI", 9F);
            highlighter.ForeColor = Color.LightGray;
            highlighter.GradientColor1 = Color.FromArgb(45, 45, 48);
            highlighter.GradientColor2 = Color.FromArgb(192, 255, 255);
            highlighter.GroupName = "Devices";
            highlighter.HoverAnimationDurationMs = 0;
            highlighter.HoverBorderColor = Color.FromArgb(180, 180, 192);
            highlighter.HoverColor = Color.FromArgb(45, 45, 48);
            highlighter.HoverTextColor = Color.White;
            highlighter.IndicatorColor = Color.FromArgb(52, 152, 219);
            highlighter.IndicatorWidth = 4;
            highlighter.Location = new Point(0, 0);
            highlighter.Margin = new Padding(0);
            highlighter.Name = "Highlighter";
            highlighter.PressAnimationDurationMs = 0;
            highlighter.PressedColor = Color.FromArgb(45, 45, 48);
            highlighter.ReadOnlyColor = Color.FromArgb(45, 45, 48);
            highlighter.ReadOnlyTextColor = Color.DimGray;
            highlighter.RippleColor = Color.Blue;
            highlighter.SelectedBackColor = Color.FromArgb(45, 45, 48);
            highlighter.SelectedBorderColor = Color.Azure;
            highlighter.SelectedTextColor = Color.FromArgb(52, 152, 219);
            highlighter.Size = new Size(920, 70);
            highlighter.TabIndex = 8;
            highlighter.TabStop = false;
            highlighter.Text = "  ";
            highlighter.TopLeftRadius = 1;
            highlighter.TopRightRadius = 5;

            // left status strip
            var statusIndicator = new Panel();
            statusIndicator.Name = "StatusIndicator";
            statusIndicator.Location = new Point(0, 0);
            statusIndicator.Size = new Size(5, 70);
            statusIndicator.BackColor = StatusColor;

            // device icon
            var deviceIcon = new PictureBox();
            deviceIcon.Name = "DeviceIcon";
            deviceIcon.Location = new Point(10, 10);
            deviceIcon.Size = new Size(50, 50);
            deviceIcon.SizeMode = PictureBoxSizeMode.Zoom;
            deviceIcon.Image = Properties.Resources.Devices;

            // device name
            var deviceName = new SiticoneLabel();
            deviceName.Name = "DeviceName";
            deviceName.Location = new Point(62, 10);
            deviceName.Size = new Size(240, 39);
            deviceName.Font = new Font("Segoe UI", 20F, FontStyle.Regular, GraphicsUnit.Point, 0);
            deviceName.ForeColor = Color.White;
            deviceName.BackColor = Color.Transparent;
            deviceName.Text = Name;

            // account dots
            int startX = 75;
            int dotY = 45;

            for (int i = 0; i < MaxAccounts; i++)
            {
                var accDot = new SiticoneRadialButton();
                accDot.Name = $"Acc{i + 1}";
                accDot.Location = new Point(startX + (i * 15), dotY);
                accDot.Size = new Size(10, 10);
                accDot.Margin = new Padding(0);
                accDot.Text = " ";
                accDot.IsReadOnly = true;
                accDot.EnableRippleEffect = false;
                accDot.EnableShadow = false;
                accDot.CanBeep = false;
                accDot.CanGlow = false;
                accDot.CanShake = false;
                accDot.BorderWidth = 2;
                accDot.BaseColor = i < ActiveAccounts ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128);
                accDot.BorderColor = i < ActiveAccounts ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128);
                accDot.HoverColor = accDot.BaseColor;
                DeviceButton.Controls.Add(accDot);
            }

            // first divider
            var inBorder1 = new Panel();
            inBorder1.Name = "InBorder1";
            inBorder1.Location = new Point(315, 10);
            inBorder1.Size = new Size(2, 50);
            inBorder1.BackColor = Color.Gray;

            // last sync label
            var lastSync = new SiticoneLabel();
            lastSync.Name = "LastSync";
            lastSync.Location = new Point(360, 16);
            lastSync.Size = new Size(113, 39);
            lastSync.Font = new Font("Segoe UI", 16.7F);
            lastSync.ForeColor = Color.White;
            lastSync.BackColor = Color.Transparent;
            lastSync.Text = "Last Sync:";

            var lastSyncTxt = new SiticoneLabel();
            lastSyncTxt.Name = "LastSyncTxt";
            lastSyncTxt.Location = new Point(478, 16);
            lastSyncTxt.Size = new Size(172, 39);
            lastSyncTxt.Font = new Font("Segoe UI", 16.7F);
            lastSyncTxt.ForeColor = Color.White;
            lastSyncTxt.BackColor = Color.Transparent;
            lastSyncTxt.Text = LastSyncText;

            // second divider
            var inBorder2 = new Panel();
            inBorder2.Name = "InBorder2";
            inBorder2.Location = new Point(666, 9);
            inBorder2.Size = new Size(2, 50);
            inBorder2.BackColor = Color.Gray;

            // quick reboot button
            QuickRebootButton = new SiticoneButtonAdvanced();
            QuickRebootButton.BackColor = Color.Transparent;
            QuickRebootButton.BadgeBackColor = Color.Red;
            QuickRebootButton.BadgeForeColor = Color.White;
            QuickRebootButton.BadgeRadius = 8;
            QuickRebootButton.BadgeRightMargin = 10;
            QuickRebootButton.BadgeValue = 0;
            QuickRebootButton.BorderColor = Color.FromArgb(16, 100, 220);
            QuickRebootButton.BorderColorEnd = Color.Gray;
            QuickRebootButton.BorderColorStart = Color.White;
            QuickRebootButton.BorderRadiusBottomLeft = 4;
            QuickRebootButton.BorderRadiusBottomRight = 4;
            QuickRebootButton.BorderRadiusTopLeft = 4;
            QuickRebootButton.BorderRadiusTopRight = 4;
            QuickRebootButton.BorderThickness = 0;
            QuickRebootButton.ButtonColorEnd = Color.FromArgb(77, 77, 85);
            QuickRebootButton.ButtonColorStart = Color.FromArgb(77, 77, 85);
            QuickRebootButton.ButtonImage = null;
            QuickRebootButton.CanBeep = false;
            QuickRebootButton.CanShake = false;
            QuickRebootButton.ClickSoundPath = null;
            QuickRebootButton.DisabledOverlayOpacity = 0.5F;
            QuickRebootButton.EnableBorderGradient = false;
            QuickRebootButton.EnableClickSound = false;
            QuickRebootButton.EnableFocusBorder = false;
            QuickRebootButton.EnableHoverSound = false;
            QuickRebootButton.EnablePressScale = false;
            QuickRebootButton.EnableTextShadow = false;
            QuickRebootButton.FocusBorderColor = Color.FromArgb(100, 150, 255);
            QuickRebootButton.FocusBorderThickness = 2;
            QuickRebootButton.Font = new Font("Trebuchet MS", 10F);
            QuickRebootButton.ForeColor = Color.White;
            QuickRebootButton.HoverColor = Color.FromArgb(20, 255, 255, 255);
            QuickRebootButton.HoverSoundPath = null;
            QuickRebootButton.HoverTransitionSpeed = 0.08F;
            QuickRebootButton.ImageAlign = ContentAlignment.MiddleLeft;
            QuickRebootButton.ImageLeftMargin = 5;
            QuickRebootButton.ImageRightMargin = 8;
            QuickRebootButton.ImageSize = 24;
            QuickRebootButton.IsReadOnly = false;
            QuickRebootButton.Location = new Point(693, 15);
            QuickRebootButton.MakeRadial = false;
            QuickRebootButton.Margin = new Padding(0);
            QuickRebootButton.Name = "QRebootButton";
            QuickRebootButton.PressAnimationSpeed = 0.2F;
            QuickRebootButton.PressDepth = 1;
            QuickRebootButton.RippleColor = Color.FromArgb(120, 255, 255, 255);
            QuickRebootButton.RippleExpandSpeedFactor = 0.05F;
            QuickRebootButton.RippleFadeSpeedFactor = 0.03F;
            QuickRebootButton.ShadowBlurFactor = 0.7F;
            QuickRebootButton.ShadowColor = Color.FromArgb(80, 26, 115, 232);
            QuickRebootButton.ShadowOffsetX = 0;
            QuickRebootButton.ShadowOffsetY = 0;
            QuickRebootButton.Size = new Size(99, 40);
            QuickRebootButton.TabIndex = 3;
            QuickRebootButton.Text = "Quick Reboot";
            QuickRebootButton.TextAlign = ContentAlignment.MiddleCenter;
            QuickRebootButton.TextPaddingBottom = 0;
            QuickRebootButton.TextPaddingLeft = 0;
            QuickRebootButton.TextPaddingRight = 0;
            QuickRebootButton.TextPaddingTop = 0;
            QuickRebootButton.TextShadowColor = Color.FromArgb(100, 0, 0, 0);
            QuickRebootButton.TextShadowOffsetX = 0;
            QuickRebootButton.TextShadowOffsetY = 0;

            // options button
            OptionsButton = new SiticoneTextButton();
            OptionsButton.Name = "Options";
            OptionsButton.Location = new Point(874, 2);
            OptionsButton.Size = new Size(36, 21);
            OptionsButton.Margin = new Padding(0);
            OptionsButton.Text = ". . .";
            OptionsButton.Font = new Font("Showcard Gothic", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            OptionsButton.TextColor = Color.FromArgb(180, 180, 180);
            OptionsButton.HoverTextColor = SystemColors.Highlight;
            OptionsButton.BackColor = Color.Transparent;
            OptionsButton.Cursor = Cursors.Hand;
            OptionsButton.EnableRippleEffect = false;
            OptionsButton.EnablePressAnimation = false;
            OptionsButton.ShowUnderline = false;

            DeviceButton.Controls.Add(highlighter);
            DeviceButton.Controls.Add(statusIndicator);
            DeviceButton.Controls.Add(deviceIcon);
            DeviceButton.Controls.Add(deviceName);
            DeviceButton.Controls.Add(inBorder1);
            DeviceButton.Controls.Add(lastSync);
            DeviceButton.Controls.Add(lastSyncTxt);
            DeviceButton.Controls.Add(inBorder2);
            DeviceButton.Controls.Add(QuickRebootButton);
            DeviceButton.Controls.Add(OptionsButton);

            highlighter.Click += (_, __) => ClearStickyCardSelection();
            HookHoverRecursive(DeviceButton);
            HookCardClickRecursive(DeviceButton);
            SetCardHover(false);
            highlighter.SendToBack();
        }
    }
}
