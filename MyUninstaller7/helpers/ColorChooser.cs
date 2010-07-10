using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MyUninstaller7 {
    public partial class ColorChooser : Form {
        private ColorChooser() {
            InitializeComponent();
        }
        public static Color? Choose() {
            ColorChooser colorPicker = new ColorChooser();
            colorPicker.ShowDialog();
            return colorPicker.ColorClicked;
        }

        // Given H,S,L in range of 0-1
        // Returns a Color (RGB struct) in range of 0-255
        public static Color HSL2RGB(double h, double sl, double l) {
            double v;
            double r, g, b;

            r = l;   // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0) {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant) {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }
            return Color.FromArgb((int)(r * 255.0f), (int)(g * 255.0f), (int)(b * 255.0f));
        }
        
        private const int nButtons = 20;
        private List<Button> buttons = new List<Button>();
        private void ColorPicker_Load(object sender, EventArgs e) {
            for (int i = 0; i < nButtons; ++i) {
                Button btn = new Button();
                if (i >= 1 && i < nButtons - 1) {
                    double hue = (double)(i - 1) / 9 + 0.01;
                    double light = 0.95 - Math.Floor(hue) * 0.15;
                    hue = hue - Math.Floor(hue);
                    btn.BackColor = HSL2RGB(hue, 1, light);
                }
                btn.Width = 60;
                flowLayoutPanel1.Controls.Add(btn);
                btn.Click += new EventHandler(btn_Click);
                buttons.Add(btn);
                btn.FlatStyle = FlatStyle.Popup;
            }
            Width = buttons[3].Right + Width - ClientRectangle.Width + 5;
            Height = buttons[nButtons - 1].Bottom + Height - ClientRectangle.Height + 5;
            buttons[0].Text = "No color";
            buttons[nButtons-1].Text = "Cancel";
            CancelButton = buttons[nButtons - 1];
            //buttons[nButtons - 1].Select();
        }

        public Color? ColorClicked;
        void btn_Click(object sender, EventArgs e) {
            if (sender.Equals(buttons[0])) ColorClicked = SystemColors.Window;
            else if (sender.Equals(buttons[nButtons - 1])) ColorClicked = null;
            else ColorClicked = ((Button)sender).BackColor;
            Close();
        }
    }
}
