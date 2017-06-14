using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {
    class Screen {
        /*

        private Bitmap mapaDeBits;
        private List<Bitmap> buffer;

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
            mapaDeBits.SetPixel(x, y, ccolor);
            //SolidBrush pincel = new SolidBrush(ccolor);
            //graph.FillRectangle(pincel, x*2, y*2, 1*2, 1*2);
        }

        public void drawPixel(int r, int g, int b, int x, int y) {
            Graphics graph = panel1.CreateGraphics();
            SolidBrush pincel = new SolidBrush(Color.FromArgb(255, r, g, b));
            graph.FillRectangle(pincel, x * 2, y * 2, 1 * 2, 1 * 2);
        }

        public void postDraw(int i) {
            /*Bitmap result = new Bitmap(320, 288);
            using (Graphics g = Graphics.FromImage(result)) {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                g.DrawImage(mapaDeBits, new Rectangle(0, 0, 320, 288), new Rectangle(0, 0, GPU.width, GPU.height), GraphicsUnit.Pixel);
            }
            //throw new NotImplementedException();
            //graph.DrawImage(result, new Rectangle(0,0, 320, 288));
            graph.DrawImage(mapaDeBits, 0, 0, 320, 288);
        }
    /*
        public void preDraw(int y) {
            //throw new NotImplementedException();
        }

        public void refreshGBScreen() {
            graph.FillRectangle(new SolidBrush(Color.White), 0, 0, panel1.Width, panel1.Height);
            //panel1.Refresh();
            //throw new NotImplementedException();
        }*/
    }
}
