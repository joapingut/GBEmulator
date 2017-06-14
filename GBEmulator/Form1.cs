using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GBEmulator.Core;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GBEmulator
{
    public partial class Form1 : Form, ScreenIF
    {
        Graphics graph;
        byte[] pixeles = new byte[160 * 144 * 4];
        Bitmap mapaDeBits;

        public Form1()
        {
            InitializeComponent();
            graph = panel1.CreateGraphics();
            mapaDeBits = new Bitmap(GPU.width, GPU.height);
            //Program.start = true;
            /*Form2 frof = new Form2();
            Thread thread = new Thread(new ThreadStart(delegate {
                while (Program.cpe == null)
                    continue;
                Core.Debugger deg = new Core.Debugger(Program.cpe.memory, (ScreenIF)frof);
                deg.printBG();
            }));*/
            //thread.Start();
            //frof.Show();
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
            //mapaDeBits.SetPixel(x, y, ccolor);
            pixeles[(((x) + (y * 160))*4)] = ccolor.R;
            pixeles[(((x) + (y * 160))*4) + 1] = ccolor.G;
            pixeles[(((x) + (y * 160))*4) + 2] = ccolor.B;
            pixeles[(((x) + (y * 160))* 4) + 3] = ccolor.A;
            //SolidBrush pincel = new SolidBrush(ccolor);
            //graph.FillRectangle(pincel, x*2, y*2, 1*2, 1*2);
        }

        public void drawPixel(int r, int g, int b, int x, int y, int a = 255) {
            pixeles[(((x) + (y * 160)) * 4)] = (byte)r;
            pixeles[(((x) + (y * 160)) * 4) + 1] = (byte)g;
            pixeles[(((x) + (y * 160)) * 4) + 2] = (byte)b;
            pixeles[(((x) + (y * 160)) * 4) + 3] = (byte)a;
        }

        public void postDraw(int line) {
            /*Bitmap result = new Bitmap(320, 288);
            using (Graphics g = Graphics.FromImage(result)) {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                g.DrawImage(mapaDeBits, new Rectangle(0, 0, 320, 288), new Rectangle(0, 0, GPU.width, GPU.height), GraphicsUnit.Pixel);
            }*/
            //throw new NotImplementedException();
            //graph.DrawImage(result, new Rectangle(0,0, 320, 288));
            if (line >= 143 && !Program.dispose) {
                if (Program.skipFarme > 0)
                {
                    Program.skipFarme -= 1;
                }
                else {
                    graph.DrawImage(CopyDataToBitmap(pixeles), 0, 0, 320, 288);
                }
            }

                /*new Bitmap(320, 288, PixelFormat.Format32bppArgb);
                graph.DrawImage(mapaDeBits, 0,0,320,288);*/
        }

        private Bitmap CopyDataToBitmap(byte[] data) {
            //Here create the Bitmap to the know height, width and format
            Bitmap bmp = new Bitmap(160, 144, PixelFormat.Format32bppArgb);

            //Create a BitmapData and Lock all pixels to be written 
            BitmapData bmpData = bmp.LockBits(
                                 new Rectangle(0, 0, bmp.Width, bmp.Height),
                                 ImageLockMode.WriteOnly, bmp.PixelFormat);

            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);


            //Unlock the pixels
            bmp.UnlockBits(bmpData);


            //Return the bitmap 
            return bmp;
        }

        public void preDraw(int line) {
            //throw new NotImplementedException();
        }

        public void refreshGBScreen() {
            graph.FillRectangle(new SolidBrush(Color.White), 0,0, panel1.Width, panel1.Height);
            //panel1.Refresh();
            //throw new NotImplementedException();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.A:
                    Pad.changekey(pad_NAMES.A, true);
                    break;
                case Keys.B:
                    Pad.changekey(pad_NAMES.B, true);
                    break;
                case Keys.Space:
                    Pad.changekey(pad_NAMES.START, true);
                    break;
                case Keys.Z:
                    Pad.changekey(pad_NAMES.SELECT, true);
                    break;
                case Keys.Up:
                    Pad.changekey(pad_NAMES.UP, true);
                    break;
                case Keys.Left:
                    Pad.changekey(pad_NAMES.LEFT, true);
                    break;
                case Keys.Right:
                    Pad.changekey(pad_NAMES.RIGHT, true);
                    break;
                case Keys.Down:
                    Pad.changekey(pad_NAMES.DOWN, true);
                    break;
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.A:
                    Pad.changekey(pad_NAMES.A, false);
                    break;
                case Keys.B:
                    Pad.changekey(pad_NAMES.B, false);
                    break;
                case Keys.Space:
                    Pad.changekey(pad_NAMES.START, false);
                    break;
                case Keys.Z:
                    Pad.changekey(pad_NAMES.SELECT, false);
                    break;
                case Keys.Up:
                    Pad.changekey(pad_NAMES.UP, false);
                    break;
                case Keys.Left:
                    Pad.changekey(pad_NAMES.LEFT, false);
                    break;
                case Keys.Right:
                    Pad.changekey(pad_NAMES.RIGHT, false);
                    break;
                case Keys.Down:
                    Pad.changekey(pad_NAMES.DOWN, false);
                    break;
            }
        }

        private void cargarToolStripMenuItem_Click(object sender, EventArgs e) {
            Program.start = false;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            //openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "GB roms (*.gb)|*.gb|GBC roms (*.gbc)|*.gbc|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                Program.start = false;
                Program.cpe.restart(openFileDialog1.FileName);
                Program.start = true;
            } else {
                Program.start = true;
            }
        }
    }
}
