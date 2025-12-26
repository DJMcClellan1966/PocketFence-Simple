#nullable disable
using System.Drawing;
using System.Windows.Forms;

namespace PocketFence.UI
{
    public partial class SplashForm : Form
    {
        private Label _titleLabel;
        private Label _versionLabel;
        private Label _statusLabel;
        private ProgressBar _progressBar;
        private PictureBox _iconPictureBox;

        public SplashForm()
        {
            InitializeComponent();
            CenterToScreen();
        }

        private void InitializeComponent()
        {
            // Form setup
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(500, 300);
            this.BackColor = Color.FromArgb(25, 25, 25);
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create border panel for visual effect
            var borderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(0, 120, 215),
                Padding = new Padding(2)
            };

            var innerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 25, 25)
            };

            // Icon/Logo area
            _iconPictureBox = new PictureBox
            {
                Size = new Size(64, 64),
                Location = new Point(50, 50),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            // Set a shield icon (using system icon)
            try
            {
                _iconPictureBox.Image = SystemIcons.Shield.ToBitmap();
            }
            catch
            {
                // If shield icon fails, create a simple colored rectangle
                var bitmap = new Bitmap(64, 64);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.FromArgb(0, 120, 215));
                    g.FillRectangle(Brushes.White, 16, 16, 32, 32);
                }
                _iconPictureBox.Image = bitmap;
            }

            // Title label
            _titleLabel = new Label
            {
                Text = "PocketFence-Simple",
                Location = new Point(130, 60),
                Size = new Size(300, 32),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 18, FontStyle.Bold)
            };

            // Version label
            _versionLabel = new Label
            {
                Text = "Parental Control Hotspot v1.0",
                Location = new Point(130, 95),
                Size = new Size(300, 20),
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10)
            };

            // Status label
            _statusLabel = new Label
            {
                Text = "Initializing...",
                Location = new Point(50, 180),
                Size = new Size(400, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Progress bar
            _progressBar = new ProgressBar
            {
                Location = new Point(50, 210),
                Size = new Size(400, 20),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 50,
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // Copyright label
            var copyrightLabel = new Label
            {
                Text = "© 2025 PocketFence-Simple",
                Location = new Point(50, 250),
                Size = new Size(400, 20),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Add controls to inner panel
            innerPanel.Controls.AddRange(new Control[]
            {
                _iconPictureBox,
                _titleLabel,
                _versionLabel,
                _statusLabel,
                _progressBar,
                copyrightLabel
            });

            // Add inner panel to border panel
            borderPanel.Controls.Add(innerPanel);
            
            // Add border panel to form
            this.Controls.Add(borderPanel);

            // Add fade-in effect
            this.Opacity = 0;
            var fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 50;
            fadeTimer.Tick += (sender, e) =>
            {
                if (this.Opacity < 1)
                {
                    this.Opacity += 0.1;
                }
                else
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                }
            };
            fadeTimer.Start();
        }

        public void UpdateStatus(string status)
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = status;
                Application.DoEvents(); // Force UI update
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Add a subtle shadow effect
            var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0));
            e.Graphics.FillRectangle(shadowBrush, 3, 3, this.Width - 3, this.Height - 3);
            shadowBrush.Dispose();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }
    }
}