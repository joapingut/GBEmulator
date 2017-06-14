using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {

    class GPU {

        public const int width = 160;
        public const int height = 144;
        public const int LCD_MODE_0 = 204; //204 //201-207
        public const int LCD_MODE_1 = 456; //456 //456 * 10 = 4560
        public const int LCD_MODE_2 = 80; //80  //77-83
        public const int LCD_MODE_3 = 172;

        private int velovityMulti = 1;

        public void changeDoubleSpeed(bool doubleSpeed) {
            if (doubleSpeed)
                velovityMulti = 2;
            else
                velovityMulti = 1;
        }

        public Memory memory;
        public Registers registers;
        public CPU cpu;

        public ScreenIF screen;
        public List<int> oamList;
        public bool[,] priorityBGWind;

        public int clock_lcd;

        public byte mode = 0;

        public GPU(CPU cp, Memory men, Registers reg, ScreenIF screen) {
            this.memory = men;
            this.cpu = cp;
            this.registers = reg;
            this.memory.gpu = this;
            this.screen = screen;
            clock_lcd = 0;
            oamList = new List<int>();
            priorityBGWind = new bool[width, height];
        }

        public void updateStateLCDOn() {
            byte mode = memory.readByte(Memory.STAT);
            mode = (byte)(mode & 0x03);
            //Console.WriteLine("LCD - mode {0:X} : {1}", mode, clock_lcd);
            switch (mode) {
                case 0: //Modo 0, es decir, ya se ha terminado el H-blank
                    if (clock_lcd >= LCD_MODE_0 * velovityMulti) {
                        byte ly = memory.readByte(Memory.LY);
                        ly += 1;
                        //Console.WriteLine("LCD - mode 0 ly: {0:X}", ly);
                        memory.writeByte(Memory.LY, ly, false);
                        checkLYC();
                        clock_lcd -= LCD_MODE_0 * velovityMulti;
                        if (ly >= height) { //Si hemos llegado a la linea 144 (la ultima de la gameboy) pasamos al modo 1 (V-blank)
                            byte stat = memory.readByte(Memory.STAT);
                            memory.writeByte(Memory.STAT, (byte)((stat & ~0x03) | 0x01), false);
                            cpu.vBlankPending = true;
                            cpu.onEndFrame();
                        } else { //En caso contrario, pasamos al modo 2
                            byte stat = memory.readByte(Memory.STAT);
                            memory.writeByte(Memory.STAT, (byte)((stat & ~0x03) | 0x02), false);
                            //Si la interrupcion OAM esta actia, hay que poner a 1 el bit de interrupcion en STAT
                            stat = memory.readByte(Memory.STAT);
                            if ((stat & 0x20) != 0) {
                                cpu.setInterruptionFlag(1);
                            }
                        }
                    }
                    break;
                case 1://Modo 1, estamos en V-Blank
                    /** Aqui hay que meter un retraso porque algunos juegos esperan 24 ciclos extras antes de esta condicion*/
                    if (cpu.vBlankPending && (clock_lcd >= 24)) {
                        //interrupcion V-blank
                        cpu.setInterruptionFlag(0);
                        byte stat = memory.readByte(Memory.STAT);
                        stat = (byte)(stat & 0x10);
                        //Si la interrupcion V-blank esta activa, debemos poner a 1 el flag de interrupcion LCD en STAT
                        if (stat != 0) {
                            cpu.setInterruptionFlag(1);
                        }
                        cpu.vBlankPending = false;
                    }

                    if (clock_lcd >= LCD_MODE_1 * velovityMulti) {
                        clock_lcd -= LCD_MODE_1 * velovityMulti;
                        byte ly = memory.readByte(Memory.LY);
                        if (ly == 152) {
                            ly += 1;
                            memory.writeByte(Memory.LY, ly, false);
                            clock_lcd += (LCD_MODE_1 - 4) * velovityMulti;
                            checkLYC();
                        } else if (ly == 153) {
                            ly = 0;
                            memory.writeByte(Memory.LY, ly, false);
                            clock_lcd += 4;
                            checkLYC();
                        } else if (ly == 0) {
                            ly = 0;
                            memory.writeByte(Memory.LY, ly, false);
                            //Pasamos al modo 2
                            byte stat = memory.readByte(Memory.STAT);
                            stat = (byte)((stat & ~0x03) | 0x02);
                            memory.writeByte(Memory.STAT, stat, false);
                            //stat = memory.readByte(Memory.STAT);
                            stat = (byte)(stat & 0x20);
                            if (stat != 0) {
                                cpu.setInterruptionFlag(1);
                            }
                        } else {
                            ly += 1;
                            memory.writeByte(Memory.LY, ly, false);
                            checkLYC();
                        }
                    }
                    break;
                case 2:
                    //Cuando se da esta condicion, la GPU esta usando la memoria OAM
                    if (clock_lcd >= LCD_MODE_2 * velovityMulti) {
                        clock_lcd -= LCD_MODE_2 * velovityMulti;
                        byte stat = memory.readByte(Memory.STAT);
                        stat = (byte)((stat & ~0x03) | 0x03);
                        //cambiamos al modo 3
                        memory.writeByte(Memory.STAT, stat, false);
                    }
                    break;
                case 3:// Modo usado para pasar la info al LCD
                    if (clock_lcd >= LCD_MODE_3 * velovityMulti) {
                        clock_lcd -= LCD_MODE_3 * velovityMulti;
                        //pasar al modo 0
                        byte stat = memory.readByte(Memory.STAT);
                        stat = (byte)(stat & ~0x03);
                        memory.writeByte(Memory.STAT, stat, false);
                        stat = memory.readByte(Memory.STAT);
                        stat = (byte)(stat & 0x08);
                        if (stat != 0) {
                            cpu.setInterruptionFlag(1);
                        }
                        if (cpu.gbcMode)
                            memory.updateHDMA();
                        updateLine(memory.readByte(Memory.LY));
                    }
                    break;
            }
        }

        private void updateLine(byte line) {
            //throw new NotImplementedException();
            if (screen != null)
                screen.preDraw(line);

            if (cpu.gbcMode) {
                orderOAM(line);
                newUpdateBGColor(line);
                newUpdateWinColor(line);
                newUpdateOAMColor(line);
            } else {
                orderOAM(line);
                //updateBG(line);
                newUpdateBG(line);
                //updateWin(line);
                newUpdateWin(line);
                //updateOAM(line);
                newUpdateOAM(line);
            }

            if (screen != null && line >= 143)
                screen.postDraw(line);

        }

        private void newUpdateOAMColor(int y) {
            if ((memory.readByte(Memory.LCDC) & 0x02) == 0)
                return;
            bool mode16 = (memory.readByte(Memory.LCDC) & 0x04) != 0;
            oamList.Reverse();
            foreach (int addressSpriteOAM in this.oamList) {
                int ySprite = memory.readByte((ushort)addressSpriteOAM) - 16;
                int xSprite = memory.readByte((ushort)(addressSpriteOAM + 1)) - 8;
                if ((y) < 0)
                    continue;
                int tileId = memory.readByte((ushort)(addressSpriteOAM + 2));
                if (mode16)
                    tileId = tileId & 0xFE;
                int attrSprite = memory.readByte((ushort)(addressSpriteOAM + 3));
                bool xFlip = (attrSprite & 0x20) != 0;
                bool yFlip = (attrSprite & 0x40) != 0;
                int spritePriority = (attrSprite & 0x80);
                for (int x = xSprite; (x < xSprite + 8) && (x < width); x++) {
                    if (x < 0)
                        continue;
                    bool paintSprite = objAboveBG(spritePriority, x, y);
                    Pixel color = newGetColorSprite(tileId, mode16, Math.Abs(x - xSprite), Math.Abs((y - ySprite)), attrSprite & 0x08, attrSprite & 0x07, xFlip, yFlip);
                    if (paintSprite && color.a != 0 && screen != null) {
                        screen.drawPixel(color.r, color.g, color.b, x, y, color.a);
                    }
                }
            }
        }

        private void newUpdateOAM(int y) {
            if ((memory.readByte(Memory.LCDC) & 0x02) == 0)
                return;
            bool mode16 = (memory.readByte(Memory.LCDC) & 0x04) != 0;
            int[] pallete0 = getDMGPalette(Memory.OBP0);
            int[] pallete1 = getDMGPalette(Memory.OBP1);
            oamList.Reverse();
            foreach (int addressSpriteOAM in this.oamList) {
                int ySprite = memory.readByte((ushort)addressSpriteOAM) - 16;
                int xSprite = memory.readByte((ushort)(addressSpriteOAM + 1)) - 8;
                if ((y) < 0)
                    continue;
                int tileId = memory.readByte((ushort)(addressSpriteOAM + 2));
                if (mode16)
                    tileId = tileId & 0xFE;
                int attrSprite = memory.readByte((ushort)(addressSpriteOAM + 3));
                bool xFlip = (attrSprite & 0x20) != 0;
                bool yFlip = (attrSprite & 0x40) != 0;
                int spritePriority = (attrSprite & 0x80);
                for (int x = xSprite; (x < xSprite + 8) && (x < width); x++) {
                    if(x <0)
                        continue;
                    int indec = newGetGrayScaleSprite(tileId, mode16, Math.Abs(x - xSprite), Math.Abs((y - ySprite)), xFlip, yFlip);
                    int color = ((attrSprite & 0x10) != 0) ? pallete1[indec] : pallete0[indec];
                    //int transparente = ((attrSprite & 0x10) != 0) ? pallete1[0] : pallete0[0];
                    //color = Math.Abs(3- color);
                    bool paintSprite = objAboveBG(spritePriority, x, y);

                    //bool paintSprite = true;
                    if (indec != 0 && paintSprite) {
                        screen.drawPixel(color, x, y);
                    }
                }
            }
       }

        private Pixel newGetColorSprite(int idSprite, bool mode16, int x, int y, int bank, int paletteIndex, bool flipY = false, bool flipX = false) {
            if (bank != 0) {
                bank = 1;
            }
            ushort addressTile = (ushort)(0x8000 + (idSprite * 0x10));
            int xTile = (x % 8);//Math.Abs((x % 8) - 7);
            int yTile;
            if (mode16) {
                if (flipX)
                    yTile = Math.Abs(15 - (y % 16));
                else
                    yTile = y % 16;
            } else {
                if (flipX)
                    yTile = Math.Abs(7 - (y % 8));
                else
                    yTile = y % 8;
            }
            byte line0 = memory.readVideoRamBank((ushort)(addressTile + (yTile * 2)), bank);
            addressTile += 1;
            byte line1 = memory.readVideoRamBank((ushort)(addressTile + (yTile * 2)), bank);
            int index;
            //FlipX
            if (flipY)
                index = (((line1 >> (xTile)) & 0x1) << 1) | ((line0 >> (xTile)) & 0x1);
            else
                index = (((line1 >> (7 - xTile)) & 0x1) << 1) | ((line0 >> (7 - xTile)) & 0x1);
            byte[][] pallete = getColorPalette(paletteIndex * 8, true);
            Pixel pix = new Pixel();
            pix.r = pallete[index][2];
            pix.g = pallete[index][1];
            pix.b = pallete[index][0];
            if(index == 0)
                pix.a = 0;
            else
                pix.a = 255;
            return pix;
        }

        private int newGetGrayScaleSprite(int idSprite, bool mode16, int x, int y, bool flipY = false, bool flipX = false) {
            ushort addressTile = (ushort)(0x8000 +(idSprite * 0x10));
            int xTile = (x % 8);//Math.Abs((x % 8) - 7);
            int yTile;
            if (mode16) {
                if (flipX)
                    yTile = Math.Abs(15 - (y % 16));
                else
                    yTile = y % 16;
            } else {
                if (flipX)
                    yTile = Math.Abs(7 - (y % 8));
                else
                    yTile = y % 8;
            }
            byte line0 = memory.readByte((ushort)(addressTile + (yTile * 2)));
            addressTile += 1;
            byte line1 = memory.readByte((ushort)(addressTile + (yTile * 2)));
            if (flipY)
                return (((line1 >> (xTile)) & 0x1) << 1) | ((line0 >> (xTile)) & 0x1);
            return (((line1 >> (7 - xTile)) & 0x1) << 1) | ((line0 >> (7 - xTile)) & 0x1);
        }

        private bool objAboveBG(int spritePriority, int x, int y) {
            if (cpu.gbcMode) {
                bool paintSprite = ((memory.readByte(Memory.LCDC) & 0x01) == 0);
                if (!paintSprite) {
                    if (priorityBGWind[x, y])
                        paintSprite = false;
                    else
                        paintSprite = spritePriority != 0 ? false : true;
                }
                return paintSprite;
            } else {
                if (x < width && y < height)
                    return spritePriority == 0 || !priorityBGWind[x, y];
                else
                    return true;
            }
        }

        private void newUpdateWin(int y) {
            byte lcdc = memory.readByte(Memory.LCDC);
            if ((lcdc & 0x20) == 0)
                return;
            if((lcdc & 0x01) == 0)
                return;
            int wndPosX = memory.readByte(Memory.WX)-7;
            int wndPosY = memory.readByte(Memory.WY);

            if (y < wndPosY)
                return;
            int xIni;
            if (wndPosX < 0)
                xIni = 0;
            else if (wndPosX > width)
                return;
            //xIni = width;
            else
                xIni = wndPosX;
            int baseAddress = (lcdc & 0x40) != 0 ? 0x9C00 : 0x9800;
            bool lower = (lcdc & 0x10) != 0;
            int[] paleta = getDMGPalette(Memory.BGP);
            int scx = wndPosX;
            int yReal = (y + wndPosY);
            if (yReal < 0)
                yReal += 256;
            else if (yReal > 255)
                yReal -= 256;
            for (int x = xIni; x < width; x++) {
                int xReal = x + scx;
                if (xReal > 255)
                    xReal -= 256;
                int color = newGetGreyScale(baseAddress, lower, xReal, yReal);
                if (color > 0)
                    priorityBGWind[x, y] = true;
                else
                    priorityBGWind[x, y] = false;
                if (screen != null) {
                    screen.drawPixel(paleta[color], x, y);
                }
            }
        }

        private void newUpdateWinColor(int y) {
            byte lcdc = memory.readByte(Memory.LCDC);
            if ((lcdc & 0x20) == 0)
                return;
            if ((lcdc & 0x01) == 0)
                return;
            int wndPosX = memory.readByte(Memory.WX) - 7;
            int wndPosY = memory.readByte(Memory.WY);

            if (y < wndPosY)
                return;
            int xIni;
            if (wndPosX < 0)
                xIni = 0;
            else if (wndPosX > width)
                return;
            //xIni = width;
            else
                xIni = wndPosX;
            int baseAddress = (lcdc & 0x40) != 0 ? 0x9C00 : 0x9800;
            bool lower = (lcdc & 0x10) != 0;
            int scx = wndPosX;
            int yReal = (y + wndPosY);
            if (yReal < 0)
                yReal += 256;
            else if (yReal > 255)
                yReal -= 256;
            for (int x = xIni; x < width; x++) {
                int xReal = x + scx;
                if (xReal > 255)
                    xReal -= 256;
                Pixel color = newGetColor(baseAddress, lower, xReal, yReal, x, y);
                if (screen != null) {
                    screen.drawPixel(color.r, color.g, color.b, x, y);
                }
            }
        }

        private void newUpdateBGColor(int y) {
            byte lcdc = memory.readByte(Memory.LCDC);
            bool display = true;
            if (!cpu.gbcMode && (lcdc & 0x01) == 0) {
                display = false;
            }
            //Pantalla en negro porque el LCD esta apagado
            if (!display && screen != null) {
                for (int x = 0; x < width; x++) {
                    screen.drawPixel(0, 0, 0, x, y);
                    priorityBGWind[x, y] = false;
                }
                return;
            }
            int baseAddress = (lcdc & 0x08) != 0 ? 0x9C00 : 0x9800;
            bool lower = (lcdc & 0x10) != 0;
            int scx = memory.readByte(Memory.SCX);
            int yReal = (y + memory.readByte(Memory.SCY));
            if (yReal < 0)
                yReal += 256;
            else if (yReal > 255)
                yReal -= 256;
            for (int x = 0; x < width; x++) {
                int xReal = x + scx;
                if (xReal > 255)
                    xReal -= 256;
                Pixel color = newGetColor(baseAddress, lower, xReal, yReal, x, y);
                if (screen != null) {
                    screen.drawPixel(color.r, color.g, color.b, x, y);
                }
            }
        }

        private void newUpdateBG(int y) {
            byte lcdc = memory.readByte(Memory.LCDC);
            //byte scx = memory.readByte(Memory.SCX);
           // int lcdon = lcdc & 0x80;
            //int lcd0 = lcdc & 0x01;
            bool display = true;
            if (!cpu.gbcMode && (lcdc & 0x01) == 0) {
                display = false;
            }
            //Pantalla en negro porque el LCD esta apagado
            if (!display && screen != null) {
                for (int x = 0; x < width; x++) {
                    screen.drawPixel(3, x, y);
                    priorityBGWind[x, y] = false;
                }
                return;
            }
            int baseAddress = (lcdc & 0x08) != 0 ? 0x9C00 : 0x9800;
            bool lower = (lcdc & 0x10) != 0;
            int[] paleta = getDMGPalette(Memory.BGP);
            int scx = memory.readByte(Memory.SCX);
            int yReal = (y + memory.readByte(Memory.SCY));
            if (yReal < 0)
                yReal += 256;
            else if (yReal > 255)
                yReal -= 256;
            for (int x = 0; x < width; x++) {
                int xReal = x + scx;
                if (xReal > 255)
                    xReal -= 256;
                int color = newGetGreyScale(baseAddress, lower, xReal, yReal);
                if (color > 0)
                    priorityBGWind[x, y] = true;
                else
                    priorityBGWind[x, y] = false;
                if (screen != null) {
                    screen.drawPixel(paleta[color], x, y);
                }
            }
        }

        private int newGetGreyScale(int baseAdress, bool lower, int x, int y) {
            byte idTile = (byte)(baseAdress + memory.readByte((ushort)(baseAdress + ((x / 8) + ((y / 8) * (32))))));
            ushort addressTile;
            if (!lower) {
                addressTile = 0x9000;
                addressTile += (ushort)((sbyte)idTile * 16);
            } else {
                addressTile = 0x8000;
                addressTile += (ushort)(idTile * 16);
            }
            int xTile = (x % 8);//Math.Abs((x % 8) - 7);
            int yTile = y % 8;
            byte line0 = memory.readByte((ushort)(addressTile + (yTile * 2)));
            addressTile += 1;
            byte line1 = memory.readByte((ushort)(addressTile + (yTile * 2)));
            return (((line1 >> (7 - xTile)) & 0x1) << 1) | ((line0 >> (7 - xTile)) & 0x1);
        }

        class Pixel {
            public int r;
            public int g;
            public int b;
            public int a;
        }

        private Pixel newGetColor(int baseAdress, bool lower, int x, int y, int dotX, int dotY) {
            byte idTile = (byte)(baseAdress + memory.readVideoRamBank((ushort)(baseAdress + ((x / 8) + ((y / 8) * (32)))), 0));
            byte atributtes = (byte)(baseAdress + memory.readVideoRamBank((ushort)(baseAdress + ((x / 8) + ((y / 8) * (32)))), 1));
            ushort addressTile;
            if (!lower) {
                addressTile = 0x9000;
                addressTile += (ushort)((sbyte)idTile * 16);
            } else {
                addressTile = 0x8000;
                addressTile += (ushort)(idTile * 16);
            }
            int tileBank = (atributtes & 0x08) != 0 ? 1: 0;
            int paletteIndex = (atributtes & 0x07);
            int xTile = (x % 8);//Math.Abs((x % 8) - 7);
            int yTile;
            //FlipY
            if ((atributtes & 0x40) != 0)
                yTile = Math.Abs(7 - y % 8);
            else
                yTile = y % 8;
            byte line0 = memory.readVideoRamBank((ushort)(addressTile + (yTile * 2)), tileBank);
            addressTile += 1;
            byte line1 = memory.readVideoRamBank((ushort)(addressTile + (yTile * 2)), tileBank);
            int index;
            //FlipX
            if ((atributtes & 0x20) != 0)
                index = (((line1 >> (xTile)) & 0x1) << 1) | ((line0 >> (xTile)) & 0x1);
            else
                index = (((line1 >> (7 - xTile)) & 0x1) << 1) | ((line0 >> (7 - xTile)) & 0x1);
            if ((atributtes & 0x80) != 0 && index > 0)
                priorityBGWind[dotX, dotY] = true;
            else
                priorityBGWind[dotX, dotY] = false;
            byte[][] pallete = getColorPalette(paletteIndex*8, false);
            Pixel pix = new Pixel();
            pix.r = pallete[index][2];
            pix.g = pallete[index][1];
            pix.b = pallete[index][0];
            if (index == 0) {
                pix.a = 0;
            }else
                pix.a = 255;
            return pix;
        }
    
        private byte[][] getColorPalette(int paletteIndex, bool sprites) {
            int address = paletteIndex;
            byte[][] result = new byte[4][];
            for (int i = 0; i < 4; i++) {
                result[i] = new byte[3];
                byte data1;
                byte data2;
                if (sprites) {
                    data1 = memory.men_obgpalette[address];
                    data2 = memory.men_obgpalette[address + 1];
                } else {
                    data1 = memory.men_bgpalette[address];
                    data2 = memory.men_bgpalette[address + 1];
                }

                result[i][0] = (byte)(data1 & 0x1F);
                result[i][1] = (byte)(((data2 & 0x03) << 3) | ((data1 & 0xE0) >> 5));
                result[i][2] = (byte)((data2 & 0x7C) >> 2);

                // Como el valor va de 0 a 31 (1F), hay que convertirlo de 0 a 255
                // para que sea mas eficiente lo hare de 0 a 248
                result[i][0] <<= 3;
                result[i][1] <<= 3;
                result[i][2] <<= 3;

                address += 2;
            }
            return result;
        }

        private void orderOAM(byte line) {
            int ySprite, hSprite, numSprite;

            oamList.Clear();

            //OAM desactivated
            if ((memory.readByte(Memory.LCDC) & 0x02) == 0)
                return;
            hSprite = (memory.readByte(Memory.LCDC) & 0x04) != 0  ? 16 : 8;
            for (ushort address = 0xFE00; address <= 0xFE9C; address += 0x04) {
                ySprite = memory.readByte(address) - 16;
                //ySprite -= 16;
                if ((line < ySprite + hSprite) && (line >= ySprite)) {
                    if (cpu.gbcMode)
                        oamList.Add(address);
                    else {
                        //aux += 1;
                        //aux = memory.readByte(aux);
                        oamList.Add(address);
                    }
                }
            }
            /*
            if (oamList.Count > 10) {
                oamList.RemoveRange(10, oamList.Count - 10);
            }*/
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

        public void checkLYC() {
            byte ly = memory.readByte(Memory.LY);
            byte lyc = memory.readByte(Memory.LYC);
            byte stat = memory.readByte(Memory.STAT);
            if (ly == lyc) {
                stat = (byte)(stat | 0x04);
                memory.writeByte(Memory.STAT, stat, false);
                if ((stat & 0x40) != 0) {
                    cpu.setInterruptionFlag(1);
                }
            } else {
                stat = (byte)(stat & ~0x04);
                memory.writeByte(Memory.STAT, stat, false);
            }
        }
    }
}
