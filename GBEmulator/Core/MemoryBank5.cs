using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {
    enum OPERATIONMODEMBC5 {
        ROM, ROMRAM, ROMRAMBATT, ROMRUMBLE, ROMRUMBLESRAM, ROMRUMBLESRAMBATT
    }
    class MemoryBank5:CartridgeIF {

        OPERATIONMODEMBC5 mode;

        byte[] completeRom;
        string fileName;

        bool operationMode = false;
        bool enableRAM = true;

        //Bancos para la RAM, cada uno de 16K
        //Se accede a ellos desde el case 0xA000: case 0xB000:
        byte[][] men_ram_banks;
        int ram_bankSelect = 0;

        //Que banco de memoria ROM esta seleccionado, el banco 0 no es elegible y siempre se carga en la memoria,
        //Se accede a ellos via:
        /*      case 0x4000:
                case 0x5000:
                case 0x6000:
                case 0x7000:*/
        int rom_bankSelect = 1;

        public MemoryBank5(byte[] completeRom, string filename, OPERATIONMODEMBC5 mode) {
            this.mode = mode;
            this.completeRom = completeRom;
            this.fileName = filename;
            switch (mode) {
                case OPERATIONMODEMBC5.ROM:
                    rom_bankSelect = 1;
                    ram_bankSelect = 0;
                    men_ram_banks = new byte[1][];
                    men_ram_banks[0] = new byte[8192];
                    break;
                case OPERATIONMODEMBC5.ROMRAMBATT:
                case OPERATIONMODEMBC5.ROMRAM:
                    rom_bankSelect = 1;
                    ram_bankSelect = 0;
                    men_ram_banks = new byte[128][];
                    for (int i = 0; i < 128; i++) {
                        men_ram_banks[i] = new byte[8192];
                    }
                    loadBATTERY();
                    break;
            }
        }

        public byte[] getRamBank(int bankId) {
            return men_ram_banks[bankId];
        }

        public byte[] getActiveRamBank() {
            return getRamBank(this.ram_bankSelect);
        }

        public byte readByteROMBankActive(ushort address) {
            int offset = 0x4000 * rom_bankSelect;
            return completeRom[offset + (address & 0x3FFF)];
        }

        public byte readByteRAMBankActive(ushort address) {
            if (!enableRAM)
                return 0;
            int offset = 0xA000;
            return getActiveRamBank()[address- offset];
        }

        public void writeByteRAMBankActive(ushort address, byte value) {
            if (!enableRAM)
                return;
            int offset = 0xA000;
            men_ram_banks[ram_bankSelect][address - offset] = value;
        }

        public byte readByteROM(ushort address) {
            return completeRom[address];
        }

        private void changeRAMBank(byte valueWrite) {
            ram_bankSelect = valueWrite & 0xF;
        }

        public void changeRAMState(byte value) {
            if (enableRAM && ((value & 0x0F) != 0xA)) {
                saveBATTERY();
                enableRAM = false;
            } else if (!enableRAM && ((value & 0x0F) == 0xA)) {
                enableRAM = true;
            }
        }

        private void changeROMBank(byte valueWrite, bool most) {
            if (most) {
                rom_bankSelect = (rom_bankSelect & 0xFF) + ((valueWrite & 0x01) << 8);
            } else {
                rom_bankSelect = (rom_bankSelect & 0x100) + (valueWrite & 0xFF);
            }
        }

        public void changeBank(ushort address, byte valuewrite, bool twoMost = false) {
            if (!twoMost) {
                if (address >= 0x2000 && address <= 0x2FFF) {
                    changeROMBank(valuewrite, false);
                } else if (address >= 0x3000 && address <= 0x3FFF) {
                    changeROMBank(valuewrite, true);
                }
                return;
            }
            changeRAMBank(valuewrite);
        }

        public void changeOperationMode(byte valuewrite) {
        }

        public void saveBATTERY() {
            if (mode == OPERATIONMODEMBC5.ROMRAMBATT || mode == OPERATIONMODEMBC5.ROMRUMBLESRAMBATT) {
                try {
                    using (BinaryWriter writer = new BinaryWriter(File.Open(Memory.BATTERYPATH + fileName + ".batt", FileMode.OpenOrCreate))) {
                        long pos = 0;
                        long pos_bank = 0;
                        int bank = 0;
                        while (pos < (4 * 8192)) {
                            writer.Write(men_ram_banks[bank][pos_bank]);
                            pos += sizeof(byte);
                            pos_bank += sizeof(byte);
                            if (pos_bank >= 8192) {
                                bank += 1;
                                pos_bank = 0;
                            }
                        }
                    }
                } catch {
                    Console.Error.WriteLine("ERROR - MBC5 - No se pudo guardar el archivo BATT");
                }
            }
        }

        public void loadBATTERY() {
            if (mode == OPERATIONMODEMBC5.ROMRAMBATT || mode == OPERATIONMODEMBC5.ROMRUMBLESRAMBATT) {
                try {
                    using (BinaryReader reader = new BinaryReader(File.Open(Memory.BATTERYPATH + fileName + ".batt", FileMode.Open))) {
                        long pos = 0;
                        long pos_bank = 0;
                        int bank = 0;
                        while (pos < (128 * 8192)) {
                            men_ram_banks[bank][pos_bank] = reader.ReadByte();
                            pos += sizeof(byte);
                            pos_bank += sizeof(byte);
                            if (pos_bank >= 8192) {
                                bank += 1;
                                pos_bank = 0;
                            }
                        }
                    }
                } catch {
                    Console.Error.WriteLine("ERROR - MBC5 - No se pudo leer el archivo BATT");
                }
            }
        }
    }
}
