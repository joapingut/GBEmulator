using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {

    interface CartridgeIF {
        byte[] getActiveRamBank();

        byte readByteROMBankActive(ushort address);

        byte readByteRAMBankActive(ushort address);

        void writeByteRAMBankActive(ushort address, byte value);

        byte readByteROM(ushort address);

        void changeBank(ushort address, byte valuewrite, bool twoMost = false);

        void changeOperationMode(byte valuewrite);

        void changeRAMState(byte value);

        void saveBATTERY();

        void loadBATTERY();

        //void changeRAMBank(byte valueWrite);

        //void changeROMBank(byte valueWrite, bool twoMost = false);
    }
}
