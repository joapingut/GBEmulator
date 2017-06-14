using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {

    /// <summary>
    /// Name of GB registers
    /// </summary>
    enum reg_NAMES {
        A, B, C, D, E, F, H, L, AF, BC, DE, HL, PC, SP, INMEDIATE, INMEDIATE16
    }

    /// <summary>
    /// Name of GB ALU flags
    /// C - Last operation produced result over 255
    /// H - Last operation produced result over 15
    /// N - Last operation was a subtraction
    /// Z - Last operation result was 0
    /// Xn - Not used
    /// </summary>
    enum flag_NAME {
        X0, X1, X2, X3, C, H, N, Z
    }

    class Registers {

        // Cada posicion del array es un registro, su posicion viene dada por reg_NAMES
        byte[] registers = new byte[8];
        // Cada posicion de la 4 a la 8 son las flags anteriores.
        public ushort stackPointer = 0;
        public ushort programCounter = 0;

        public bool halt = false;
        public bool stop = false;
        public bool IME = true; //Interrupciones
        public bool conditionalTaken = false;

        public byte getRegisterSimple(reg_NAMES name) {
            switch (name) {
                case reg_NAMES.A:
                    return registers[(int) reg_NAMES.A];
                case reg_NAMES.B:
                    return registers[(int)reg_NAMES.B];
                case reg_NAMES.C:
                    return registers[(int)reg_NAMES.C];
                case reg_NAMES.D:
                    return registers[(int)reg_NAMES.D];
                case reg_NAMES.E:
                    return registers[(int)reg_NAMES.E];
                case reg_NAMES.F:
                    return registers[(int)reg_NAMES.F];
                case reg_NAMES.H:
                    return registers[(int)reg_NAMES.H];
                case reg_NAMES.L:
                    return registers[(int)reg_NAMES.L];
                default:
                    throw new IllegalOperationCPU("ERROR - El registro pedido no es simple");
            }
        }

        public void setRegisterSimple(reg_NAMES name, byte value) {
            switch (name) {
                case reg_NAMES.A:
                    registers[(int)reg_NAMES.A] = value;
                    break;
                case reg_NAMES.B:
                    registers[(int)reg_NAMES.B] = value;
                    break;
                case reg_NAMES.C:
                    registers[(int)reg_NAMES.C] = value;
                    break;
                case reg_NAMES.D:
                    registers[(int)reg_NAMES.D] = value;
                    break;
                case reg_NAMES.E:
                    registers[(int)reg_NAMES.E] = value;
                    break;
                case reg_NAMES.F:
                    registers[(int)reg_NAMES.F] = (byte)(value & 0xF0);
                    break;
                case reg_NAMES.H:
                    registers[(int)reg_NAMES.H] = value;
                    break;
                case reg_NAMES.L:
                    registers[(int)reg_NAMES.L] = value;
                    break;
                default:
                    throw new IllegalOperationCPU("ERROR - El registro pedido no es simple");
            }
        }

        public ushort getRegisterDouble(reg_NAMES name) {
            ushort most;
            switch (name) {
                case reg_NAMES.AF:
                    most = (ushort)(registers[(int)reg_NAMES.A] << 8);
                    return (ushort)(most | registers[(int)reg_NAMES.F]);
                case reg_NAMES.BC:
                    most = (ushort)(registers[(int)reg_NAMES.B] << 8);
                    return (ushort)(most | registers[(int)reg_NAMES.C]);
                case reg_NAMES.DE:
                    most = (ushort)(registers[(int)reg_NAMES.D] << 8);
                    return (ushort)(most | registers[(int)reg_NAMES.E]);
                case reg_NAMES.HL:
                    most = (ushort)(registers[(int)reg_NAMES.H] << 8);
                    return (ushort)(most | registers[(int)reg_NAMES.L]);
                case reg_NAMES.PC:
                    return programCounter;
                case reg_NAMES.SP:
                    return stackPointer;
                default:
                    throw new IllegalOperationCPU("ERROR - El registro pedido no es doble");
            }
        }

        public void setRegisterDouble(reg_NAMES name, ushort value) {
            byte most, minus;
            switch (name) {
                case reg_NAMES.AF:
                    most = (byte)((value & 0xFF00) >> 8);
                    setRegisterSimple(reg_NAMES.A, most);
                    minus = (byte)(value & 0x00FF);
                    setRegisterSimple(reg_NAMES.F, minus);
                    break;
                case reg_NAMES.BC:
                    most = (byte)((value & 0xFF00) >> 8);
                    setRegisterSimple(reg_NAMES.B, most);
                    minus = (byte)(value & 0x00FF);
                    setRegisterSimple(reg_NAMES.C, minus);
                    break;
                case reg_NAMES.DE:
                    most = (byte)((value & 0xFF00) >> 8);
                    setRegisterSimple(reg_NAMES.D, most);
                    minus = (byte)(value & 0x00FF);
                    setRegisterSimple(reg_NAMES.E, minus);
                    break;
                case reg_NAMES.HL:
                    most = (byte)((value & 0xFF00) >> 8);
                    setRegisterSimple(reg_NAMES.H, most);
                    minus = (byte)(value & 0x00FF);
                    setRegisterSimple(reg_NAMES.L, minus);
                    break;
                case reg_NAMES.PC:
                    programCounter = value;
                    break;
                case reg_NAMES.SP:
                    stackPointer = value;
                    break;
                default:
                    throw new IllegalOperationCPU("ERROR - El registro pedido no es doble");
            }
        }

        public byte getFlag(flag_NAME name) {
            switch (name) {
                case flag_NAME.C:
                    return (byte)((registers[(int)reg_NAMES.F] & 0x10) >> 4);
                case flag_NAME.H:
                    return (byte)((registers[(int)reg_NAMES.F] & 0x20) >> 5);
                case flag_NAME.N:
                    return (byte)((registers[(int)reg_NAMES.F] & 0x40) >> 6);
                case flag_NAME.Z:
                    return (byte)(registers[(int)reg_NAMES.F] >> 7);
                default:
                    throw new IllegalOperationCPU("This flag is not used");
            }
        }

        public void setFlag(flag_NAME name, byte value) {
            switch (name) {
                case flag_NAME.C:
                    registers[(int)reg_NAMES.F] = (byte)(((registers[(int)reg_NAMES.F] & 0xEF) | value << 4) & 0xF0);
                    break;
                case flag_NAME.H:
                    registers[(int)reg_NAMES.F] = (byte)(((registers[(int)reg_NAMES.F] & 0xDF) | value << 5) & 0xF0);
                    break;
                case flag_NAME.N:
                    registers[(int)reg_NAMES.F] = (byte)(((registers[(int)reg_NAMES.F] & 0xBF) | value << 6) & 0xF0);
                    break;
                case flag_NAME.Z:
                    registers[(int)reg_NAMES.F] = (byte)(((registers[(int)reg_NAMES.F] & 0x7F) | value << 7) & 0xF0);
                    break;
                default:
                    throw new IllegalOperationCPU("This flag is not used");
            }
        }

        public void resetRegisters() {
            if (CPU.FORCEGB)
                registers[0] = 0x01; // A GB only
            else
                registers[0] = 0x11; // A // GBC
            registers[1] = 0x00; // B
            registers[2] = 0x13; // C
            registers[3] = 0x00; // D
            registers[4] = 0xD8; // E
            registers[5] = 0xB0; // F
            registers[6] = 0x01; // H
            registers[7] = 0x4D; // L
            stackPointer = 0xFFFE;
            programCounter = 0x0100;
            halt = false;
            stop = false;
        }
    }
}
