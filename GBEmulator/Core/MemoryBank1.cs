using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {
    enum OPERATIONMODEMBC1 {
        ROM, ROMRAM, ROMRAMBATT
    }
    class MemoryBank1:CartridgeIF {

        OPERATIONMODEMBC1 mode;

        bool operationMode = false; //false mode 4/32; true mode 16/8

        bool enableRAM = true;

        byte[] completeRom;
        string fileName;

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

        public MemoryBank1(byte[] completeRom, string filename, OPERATIONMODEMBC1 mode) {
            this.mode = mode;
            this.fileName = filename;
            this.completeRom = completeRom;
            switch (mode) {
                case OPERATIONMODEMBC1.ROM:
                    rom_bankSelect = 1;
                    ram_bankSelect = 0;
                    men_ram_banks = new byte[1][];
                    men_ram_banks[0] = new byte[8192];
                    break;
                case OPERATIONMODEMBC1.ROMRAMBATT:
                case OPERATIONMODEMBC1.ROMRAM:
                    rom_bankSelect = 1;
                    ram_bankSelect = 0;
                    men_ram_banks = new byte[4][];
                    for (int i = 0; i < 4; i++) {
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
            return getActiveRamBank()[address - offset];
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
            ram_bankSelect = valueWrite & 0x3;
        }

        private void changeROMBank(byte valueWrite, bool twoMost = false) {
            if (twoMost) {
                rom_bankSelect = (rom_bankSelect & 0x1F) | (valueWrite & 0x60);
            } else {
                if (valueWrite == 0)
                    valueWrite = 1;
                rom_bankSelect = (rom_bankSelect & 0x60) | (valueWrite & 0x1F);
            }
        }

        public void changeBank(ushort address, byte valuewrite, bool twoMost = false) {
            if (!twoMost) {
                changeROMBank(valuewrite, twoMost);
                return;
            }
            if (operationMode) {
                changeROMBank(valuewrite, twoMost);
                return;
            }
            changeRAMBank(valuewrite);
        }

        public void changeOperationMode(byte valuewrite) {
            if (!enableRAM)
                return;
            if (valuewrite == 0) {
                operationMode = false;
            } else if (valuewrite == 1) {
                operationMode = true;
            }
        }

        public void changeRAMState(byte value) {
            if (enableRAM && ((value & 0x0F) != 0xA)) {
                saveBATTERY();
                enableRAM = false;
            } else if(!enableRAM && ((value & 0x0F) == 0xA)) {
                enableRAM = true;
            }
        }

        public void saveBATTERY() {
            if (mode == OPERATIONMODEMBC1.ROMRAMBATT) {
                try {
                    using (BinaryWriter writer = new BinaryWriter(File.Open(Memory.BATTERYPATH + fileName+".batt", FileMode.OpenOrCreate))) {
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
                    Console.Error.WriteLine("ERROR - MBC1 - No se pudo guardar el archivo BATT");
                }
            }
        }

        public void loadBATTERY() {
            if (mode == OPERATIONMODEMBC1.ROMRAMBATT) {
                try {
                    using (BinaryReader reader = new BinaryReader(File.Open(Memory.BATTERYPATH + fileName + ".batt", FileMode.Open))) {
                        long pos = 0;
                        long pos_bank = 0;
                        int bank = 0;
                        while (pos < (4 * 8192)) {
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
                    Console.Error.WriteLine("ERROR - MBC1 - No se pudo leer el archivo BATT");
                }
            }
        }
    }
}
