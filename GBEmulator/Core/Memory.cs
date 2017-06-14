using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {
    class Memory {

        //Lista con las direcciones de memoria de registros especiales de I/O
        public const ushort P1 = 0xFF00; // Registro con la informacion de la cruceta. Ver docu para saber que bit es cada direccion
        public const ushort SB = 0xFF01; // Registro que contiene el valor leido/escrito en el puerto serie (cable link)
        public const ushort SC = 0xFF02; // Registro de control de la comunicacion Serie, leer docu para saber que hace cada bit
        public const ushort DIV = 0xFF04; // Registro con el divisor del reloj, se incrementa 16384 veces por segundo (teoricamente)
        public const ushort TIMA = 0xFF05; // Timer counter, el contador del reloj, se incrementa segun la frecuencia especificada en el TAC
        public const ushort TMA = 0xFF06; // Registro que contien el valor que se cargara en TIMA cuando desborde
        public const ushort TAC = 0xFF07; // Timer control , especifica si el reloj esta activo y a que frecuencia funciona (mirar docu). Solo se usan los 3 primeros bits
        public const ushort IF = 0xFF0F; // Flags de interrupciones. Indica que interrupciones se han activado.
        public const ushort NR10 = 0xFF10; /*-- NR, registros de sonido, mirar docu para mas info --*/
        public const ushort NR11 = 0xFF11;
        public const ushort NR12 = 0xFF12;
        public const ushort NR13 = 0xFF13;
        public const ushort NR14 = 0xFF14;
        public const ushort NR21 = 0xFF16;
        public const ushort NR22 = 0xFF17;
        public const ushort NR23 = 0xFF18;
        public const ushort NR24 = 0xFF19;
        public const ushort NR30 = 0xFF1A;
        public const ushort NR31 = 0xFF1B;
        public const ushort NR32 = 0xFF1C;
        public const ushort NR33 = 0xFF1D;
        public const ushort NR34 = 0xFF1E;
        public const ushort NR41 = 0xFF20;
        public const ushort NR42 = 0xFF21;
        public const ushort NR43 = 0xFF22;
        public const ushort NR44 = 0xFF23;
        public const ushort NR50 = 0xFF24;
        public const ushort NR51 = 0xFF25;
        public const ushort NR52 = 0xFF26; /*----*/
        public const ushort LCDC = 0xFF40; // Registro de control de la pantalla
        public const ushort STAT = 0xFF41; // Registro con el estado actual de la pantalla
        public const ushort SCY = 0xFF42; // Scroll Y, contiene el valor con el scroll en el eje Y de BG
        public const ushort SCX = 0xFF43; // Scroll X, contiene el valor con el scroll en el eje X de BG
        public const ushort LY = 0xFF44; // Indica la linea vertical que se va a transferir a la pantalla
        public const ushort LYC = 0xFF45; // Se usa para compararlo con LY, si son iguales, se activa la flag coincident en STAT
        public const ushort DMA = 0xFF46; // Direccion de inicio de la transferencia DMA, la direccion esta codificada para entrar en 1 byte, ver docu.
        public const ushort BGP = 0xFF47; // Contiene la paleta de color para BG y Window. (cada par de bits indica un color)
        public const ushort OBP0 = 0xFF48; // Contiene la paleta de color para el objeto 0.
        public const ushort OBP1 = 0xFF49; // Contiene la paleta de color para el objeto 1.
        public const ushort WY = 0xFF4A; //Coordenada de inicio Y de Window
        public const ushort WX = 0xFF4B; //Cordenada de inicio X de WIndow
        public const ushort KEY1 = 0xFF4D;/*--GBC--*/
        public const ushort VBK = 0xFF4F;
        public const ushort HDMA1 = 0xFF51;
        public const ushort HDMA2 = 0xFF52;
        public const ushort HDMA3 = 0xFF53;
        public const ushort HDMA4 = 0xFF54;
        public const ushort HDMA5 = 0xFF55;
        public const ushort BGPI = 0xFF68;
        public const ushort BGPD = 0xFF69;
        public const ushort OBPI = 0xFF6A;
        public const ushort OBPD = 0xFF6B;
        public const ushort SVBK = 0xFF70;/*----*/
        public const ushort IE = 0xFFFF; //Registro que contiene el estado de las interrupciones, si se atienden o no.

        public const int HDMA_Cycles = 8;

        public const string BATTERYPATH = "battery\\";

        //byte[] complete_ROM;
        CartridgeIF cartucho;

        bool isBiosLoad = true; //Indica si los primeros 256 bytes de la memoria direccioan a la BIOS o a la ROM, siempre true al iniciar/reset
        byte[] men_bios = new byte[256]; // Bios de la GB, se encarga de hacer comprobaciones y cargar el logo de Nintendo

        // men_rom eliminada ya que es redundante, se encuentran en los primeros 32kB de complete_ROM
        //byte[] men_rom = new byte[32768]; // 32kB de memoria usada para, primeros 16kB siempre son los 16 primero de la ROM, el otro 16kB es intercambiable
        //byte[] men_wram = new byte[8192]; // 8kB de memoria RAM en la GB
        //byte[] men_eram = new byte[8192]; // 8kB de memoria RAM en el cartucho, intercambiables
        byte[] men_zram = new byte[128]; // 128 bytes de RAM de la GB, llamada pagina cero porque es la porcion de RAM mas usada
        //byte[] men_vram = new byte[8192]; // 8kB de memoria grafica
        byte[] men_oam = new byte[160]; // Region especial de la GPU
        byte[] men_io = new byte[128]; // Registros de entrada y salida

        byte[][] men_wram_banks = new byte[8][];
        byte[][] men_vram_banks = new byte[2][];
        public byte[] men_bgpalette = new byte[64];
        public byte[] men_obgpalette = new byte[64];

        int workinRamBankSelected = 1;
        int videoRamBankSelected = 0;

        Registers register;
        CPU cpu;
        Pad pad;
        public GPU gpu;

        public Memory(CPU cp, Registers register, Pad pad) {
            this.register = register;
            this.cpu = cp;
            this.pad = pad;
            resetMemory();
            if (File.Exists("ressources\\DMG_ROM.bin")) {
                using (BinaryReader reader = new BinaryReader(File.Open("ressources\\DMG_ROM.bin", FileMode.Open))) {
                    long pos = 0;
                    long lenght = reader.BaseStream.Length;
                    while (pos < lenght) {
                        men_bios[pos] = reader.ReadByte();
                        pos += sizeof(byte);
                    }
                    Console.WriteLine("COMPLETE - BIOS load into memory");
                    /*for (int i = 0; i < men_rom.Length; i++) {
                        men_rom[i] = complete_ROM[i];
                    }*/
                }
            } else {
                Console.Error.WriteLine("ERROR - File of ROM doesn't exist");
            }

            if (!Directory.Exists(BATTERYPATH)) {
                Directory.CreateDirectory(BATTERYPATH);
            }

            for (int i = 0; i < 2; i++) {
                men_wram_banks[i] = new byte[4096];
            }
            men_vram_banks[0] = new byte[8192];
        }

        public void restart() {
            resetMemory();
            videoRamBankSelected = 0;
            workinRamBankSelected = 1;
            men_wram_banks = new byte[8][];
            men_vram_banks = new byte[2][];
            men_bgpalette = new byte[64];
            men_obgpalette = new byte[64];
            for (int i = 0; i < 2; i++) {
                men_wram_banks[i] = new byte[4096];
            }
            men_vram_banks[0] = new byte[8192];
            cartucho = null;
        }

        private void resetMemory() {
            men_io[(TIMA & 0xFF)] = 0;
            men_io[(TMA & 0xFF)] = 0;
            men_io[(TAC & 0xFF)] = 0x00;
            men_io[(LCDC & 0xFF)] = 0x91;
            men_io[(STAT & 0xFF)] = 0x01;
            men_io[(BGP & 0xFF)] = 0xFC;
            men_io[(LY & 0xFF)] = 0x90;
            men_io[(IF & 0xFF)] = 0xE1;
            men_io[(OBP0 & 0xFF)] = 0xFF;
            men_io[(OBP1 & 0xFF)] = 0xFF;
            men_io[NR10 & 0xFF] = 0x80;
            men_io[NR11 & 0xFF] = 0xBF;
            men_io[NR12 & 0xFF] = 0xF3;
            men_io[NR14 & 0xFF] = 0xBF;
            men_io[NR21 & 0xFF] = 0x3F;
            men_io[NR22 & 0xFF] = 0x00;
            men_io[NR24 & 0xFF] = 0xBF;
            men_io[NR30 & 0xFF] = 0x7F;
            men_io[NR31 & 0xFF] = 0xFF;
            men_io[NR32 & 0xFF] = 0x9F;
            men_io[NR33 & 0xFF] = 0xBF;
            men_io[NR41 & 0xFF] = 0xFF;
            men_io[NR42 & 0xFF] = 0x00;
            men_io[NR43 & 0xFF] = 0x00;
            men_io[NR30 & 0xFF] = 0xBF;
            men_io[NR50 & 0xFF] = 0x77;
            men_io[NR51 & 0xFF] = 0xF3;
            //men_io[NR52 & 0xFF] = 0xF1;
            men_io[NR52 & 0xFF] = 0xF0;
        }

        /// <summary>
        /// Read 1 byte from memory
        /// </summary>
        /// <param name="address">The memory address</param>
        /// <returns>The byte read</returns>
        public byte readByte(ushort address) {
            switch (address & 0xF000) {
                // BIOS y ROM0
                case 0x0000:
                    //Comprobamos si la BIOS esa cargada
                    if (isBiosLoad) {
                        //Comprobamos si la direccion es menor de 0x0100
                        if (address < 0x0100)
                            //Si lo es, aun estamos en la BIOS, 
                            return men_bios[address];
                        else
                            //Aqui hay que comprobar si el PC es 0x100, en cuyo caso la BIOS a terminado y empezamos a leer de la ROM
                            if (register.programCounter == 0x0100)
                                isBiosLoad = false;
                    }
                    //Si lo anterior no se cumple o ya hemos acabado con la BIOS (else anterior)
                    //return complete_ROM[address];
                    return cartucho.readByteROM(address);

                // ROM0
                case 0x1000:
                case 0x2000:
                case 0x3000:
                    //Simplemente devolvemos el valor de la memoria
                    //return complete_ROM[address];
                    return cartucho.readByteROM(address);

                // ROM1 (unbanked) (16k)
                case 0x4000:
                case 0x5000:
                case 0x6000:
                case 0x7000:
                    //return complete_ROM[address];
                    return cartucho.readByteROMBankActive(address);

                // Graphics: VRAM (8k)
                case 0x8000:
                case 0x9000:
                    //return men_vram[(address - 0x8000) & 0xFFFF];
                    return readVideoRamBank(address);

                // External RAM (8k)
                case 0xA000:
                case 0xB000:
                    //return men_eram[(address - 0xA000) & 0xFFFF];
                    return cartucho.readByteRAMBankActive(address);

                // Working RAM (8k)
                case 0xC000:
                    return men_wram_banks[0][(address - 0xC000) & 0xFFFF];
                case 0xD000:
                    //return men_wram[(address - 0xC000) & 0xFFFF];
                    return readWorkingRamBAnk(address, false);

                // Working RAM shadow
                // Region especial de la memoria de la GB, es una area espejo de la RAM integrada
                case 0xE000:
                    //return men_wram[(address - 0xE000) & 0xFFFF];
                    return men_wram_banks[0][(address - 0xE000) & 0xFFFF];

                // Working RAM shadow, I/O, Zero-page RAM
                // Segunda region especial, compartida entre la los dispositivos I/O (cable link, LCD, botones, etc), la "pagina" 0 de la RAM y el resto de la zona de espejo
                case 0xF000:
                    switch (address & 0x0F00) {
                        // Working RAM shadow
                        case 0x000:
                        case 0x100:
                        case 0x200:
                        case 0x300:
                        case 0x400:
                        case 0x500:
                        case 0x600:
                        case 0x700:
                        case 0x800:
                        case 0x900:
                        case 0xA00:
                        case 0xB00:
                        case 0xC00:
                        case 0xD00:
                            //return men_wram[(address - 0xE000) & 0xFFFF];
                            return readWorkingRamBAnk(address, true);

                        // Graphics: object attribute memory
                        // OAM is 160 bytes, remaining bytes read as 0
                        case 0xE00:
                            if (address <= 0xFEA0)
                                return men_oam[address & 0xFF];
                            else
                                return 0;
                        // Zero-page
                        case 0xF00:
                            if (address >= 0xFF80) {
                                return men_zram[address & 0x7F];
                            } else {
                                if ((address >= 0xFF10) && (address <= 0xFF3F))
                                {
                                    if (cpu.audioCoreF != null)
                                        return cpu.audioCoreF.memoryRead(address);
                                }
                                if (address == BGPD) {
                                    if (!cpu.gbcMode)
                                        return 0;
                                    int index = men_io[BGPI & 0xFF] & 0x3F;
                                    return men_bgpalette[index];
                                } else if (address == OBPD) {
                                    if (!cpu.gbcMode)
                                        return 0;
                                    int index = men_io[OBPI & 0xFF] & 0x3F;
                                    return men_obgpalette[index];
                                }
                                return men_io[address & 0xFF];
                            }
                        default:
                            throw new IllegalAccessMemory();
                    }
                default:
                    throw new IllegalAccessMemory();
            }
        }

        /// <summary>
        /// Read 1 word (2 bytes) from memory.
        /// </summary>
        /// <param name="address"> The memory address</param>
        /// <returns>The word read</returns>
        public ushort readWord(ushort address) {
            ushort most = (ushort)(readByte((ushort)(address + 1)) << 8);
            return (ushort) (readByte(address) | most); 
        }

        public void writeByte(ushort address, byte value, bool fromOP = true) {
            /*if (address == 0xff8000) {
                Console.WriteLine();
                return;
            }*/
            switch (address & 0xF000) {
                // ROM bank 0
                // No acepta escritura
                case 0x0000:
                case 0x1000:
                    cartucho.changeRAMState(value);
                    break;
                case 0x2000:
                case 0x3000:
                    cartucho.changeBank(address, value, false);
                    break;

                // ROM bank 1
                // No acepta escritura
                case 0x4000:
                case 0x5000:
                    cartucho.changeBank(address, value, true);
                    break;
                case 0x6000:
                case 0x7000:
                    cartucho.changeOperationMode(value);
                    break;

                // VRAM
                case 0x8000:
                case 0x9000:
                    //men_vram[(address - 0x8000) & 0xFFFF] = value;
                    writeVideoRamBank(address, value);
                    //GPU.updatetile(address & 0x1FFF, value);
                    break;

                // External RAM
                case 0xA000:
                case 0xB000:
                    //men_vram[(address - 0xA000) & 0xFFFF] = value;
                    cartucho.writeByteRAMBankActive(address, value);
                    break;

                // Work RAM and echo
                case 0xC000:
                    men_wram_banks[0][(address - 0xC000) & 0xFFFF] = value;
                    break;
                case 0xD000:
                    //men_wram[(address - 0xC000) & 0xFFFF] = value;
                    writeWorkingRamBank(address, value, false);
                    break;
                case 0xE000:
                    //men_wram[(address - 0xE000) & 0xFFFF] = value;
                    men_wram_banks[0][(address - 0xE000) & 0xFFFF] = value;
                    break;

                // Everything else
                case 0xF000:
                    switch (address & 0x0F00) {
                        // Echo RAM
                        case 0x000:
                        case 0x100:
                        case 0x200:
                        case 0x300:
                        case 0x400:
                        case 0x500:
                        case 0x600:
                        case 0x700:
                        case 0x800:
                        case 0x900:
                        case 0xA00:
                        case 0xB00:
                        case 0xC00:
                        case 0xD00:
                            //men_wram_banks[0][(address - 0xE000) & 0xFFFF] = value;
                            writeWorkingRamBank(address, value, true);
                            break;

                        // OAM
                        case 0xE00:
                            if ((address & 0xFF) < 0xA0)
                                men_oam[address & 0xFF] = value;
                            //GPU.updateoam(addr, val);
                            break;

                        // Zeropage RAM, I/O
                        case 0xF00:
                            if (address > 0xFF7F) {
                                men_zram[address & 0x7F] = value;
                            } else
                                inputOutputWriteHandler(address, value, fromOP);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        private void writeVideoRamBank(ushort address, byte value) {
            if (videoRamBankSelected == 0)
                men_vram_banks[0][(address - 0x8000) & 0xFFFF] = value;
            else
                men_vram_banks[1][(address - 0x8000) & 0xFFFF] = value;
        }

        private byte readVideoRamBank(ushort address) {
            if (videoRamBankSelected == 0)
                return men_vram_banks[0][(address - 0x8000) & 0xFFFF];
            else
                return men_vram_banks[1][(address - 0x8000) & 0xFFFF];
        }

        public byte readVideoRamBank(ushort address, int bank) {
            return men_vram_banks[bank][(address - 0x8000) & 0xFFFF];
        }

        private void writeWorkingRamBank(ushort address, byte value, bool echo) {
            if (!echo)
                men_wram_banks[workinRamBankSelected][(address - 0xD000) & 0xFFFF] = value;
            else
                men_wram_banks[workinRamBankSelected][(address - 0xF000) & 0xFFFF] = value;
        }

        private byte readWorkingRamBAnk(ushort address, bool echo) {
            if (!echo)
                return men_wram_banks[workinRamBankSelected][(address - 0xD000) & 0xFFFF];
            else
                return men_wram_banks[workinRamBankSelected][(address - 0xF000) & 0xFFFF];
        }


        private void inputOutputWriteHandler(ushort address, byte value, bool fromOP = true) {
            int mask = (address & 0xFF);
            if (!fromOP) {
                men_io[mask] = value;
                return;
            }
            switch (address) {
                case DMA:
                    onDMARead(value);
                    break;
                case P1:
                    onP1Changed(value);
                    break;
                case TAC:
                    onTACchanged(value);
                    break;
                case STAT:
                    onSTATChanged(value);
                    break;
                case LY:
                    men_io[mask] = 0;
                    gpu.checkLYC();
                    break;
                case LYC:
                    men_io[mask] = value;
                    gpu.checkLYC();
                    break;
                case DIV:
                    men_io[mask] = 0;
                    break;
                case LCDC:
                    onLCDCChanged(value);
                    break;
                case VBK:
                    if (!cpu.gbcMode)
                        break;
                    if (value > 1)
                        break;
                    men_io[mask] = (byte)(value & 0x1);
                    if (value == 0) {
                        videoRamBankSelected = 0;
                    } else {
                        videoRamBankSelected = 1;
                    }
                    break;
                case HDMA1:
                case HDMA2:
                case HDMA3:
                case HDMA4:
                    if (!cpu.gbcMode)
                        break;
                    men_io[mask] = value;
                    break;
                case HDMA5:
                    if (!cpu.gbcMode)
                        break;
                    men_io[mask] = updateHDMA5(value);
                    break;
                case KEY1:
                    if (!cpu.gbcMode)
                        break;
                    //men_io[mask] = (byte)((men_io[mask] & 0x80) | (value & 0x01));
                    men_io[mask] = (byte)((value & (0x7F)) | (men_io[mask] & 0x80));
                    break;
                case SVBK:
                    if (!cpu.gbcMode || (value) > 0x7)
                        break;
                    if ((value & 0x07) == 0) {
                        workinRamBankSelected = 1;
                        men_io[mask] = (byte)((value & 0xBF) | 1);
                    }else {
                        workinRamBankSelected = (value & 0x07);
                        men_io[mask] = (byte)(value & 0xBF);
                    }    
                    break;
                case BGPI:
                    if (!cpu.gbcMode)
                        break;
                    men_io[mask] = (byte)(value & ~0x40);
                    break;
                case BGPD:
                    if (!cpu.gbcMode)
                        break;
                    int index = men_io[BGPI & 0xFF] & 0x3F;
                    men_bgpalette[index] = value;
                    if ((men_io[BGPI & 0xFF] & 0x80) != 0) {
                        men_io[BGPI & 0xFF] = (byte)(((men_io[BGPI & 0xFF] + 1) & 0x3F) | (men_io[BGPI & 0xFF] & 0x80));
                    }
                    break;
                case OBPI:
                    if (!cpu.gbcMode)
                        break;
                    men_io[mask] = (byte)(value & ~0x40);
                    break;
                case OBPD:
                    if (!cpu.gbcMode)
                        break;
                    int index2 = men_io[OBPI & 0xFF] & 0x3F;
                    men_obgpalette[index2] = value;
                    if ((men_io[OBPI & 0xFF] & 0x80) != 0) {
                        men_io[OBPI & 0xFF] = (byte)(((men_io[OBPI & 0xFF] + 1) & 0x3F) | (men_io[OBPI & 0xFF] & 0x80));
                    }
                    break;
                case IF:
                    men_io[mask] = (byte)(0xE0 | (value & 0x1F));
                    break;
                case SB:
                    men_io[mask] = value;
                    break;
                case SC:
                    men_io[mask] = value;
                    break;
                case TIMA:
                    men_io[mask] = value;
                    break;
                case TMA:
                    men_io[mask] = value;
                    break;
                case SCY:
                    men_io[mask] = value;
                    break;
                case SCX:
                    men_io[mask] = value;
                    break;
                case BGP:
                    men_io[mask] = value;
                    break;
                case OBP0:
                    men_io[mask] = value;
                    break;
                case OBP1:
                    men_io[mask] = value;
                    break;
                case WX:
                    men_io[mask] = value;
                    break;
                case WY:
                    men_io[mask] = value;
                    break;
                case IE:
                    men_io[mask] = value;
                    break;
                default:
                    if ((address >= 0xFF10) && (address <= 0xFF3F)) {
                        if (address == NR52)
                            men_io[mask] = (byte)(value & 0xF0);
                        men_io[mask] = value;
                        if (cpu.audioCoreF != null)
                            cpu.audioCoreF.memoryWrite(address, value);
                    }
                    break;
            }
        }

        private void onLCDCChanged(byte value) {
            byte actual = readByte(LCDC);
            int oldScreenOn = actual & 0x80;
            int newScreenOn = value & 0x80;
            if (oldScreenOn != 0 && newScreenOn == 0) {
                writeByte(LY, 0, false);
                byte stat = readByte(STAT);
                writeByte(STAT, (byte)(stat & ~0x03), false);
            } else if (oldScreenOn == 0 && newScreenOn != 0) {
                gpu.checkLYC();
            }

            gpu.clock_lcd = 0;
            men_io[(LCDC & 0xFF)] = value;
        }

        private void onSTATChanged(byte value) {
            byte old = men_io[STAT & 0xFF];
            byte newValue = (byte)((value & ~0x07) | (old & 0x07));
            men_io[STAT & 0xFF] = newValue;
            if ((newValue & 0x20) != 0 && (newValue & 0x11) == 2)
                cpu.setInterruptionFlag(1);
            if ((newValue & 0x10) != 0 && (newValue & 0x11) == 1)
                cpu.setInterruptionFlag(1);
            if ((newValue & 0x8) != 0 && (newValue & 0x11) == 0)
                cpu.setInterruptionFlag(1);
            gpu.checkLYC();
        }

        private void onTACchanged(byte value) {
            byte newValue = (byte)(value & 0x07);
            byte old = men_io[TAC & 0xFF];
            if (((newValue & 0x03) != (old & 0x03)) || ((newValue & 0x04) == 0)) {
                gpu.clock_lcd = 0;
                men_io[TIMA & 0xFF] = men_io[TMA & 0xFF];
            }
            men_io[TAC & 0xFF] = newValue;
        }

        private void onP1Changed(byte value) {
            byte old = men_io[P1 & 0xFF];
            byte newValue = (byte)((value & 0xF0) | (old & ~0xF0));
            byte padValue = pad.update(newValue);
            if ((padValue != old) && ((padValue & 0x0F) != 0x0F)){
                // HA cambiado el valor del pad, hay que producir una interrupcion
                //men_io[IF & 0xFF] = (byte)(men_io[IF & 0xFF] | 0x10);
                cpu.setInterruptionFlag(4);
            }
            men_io[P1 & 0xFF] = padValue;
        }

        private void onDMARead(byte value) {
            //men_io[DMA & 0xFF] = value;
            ushort address;
            for (int i = 0; i < 0xA0; i++) {
                address = (ushort)((value << 8) + i);
                men_oam[i] = readByte(address);
            }
        }

        public void writeWord(ushort address, ushort value) {
            byte most = (byte)(value >> 8);
            writeByte(address, (byte)(value & 0xFF));
            writeByte((ushort)(address + 1), most);
        }


        public void loadRom(string filePath) {
            byte[] complete_ROM;
            if (File.Exists(filePath)) {
                using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open))) {
                    string filename = Path.GetFileNameWithoutExtension(filePath);
                    long pos = 0;
                    long lenght = reader.BaseStream.Length;
                    complete_ROM = new byte[lenght];
                    while (pos < lenght) {
                        complete_ROM[pos] = reader.ReadByte();
                        pos += sizeof(byte);
                    }
                    if (!CPU.FORCEGB && (complete_ROM[0x0143] == 0x80 || complete_ROM[0x0143] == 0xC0)) {
                        cpu.gbcMode = true;
                        for (int i = 0; i < 8; i++) {
                            men_wram_banks[i] = new byte[4096];
                        }
                        men_vram_banks[1] = new byte[8192];
                    } else {
                        cpu.gbcMode = false;
                        for (int i = 0; i < 2; i++) {
                            men_wram_banks[i] = new byte[4096];
                        }
                    }
                    switch (complete_ROM[0x0147]) {
                        case 0x0:
                        case 0x1:
                            cartucho = new MemoryBank1(complete_ROM, filename, OPERATIONMODEMBC1.ROM);
                            break;
                        case 0x2:
                            cartucho = new MemoryBank1(complete_ROM, filename, OPERATIONMODEMBC1.ROMRAM);
                            break;
                        case 0x3:
                            cartucho = new MemoryBank1(complete_ROM, filename, OPERATIONMODEMBC1.ROMRAMBATT);
                            break;
                        case 0x5:
                            cartucho = new MemoryBank2(complete_ROM, filename, OPERATIONMODEMBC2.ROM);
                            break;
                        case 0x6:
                            cartucho = new MemoryBank2(complete_ROM, filename, OPERATIONMODEMBC2.ROMBATT);
                            break;
                        case 0xF:
                            cartucho = new MemoryBank3(complete_ROM, filename, OPERATIONMODEMBC3.ROMTIMERBATT);
                            break;
                        case 0x10:
                            cartucho = new MemoryBank3(complete_ROM, filename, OPERATIONMODEMBC3.ROMRAMTIMERBATT);
                            break;
                        case 11:
                            cartucho = new MemoryBank3(complete_ROM, filename, OPERATIONMODEMBC3.ROM);
                            break;
                        case 0x12:
                            cartucho = new MemoryBank3(complete_ROM, filename, OPERATIONMODEMBC3.ROMRAM);
                            break;
                        case 0x13:
                            cartucho = new MemoryBank3(complete_ROM, filename, OPERATIONMODEMBC3.ROMRAMBATT);
                            break;
                        case 0x19:
                            cartucho = new MemoryBank5(complete_ROM, filename, OPERATIONMODEMBC5.ROM);
                            break;
                        case 0x1A:
                            cartucho = new MemoryBank5(complete_ROM, filename, OPERATIONMODEMBC5.ROMRAM);
                            break;
                        case 0x1B:
                            cartucho = new MemoryBank5(complete_ROM, filename, OPERATIONMODEMBC5.ROMRAMBATT);
                            break;
                        default:
                            cartucho = null;
                            break;

                    }
                    //cartucho = new MemoryBank1(complete_ROM, OPERATIONMODE.ROM);
                    Console.WriteLine("COMPLETE - ROM load into memory");
                }
            } else {
                Console.Error.WriteLine("ERROR - File of ROM doesn't exist");
            }
        }

        bool hdma5Active = false;

        //De la ROM a VRAM
        public byte updateHDMA5(byte value) {
            int mode = value & 0x80;

            //Parada manual
            if (hdma5Active && mode == 0) {
                hdma5Active = false;
                //value = (byte)(readByte(HDMA5) | 0x80);
                value = (byte)(value | 0x80);
            } else {
                if (mode == 0) {
                    ushort source = (ushort)((readByte(HDMA1) << 8) | (readByte(HDMA2) & 0xF0));
                    ushort destiny = (ushort)(((readByte(HDMA3) & 0x1F) << 8) | (readByte(HDMA4) & 0xF0));
                    destiny += 0x8000;
                    int lenght = (((value & 0x7F) + 1) * 0x10);

                    for (int i = 0; i < lenght; i++)
                        writeByte((ushort)(destiny + i), readByte((ushort)(source + i)), false);
                    destiny -= 0x8000;
                    writeByte(HDMA1, (byte)((source + lenght) >> 8), false);
                    writeByte(HDMA2, (byte)((source + lenght) & 0xF0), false);
                    writeByte(HDMA3, (byte)((destiny + lenght) >> 8), false);
                    writeByte(HDMA4, (byte)((destiny + lenght) & 0xF0), false);
                    value = 0xFF;
                    cpu.clock_cycles += (HDMA_Cycles * lenght)/ (0x10 * cpu.velovityMulti);
                    hdma5Active = false;
                } else {
                    hdma5Active = true;
                    value = (byte)((value & 0x7F) | 0x80);
                }
            }

            return value;
        }

        public void updateHDMA() {
            if (hdma5Active) {
                ushort source = (ushort)((readByte(Memory.HDMA1) << 8) | (readByte(Memory.HDMA2) & 0xF0));
                ushort destine = (ushort)(((readByte(Memory.HDMA3) & 0x1F )<< 8) | (readByte(Memory.HDMA4) & 0xF0));
                destine += 0x8000;
                for (int i = 0; i < 0x10; i++)
                    writeByte((ushort)(destine + i), readByte((ushort)(source + i)), false);
                destine -= 0x8000;
                int srcEnd = (source + 0x10);
                int dstEnd = (destine + 0x10);
                writeByte(Memory.HDMA1, (byte)(srcEnd >> 8), false);
                writeByte(Memory.HDMA2, (byte)(srcEnd & 0xF0), false);
                writeByte(Memory.HDMA3, (byte)(dstEnd >> 8), false);
                writeByte(Memory.HDMA4, (byte)(dstEnd & 0xF0), false);

                byte hdma5 = readByte(Memory.HDMA5);
                hdma5 -= 1;
                if ((hdma5 & 0x7F) == 0x7F) {
                    hdma5Active = false;
                    writeByte(Memory.HDMA5, 0x80, false);
                } else {
                    writeByte(Memory.HDMA5, (byte)(hdma5 & 0x7F), false);
                }

                if ((readByte(Memory.KEY1) & 0x80) != 0)
                    cpu.clock_cycles += HDMA_Cycles * 2;
                else
                    cpu.clock_cycles += HDMA_Cycles;
            }
        }
    }
}
