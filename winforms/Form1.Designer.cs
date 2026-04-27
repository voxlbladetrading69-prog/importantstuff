namespace Opus
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Logo = new PictureBox();
            TitleText = new Label();
            Undertext = new Label();
            mySiticoneLicenseSettings1 = new SiticoneNetCoreUI.MySiticoneLicenseSettings();
            closeButton = new SiticoneNetCoreUI.SiticoneCloseButton();
            WhitelistPanel = new SiticoneNetCoreUI.SiticonePanel();
            SignInButton = new SiticoneNetCoreUI.SiticoneActivityButton();
            WhitelistText = new Label();
            WhitelistBox = new TextBox();
            ((System.ComponentModel.ISupportInitialize)Logo).BeginInit();
            WhitelistPanel.SuspendLayout();
            SuspendLayout();
            // 
            // Logo
            // 
            Logo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
            Logo.BackgroundImage = Properties.Resources.Opus2;
            Logo.BackgroundImageLayout = ImageLayout.Zoom;
            Logo.InitialImage = Properties.Resources.Opus2;
            Logo.Location = new Point(115, 3);
            Logo.Margin = new Padding(0);
            Logo.Name = "Logo";
            Logo.Size = new Size(120, 142);
            Logo.TabIndex = 0;
            Logo.TabStop = false;
            // 
            // TitleText
            // 
            TitleText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            TitleText.Font = new Font("Eras Demi ITC", 26.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            TitleText.ForeColor = Color.White;
            TitleText.Location = new Point(14, 145);
            TitleText.Name = "TitleText";
            TitleText.Size = new Size(314, 40);
            TitleText.TabIndex = 2;
            TitleText.Text = "Welcome to Opus";
            TitleText.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Undertext
            // 
            Undertext.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Undertext.Font = new Font("Gill Sans MT", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Undertext.ForeColor = Color.DarkGray;
            Undertext.ImageAlign = ContentAlignment.TopCenter;
            Undertext.Location = new Point(5, 185);
            Undertext.Name = "Undertext";
            Undertext.Size = new Size(338, 25);
            Undertext.TabIndex = 3;
            Undertext.Text = "Input your whitelist code to access Dashboard";
            Undertext.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // closeButton
            // 
            closeButton.BackgroundImageLayout = ImageLayout.None;
            closeButton.CornerRadius = 16F;
            closeButton.CountdownFont = new Font("Segoe UI", 9F);
            closeButton.Cursor = Cursors.Default;
            closeButton.EnableAnimation = false;
            closeButton.IconAnimation = SiticoneNetCoreUI.SiticoneCloseButton.CloseIconAnimation.Fade;
            closeButton.IconColor = Color.White;
            closeButton.Location = new Point(311, 3);
            closeButton.Name = "closeButton";
            closeButton.ShowTooltip = false;
            closeButton.Size = new Size(32, 32);
            closeButton.TabIndex = 5;
            closeButton.Text = "siticoneCloseButton1";
            closeButton.TooltipText = "";
            // 
            // WhitelistPanel
            // 
            WhitelistPanel.AcrylicTintColor = Color.Purple;
            WhitelistPanel.BackColor = Color.FromArgb(15, 15, 17);
            WhitelistPanel.BorderAlignment = System.Drawing.Drawing2D.PenAlignment.Outset;
            WhitelistPanel.BorderDashPattern = null;
            WhitelistPanel.BorderGradientEndColor = Color.Navy;
            WhitelistPanel.BorderGradientStartColor = Color.Purple;
            WhitelistPanel.BorderThickness = 2F;
            WhitelistPanel.Controls.Add(SignInButton);
            WhitelistPanel.Controls.Add(WhitelistText);
            WhitelistPanel.Controls.Add(WhitelistBox);
            WhitelistPanel.CornerRadiusBottomLeft = 4F;
            WhitelistPanel.CornerRadiusBottomRight = 4F;
            WhitelistPanel.CornerRadiusTopLeft = 4F;
            WhitelistPanel.CornerRadiusTopRight = 4F;
            WhitelistPanel.EnableAcrylicEffect = true;
            WhitelistPanel.EnableMicaEffect = true;
            WhitelistPanel.EnableRippleEffect = false;
            WhitelistPanel.FillColor = Color.FromArgb(15, 15, 17);
            WhitelistPanel.ForeColor = Color.FromArgb(255, 255, 192);
            WhitelistPanel.GradientColors = new Color[]
    {
    Color.White,
    Color.LightGray,
    Color.Gray
    };
            WhitelistPanel.GradientPositions = new float[]
    {
    0F,
    0.5F,
    1F
    };
            WhitelistPanel.Location = new Point(25, 240);
            WhitelistPanel.Margin = new Padding(0);
            WhitelistPanel.Name = "WhitelistPanel";
            WhitelistPanel.PatternStyle = System.Drawing.Drawing2D.HatchStyle.Max;
            WhitelistPanel.RippleAlpha = 50;
            WhitelistPanel.RippleAlphaDecrement = 3;
            WhitelistPanel.RippleColor = Color.FromArgb(50, 255, 255, 255);
            WhitelistPanel.RippleMaxSize = 600F;
            WhitelistPanel.RippleSpeed = 15F;
            WhitelistPanel.ShowBorder = true;
            WhitelistPanel.Size = new Size(300, 170);
            WhitelistPanel.TabIndex = 6;
            WhitelistPanel.TabStop = true;
            WhitelistPanel.TrackSystemTheme = false;
            WhitelistPanel.UseBorderGradient = true;
            WhitelistPanel.UseMultiGradient = false;
            WhitelistPanel.UsePatternTexture = false;
            WhitelistPanel.UseRadialGradient = false;
            // 
            // SignInButton
            // 
            SignInButton.ActivityDuration = 2000;
            SignInButton.ActivityIndicatorColor = Color.FromArgb(0, 0, 64);
            SignInButton.ActivityIndicatorSize = 4;
            SignInButton.ActivityIndicatorSpeed = 25;
            SignInButton.ActivityIndicatorStyle = SiticoneNetCoreUI.SiticoneActivityButton.ActivityIndicatorStyleFx.RotatingSquares;
            SignInButton.ActivityText = "Checking License";
            SignInButton.AnimationEasing = SiticoneNetCoreUI.SiticoneActivityButton.AnimationEasingType.EaseOutQuad;
            SignInButton.BackColor = Color.Transparent;
            SignInButton.BaseColor = Color.FromArgb(240, 240, 240);
            SignInButton.BorderColor = Color.FromArgb(200, 200, 200);
            SignInButton.BorderWidth = 2;
            SignInButton.CornerRadiusBottomLeft = 5;
            SignInButton.CornerRadiusBottomRight = 5;
            SignInButton.CornerRadiusTopLeft = 5;
            SignInButton.CornerRadiusTopRight = 5;
            SignInButton.DisabledColor = Color.FromArgb(160, 160, 160);
            SignInButton.Elevation = 2F;
            SignInButton.Font = new Font("Eras Demi ITC", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            SignInButton.HoverAnimationDuration = 200;
            SignInButton.HoverColor = Color.FromArgb(220, 220, 220);
            SignInButton.Location = new Point(55, 105);
            SignInButton.Name = "SignInButton";
            SignInButton.PressAnimationDuration = 150;
            SignInButton.PressedColor = Color.FromArgb(200, 200, 200);
            SignInButton.PressedElevation = 1F;
            SignInButton.RippleColor = Color.FromArgb(100, 180, 180, 180);
            SignInButton.RippleDuration = 1800;
            SignInButton.RippleSize = 5;
            SignInButton.ShowActivityText = false;
            SignInButton.Size = new Size(190, 40);
            SignInButton.TabIndex = 9;
            SignInButton.Text = "Sign In";
            SignInButton.TextColor = Color.FromArgb(60, 60, 60);
            SignInButton.Theme = SiticoneNetCoreUI.SiticoneActivityButton.ActivityButtonTheme.Custom;
            SignInButton.UseAnimation = true;
            SignInButton.UseElevation = false;
            SignInButton.UseRippleEffect = true;
            // 
            // WhitelistText
            // 
            WhitelistText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            WhitelistText.Font = new Font("Gill Sans MT", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            WhitelistText.ForeColor = Color.White;
            WhitelistText.ImageAlign = ContentAlignment.MiddleLeft;
            WhitelistText.Location = new Point(9, 22);
            WhitelistText.Name = "WhitelistText";
            WhitelistText.RightToLeft = RightToLeft.No;
            WhitelistText.Size = new Size(124, 24);
            WhitelistText.TabIndex = 7;
            WhitelistText.Text = "Whitelist Code";
            WhitelistText.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // WhitelistBox
            // 
            WhitelistBox.BackColor = Color.FromArgb(25, 25, 30);
            WhitelistBox.BorderStyle = BorderStyle.None;
            WhitelistBox.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            WhitelistBox.ForeColor = SystemColors.InactiveBorder;
            WhitelistBox.Location = new Point(12, 49);
            WhitelistBox.Margin = new Padding(0);
            WhitelistBox.Name = "WhitelistBox";
            WhitelistBox.PlaceholderText = "XXXX-XXXX-XXXX";
            WhitelistBox.Size = new Size(280, 28);
            WhitelistBox.TabIndex = 8;
            WhitelistBox.UseSystemPasswordChar = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(17, 17, 25);
            ClientSize = new Size(350, 456);
            Controls.Add(WhitelistPanel);
            Controls.Add(closeButton);
            Controls.Add(Undertext);
            Controls.Add(TitleText);
            Controls.Add(Logo);
            ForeColor = Color.FromArgb(64, 64, 64);
            FormBorderStyle = FormBorderStyle.None;
            Name = "Form1";
            Padding = new Padding(3, 0, 3, 3);
            RightToLeft = RightToLeft.No;
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)Logo).EndInit();
            WhitelistPanel.ResumeLayout(false);
            WhitelistPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox Logo;
        private Label TitleText;
        private Label Undertext;
        private SiticoneNetCoreUI.MySiticoneLicenseSettings mySiticoneLicenseSettings1;
        private SiticoneNetCoreUI.SiticoneCloseButton closeButton;
        private SiticoneNetCoreUI.SiticonePanel WhitelistPanel;
        private SiticoneNetCoreUI.SiticoneActivityButton SignInButton;
        private Label WhitelistText;
        private TextBox WhitelistBox;
    }
}
