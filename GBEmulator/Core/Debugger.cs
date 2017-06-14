using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {
    class Debugger {

        Memory memory;
        ScreenIF screen;
        ScreenIF screen2;

        public static Boolean mode = true;
        public static Boolean lowerTiles = true;
        public static Boolean mapBase = true;
        public static Boolean refresh = true;

        public Debugger(Memory men, ScreenIF scre) {
            memory = men;
            screen = scre;
        }

        public void printBG() {
            DateTime inicio = DateTime.Now;
            long desired = 16;
            while (true) {
                if (refresh) {
                    screen.refreshGBScreen();
                    refresh = false;
                }
                if (mode) {
                    /*for (int y = 0; y < 256; y++) {
                        updateBG(y);
                        //updateSprite(y);
                    }*/
                    newUpdateBG();
                    desired = 24;
                } else {
                    for (int i = 0; i < 384; i++){
                        for (int y = 0; y < 8; y++) {
                            byte line0 = memory.readByte((ushort)(0x8000 + (i * 0x10) + (y * 2)));
                            byte line1 = memory.readByte((ushort)(0x8000 + (i * 0x10) + (y * 2) + 1));
                            for (int x = 0; x < 8; x++) {
                                screen.drawPixel((((line1 >> (7-x)) & 0x1) << 1) | ((line0 >> (7-x)) & 0x1), (((8*i)%128) + x), (((i*8)/128)*8 + y));
                            }
                        }
                    }
                    desired = 1024;
                }
                DateTime ahora = DateTime.Now;
                long elapse = (ahora - inicio).Milliseconds;
                if (elapse < desired) {
                    System.Threading.Thread.Sleep((int)(desired - elapse));
                    inicio = ahora;
                }
            }
        }

        public void newUpdateBG() {
            ushort baseAddress;
            if (mapBase)
                baseAddress = 0x9800;
            else
                baseAddress = 0x9C00;
            bool lower = lowerTiles;
            int[] paleta = getDMGPalette(Memory.BGP);
            for (int y = 0; y < 256; y++) {
                for (int x = 0; x < 256; x++) {
                    int color = newGetColor(baseAddress, lower, x, y);
                    if (screen != null) {
                        screen.drawPixel(paleta[color], x, y);
                    }
                }
            }
        }

        public int[] getDMGPalette(ushort address) {
            int[] palette = new int[4];
            byte paletteData = memory.readByte(address);
            palette[0] = Math.Abs((paletteData & 0x03) - 3);
            palette[1] = Math.Abs(((paletteData & 0x0C) >> 2) - 3);
            palette[2] = Math.Abs(((paletteData & 0x30) >> 4) - 3);
            palette[3] = Math.Abs(((paletteData & 0xC0) >> 6) - 3);
            return palette;
        }

        private int newGetColor(int baseAdress, bool lower,int x, int y) {
            byte idTile = (byte)(baseAdress + memory.readByte((ushort)(baseAdress + ((x/8) + ((y/8)*32)))));
            ushort addressTile;
            if (!lower) {
                addressTile = 0x9000;
                addressTile += (ushort)((sbyte)idTile * 16);
            } else {
                addressTile = 0x8000;
                addressTile += (ushort)(idTile * 16);
            }
            int xTile = x % 8;
            int yTile = y % 8;
            byte line0 = memory.readByte((ushort)(addressTile + (yTile * 2)));
            addressTile += 1;
            byte line1 = memory.readByte((ushort)(addressTile + (yTile * 2)));
            return (((line1 >> (7 - xTile)) & 0x1) << 1) | ((line0 >> (7 - xTile)) & 0x1);
        }
    }
}
