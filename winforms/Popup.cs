using SiticoneNetCoreUI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Opus
{
    /// <summary>
    /// Reusable onboarding/tutorial popup that can optionally dim the entire host form
    /// except for one focused control.
    /// </summary>
    public class Popup : Panel
    {
        private readonly PictureBox _imageBox;
        private readonly SiticoneLabel _titleLabel;
        private readonly SiticoneLabel _textLabel;
        private readonly SiticoneButtonAdvanced _proceedButton;

        private readonly Panel _focusTop = CreateFocusPanel();
        private readonly Panel _focusLeft = CreateFocusPanel();
        private readonly Panel _focusRight = CreateFocusPanel();
        private readonly Panel _focusBottom = CreateFocusPanel();

        private Action? _proceedAction;

        public Control? FocusedControl { get; private set; }
        public bool FocusEnabled { get; private set; }

        public Popup(Image? image, string title, string text, Control? focusedControl, bool focusEnabled = true)
        {
            FocusedControl = focusedControl;
            FocusEnabled = focusEnabled;

            Size = new Size(520, 240);
            BackColor = Color.FromArgb(35, 35, 40);
            BorderStyle = BorderStyle.FixedSingle;

            _imageBox = new PictureBox
            {
                Image = image,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(20, 20),
                Size = new Size(84, 84),
                BackColor = Color.Transparent
            };

            _titleLabel = new SiticoneLabel
            {
                Text = title,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(120, 20),
                Size = new Size(380, 36),
                BackColor = Color.Transparent
            };

            _textLabel = new SiticoneLabel
            {
                Text = text,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.Gainsboro,
                Location = new Point(120, 64),
                Size = new Size(370, 105),
                BackColor = Color.Transparent
            };

            _proceedButton = new SiticoneButtonAdvanced
            {
                Text = "Proceed",
                Size = new Size(110, 36),
                Location = new Point(380, 185),
                BackColor = Color.FromArgb(34, 153, 186),
                ForeColor = Color.White
            };
            _proceedButton.Click += (_, __) => _proceedAction?.Invoke();

            Controls.Add(_imageBox);
            Controls.Add(_titleLabel);
            Controls.Add(_textLabel);
            Controls.Add(_proceedButton);
        }

        public Popup WithProceedAction(Action proceedAction)
        {
            _proceedAction = proceedAction;
            return this;
        }

        public void SetProceedAction(Action proceedAction)
        {
            _proceedAction = proceedAction;
        }

        public void ShowOn(Form host)
        {
            if (host == null) return;

            if (FocusEnabled)
            {
                ApplyFocusEffect(host);
            }

            if (Parent != host)
            {
                host.Controls.Add(this);
            }

            BringToFront();
            Location = new Point(
                Math.Max(20, (host.ClientSize.Width - Width) / 2),
                Math.Max(20, host.ClientSize.Height - Height - 20));
        }

        public void ClosePopup()
        {
            RemoveFocusEffect();
            Parent?.Controls.Remove(this);
            Dispose();
        }

        private void ApplyFocusEffect(Form host)
        {
            RemoveFocusEffect();

            host.Controls.Add(_focusTop);
            host.Controls.Add(_focusLeft);
            host.Controls.Add(_focusRight);
            host.Controls.Add(_focusBottom);

            _focusTop.BringToFront();
            _focusLeft.BringToFront();
            _focusRight.BringToFront();
            _focusBottom.BringToFront();

            var target = FocusedControl;
            if (target == null || target.IsDisposed || target.FindForm() != host)
            {
                _focusTop.Bounds = host.ClientRectangle;
                _focusLeft.Bounds = Rectangle.Empty;
                _focusRight.Bounds = Rectangle.Empty;
                _focusBottom.Bounds = Rectangle.Empty;
                return;
            }

            var focusRect = host.RectangleToClient(target.RectangleToScreen(target.ClientRectangle));
            focusRect.Inflate(6, 6);

            var w = host.ClientSize.Width;
            var h = host.ClientSize.Height;

            _focusTop.Bounds = new Rectangle(0, 0, w, Math.Max(0, focusRect.Top));
            _focusLeft.Bounds = new Rectangle(0, focusRect.Top, Math.Max(0, focusRect.Left), Math.Max(0, focusRect.Height));
            _focusRight.Bounds = new Rectangle(Math.Min(w, focusRect.Right), focusRect.Top, Math.Max(0, w - focusRect.Right), Math.Max(0, focusRect.Height));
            _focusBottom.Bounds = new Rectangle(0, Math.Min(h, focusRect.Bottom), w, Math.Max(0, h - focusRect.Bottom));

            target.BringToFront();
        }

        private void RemoveFocusEffect()
        {
            RemovePanel(_focusTop);
            RemovePanel(_focusLeft);
            RemovePanel(_focusRight);
            RemovePanel(_focusBottom);
        }

        private static Panel CreateFocusPanel()
            => new Panel
            {
                BackColor = Color.FromArgb(140, 0, 0, 0)
            };

        private static void RemovePanel(Control panel)
        {
            if (panel.Parent != null)
            {
                panel.Parent.Controls.Remove(panel);
            }
        }
    }
}
