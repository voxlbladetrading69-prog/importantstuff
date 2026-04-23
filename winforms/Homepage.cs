using SiticoneNetCoreUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using Supabase;
namespace Opus
{
    public partial class Homepage : Form
    {
        // state variables
        string activePanel = "DevicesPanel";
        private Device? _selectedDevice;
        private string activeDeviceDetailTab = "DeviceOverview";
        private readonly string _conn = "Host=aws-1-ap-southeast-2.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.pozhzivlssyhcynpctiz;Password=plshelpmedead123;SSL Mode=Require;Trust Server Certificate=true;Timeout=5;Command Timeout=10";

        private readonly DeviceCacheService _cacheService;
        private readonly System.Windows.Forms.Timer _refreshTimer;
        //
        public Homepage()
        {
            InitializeComponent();
            _cacheService = new DeviceCacheService(_conn);
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 15_000 };
            _refreshTimer.Tick += async (_, __) => await RefreshFromCacheAsync();
            Device.BindStatsLabels( // set to the text labels 
                TotalText,
                ActiveText,
                InactiveText
            );
            Device.BindOverviewPanelLabels(
                ActiveDevicesText,      // the big "1"
                ActiveDevicesLastDay         // "+0 in the last 24 Hours"
            );
            this.Load += InitiateDbConnection;
            this.FormClosing += (_, __) => _refreshTimer.Stop();
        }
        private async void InitiateDbConnection(object? sender, EventArgs e)
        {
            try
            {
                await _cacheService.InitializeAsync();
                RenderDevicesFromCache();
                _refreshTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Load failed");
            }
            //for (int i = 0; i < 10; i++) // add some fake devices for demo purposes
            //{
            //    Device newDevice = new Device("Samphone", 1, 5, $"{i + 1} hours ago", Color.FromArgb(255, 128, 128));
            //    DevicesFlowLayoutPanel.Controls.Add(newDevice.DeviceButton);
            //}
        }
        private async Task RefreshFromCacheAsync()
        {
            try
            {
                var changedRows = await _cacheService.RefreshAsync();
                if (changedRows > 0)
                {
                    RenderDevicesFromCache();
                }
            }
            catch
            {
                // Keep UI responsive if a refresh cycle fails.
            }
        }

        private void RenderDevicesFromCache()
        {
            var devices = _cacheService.BuildDashboardDevices();
            Device.ResetStats();
            DevicesFlowLayoutPanel.SuspendLayout();
            DevicesFlowLayoutPanel.Controls.Clear();

            foreach (var d in devices)
            {
                d.DeviceButton.Cursor = Cursors.Hand;
                DevicesFlowLayoutPanel.Controls.Add(d.DeviceButton);
            }

            DevicesFlowLayoutPanel.ResumeLayout();
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
            SiticoneNetCoreUI.SiticoneDashboardButtonAdvanced btn = sender as SiticoneNetCoreUI.SiticoneDashboardButtonAdvanced;
            foreach (Control control in Container.Controls)
            {
                if (control is SiticoneNetCoreUI.SiticoneDashboardButtonAdvanced child)
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
            }
            DashboardButton_Click(HomeButton, EventArgs.Empty);
            GradientToggler(HomeButton, EventArgs.Empty);
            PositionStatsCards();
            //Device newDevice2 = new Device("linging phone", 0, 5, "2 hours ago", Color.FromArgb(255, 128, 128));
            //DevicesFlowLayoutPanel.Controls.Add(newDevice2.DeviceButton);
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

        private static void UpdateStats()
        {
            if (_totalDevicesLabel != null)
                _totalDevicesLabel.Text = _devices.Count.ToString();

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

            HookHoverRecursive(DeviceButton);
            SetCardHover(false);
            highlighter.SendToBack();
        }
    }
}
