using GBEmulator.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GBEmulator {
    public partial class Form2 : Form, ScreenIF {

        Graphics graph;

        public Form2() {
            InitializeComponent();
            label1.Text = "BG";
            graph = panel1.CreateGraphics();
        }

        public void clear() {
            panel1.Invalidate();
            graph.FillRectangle(new SolidBrush(Color.White), 0, 0, panel1.Width, panel1.Height);
            panel1.Refresh();
        }

        public void drawPixel(int color, int x, int y) {
            //Console.WriteLine("PRINT1 - color " + color );
            //return;
            Color ccolor;
            switch (color) {
                case 0:
                    ccolor = Color.Black;
                    break;
                case 1:
                    ccolor = Color.FromArgb(255, 170, 170, 170);
                    break;
                case 2:
                    ccolor = Color.FromArgb(255, 85, 85, 85);
                    break;
                case 3:
                    ccolor = Color.White;
                    break;
                default:
                    ccolor = Color.White;
                    break;
            }
            SolidBrush pincel = new SolidBrush(ccolor);
            graph.FillRectangle(pincel, x*2, y*2, 1*2, 1*2);
        }

        public void drawPixel(int r, int g, int b, int x, int y, int a = 255) {
            Graphics graph = panel1.CreateGraphics();
            SolidBrush pincel = new SolidBrush(Color.FromArgb(a, r, g, b));
            graph.FillRectangle(pincel, x * 2, y * 2, 1 * 2, 1 * 2);
        }

        public void postDraw(int line) {
            //throw new NotImplementedException();
        }

        public void preDraw(int line) {
            //throw new NotImplementedException();
        }

        public void refreshGBScreen() {
            graph.FillRectangle(new SolidBrush(Color.White), 0, 0, panel1.Width, panel1.Height);
            //panel1.Refresh();
            //throw new NotImplementedException();
        }

        private void button1_Click(object sender, EventArgs e) {
            if (Debugger.mode) {
                Debugger.mode = false;
                Debugger.refresh = true;
                label1.Text = "Sprites";
            } else {
                Debugger.mode = true;
                Debugger.refresh = true;
                label1.Text = "BG";
            }
        }

        private void button3_Click(object sender, EventArgs e) {
            if (Debugger.mapBase) {
                Debugger.mapBase = false;
                Debugger.refresh = true;
                label3.Text = "0x9C00";
            } else {
                Debugger.mapBase = true;
                Debugger.refresh = true;
                label3.Text = "0x9800";
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            if (Debugger.lowerTiles) {
                Debugger.lowerTiles = false;
                Debugger.refresh = true;
                label2.Text = "0x8800";
            } else {
                Debugger.lowerTiles = true;
                Debugger.refresh = true;
                label2.Text = "0x8000";
            }
        }
    }
}
