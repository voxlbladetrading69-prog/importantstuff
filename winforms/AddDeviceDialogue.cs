namespace Opus
{
    partial class AddDeviceDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddDeviceDialog));
            SubtitleLabel = new SiticoneNetCoreUI.SiticoneLabel();
            TitleLabel = new SiticoneNetCoreUI.SiticoneLabel();
            HwidTextBox = new SiticoneNetCoreUI.SiticoneTextBoxAdvanced();
            AddButton = new SiticoneNetCoreUI.SiticoneButtonAdvanced();
            CancelButton = new SiticoneNetCoreUI.SiticoneButtonAdvanced();
            siticoneBorderlessForm1 = new SiticoneNetCoreUI.SiticoneBorderlessForm(components);
            PromptPanel = new SiticoneNetCoreUI.SiticoneAdvancedPanel();
            PromptPanel.SuspendLayout();
            SuspendLayout();
            // 
            // SubtitleLabel
            // 
            SubtitleLabel.AutoSize = true;
            SubtitleLabel.BackColor = Color.Transparent;
            SubtitleLabel.Font = new Font("Segoe UI", 10F);
            SubtitleLabel.ForeColor = Color.FromArgb(170, 170, 180);
            SubtitleLabel.Location = new Point(22, 33);
            SubtitleLabel.Name = "SubtitleLabel";
            SubtitleLabel.Size = new Size(223, 19);
            SubtitleLabel.TabIndex = 1;
            SubtitleLabel.Text = "Add a device by entering its HWID.";
            // 
            // TitleLabel
            // 
            TitleLabel.AutoSize = true;
            TitleLabel.BackColor = Color.Transparent;
            TitleLabel.Font = new Font("Trebuchet MS", 16F, FontStyle.Bold);
            TitleLabel.ForeColor = Color.White;
            TitleLabel.Location = new Point(8, 4);
            TitleLabel.Name = "TitleLabel";
            TitleLabel.Size = new Size(130, 27);
            TitleLabel.TabIndex = 0;
            TitleLabel.Text = "Add Device";
            // 
            // HwidTextBox
            // 
            HwidTextBox.BackColor = Color.Transparent;
            HwidTextBox.BackgroundColor = Color.FromArgb(40, 48, 48);
            HwidTextBox.BorderColor = Color.DarkGray;
            HwidTextBox.BottomLeftCornerRadius = 21;
            HwidTextBox.BottomRightCornerRadius = 21;
            HwidTextBox.FocusBorderColor = Color.DodgerBlue;
            HwidTextBox.FocusImage = null;
            HwidTextBox.ForeColor = Color.White;
            HwidTextBox.HoverBorderColor = Color.Gray;
            HwidTextBox.HoverImage = null;
            HwidTextBox.IdleImage = null;
            HwidTextBox.Location = new Point(22, 60);
            HwidTextBox.MakeRadial = true;
            HwidTextBox.Margin = new Padding(0);
            HwidTextBox.Name = "HwidTextBox";
            HwidTextBox.PlaceholderColor = Color.Gray;
            HwidTextBox.PlaceholderFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            HwidTextBox.PlaceholderText = "Enter HWID";
            HwidTextBox.ReadOnlyColors.BackgroundColor = Color.FromArgb(245, 245, 245);
            HwidTextBox.ReadOnlyColors.BorderColor = Color.FromArgb(200, 200, 200);
            HwidTextBox.ReadOnlyColors.PlaceholderColor = Color.FromArgb(150, 150, 150);
            HwidTextBox.ReadOnlyColors.TextColor = Color.FromArgb(100, 100, 100);
            HwidTextBox.Size = new Size(384, 42);
            HwidTextBox.TabIndex = 2;
            HwidTextBox.TextColor = Color.White;
            HwidTextBox.TextContent = "";
            HwidTextBox.TopLeftCornerRadius = 10;
            HwidTextBox.TopRightCornerRadius = 10;
            HwidTextBox.ValidationPattern = "";
            // 
            // AddButton
            // 
            AddButton.BackColor = Color.Transparent;
            AddButton.BadgeBackColor = Color.Red;
            AddButton.BadgeForeColor = Color.White;
            AddButton.BadgeRadius = 8;
            AddButton.BadgeRightMargin = 10;
            AddButton.BadgeValue = 0;
            AddButton.BorderColor = Color.FromArgb(150, 255, 255, 255);
            AddButton.BorderColorEnd = Color.Gray;
            AddButton.BorderColorStart = Color.White;
            AddButton.BorderRadiusBottomLeft = 35;
            AddButton.BorderRadiusBottomRight = 35;
            AddButton.BorderRadiusTopLeft = 35;
            AddButton.BorderRadiusTopRight = 35;
            AddButton.BorderThickness = 0;
            AddButton.ButtonColorEnd = Color.FromArgb(26, 115, 232);
            AddButton.ButtonColorStart = Color.FromArgb(26, 115, 232);
            AddButton.ButtonImage = null;
            AddButton.CanBeep = false;
            AddButton.CanShake = false;
            AddButton.ClickSoundPath = null;
            AddButton.DisabledOverlayOpacity = 0.5F;
            AddButton.EnableBorderGradient = false;
            AddButton.EnableClickSound = false;
            AddButton.EnableFocusBorder = false;
            AddButton.EnableHoverSound = false;
            AddButton.EnablePressScale = false;
            AddButton.EnableTextShadow = false;
            AddButton.FocusBorderColor = Color.FromArgb(100, 150, 255);
            AddButton.FocusBorderThickness = 2;
            AddButton.Font = new Font("Trebuchet MS", 11F, FontStyle.Bold);
            AddButton.ForeColor = Color.White;
            AddButton.HoverColor = Color.FromArgb(20, 0, 0, 0);
            AddButton.HoverSoundPath = null;
            AddButton.HoverTransitionSpeed = 0.08F;
            AddButton.ImageAlign = ContentAlignment.MiddleLeft;
            AddButton.ImageLeftMargin = 5;
            AddButton.ImageRightMargin = 8;
            AddButton.ImageSize = 24;
            AddButton.IsReadOnly = false;
            AddButton.Location = new Point(301, 115);
            AddButton.MakeRadial = false;
            AddButton.Name = "AddButton";
            AddButton.PressAnimationSpeed = 0.2F;
            AddButton.PressDepth = 1;
            AddButton.RippleColor = Color.FromArgb(60, 255, 255, 255);
            AddButton.RippleExpandSpeedFactor = 0.05F;
            AddButton.RippleFadeSpeedFactor = 0.03F;
            AddButton.ShadowBlurFactor = 0.85F;
            AddButton.ShadowColor = Color.FromArgb(70, 0, 0, 0);
            AddButton.ShadowOffsetX = 3;
            AddButton.ShadowOffsetY = 3;
            AddButton.Size = new Size(105, 40);
            AddButton.TabIndex = 4;
            AddButton.Text = "Add";
            AddButton.TextAlign = ContentAlignment.MiddleCenter;
            AddButton.TextPaddingBottom = 0;
            AddButton.TextPaddingLeft = 0;
            AddButton.TextPaddingRight = 0;
            AddButton.TextPaddingTop = 0;
            AddButton.TextShadowColor = Color.FromArgb(100, 0, 0, 0);
            AddButton.TextShadowOffsetX = 1;
            AddButton.TextShadowOffsetY = 1;
            AddButton.Click += AddButton_Click;
            // 
            // CancelButton
            // 
            CancelButton.BackColor = Color.Transparent;
            CancelButton.BadgeBackColor = Color.Red;
            CancelButton.BadgeForeColor = Color.White;
            CancelButton.BadgeRadius = 8;
            CancelButton.BadgeRightMargin = 10;
            CancelButton.BadgeValue = 0;
            CancelButton.BorderColor = Color.FromArgb(70, 70, 80);
            CancelButton.BorderColorEnd = Color.Gray;
            CancelButton.BorderColorStart = Color.White;
            CancelButton.BorderRadiusBottomLeft = 35;
            CancelButton.BorderRadiusBottomRight = 35;
            CancelButton.BorderRadiusTopLeft = 35;
            CancelButton.BorderRadiusTopRight = 35;
            CancelButton.BorderThickness = 1;
            CancelButton.ButtonColorEnd = Color.FromArgb(45, 45, 48);
            CancelButton.ButtonColorStart = Color.FromArgb(45, 45, 48);
            CancelButton.ButtonImage = null;
            CancelButton.CanBeep = false;
            CancelButton.CanShake = false;
            CancelButton.ClickSoundPath = null;
            CancelButton.DisabledOverlayOpacity = 0.5F;
            CancelButton.EnableBorderGradient = false;
            CancelButton.EnableClickSound = false;
            CancelButton.EnableFocusBorder = false;
            CancelButton.EnableHoverSound = false;
            CancelButton.EnablePressScale = false;
            CancelButton.EnableTextShadow = false;
            CancelButton.FocusBorderColor = Color.FromArgb(100, 150, 255);
            CancelButton.FocusBorderThickness = 2;
            CancelButton.Font = new Font("Trebuchet MS", 11F, FontStyle.Bold);
            CancelButton.ForeColor = Color.White;
            CancelButton.HoverColor = Color.FromArgb(20, 0, 0, 0);
            CancelButton.HoverSoundPath = null;
            CancelButton.HoverTransitionSpeed = 0.08F;
            CancelButton.ImageAlign = ContentAlignment.MiddleLeft;
            CancelButton.ImageLeftMargin = 5;
            CancelButton.ImageRightMargin = 8;
            CancelButton.ImageSize = 24;
            CancelButton.IsReadOnly = false;
            CancelButton.Location = new Point(180, 115);
            CancelButton.MakeRadial = false;
            CancelButton.Name = "CancelButton";
            CancelButton.PressAnimationSpeed = 0.2F;
            CancelButton.PressDepth = 1;
            CancelButton.RippleColor = Color.FromArgb(60, 255, 255, 255);
            CancelButton.RippleExpandSpeedFactor = 0.05F;
            CancelButton.RippleFadeSpeedFactor = 0.03F;
            CancelButton.ShadowBlurFactor = 0.85F;
            CancelButton.ShadowColor = Color.FromArgb(70, 0, 0, 0);
            CancelButton.ShadowOffsetX = 3;
            CancelButton.ShadowOffsetY = 3;
            CancelButton.Size = new Size(105, 40);
            CancelButton.TabIndex = 3;
            CancelButton.Text = "Cancel";
            CancelButton.TextAlign = ContentAlignment.MiddleCenter;
            CancelButton.TextPaddingBottom = 0;
            CancelButton.TextPaddingLeft = 0;
            CancelButton.TextPaddingRight = 0;
            CancelButton.TextPaddingTop = 0;
            CancelButton.TextShadowColor = Color.FromArgb(100, 0, 0, 0);
            CancelButton.TextShadowOffsetX = 1;
            CancelButton.TextShadowOffsetY = 1;
            CancelButton.Click += CancelButton_Click;
            // 
            // siticoneBorderlessForm1
            // 
            siticoneBorderlessForm1.NavBarColor = Color.FromArgb(26, 26, 30);
            siticoneBorderlessForm1.NavBarHeight = 20;
            siticoneBorderlessForm1.TargetForm = this;
            // 
            // PromptPanel
            // 
            PromptPanel.ActiveBackColor = Color.Empty;
            PromptPanel.ActiveBorderColor = Color.Empty;
            PromptPanel.AdvancedBorderStyle = SiticoneNetCoreUI.SiticoneAdvancedPanel.BorderStyleEx.Solid;
            PromptPanel.AnimationDuration = 500;
            PromptPanel.AnimationType = SiticoneNetCoreUI.SiticoneAdvancedPanel.AnimationTypeEx.Fade;
            PromptPanel.BackColor = Color.FromArgb(36, 36, 40);
            PromptPanel.BackgroundImageCustom = null;
            PromptPanel.BackgroundImageOpacity = 1F;
            PromptPanel.BackgroundImageSizeMode = SiticoneNetCoreUI.SiticoneAdvancedPanel.ImageSizeModeEx.Stretch;
            PromptPanel.BackgroundOverlayColor = Color.FromArgb(0, 0, 0, 0);
            PromptPanel.BorderColor = Color.FromArgb(60, 60, 70);
            PromptPanel.BorderDashPattern = null;
            PromptPanel.BorderGlowColor = Color.Cyan;
            PromptPanel.BorderGlowSize = 3F;
            PromptPanel.BorderWidth = 1F;
            PromptPanel.BottomLeftRadius = 5;
            PromptPanel.BottomRightRadius = 5;
            PromptPanel.ContentAlignmentCustom = ContentAlignment.MiddleCenter;
            PromptPanel.Controls.Add(TitleLabel);
            PromptPanel.Controls.Add(CancelButton);
            PromptPanel.Controls.Add(SubtitleLabel);
            PromptPanel.Controls.Add(HwidTextBox);
            PromptPanel.Controls.Add(AddButton);
            PromptPanel.CornerPadding = new Padding(0);
            PromptPanel.DisabledBackColor = Color.Empty;
            PromptPanel.DisabledBorderColor = Color.Empty;
            PromptPanel.Dock = DockStyle.Bottom;
            PromptPanel.DoubleBorderSpacing = 2F;
            PromptPanel.EasingType = SiticoneNetCoreUI.SiticoneAdvancedPanel.EasingTypeEx.Linear;
            PromptPanel.EnableAnimation = false;
            PromptPanel.EnableBackgroundImage = false;
            PromptPanel.EnableBorderGlow = false;
            PromptPanel.EnableDoubleBorder = false;
            PromptPanel.EnableGradient = false;
            PromptPanel.EnableInnerShadow = false;
            PromptPanel.EnableShadow = false;
            PromptPanel.EnableSmartPadding = true;
            PromptPanel.EnableStateStyles = false;
            PromptPanel.FlowDirectionCustom = FlowDirection.LeftToRight;
            PromptPanel.GradientAngle = 90F;
            PromptPanel.GradientEndColor = Color.LightGray;
            PromptPanel.GradientStartColor = Color.White;
            PromptPanel.GradientType = SiticoneNetCoreUI.SiticoneAdvancedPanel.GradientTypeEx.Linear;
            PromptPanel.HoverBackColor = Color.Empty;
            PromptPanel.HoverBorderColor = Color.Empty;
            PromptPanel.InnerShadowColor = Color.Black;
            PromptPanel.InnerShadowDepth = 3;
            PromptPanel.InnerShadowOpacity = 0.2F;
            PromptPanel.Location = new Point(0, 17);
            PromptPanel.Margin = new Padding(0);
            PromptPanel.Name = "PromptPanel";
            PromptPanel.RadialGradientCenter = (PointF)resources.GetObject("PromptPanel.RadialGradientCenter");
            PromptPanel.RadialGradientRadius = 1F;
            PromptPanel.ScaleRatio = 0.8F;
            PromptPanel.SecondaryBorderColor = Color.DarkGray;
            PromptPanel.ShadowBlur = 10;
            PromptPanel.ShadowColor = Color.Black;
            PromptPanel.ShadowDepth = 5;
            PromptPanel.ShadowOffset = new Point(2, 2);
            PromptPanel.ShadowOpacity = 0.3F;
            PromptPanel.Size = new Size(430, 181);
            PromptPanel.SlideDirection = new Point(0, -30);
            PromptPanel.TabIndex = 0;
            PromptPanel.TopLeftRadius = 5;
            PromptPanel.TopRightRadius = 5;
            // 
            // AddDeviceDialog
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(36, 36, 40);
            ClientSize = new Size(430, 198);
            Controls.Add(PromptPanel);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddDeviceDialog";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Add Device";
            PromptPanel.ResumeLayout(false);
            PromptPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private SiticoneNetCoreUI.SiticoneLabel SubtitleLabel;
        private SiticoneNetCoreUI.SiticoneLabel TitleLabel;
        private SiticoneNetCoreUI.SiticoneButtonAdvanced AddButton;
        private SiticoneNetCoreUI.SiticoneButtonAdvanced CancelButton;
        private SiticoneNetCoreUI.SiticoneTextBoxAdvanced HwidTextBox;
        private SiticoneNetCoreUI.SiticoneBorderlessForm siticoneBorderlessForm1;
        private SiticoneNetCoreUI.SiticoneAdvancedPanel PromptPanel;
    }
}
