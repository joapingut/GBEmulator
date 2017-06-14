using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {
    enum OPERATIONMODEMBC3 {
        ROM, ROMRAM, ROMRAMBATT, ROMTIMERBATT, ROMRAMTIMERBATT
    }
    class MemoryBank3:CartridgeIF {

        OPERATIONMODEMBC3 mode;

        byte[] completeRom;
        string fileName;
        bool enableRAM = true;

        RTC rtc;
        bool rtcMode = false;

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
        

        public MemoryBank3(byte[] completeRom, string filename, OPERATIONMODEMBC3 mode) {
            this.mode = mode;
            this.completeRom = completeRom;
            this.fileName = filename;
            switch (mode) {
                case OPERATIONMODEMBC3.ROM:
                    rom_bankSelect = 1;
                    ram_bankSelect = 0;
                    men_ram_banks = new byte[1][];
                    men_ram_banks[0] = new byte[8192];
                    break;
                case OPERATIONMODEMBC3.ROMRAMBATT:
                case OPERATIONMODEMBC3.ROMRAM:
                    rom_bankSelect = 1;
                    ram_bankSelect = 0;
                    men_ram_banks = new byte[4][];
                    for (int i = 0; i < 4; i++) {
                        men_ram_banks[i] = new byte[8192];
                    }
                    loadBATTERY();
                    break;
                case OPERATIONMODEMBC3.ROMRAMTIMERBATT:
                    rtc = new RTC();
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
            if ((mode == OPERATIONMODEMBC3.ROMRAMTIMERBATT) && (rtcMode)) {
                return rtc.readSelected();
            }else
                return getActiveRamBank()[address- offset];
        }

        public void writeByteRAMBankActive(ushort address, byte value) {
            if (!enableRAM)
                return;
            if (rtcMode) {
                rtc.writeSelected(value);
                return;
            }
            int offset = 0xA000;
            men_ram_banks[ram_bankSelect][address - offset] = value;
        }

        public byte readByteROM(ushort address) {
            return completeRom[address];
        }

        private void changeRAMBank(byte valueWrite) {
            if (valueWrite < 4) {
                ram_bankSelect = valueWrite & 0x3;
                rtcMode = false;
            } else {
                rtc.changeSelected((byte)(valueWrite - 8));
                rtcMode = true;
            }
        }

        private void changeROMBank(byte valueWrite) {
            /*if (valueWrite == 0)
                valueWrite = 1;*/
            rom_bankSelect = (valueWrite & 0x7F);
        }

        public void changeBank(ushort address, byte valuewrite, bool twoMost = false) {
            if (!twoMost) {
                changeROMBank(valuewrite);
                return;
            }
            if (mode == OPERATIONMODEMBC3.ROM) {
                //changeROMBank(valuewrite, twoMost);
                return;
            }
            changeRAMBank(valuewrite);
        }

        public void changeOperationMode(byte valuewrite) {
            if (!enableRAM)
                return;
            if (rtc != null && rtcMode) {
                rtc.updateFixedRegs(valuewrite);
            }
        }

        public void changeRAMState(byte value) {
            if (enableRAM && ((value & 0x0F) != 0xA)) {
                saveBATTERY();
                enableRAM = false;
            } else if (!enableRAM && ((value & 0x0F) == 0xA)) {
                enableRAM = true;
            }
        }

        public void saveBATTERY() {
            if (mode == OPERATIONMODEMBC3.ROMRAMTIMERBATT || mode == OPERATIONMODEMBC3.ROMRAMBATT) {
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
                    if (mode == OPERATIONMODEMBC3.ROMRAMTIMERBATT || mode == OPERATIONMODEMBC3.ROMTIMERBATT)
                        rtc.saveRTC(fileName);
                } catch {
                    Console.Error.WriteLine("ERROR - MBC3 - No se pudo guardar el archivo BATT");
                }
            }
        }

        public void loadBATTERY() {
            if (mode == OPERATIONMODEMBC3.ROMRAMTIMERBATT || mode == OPERATIONMODEMBC3.ROMRAMBATT) {
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
                        if(mode == OPERATIONMODEMBC3.ROMRAMTIMERBATT || mode == OPERATIONMODEMBC3.ROMTIMERBATT)
                            rtc.loadRTC(fileName);
                    }
                } catch {
                    Console.Error.WriteLine("ERROR - MBC3 - No se pudo leer el archivo BATT");
                }
            }
        }
    }
}
