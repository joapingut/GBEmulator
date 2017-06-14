using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace GBEmulator{

    class GameScreen : OpenTK.GameWindow, Core.ScreenIF
    {

        Color[] pixeles = new Color[160 * 144];

        public GameScreen() : base(160, 144, OpenTK.Graphics.GraphicsMode.Default, "TEST") {
            
        }

        public void clear(){
            refreshGBScreen();
        }

        public void drawPixel(int color, int x, int y){
            Color ccolor;
            switch (color)
            {
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
            pixeles[(((x) + (y * 160)))] = ccolor;
        }

        public void drawPixel(int r, int g, int b, int x, int y, int a = 255){
            Color ccolor = Color.FromArgb(a, b, g, r);
            pixeles[(((x) + (y * 160)))] = ccolor;
        }

        public void postDraw(int line){
            if (line >= 143 && !Program.dispose)
            {
                if (Program.skipFarme > 0)
                {
                    Program.skipFarme -= 1;
                }
                else {
                   
                }
            }
        }

        public void preDraw(int y)
        {
            //throw new NotImplementedException();
        }

        public void refreshGBScreen(){
            GL.ClearColor(Color.White);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        protected override void OnRenderFrame(FrameEventArgs e){
            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, 160, 144, 0, -1, 1);

            for (int x = 0; x < 160; x += 1) {
                for (int y = 0; y < 144; y += 1) {
                    Color c = pixeles[((x) + (y * 160))];
                    GL.Color4(c);
                    GL.Rect(new RectangleF(x, y, 1, 1));
                }
            }
            /*GL.Begin(PrimitiveType.Triangles);

            GL.Color3(Color.Red); GL.Vertex2(-1.0f, -1.0f);
            GL.Color3(Color.Pink); GL.Vertex2(1.0f, -1.0f);
            GL.Color3(Color.Blue); GL.Vertex2(1.0f, 1.0f);

            GL.Color3(Color.Red); GL.Vertex2(-1.0f, -1.0f);
            GL.Color3(Color.Pink); GL.Vertex2(-1.0f, 1.0f);
            GL.Color3(Color.Blue); GL.Vertex2(1.0f, 1.0f);

            GL.End();*/
            this.SwapBuffers();
        }

        protected override void OnResize(EventArgs e){
            base.OnResize(e);
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }
    }
}
