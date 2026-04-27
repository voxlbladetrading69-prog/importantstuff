using Krypton.Toolkit;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormAnimation;

namespace Opus
{
    public partial class Form1 : Form
    {
        private readonly DbService _db;
        private bool _isSigningIn;
        private bool _dbReady;
        public Form1()
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.UserPaint |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
            this.MouseDown += FormDrag_MouseDown;
            SignInButton.Click += SignInButton_Click;
            const string conn =
                "Host=aws-1-ap-southeast-2.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.pozhzivlssyhcynpctiz;Password=plshelpmedead123;SSL Mode=Require;Trust Server Certificate=true;Timeout=15;Command Timeout=30"; 
            _db = new DbService(conn);
            this.Load += Form1_Load;
        }

        // IMPORTANT STUFF BELOW, DO NOT TOUCH UNLESS YOU KNOW WHAT YOU ARE DOING
        private Point titleStart, underStart, panelStart;
        private async void Form1_Load(object? sender, EventArgs e)
        {
            try
            {
                SignInButton.Enabled = false;
                await _db.EnsureAccessTokensTableAsync();
                _dbReady = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize Access Tokens table.\n\n{ex.Message}", "Database!! Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SignInButton.Enabled = true;
            }
        }
        //
        // METHODS
        //
        private async void SignInButton_Click(object sender, EventArgs e)
        {
            if (_isSigningIn) return;
            if (!_dbReady)
            {
                MessageBox.Show("Database is still initializing. Please wait a moment and try again.", "Please Wait",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isSigningIn = true;
            SignInButton.Enabled = false;
            string enteredCode = WhitelistBox.Text.Trim();
            AccessToken? matchedToken = null;
            try
            {
                matchedToken = await _db.GetValidAccessTokenAsync(enteredCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not verify access token.\n\n{ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                SignInButton.StopActivity();
                _isSigningIn = false;
                SignInButton.Enabled = true;
                return;
            }
            if (matchedToken != null)
            {
                Logo.Anchor = AnchorStyles.None;
                this.Region = null;
                // Move title up
                new Animator2D(new Path2D(
                    titleStart.X, titleStart.X,
                    titleStart.Y, titleStart.Y - 228,
                    400,0, AnimationFunctions.CubicEaseOut))
                .Play(TitleText, Animator2D.KnownProperties.Location);

                // Move undertext up
                new Animator2D(new Path2D(
                    underStart.X, underStart.X,
                    underStart.Y, underStart.Y - 228,
                    400, 0, AnimationFunctions.CubicEaseOut))
                .Play(Undertext, Animator2D.KnownProperties.Location);

                // Move panel down
                new Animator2D(new Path2D(
                    panelStart.X, panelStart.X,
                    panelStart.Y, panelStart.Y + 378,
                    400, 0, AnimationFunctions.CubicEaseOut))
                .Play(WhitelistPanel, Animator2D.KnownProperties.Location);
                
                int targetX = (this.Width - Logo.Width) / 2;
                int targetY = (this.Height - Logo.Height) / 2;

                // Recenter
                new Animator2D(new Path2D(
                    Logo.Left, targetX,
                    Logo.Top, targetY,
                    400, 0, AnimationFunctions.CubicEaseOut))
                .Play(Logo, Animator2D.KnownProperties.Location);

                await Task.Delay(500);

                targetX = (350 - Logo.Width) / 2;
                targetY = (150 - Logo.Height) / 2;

                // Resize
                new Animator2D(new Path2D(
                    this.Width, 350,
                    this.Height, 150,
                    400, 0, AnimationFunctions.Liner))
                .Play(this, Animator2D.KnownProperties.Size);

                // Recenter pt2
                new Animator2D(new Path2D(
                    Logo.Left, targetX,
                    Logo.Top, targetY,
                    400, 0, AnimationFunctions.Liner))
                .Play(Logo, Animator2D.KnownProperties.Location);

                await Task.Delay(1000);

                // Expand form to Homepage size and center it on screen,
                // while moving/resizing the logo into its header position.

                Rectangle screen = Screen.FromControl(this).WorkingArea;

                int targetFormWidth = 1167;
                int targetFormHeight = 600;

                // final centered form position
                int targetFormX = screen.Left + (screen.Width - targetFormWidth) / 2;
                int targetFormY = screen.Top + (screen.Height - targetFormHeight) / 2;

                int targetCloseX = targetFormWidth - closeButton.Width - 10;
                int targetCloseY = 5; // or 0 depending on your header alignment

                // Make sure it follows top-right logic
                closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                // Animate to final position
                new Animator2D(new Path2D(
                    closeButton.Left, targetCloseX,
                    closeButton.Top, targetCloseY,
                    450, 0, AnimationFunctions.CubicEaseOut))
                .Play(closeButton, Animator2D.KnownProperties.Location);

                Logo.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                // animate form size
                new Animator2D(new Path2D(
                    this.Width, targetFormWidth,
                    this.Height, targetFormHeight,
                    450, 0, AnimationFunctions.Liner))
                .Play(this, Animator2D.KnownProperties.Size);

                // animate form location so it ends centered on screen
                new Animator2D(new Path2D(
                    this.Left, targetFormX,
                    this.Top, targetFormY,
                    450, 0, AnimationFunctions.Liner))
                .Play(this, Animator2D.KnownProperties.Location);

                // Move logo to header position
                int finalLogoX = 10;
                int finalLogoY = 0;

                new Animator2D(new Path2D(
                    Logo.Left, finalLogoX,
                    Logo.Top, finalLogoY,
                    450, 0, AnimationFunctions.CubicEaseOut))
                .Play(Logo, Animator2D.KnownProperties.Location);

                // Resize logo to 25x25
                new Animator2D(new Path2D(
                    Logo.Width, 25,        // Width: current → 25
                    Logo.Height, 25,       // Height: current → 25
                    450, 0, AnimationFunctions.CubicEaseOut))
                .Play(Logo, Animator2D.KnownProperties.Size);


                await Task.Delay(460);

                // Open homepage in same place
                Homepage home = new Homepage(matchedToken.Username);
                home.StartPosition = FormStartPosition.Manual;
                home.Location = this.Location;
                home.Opacity = 0;
                home.Show();

                for (double i = 0; i <= 1; i += 0.1)
                {
                    home.Opacity = i;
                    this.Opacity = 1 - i;
                    await Task.Delay(15);
                }

                this.Hide();
            }
            else
            {
                MessageBox.Show("Invalid or expired access token.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SignInButton.StopActivity();
                _isSigningIn = false;
                SignInButton.Enabled = true;
            }
        }
        // custom rounding 
        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            int diameter = radius * 2;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void RoundCorners(Control target, int radius)
        {
            if (target.Width <= 0 || target.Height <= 0)
                return;

            using (GraphicsPath path = GetRoundedPath(target.ClientRectangle, radius))
            {
                target.Region = new Region(path);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            titleStart = TitleText.Location;
            underStart = Undertext.Location;
            panelStart = WhitelistPanel.Location;
            RoundCorners(this, 8);
        }
        // DO NO CHANGE ANYTHING BELOW
        // Form Dragging
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private void FormDrag_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }
    }
}
