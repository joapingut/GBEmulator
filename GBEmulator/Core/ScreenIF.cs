using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {

    interface ScreenIF {

        void drawPixel(int color, int x, int y);
        void drawPixel(int r, int g, int b, int x, int y, int a = 255);
        void refreshGBScreen();
        void clear();

        void preDraw(int y);
        void postDraw(int y);
    }
}
