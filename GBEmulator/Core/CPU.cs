using nanoboy.Core.Audio;
using nanoboy.Core.Audio.Backend.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core{

    class CPU {

        private static bool DEBUG = false;
        public const int CyclesPerFrame = 70224;//70224

        public int velovityMulti = 1;
        public bool gbcMode = false;
        public const bool FORCEGB = false;

        public void changeDoubleSpeed(bool doubleSpeed) {
            if (doubleSpeed)
                velovityMulti = 2;
            else
                velovityMulti = 1;
        }

        public Memory memory;
        public Registers registers;
        //public SoundSystem sound;
        //public SdlSound soundSustem;
        //public GBPAPU papu;
        public Audio audioCoreF;
        private ALSoundOut soundout;
        public GPU gpu;
        public Pad pad;

        // CPU clocks
        public int clock_cycles;
        int clock_timer;
        int cyclesDIV;

        int currentOP;

        public bool newInterrupt = false;
        public bool vBlankPending = false;
        bool firstAfterEI = true;

        public CPU(ScreenIF screen) {
            this.registers = new Registers();
            this.pad = new Pad();
            this.memory = new Memory(this, registers, pad);
            this.gpu = new GPU(this, memory, registers, screen);
            //this.sound = new SoundSystem(memory);
            //this.soundSustem = new SdlSound();
            //this.papu = new GBPAPU(memory);
            this.audioCoreF = new Audio();
            this.soundout = new ALSoundOut(audioCoreF);
            audioCoreF.Channel1.Enabled = true;
            audioCoreF.Channel2.Enabled = true;
            audioCoreF.Channel3.Enabled = true;
            audioCoreF.Channel4.Enabled = true;
            audioCoreF.Enabled = true;
            registers.resetRegisters();
            registers.programCounter = 0x0100;
            clock_cycles = 0;
            clock_timer = 0;
            cyclesDIV = 0;
        }

        public void restart(string rompath) {
            clock_cycles = 0;
            clock_timer = 0;
            clock_serial = 0;
            changeDoubleSpeed(false);
            gpu.changeDoubleSpeed(false);
            registers.resetRegisters();
            memory.restart();
            memory.loadRom(rompath);
        }

        public int executeOneFrame() {
            return execute(CyclesPerFrame * velovityMulti);
        }

        public void onEndFrame() {
            //Nada de momento, pero habria que actulizar la pantalla con un refresh y el puerto serie
            if(gpu.screen == null)
                gpu.screen.refreshGBScreen();
        }

        public int execute(int cyclesToExecute) {
            byte opCode = 0, nextOpCode = 0, lastOpCode = 0;
            ushort lasPC = 0;

            int cycles = 0;

            while (cycles < cyclesToExecute) {
                lastOpCode = opCode;
                opCode = memory.readByte(registers.programCounter);
                nextOpCode = memory.readByte((ushort)(registers.programCounter + 1));
                currentOP = opCode;
                /* LOG */
                //Console.WriteLine("Program counter: {0:X} OP: {1:X} SP: {2:X}", registers.programCounter, opCode, registers.stackPointer );
                /*if (ccounter > 800) {
                    Console.Clear();
                    ccounter = 0;
                }*/

                //Debugger.addInst(opCode, registers.programCounter, registers.stackPointer);
                //if (registers.programCounter == 0x40c0 || (registers.programCounter >= 0x8000 && registers.programCounter <= 0xA000)) //0x352 //tetris infinite loop 36c
                    //Console.WriteLine("START GB");

                lasPC = registers.programCounter;
                if (!registers.halt) {
                    switch (opCode) {
                        case (0x00): NOP(); break;
                        case (0x01): LD_n_nn(reg_NAMES.BC); break;
                        case (0x02): LD_n_A(reg_NAMES.BC); break;
                        case (0x03): INC_nn(reg_NAMES.BC); break;
                        case (0x04): INC_n(reg_NAMES.B); break;
                        case (0x05): DEC_n(reg_NAMES.B); break;
                        case (0x06): LD_nn_n(reg_NAMES.B); break;
                        case (0x07): RLC_n(reg_NAMES.A); break;
                        case (0x08): LD_nn_SP(); break;
                        case (0x09): ADD_HL_n(reg_NAMES.BC); break;
                        case (0x0A): LD_A_n(reg_NAMES.BC); break;
                        case (0x0B): DEC_nn(reg_NAMES.BC); break;
                        case (0x0C): INC_n(reg_NAMES.C); break;
                        case (0x0D): DEC_n(reg_NAMES.C); break;
                        case (0x0E): LD_nn_n(reg_NAMES.C); break;
                        case (0x0F): RRC_n(reg_NAMES.A); break;

                        case (0x10): STOP(); changeCPUSpeed(); break;
                        case (0x11): LD_n_nn(reg_NAMES.DE); break;
                        case (0x12): LD_n_A(reg_NAMES.DE); break;
                        case (0x13): INC_nn(reg_NAMES.DE); break;
                        case (0x14): INC_n(reg_NAMES.D); break;
                        case (0x15): DEC_n(reg_NAMES.D); break;
                        case (0x16): LD_nn_n(reg_NAMES.D); break;
                        case (0x17): RL_n(reg_NAMES.A); break;
                        case (0x18): JR_n(); break;
                        case (0x19): ADD_HL_n(reg_NAMES.DE); break;
                        case (0x1A): LD_A_n(reg_NAMES.DE); break;
                        case (0x1B): DEC_nn(reg_NAMES.DE); break;
                        case (0x1C): INC_n(reg_NAMES.E); break;
                        case (0x1D): DEC_n(reg_NAMES.E); break;
                        case (0x1E): LD_nn_n(reg_NAMES.E); break;
                        case (0x1F): RR_n(reg_NAMES.A); break;

                        case (0x20): JR_CC_n(flag_NAME.Z, 0); break;
                        case (0x21): LD_n_nn(reg_NAMES.HL); break;
                        case (0x22): LDI_HL_A(); break;
                        case (0x23): INC_nn(reg_NAMES.HL); break;
                        case (0x24): INC_n(reg_NAMES.H); break;
                        case (0x25): DEC_n(reg_NAMES.H); break;
                        case (0x26): LD_nn_n(reg_NAMES.H); break;
                        case (0x27): DAA(); break;
                        case (0x28): JR_CC_n(flag_NAME.Z, 1); break;
                        case (0x29): ADD_HL_n(reg_NAMES.HL); break;
                        case (0x2A): LDI_A_HL(); break;
                        case (0x2B): DEC_nn(reg_NAMES.HL); break;
                        case (0x2C): INC_n(reg_NAMES.L); break;
                        case (0x2D): DEC_n(reg_NAMES.L); break;
                        case (0x2E): LD_nn_n(reg_NAMES.L); break;
                        case (0x2F): CPL(); break;

                        case (0x30): JR_CC_n(flag_NAME.C, 0); break;
                        case (0x31): LD_n_nn(reg_NAMES.SP); break;
                        case (0x32): LDD_HL_A(); break;
                        case (0x33): INC_nn(reg_NAMES.SP); break;
                        case (0x34): INC_n(reg_NAMES.HL); break;
                        case (0x35): DEC_n(reg_NAMES.HL); break;
                        case (0x36): LD_r_r(reg_NAMES.HL, reg_NAMES.INMEDIATE); break;
                        case (0x37): SFC(); break;
                        case (0x38): JR_CC_n(flag_NAME.C, 1); break;
                        case (0x39): ADD_HL_n(reg_NAMES.SP); break;
                        case (0x3A): LDD_A_HL(); break;
                        case (0x3B): DEC_nn(reg_NAMES.SP); break;
                        case (0x3C): INC_n(reg_NAMES.A); break;
                        case (0x3D): DEC_n(reg_NAMES.A); break;
                        case (0x3E): LD_A_n(reg_NAMES.INMEDIATE); break;
                        case (0x3F): CCF(); break;

                        case (0x40): LD_r_r(reg_NAMES.B, reg_NAMES.B); break;
                        case (0x41): LD_r_r(reg_NAMES.B, reg_NAMES.C); break;
                        case (0x42): LD_r_r(reg_NAMES.B, reg_NAMES.D); break;
                        case (0x43): LD_r_r(reg_NAMES.B, reg_NAMES.E); break;
                        case (0x44): LD_r_r(reg_NAMES.B, reg_NAMES.H); break;
                        case (0x45): LD_r_r(reg_NAMES.B, reg_NAMES.L); break;
                        case (0x46): LD_r_r(reg_NAMES.B, reg_NAMES.HL); break;
                        case (0x47): LD_n_A(reg_NAMES.B); break;
                        case (0x48): LD_r_r(reg_NAMES.C, reg_NAMES.B); break;
                        case (0x49): LD_r_r(reg_NAMES.C, reg_NAMES.C); break;
                        case (0x4A): LD_r_r(reg_NAMES.C, reg_NAMES.D); break;
                        case (0x4B): LD_r_r(reg_NAMES.C, reg_NAMES.E); break;
                        case (0x4C): LD_r_r(reg_NAMES.C, reg_NAMES.H); break;
                        case (0x4D): LD_r_r(reg_NAMES.C, reg_NAMES.L); break;
                        case (0x4E): LD_r_r(reg_NAMES.C, reg_NAMES.HL); break;
                        case (0x4F): LD_n_A(reg_NAMES.C); break;

                        case (0x50): LD_r_r(reg_NAMES.D, reg_NAMES.B); break;
                        case (0x51): LD_r_r(reg_NAMES.D, reg_NAMES.C); break;
                        case (0x52): LD_r_r(reg_NAMES.D, reg_NAMES.D); break;
                        case (0x53): LD_r_r(reg_NAMES.D, reg_NAMES.E); break;
                        case (0x54): LD_r_r(reg_NAMES.D, reg_NAMES.H); break;
                        case (0x55): LD_r_r(reg_NAMES.D, reg_NAMES.L); break;
                        case (0x56): LD_r_r(reg_NAMES.D, reg_NAMES.HL); break;
                        case (0x57): LD_n_A(reg_NAMES.D); break;
                        case (0x58): LD_r_r(reg_NAMES.E, reg_NAMES.B); break;
                        case (0x59): LD_r_r(reg_NAMES.E, reg_NAMES.C); break;
                        case (0x5A): LD_r_r(reg_NAMES.E, reg_NAMES.D); break;
                        case (0x5B): LD_r_r(reg_NAMES.E, reg_NAMES.E); break;
                        case (0x5C): LD_r_r(reg_NAMES.E, reg_NAMES.H); break;
                        case (0x5D): LD_r_r(reg_NAMES.E, reg_NAMES.L); break;
                        case (0x5E): LD_r_r(reg_NAMES.E, reg_NAMES.HL); break;
                        case (0x5F): LD_n_A(reg_NAMES.E); break;

                        case (0x60): LD_r_r(reg_NAMES.H, reg_NAMES.B); break;
                        case (0x61): LD_r_r(reg_NAMES.H, reg_NAMES.C); break;
                        case (0x62): LD_r_r(reg_NAMES.H, reg_NAMES.D); break;
                        case (0x63): LD_r_r(reg_NAMES.H, reg_NAMES.E); break;
                        case (0x64): LD_r_r(reg_NAMES.H, reg_NAMES.H); break;
                        case (0x65): LD_r_r(reg_NAMES.H, reg_NAMES.L); break;
                        case (0x66): LD_r_r(reg_NAMES.H, reg_NAMES.HL); break;
                        case (0x67): LD_n_A(reg_NAMES.H); break;
                        case (0x68): LD_r_r(reg_NAMES.L, reg_NAMES.B); break;
                        case (0x69): LD_r_r(reg_NAMES.L, reg_NAMES.C); break;
                        case (0x6A): LD_r_r(reg_NAMES.L, reg_NAMES.D); break;
                        case (0x6B): LD_r_r(reg_NAMES.L, reg_NAMES.E); break;
                        case (0x6C): LD_r_r(reg_NAMES.L, reg_NAMES.H); break;
                        case (0x6D): LD_r_r(reg_NAMES.L, reg_NAMES.L); break;
                        case (0x6E): LD_r_r(reg_NAMES.L, reg_NAMES.HL); break;
                        case (0x6F): LD_n_A(reg_NAMES.L); break;

                        case (0x70): LD_r_r(reg_NAMES.HL, reg_NAMES.B); break;
                        case (0x71): LD_r_r(reg_NAMES.HL, reg_NAMES.C); break;
                        case (0x72): LD_r_r(reg_NAMES.HL, reg_NAMES.D); break;
                        case (0x73): LD_r_r(reg_NAMES.HL, reg_NAMES.E); break;
                        case (0x74): LD_r_r(reg_NAMES.HL, reg_NAMES.H); break;
                        case (0x75): LD_r_r(reg_NAMES.HL, reg_NAMES.L); break;
                        case (0x76): HALT(); break;
                        case (0x77): LD_n_A(reg_NAMES.HL); break;
                        case (0x78): LD_A_n(reg_NAMES.B); break;
                        case (0x79): LD_A_n(reg_NAMES.C); break;
                        case (0x7A): LD_A_n(reg_NAMES.D); break;
                        case (0x7B): LD_A_n(reg_NAMES.E); break;
                        case (0x7C): LD_A_n(reg_NAMES.H); break;
                        case (0x7D): LD_A_n(reg_NAMES.L); break;
                        case (0x7E): LD_A_n(reg_NAMES.HL); break;
                        case (0x7F): LD_n_A(reg_NAMES.A); break;

                        case (0x80): ADD_A_n(reg_NAMES.B); break;
                        case (0x81): ADD_A_n(reg_NAMES.C); break;
                        case (0x82): ADD_A_n(reg_NAMES.D); break;
                        case (0x83): ADD_A_n(reg_NAMES.E); break;
                        case (0x84): ADD_A_n(reg_NAMES.H); break;
                        case (0x85): ADD_A_n(reg_NAMES.L); break;
                        case (0x86): ADD_A_n(reg_NAMES.HL); break;
                        case (0x87): ADD_A_n(reg_NAMES.A); break;
                        case (0x88): ADC_A_n(reg_NAMES.B); break;
                        case (0x89): ADC_A_n(reg_NAMES.C); break;
                        case (0x8A): ADC_A_n(reg_NAMES.D); break;
                        case (0x8B): ADC_A_n(reg_NAMES.E); break;
                        case (0x8C): ADC_A_n(reg_NAMES.H); break;
                        case (0x8D): ADC_A_n(reg_NAMES.L); break;
                        case (0x8E): ADC_A_n(reg_NAMES.HL); break;
                        case (0x8F): ADC_A_n(reg_NAMES.A); break;

                        case (0x90): SUB_n(reg_NAMES.B); break;
                        case (0x91): SUB_n(reg_NAMES.C); break;
                        case (0x92): SUB_n(reg_NAMES.D); break;
                        case (0x93): SUB_n(reg_NAMES.E); break;
                        case (0x94): SUB_n(reg_NAMES.H); break;
                        case (0x95): SUB_n(reg_NAMES.L); break;
                        case (0x96): SUB_n(reg_NAMES.HL); break;
                        case (0x97): SUB_n(reg_NAMES.A); break;
                        case (0x98): SBC_A_n(reg_NAMES.B); break;
                        case (0x99): SBC_A_n(reg_NAMES.C); break;
                        case (0x9A): SBC_A_n(reg_NAMES.D); break;
                        case (0x9B): SBC_A_n(reg_NAMES.E); break;
                        case (0x9C): SBC_A_n(reg_NAMES.H); break;
                        case (0x9D): SBC_A_n(reg_NAMES.L); break;
                        case (0x9E): SBC_A_n(reg_NAMES.HL); break;
                        case (0x9F): SBC_A_n(reg_NAMES.A); break;

                        case (0xA0): AND_n(reg_NAMES.B); break;
                        case (0xA1): AND_n(reg_NAMES.C); break;
                        case (0xA2): AND_n(reg_NAMES.D); break;
                        case (0xA3): AND_n(reg_NAMES.E); break;
                        case (0xA4): AND_n(reg_NAMES.H); break;
                        case (0xA5): AND_n(reg_NAMES.L); break;
                        case (0xA6): AND_n(reg_NAMES.HL); break;
                        case (0xA7): AND_n(reg_NAMES.A); break;
                        case (0xA8): XOR_n(reg_NAMES.B); break;
                        case (0xA9): XOR_n(reg_NAMES.C); break;
                        case (0xAA): XOR_n(reg_NAMES.D); break;
                        case (0xAB): XOR_n(reg_NAMES.E); break;
                        case (0xAC): XOR_n(reg_NAMES.H); break;
                        case (0xAD): XOR_n(reg_NAMES.L); break;
                        case (0xAE): XOR_n(reg_NAMES.HL); break;
                        case (0xAF): XOR_n(reg_NAMES.A); break;

                        case (0xB0): OR_n(reg_NAMES.B); break;
                        case (0xB1): OR_n(reg_NAMES.C); break;
                        case (0xB2): OR_n(reg_NAMES.D); break;
                        case (0xB3): OR_n(reg_NAMES.E); break;
                        case (0xB4): OR_n(reg_NAMES.H); break;
                        case (0xB5): OR_n(reg_NAMES.L); break;
                        case (0xB6): OR_n(reg_NAMES.HL); break;
                        case (0xB7): OR_n(reg_NAMES.A); break;
                        case (0xB8): CP_n(reg_NAMES.B); break;
                        case (0xB9): CP_n(reg_NAMES.C); break;
                        case (0xBA): CP_n(reg_NAMES.D); break;
                        case (0xBB): CP_n(reg_NAMES.E); break;
                        case (0xBC): CP_n(reg_NAMES.H); break;
                        case (0xBD): CP_n(reg_NAMES.L); break;
                        case (0xBE): CP_n(reg_NAMES.HL); break;
                        case (0xBF): CP_n(reg_NAMES.A); break;

                        case (0xC0): RET_cc(flag_NAME.Z, 0); break;
                        case (0xC1): POP_nn(reg_NAMES.BC); break;
                        case (0xC2): JP_cc_nn(flag_NAME.Z, 0); break;
                        case (0xC3): JP_nn(); break;
                        case (0xC4): CALL_cc_nn(flag_NAME.Z, 0); break;
                        case (0xC5): PUSH_nn(reg_NAMES.BC); break;
                        case (0xC6): ADD_A_n(reg_NAMES.INMEDIATE); break;
                        case (0xC7): RST_n(0x00); break;
                        case (0xC8): RET_cc(flag_NAME.Z, 1); break;
                        case (0xC9): RET(); break;
                        case (0xCA): JP_cc_nn(flag_NAME.Z, 1); break;
                        case (0xCB): OpCodeCB(); break;
                        case (0xCC): CALL_cc_nn(flag_NAME.Z, 1); break;
                        case (0xCD): CALL_nn(); break;
                        case (0xCE): ADC_A_n(reg_NAMES.INMEDIATE); break;
                        case (0xCF): RST_n(0x08); break;

                        case (0xD0): RET_cc(flag_NAME.C, 0); break;
                        case (0xD1): POP_nn(reg_NAMES.DE); break;
                        case (0xD2): JP_cc_nn(flag_NAME.C, 0); break;
                        case (0xD4): CALL_cc_nn(flag_NAME.C, 0); break;
                        case (0xD5): PUSH_nn(reg_NAMES.DE); break;
                        case (0xD6): SUB_n(reg_NAMES.INMEDIATE); break;
                        case (0xD7): RST_n(0x10); break;
                        case (0xD8): RET_cc(flag_NAME.C, 1); break;
                        case (0xD9): RETI(); break;
                        case (0xDA): JP_cc_nn(flag_NAME.C, 1); break;
                        case (0xDC): CALL_cc_nn(flag_NAME.C, 1); break;
                        case (0xDE): SBC_A_n(reg_NAMES.INMEDIATE); break;
                        case (0xDF): RST_n(0x18); break;

                        case (0xE0): LDH_n_A(); break;
                        case (0xE1): POP_nn(reg_NAMES.HL); break;
                        case (0xE2): LD_C_A(); break;
                        case (0xE5): PUSH_nn(reg_NAMES.HL); break;
                        case (0xE6): AND_n(reg_NAMES.INMEDIATE); break;
                        case (0xE7): RST_n(0x20); break;
                        case (0xE8): ADD_SP_n(); break;
                        case (0xE9): JP_HL(); break;
                        case (0xEA): LD_n_A(reg_NAMES.INMEDIATE16); break;
                        case (0xEE): XOR_n(reg_NAMES.INMEDIATE); break;
                        case (0xEF): RST_n(0x28); break;

                        case (0xF0): LDH_A_n(); break;
                        case (0xF1): POP_nn(reg_NAMES.AF); break;
                        case (0xF2): LD_A_C(); break;
                        case (0xF3): DI(); break;
                        case (0xF5): PUSH_nn(reg_NAMES.AF); break;
                        case (0xF6): OR_n(reg_NAMES.INMEDIATE); break;
                        case (0xF7): RST_n(0x30); break;
                        case (0xF8): LDHL_SP_n(); break;
                        case (0xF9): LD_SP_LH(); break;
                        case (0xFA): LD_A_n(reg_NAMES.INMEDIATE16); break;
                        case (0xFB): EI(); break;
                        case (0xFE): CP_n(reg_NAMES.INMEDIATE); break;
                        case (0xFF): RST_n(0x38); break;

                        default:
                            throw new IllegalOperationCPU("ERROR - OpCode invalid " + opCode);
                    }//END switch
                }// END IF halt

                if (registers.conditionalTaken)
                    registers.conditionalTaken = false;

                if (newInterrupt) {
                    clock_cycles += 20;
                    newInterrupt = false;
                }

                if (registers.halt) {
                    //clock_cycles += 4;
                    registers.halt = false;
                }

                int tmpcycles = clock_cycles;

                updateTimer(clock_cycles);
                updateStateLCD(clock_cycles);
                updateSerial(clock_cycles);
                interrupts();
/*
                if (sound != null)
                    sound.endOP();
                if (papu != null)
                    papu.doStep(clock_cycles);*/

                cycles += clock_cycles;

                clock_cycles -= tmpcycles;
                //call others sub-system
                if (audioCoreF != null)
                    audioCoreF.Tick();
            }
           /* if (soundSustem != null)
                soundSustem.getApu().endFarme();*/
            
            return clock_cycles;
        }

        public void interrupts() {
            if (!registers.IME)
                return;
            if (firstAfterEI) {
                firstAfterEI = false;
                return;
            }
            byte actives = (byte)(memory.readByte(Memory.IE) & memory.readByte(Memory.IF));

            if ((actives & 0x1F) == 0x00)
                return;

            registers.IME = false;
            registers.halt = false;
            PUSH_PC();
            newInterrupt = true;

            byte value = memory.readByte(Memory.IF);
            if ((actives & 0x01) == 0x01) { //Interrupcion V-Blank, la mas prioritaria
                registers.programCounter = 0x40;
                memory.writeByte(Memory.IF, (byte)(value & ~0x01), false);
                vBlankPending = false;
            } else if ((actives & 0x02) == 0x02) { //LCD STAT
                registers.programCounter = 0x48;
                memory.writeByte(Memory.IF, (byte)(value & ~0x02), false);
            } else if ((actives & 0x04) == 0x04) { // Timer
                registers.programCounter = 0x50;
                memory.writeByte(Memory.IF, (byte)(value & ~0x04), false);
            } else if ((actives & 0x08) == 0x08) { // Serie
                registers.programCounter = 0x58;
                memory.writeByte(Memory.IF, (byte)(value & ~0x08), false);
            } else if ((actives & 0x10) == 0x10) { // Cruceta
                registers.programCounter = 0x60;
                memory.writeByte(Memory.IF, (byte)(value & ~0x10), false);
            }
        }

        private void updateTimer(int cycles) {
            byte tac = memory.readByte(Memory.TAC);

            //Indica en numero de ciclos, las distintas velocidades del timer, 4096, 262144, 65536, 16384
            ushort[] overflow = { 1024, 16, 64, 256 };

            //Comprobamos si el BIT 2, de TAC esta activo, si es así el timer lo está tambien.
            if ((tac & 0x04) != 0) {
                clock_timer += cycles;

                //Valor del registro 
                ushort cyclesOverflow = overflow[(tac & 0x03)];

                while (clock_timer > cyclesOverflow) {
                    byte tima = memory.readByte(Memory.TIMA);

                    if (tima == 0xFF) {
                        memory.writeByte(Memory.TIMA, memory.readByte(Memory.TMA), false);
                        setInterruptionFlag(2);
                    } else {
                        tima += 1;
                        memory.writeByte(Memory.TIMA, tima, false);
                    }
                    clock_timer -= cyclesOverflow;
                }
            }

            cyclesDIV += cycles;

            while (cyclesDIV >= 256) {
                byte div = memory.readByte(Memory.DIV);
                div += 1;
                memory.writeByte(Memory.DIV, div, false);
                cyclesDIV -= 256;
            }
        }

        private void updateStateLCD(int cycles) {
            gpu.clock_lcd += cycles;
            byte lcdOn = memory.readByte(Memory.LCDC);
            if ((lcdOn & 0x80) != 0) {
                gpu.updateStateLCDOn();
            } else {
                if (gpu.clock_lcd > CyclesPerFrame * velovityMulti) {
                    onEndFrame();
                    gpu.clock_lcd -= CyclesPerFrame * velovityMulti;
                }
            }
        }

        int clock_serial = 0;
        int bitSerial = -1;

        private void updateSerial(int cycles) {
            byte serialControl = memory.readByte(Memory.SC);

            if ((serialControl & 0x80) != 0 && (serialControl & 0x01) != 0) {
                //clock_serial += cycles;
                clock_serial += cycles;

                if (bitSerial < 0) {
                    bitSerial = 0;
                    clock_serial = 0;
                    return;
                }

                if (clock_serial >= 512) {
                    if (bitSerial > 7) {
                        memory.writeByte(Memory.SC, (byte)(memory.readByte(Memory.SC) & 0x7F), false);
                        setInterruptionFlag(3);
                        bitSerial = -1;
                        return;
                    }
                    memory.writeByte(Memory.SC, (byte)(memory.readByte(Memory.SC) << 1), false);
                    memory.writeByte(Memory.SC, (byte)(memory.readByte(Memory.SC) | 0x01), false);

                    clock_serial -= 512;
                    bitSerial += 1;
                }
            }
        }

        public void setInterruptionFlag(int bit) {
            int mask = 1 << bit;
            byte r_if = memory.readByte(Memory.IF);
            memory.writeByte(Memory.IF, (byte)(r_if | mask), false);
            if ((memory.readByte(Memory.IF) & mask) != 0) {
                registers.halt = false;
            }
        }


        /// <summary>
        /// Extended OpCodes execution
        /// </summary>
        private void OpCodeCB() {
            byte opCode = memory.readByte((ushort)(registers.programCounter + 1));

            switch (opCode) {
                case (0x00): RLC_n(reg_NAMES.B); break;
                case (0x01): RLC_n(reg_NAMES.C); break;
                case (0x02): RLC_n(reg_NAMES.D); break;
                case (0x03): RLC_n(reg_NAMES.E); break;
                case (0x04): RLC_n(reg_NAMES.H); break;
                case (0x05): RLC_n(reg_NAMES.L); break;
                case (0x06): RLC_n(reg_NAMES.HL); break;
                case (0x07): RLC_n(reg_NAMES.A); break;
                case (0x08): RRC_n(reg_NAMES.B); break;
                case (0x09): RRC_n(reg_NAMES.C); break;
                case (0x0A): RRC_n(reg_NAMES.D); break;
                case (0x0B): RRC_n(reg_NAMES.E); break;
                case (0x0C): RRC_n(reg_NAMES.H); break;
                case (0x0D): RRC_n(reg_NAMES.L); break;
                case (0x0E): RRC_n(reg_NAMES.HL); break;
                case (0x0F): RRC_n(reg_NAMES.A); break;

                case (0x10): RL_n(reg_NAMES.B); break;
                case (0x11): RL_n(reg_NAMES.C); break;
                case (0x12): RL_n(reg_NAMES.D); break;
                case (0x13): RL_n(reg_NAMES.E); break;
                case (0x14): RL_n(reg_NAMES.H); break;
                case (0x15): RL_n(reg_NAMES.L); break;
                case (0x16): RL_n(reg_NAMES.HL); break;
                case (0x17): RL_n(reg_NAMES.A); break;
                case (0x18): RR_n(reg_NAMES.B); break;
                case (0x19): RR_n(reg_NAMES.C); break;
                case (0x1A): RR_n(reg_NAMES.D); break;
                case (0x1B): RR_n(reg_NAMES.E); break;
                case (0x1C): RR_n(reg_NAMES.H); break;
                case (0x1D): RR_n(reg_NAMES.L); break;
                case (0x1E): RR_n(reg_NAMES.HL); break;
                case (0x1F): RR_n(reg_NAMES.A); break;

                case (0x20): SLA_n(reg_NAMES.B); break;
                case (0x21): SLA_n(reg_NAMES.C); break;
                case (0x22): SLA_n(reg_NAMES.D); break;
                case (0x23): SLA_n(reg_NAMES.E); break;
                case (0x24): SLA_n(reg_NAMES.H); break;
                case (0x25): SLA_n(reg_NAMES.L); break;
                case (0x26): SLA_n(reg_NAMES.HL); break;
                case (0x27): SLA_n(reg_NAMES.A); break;
                case (0x28): SRA_n(reg_NAMES.B); break;
                case (0x29): SRA_n(reg_NAMES.C); break;
                case (0x2A): SRA_n(reg_NAMES.D); break;
                case (0x2B): SRA_n(reg_NAMES.E); break;
                case (0x2C): SRA_n(reg_NAMES.H); break;
                case (0x2D): SRA_n(reg_NAMES.L); break;
                case (0x2E): SRA_n(reg_NAMES.HL); break;
                case (0x2F): SRA_n(reg_NAMES.A); break;

                case (0x30): SWAP_n(reg_NAMES.B); break;
                case (0x31): SWAP_n(reg_NAMES.C); break;
                case (0x32): SWAP_n(reg_NAMES.D); break;
                case (0x33): SWAP_n(reg_NAMES.E); break;
                case (0x34): SWAP_n(reg_NAMES.H); break;
                case (0x35): SWAP_n(reg_NAMES.L); break;
                case (0x36): SWAP_n(reg_NAMES.HL); break;
                case (0x37): SWAP_n(reg_NAMES.A); break;
                case (0x38): SRL_n(reg_NAMES.B); break;
                case (0x39): SRL_n(reg_NAMES.C); break;
                case (0x3A): SRL_n(reg_NAMES.D); break;
                case (0x3B): SRL_n(reg_NAMES.E); break;
                case (0x3C): SRL_n(reg_NAMES.H); break;
                case (0x3D): SRL_n(reg_NAMES.L); break;
                case (0x3E): SRL_n(reg_NAMES.HL); break;
                case (0x3F): SRL_n(reg_NAMES.A); break;

                case (0x40): BIT_b_r(0, reg_NAMES.B); break;
                case (0x41): BIT_b_r(0, reg_NAMES.C); break;
                case (0x42): BIT_b_r(0, reg_NAMES.D); break;
                case (0x43): BIT_b_r(0, reg_NAMES.E); break;
                case (0x44): BIT_b_r(0, reg_NAMES.H); break;
                case (0x45): BIT_b_r(0, reg_NAMES.L); break;
                case (0x46): BIT_b_r(0, reg_NAMES.HL); break;
                case (0x47): BIT_b_r(0, reg_NAMES.A); break;
                case (0x48): BIT_b_r(1, reg_NAMES.B); break;
                case (0x49): BIT_b_r(1, reg_NAMES.C); break;
                case (0x4A): BIT_b_r(1, reg_NAMES.D); break;
                case (0x4B): BIT_b_r(1, reg_NAMES.E); break;
                case (0x4C): BIT_b_r(1, reg_NAMES.H); break;
                case (0x4D): BIT_b_r(1, reg_NAMES.L); break;
                case (0x4E): BIT_b_r(1, reg_NAMES.HL); break;
                case (0x4F): BIT_b_r(1, reg_NAMES.A); break;

                case (0x50): BIT_b_r(2, reg_NAMES.B); break;
                case (0x51): BIT_b_r(2, reg_NAMES.C); break;
                case (0x52): BIT_b_r(2, reg_NAMES.D); break;
                case (0x53): BIT_b_r(2, reg_NAMES.E); break;
                case (0x54): BIT_b_r(2, reg_NAMES.H); break;
                case (0x55): BIT_b_r(2, reg_NAMES.L); break;
                case (0x56): BIT_b_r(2, reg_NAMES.HL); break;
                case (0x57): BIT_b_r(2, reg_NAMES.A); break;
                case (0x58): BIT_b_r(3, reg_NAMES.B); break;
                case (0x59): BIT_b_r(3, reg_NAMES.C); break;
                case (0x5A): BIT_b_r(3, reg_NAMES.D); break;
                case (0x5B): BIT_b_r(3, reg_NAMES.E); break;
                case (0x5C): BIT_b_r(3, reg_NAMES.H); break;
                case (0x5D): BIT_b_r(3, reg_NAMES.L); break;
                case (0x5E): BIT_b_r(3, reg_NAMES.HL); break;
                case (0x5F): BIT_b_r(3, reg_NAMES.A); break;

                case (0x60): BIT_b_r(4, reg_NAMES.B); break;
                case (0x61): BIT_b_r(4, reg_NAMES.C); break;
                case (0x62): BIT_b_r(4, reg_NAMES.D); break;
                case (0x63): BIT_b_r(4, reg_NAMES.E); break;
                case (0x64): BIT_b_r(4, reg_NAMES.H); break;
                case (0x65): BIT_b_r(4, reg_NAMES.L); break;
                case (0x66): BIT_b_r(4, reg_NAMES.HL); break;
                case (0x67): BIT_b_r(4, reg_NAMES.A); break;
                case (0x68): BIT_b_r(5, reg_NAMES.B); break;
                case (0x69): BIT_b_r(5, reg_NAMES.C); break;
                case (0x6A): BIT_b_r(5, reg_NAMES.D); break;
                case (0x6B): BIT_b_r(5, reg_NAMES.E); break;
                case (0x6C): BIT_b_r(5, reg_NAMES.H); break;
                case (0x6D): BIT_b_r(5, reg_NAMES.L); break;
                case (0x6E): BIT_b_r(5, reg_NAMES.HL); break;
                case (0x6F): BIT_b_r(5, reg_NAMES.A); break;

                case (0x70): BIT_b_r(6, reg_NAMES.B); break;
                case (0x71): BIT_b_r(6, reg_NAMES.C); break;
                case (0x72): BIT_b_r(6, reg_NAMES.D); break;
                case (0x73): BIT_b_r(6, reg_NAMES.E); break;
                case (0x74): BIT_b_r(6, reg_NAMES.H); break;
                case (0x75): BIT_b_r(6, reg_NAMES.L); break;
                case (0x76): BIT_b_r(6, reg_NAMES.HL); break;
                case (0x77): BIT_b_r(6, reg_NAMES.A); break;
                case (0x78): BIT_b_r(7, reg_NAMES.B); break;
                case (0x79): BIT_b_r(7, reg_NAMES.C); break;
                case (0x7A): BIT_b_r(7, reg_NAMES.D); break;
                case (0x7B): BIT_b_r(7, reg_NAMES.E); break;
                case (0x7C): BIT_b_r(7, reg_NAMES.H); break;
                case (0x7D): BIT_b_r(7, reg_NAMES.L); break;
                case (0x7E): BIT_b_r(7, reg_NAMES.HL); break;
                case (0x7F): BIT_b_r(7, reg_NAMES.A); break;

                case (0x80): RES_b_r(0, reg_NAMES.B); break;
                case (0x81): RES_b_r(0, reg_NAMES.C); break;
                case (0x82): RES_b_r(0, reg_NAMES.D); break;
                case (0x83): RES_b_r(0, reg_NAMES.E); break;
                case (0x84): RES_b_r(0, reg_NAMES.H); break;
                case (0x85): RES_b_r(0, reg_NAMES.L); break;
                case (0x86): RES_b_r(0, reg_NAMES.HL); break;
                case (0x87): RES_b_r(0, reg_NAMES.A); break;
                case (0x88): RES_b_r(1, reg_NAMES.B); break;
                case (0x89): RES_b_r(1, reg_NAMES.C); break;
                case (0x8A): RES_b_r(1, reg_NAMES.D); break;
                case (0x8B): RES_b_r(1, reg_NAMES.E); break;
                case (0x8C): RES_b_r(1, reg_NAMES.H); break;
                case (0x8D): RES_b_r(1, reg_NAMES.L); break;
                case (0x8E): RES_b_r(1, reg_NAMES.HL); break;
                case (0x8F): RES_b_r(1, reg_NAMES.A); break;

                case (0x90): RES_b_r(2, reg_NAMES.B); break;
                case (0x91): RES_b_r(2, reg_NAMES.C); break;
                case (0x92): RES_b_r(2, reg_NAMES.D); break;
                case (0x93): RES_b_r(2, reg_NAMES.E); break;
                case (0x94): RES_b_r(2, reg_NAMES.H); break;
                case (0x95): RES_b_r(2, reg_NAMES.L); break;
                case (0x96): RES_b_r(2, reg_NAMES.HL); break;
                case (0x97): RES_b_r(2, reg_NAMES.A); break;
                case (0x98): RES_b_r(3, reg_NAMES.B); break;
                case (0x99): RES_b_r(3, reg_NAMES.C); break;
                case (0x9A): RES_b_r(3, reg_NAMES.D); break;
                case (0x9B): RES_b_r(3, reg_NAMES.E); break;
                case (0x9C): RES_b_r(3, reg_NAMES.H); break;
                case (0x9D): RES_b_r(3, reg_NAMES.L); break;
                case (0x9E): RES_b_r(3, reg_NAMES.HL); break;
                case (0x9F): RES_b_r(3, reg_NAMES.A); break;

                case (0xA0): RES_b_r(4, reg_NAMES.B); break;
                case (0xA1): RES_b_r(4, reg_NAMES.C); break;
                case (0xA2): RES_b_r(4, reg_NAMES.D); break;
                case (0xA3): RES_b_r(4, reg_NAMES.E); break;
                case (0xA4): RES_b_r(4, reg_NAMES.H); break;
                case (0xA5): RES_b_r(4, reg_NAMES.L); break;
                case (0xA6): RES_b_r(4, reg_NAMES.HL); break;
                case (0xA7): RES_b_r(4, reg_NAMES.A); break;
                case (0xA8): RES_b_r(5, reg_NAMES.B); break;
                case (0xA9): RES_b_r(5, reg_NAMES.C); break;
                case (0xAA): RES_b_r(5, reg_NAMES.D); break;
                case (0xAB): RES_b_r(5, reg_NAMES.E); break;
                case (0xAC): RES_b_r(5, reg_NAMES.H); break;
                case (0xAD): RES_b_r(5, reg_NAMES.L); break;
                case (0xAE): RES_b_r(5, reg_NAMES.HL); break;
                case (0xAF): RES_b_r(5, reg_NAMES.A); break;

                case (0xB0): RES_b_r(6, reg_NAMES.B); break;
                case (0xB1): RES_b_r(6, reg_NAMES.C); break;
                case (0xB2): RES_b_r(6, reg_NAMES.D); break;
                case (0xB3): RES_b_r(6, reg_NAMES.E); break;
                case (0xB4): RES_b_r(6, reg_NAMES.H); break;
                case (0xB5): RES_b_r(6, reg_NAMES.L); break;
                case (0xB6): RES_b_r(6, reg_NAMES.HL); break;
                case (0xB7): RES_b_r(6, reg_NAMES.A); break;
                case (0xB8): RES_b_r(7, reg_NAMES.B); break;
                case (0xB9): RES_b_r(7, reg_NAMES.C); break;
                case (0xBA): RES_b_r(7, reg_NAMES.D); break;
                case (0xBB): RES_b_r(7, reg_NAMES.E); break;
                case (0xBC): RES_b_r(7, reg_NAMES.H); break;
                case (0xBD): RES_b_r(7, reg_NAMES.L); break;
                case (0xBE): RES_b_r(7, reg_NAMES.HL); break;
                case (0xBF): RES_b_r(7, reg_NAMES.A); break;

                case (0xC0): SET_b_r(0, reg_NAMES.B); break;
                case (0xC1): SET_b_r(0, reg_NAMES.C); break;
                case (0xC2): SET_b_r(0, reg_NAMES.D); break;
                case (0xC3): SET_b_r(0, reg_NAMES.E); break;
                case (0xC4): SET_b_r(0, reg_NAMES.H); break;
                case (0xC5): SET_b_r(0, reg_NAMES.L); break;
                case (0xC6): SET_b_r(0, reg_NAMES.HL); break;
                case (0xC7): SET_b_r(0, reg_NAMES.A); break;
                case (0xC8): SET_b_r(1, reg_NAMES.B); break;
                case (0xC9): SET_b_r(1, reg_NAMES.C); break;
                case (0xCA): SET_b_r(1, reg_NAMES.D); break;
                case (0xCB): SET_b_r(1, reg_NAMES.E); break;
                case (0xCC): SET_b_r(1, reg_NAMES.H); break;
                case (0xCD): SET_b_r(1, reg_NAMES.L); break;
                case (0xCE): SET_b_r(1, reg_NAMES.HL); break;
                case (0xCF): SET_b_r(1, reg_NAMES.A); break;

                case (0xD0): SET_b_r(2, reg_NAMES.B); break;
                case (0xD1): SET_b_r(2, reg_NAMES.C); break;
                case (0xD2): SET_b_r(2, reg_NAMES.D); break;
                case (0xD3): SET_b_r(2, reg_NAMES.E); break;
                case (0xD4): SET_b_r(2, reg_NAMES.H); break;
                case (0xD5): SET_b_r(2, reg_NAMES.L); break;
                case (0xD6): SET_b_r(2, reg_NAMES.HL); break;
                case (0xD7): SET_b_r(2, reg_NAMES.A); break;
                case (0xD8): SET_b_r(3, reg_NAMES.B); break;
                case (0xD9): SET_b_r(3, reg_NAMES.C); break;
                case (0xDA): SET_b_r(3, reg_NAMES.D); break;
                case (0xDB): SET_b_r(3, reg_NAMES.E); break;
                case (0xDC): SET_b_r(3, reg_NAMES.H); break;
                case (0xDD): SET_b_r(3, reg_NAMES.L); break;
                case (0xDE): SET_b_r(3, reg_NAMES.HL); break;
                case (0xDF): SET_b_r(3, reg_NAMES.A); break;

                case (0xE0): SET_b_r(4, reg_NAMES.B); break;
                case (0xE1): SET_b_r(4, reg_NAMES.C); break;
                case (0xE2): SET_b_r(4, reg_NAMES.D); break;
                case (0xE3): SET_b_r(4, reg_NAMES.E); break;
                case (0xE4): SET_b_r(4, reg_NAMES.H); break;
                case (0xE5): SET_b_r(4, reg_NAMES.L); break;
                case (0xE6): SET_b_r(4, reg_NAMES.HL); break;
                case (0xE7): SET_b_r(4, reg_NAMES.A); break;
                case (0xE8): SET_b_r(5, reg_NAMES.B); break;
                case (0xE9): SET_b_r(5, reg_NAMES.C); break;
                case (0xEA): SET_b_r(5, reg_NAMES.D); break;
                case (0xEB): SET_b_r(5, reg_NAMES.E); break;
                case (0xEC): SET_b_r(5, reg_NAMES.H); break;
                case (0xED): SET_b_r(5, reg_NAMES.L); break;
                case (0xEE): SET_b_r(5, reg_NAMES.HL); break;
                case (0xEF): SET_b_r(5, reg_NAMES.A); break;

                case (0xF0): SET_b_r(6, reg_NAMES.B); break;
                case (0xF1): SET_b_r(6, reg_NAMES.C); break;
                case (0xF2): SET_b_r(6, reg_NAMES.D); break;
                case (0xF3): SET_b_r(6, reg_NAMES.E); break;
                case (0xF4): SET_b_r(6, reg_NAMES.H); break;
                case (0xF5): SET_b_r(6, reg_NAMES.L); break;
                case (0xF6): SET_b_r(6, reg_NAMES.HL); break;
                case (0xF7): SET_b_r(6, reg_NAMES.A); break;
                case (0xF8): SET_b_r(7, reg_NAMES.B); break;
                case (0xF9): SET_b_r(7, reg_NAMES.C); break;
                case (0xFA): SET_b_r(7, reg_NAMES.D); break;
                case (0xFB): SET_b_r(7, reg_NAMES.E); break;
                case (0xFC): SET_b_r(7, reg_NAMES.H); break;
                case (0xFD): SET_b_r(7, reg_NAMES.L); break;
                case (0xFE): SET_b_r(7, reg_NAMES.HL); break;
                case (0xFF): SET_b_r(7, reg_NAMES.A); break;

                default:
                    throw new IllegalOperationCPU("ERROR - Conditional OpCode invalid " + opCode);
            }
        }


        public void changeCPUSpeed() {
            if (gbcMode && (memory.readByte(Memory.KEY1) & 0x01) != 0) {
                // Se cambia a la velocidad contraria a la que estamos
                if ((memory.readByte(Memory.KEY1) & 0x80) != 0) {
                    // Velocidad normal
                    memory.writeByte(Memory.KEY1, 0, false);
                    changeDoubleSpeed(false);
                    gpu.changeDoubleSpeed(false);
                } else {
                    // Velocidad doble
                    memory.writeByte(Memory.KEY1, 0x80, false);
                    changeDoubleSpeed(true);
                    gpu.changeDoubleSpeed(true);
                }
            }
        }

        //--INSTRUCCIONES--//

        public byte getInmediateValue8bit() {
            return memory.readByte((ushort)(registers.programCounter + 1));
        }

        public ushort getInmediateValue16bit() {
            return memory.readWord((ushort)(registers.programCounter + 1));
        }

        //Instructions

        private void NOP() {
            clock_cycles += 4;
            registers.programCounter += 1;
        }


        //LD, Load and Store

        //8 bit LD

        /// <summary>
        /// Load inmediate value to register
        /// </summary>
        /// <param name="register">Name of the register</param>
        private void LD_nn_n(reg_NAMES register) {
            registers.setRegisterSimple(register, getInmediateValue8bit());
            clock_cycles += 8;
            registers.programCounter += 2;
        }

        /// <summary>
        /// Load value of register2 on register1 or an inmediate value into register
        /// </summary>
        /// <param name="register1">Name of register1</param>
        /// <param name="register2">Name of register2</param>
        private void LD_r_r(reg_NAMES register1, reg_NAMES register2) {
            byte length = 1;
            byte cycles = 4;
            if (register1 == reg_NAMES.HL) {
                if (register2 == reg_NAMES.INMEDIATE) {
                    ushort address = registers.getRegisterDouble(register1);
                    memory.writeByte(address, getInmediateValue8bit());
                    length = 2;
                    cycles = 12;
                } else if (register2 > reg_NAMES.L) {
                    throw new IllegalOperationCPU("ERROR - No se puede usar LD_r_r para escribir entre direcciones de memoria");
                } else {
                    //Guardar contenido de register2 en la direccion contenida en HL
                    byte value = registers.getRegisterSimple(register2);
                    ushort address = registers.getRegisterDouble(register1);
                    memory.writeByte(address, value);
                    cycles = 8;
                }
            } else if (register1 <= reg_NAMES.L) {
                if (register2 == reg_NAMES.HL) {
                    //Guardar la direccion a la que apunta HL en register1
                    byte value = memory.readByte(registers.getRegisterDouble(register2));
                    registers.setRegisterSimple(register1, value);
                    cycles = 8;
                } else {
                    //Guardar registro simple en simple
                    registers.setRegisterSimple(register1, registers.getRegisterSimple(register2));
                }
            }
            clock_cycles += cycles;
            registers.programCounter += length;
        }

        /// <summary>
        /// Load register a with value of register or inmediate
        /// </summary>
        /// <param name="register">The register to put into a</param>
        private void LD_A_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;
            switch (register) {
                case reg_NAMES.INMEDIATE:
                    value = getInmediateValue8bit();
                    length = 2;
                    cycles = 8;
                    break;
                case reg_NAMES.INMEDIATE16:
                    ushort address = getInmediateValue16bit();
                    value = memory.readByte(address);
                    length = 3;
                    cycles = 16;
                    break;
                case reg_NAMES.BC:
                case reg_NAMES.DE:
                case reg_NAMES.HL:
                    value = memory.readByte(registers.getRegisterDouble(register));
                    cycles = 8;
                    break;
                default:
                    value = registers.getRegisterSimple(register);
                    break;
            }
            registers.setRegisterSimple(reg_NAMES.A, value);
            clock_cycles += cycles;
            registers.programCounter += length;
        }

        /// <summary>
        /// Load address (in double register) or register n with value on a
        /// </summary>
        /// <param name="register">The register to load with a value</param>
        public void LD_n_A(reg_NAMES register) {
            byte cycles = 4, length = 1;
            byte value = registers.getRegisterSimple(reg_NAMES.A);
            ushort address;
            switch (register) {
                case reg_NAMES.INMEDIATE16:
                    address = getInmediateValue16bit();
                    memory.writeByte(address, value);
                    length = 3;
                    cycles = 16;
                    break;
                case reg_NAMES.BC:
                case reg_NAMES.DE:
                case reg_NAMES.HL:
                    address = registers.getRegisterDouble(register);
                    memory.writeByte(address, value);
                    cycles = 8;
                    break;
                default:
                    registers.setRegisterSimple(register, value);
                    break;
            }
            clock_cycles += cycles;
            registers.programCounter += length;
        }

        /// <summary>
        /// Load in a the vale in address 0xFF00 plus value in register c
        /// </summary>
        public void LD_A_C() {
            byte c = registers.getRegisterSimple(reg_NAMES.C);
            byte value = memory.readByte((ushort)(0xFF00 + c));
            registers.setRegisterSimple(reg_NAMES.A, value);
            clock_cycles += 8;
            registers.programCounter += 1;
        }

        /// <summary>
        /// Load address 0xFF00 plus value in register c with value in a
        /// </summary>
        public void LD_C_A() {
            byte value = registers.getRegisterSimple(reg_NAMES.A);
            byte c = registers.getRegisterSimple(reg_NAMES.C);
            memory.writeByte((ushort)(0xFF00 + c), value);
            clock_cycles += 8;
            registers.programCounter += 1;
        }


        /// <summary>
        /// Load a with value in address on HL and decrement HL
        /// </summary>
        public void LDD_A_HL() {
            byte value = memory.readByte(registers.getRegisterDouble(reg_NAMES.HL));
            registers.setRegisterSimple(reg_NAMES.A, value);
            ushort hl = registers.getRegisterDouble(reg_NAMES.HL);
            hl -= 1;
            registers.setRegisterDouble(reg_NAMES.HL, hl);
            clock_cycles += 8;
            registers.programCounter += 1;
        }


        /// <summary>
        /// Load address on HL with value of a and decrement HL
        /// </summary>
        public void LDD_HL_A() {
            ushort hl = registers.getRegisterDouble(reg_NAMES.HL);
            memory.writeByte(hl, registers.getRegisterSimple(reg_NAMES.A));
            hl -= 1;
            registers.setRegisterDouble(reg_NAMES.HL, hl);
            clock_cycles += 8;
            registers.programCounter += 1;
        }

        /// <summary>
        /// Load a with value in address on HL and increment HL
        /// </summary>
        public void LDI_A_HL() {
            byte value = memory.readByte(registers.getRegisterDouble(reg_NAMES.HL));
            registers.setRegisterSimple(reg_NAMES.A, value);
            ushort hl = registers.getRegisterDouble(reg_NAMES.HL);
            hl += 1;
            registers.setRegisterDouble(reg_NAMES.HL, hl);
            clock_cycles += 8;
            registers.programCounter += 1;
        }

        /// <summary>
        /// Load address on HL with value of a and inmcrement HL
        /// </summary>
        public void LDI_HL_A() {
            ushort hl = registers.getRegisterDouble(reg_NAMES.HL);
            memory.writeByte(hl, registers.getRegisterSimple(reg_NAMES.A));
            hl += 1;
            registers.setRegisterDouble(reg_NAMES.HL, hl);
            clock_cycles += 8;
            registers.programCounter += 1;
        }


        /// <summary>
        /// Put a into memory address 0xFF00 plus inmediate 1 byte value
        /// </summary>
        public void LDH_n_A() {
            byte inmediate = getInmediateValue8bit();
            ushort address = (ushort)(0xFF00 + inmediate);
            memory.writeByte(address, registers.getRegisterSimple(reg_NAMES.A));
            clock_cycles += 12;
            registers.programCounter += 2;
        }

        /// <summary>
        /// Put a into a the value of memory address 0xFF00 plus inmediate 1 byte value
        /// </summary>
        public void LDH_A_n() {
            byte inmediate = getInmediateValue8bit();
            ushort address = (ushort)(0xFF00 + inmediate);
            registers.setRegisterSimple(reg_NAMES.A, memory.readByte(address));
            clock_cycles += 12;
            registers.programCounter += 2;
        }

        //16bit LD

        /// <summary>
        /// Put an inmediate 16 bits value into register
        /// </summary>
        /// <param name="register">The name of register</param>
        public void LD_n_nn(reg_NAMES register) {
            ushort inmediate = getInmediateValue16bit();
            registers.setRegisterDouble(register, inmediate);
            clock_cycles += 12;
            registers.programCounter += 3;
        }

        /// <summary>
        /// Put register HL value into SP (stackpointer)
        /// </summary>
        public void LD_SP_LH() {
            ushort value = registers.getRegisterDouble(reg_NAMES.HL);
            registers.setRegisterDouble(reg_NAMES.SP, value);
            clock_cycles += 8;
            registers.programCounter += 1;
        }

        /// <summary>
        /// Put SP + n (8 bit inmediate value) into HL
        /// The operation will modify flags
        /// </summary>
        public void LDHL_SP_n() {
            byte inmediate = getInmediateValue8bit();
            ushort sp = registers.getRegisterDouble(reg_NAMES.SP);

            byte h = ((sp & 0x0F) + (inmediate & 0x0F)) > 0x0F ? (byte)1 : (byte)0;
            byte c = ((sp & 0xFF) + (inmediate & 0xFF)) > 0xFF ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, h);
            registers.setFlag(flag_NAME.C, c);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.Z, 0);

            registers.setRegisterDouble(reg_NAMES.HL, (ushort)(sp + (sbyte)inmediate));

            clock_cycles += 12;
            registers.programCounter += 2;
        }


        /// <summary>
        /// Put SP at address n ( 16 bit inmediate value)
        /// </summary>
        public void LD_nn_SP() {
            ushort inmediate = getInmediateValue16bit();
            memory.writeWord(inmediate, registers.getRegisterDouble(reg_NAMES.SP));
            clock_cycles += 20;
            registers.programCounter += 3;
        }

        /// <summary>
        /// Push register pair (double register) onto stack, decrement stackpointer by 2
        /// </summary>
        /// <param name="register">The name of register</param>
        public void PUSH_nn(reg_NAMES register) {
            ushort value = registers.getRegisterDouble(register);
            registers.stackPointer -= 1;
            memory.writeByte(registers.stackPointer, (byte)((value & 0xFF00) >> 8));
            registers.stackPointer -= 1;
            memory.writeByte(registers.stackPointer, (byte)(value & 0x00FF));
            clock_cycles += 16;
            registers.programCounter += 1;
        }


        /// <summary>
        /// Pop two bytes off stack into register pair.
        /// Increment Stack Pointer(SP) twice.
        /// </summary>
        /// <param name="register">The name of register</param>
        public void POP_nn(reg_NAMES register) {
            ushort value = memory.readWord(registers.stackPointer);
            registers.setRegisterDouble(register, value);
            registers.stackPointer += 2;
            clock_cycles += 12;
            registers.programCounter += 1;
        }

        // ALU

        // 8 bit

        /// <summary>
        /// ADD value of register to value of a
        /// </summary>
        /// <param name="register">The name of register</param>
        public void ADD_A_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;

            switch (register) {
                case reg_NAMES.INMEDIATE:
                    value = getInmediateValue8bit();
                    cycles = 8;
                    length = 2;
                    break;
                case reg_NAMES.HL:
                    value = memory.readByte(registers.getRegisterDouble(reg_NAMES.HL));
                    cycles = 8;
                    break;
                default:
                    value = registers.getRegisterSimple(register);
                    break;
            }

            byte a = registers.getRegisterSimple(reg_NAMES.A);
            int result = a + value;

            byte h = ((a & 0x0F) + (value & 0x0F)) > 0x0F ? (byte)1 : (byte)0;
            byte c = result > 0xFF ? (byte)1 : (byte)0;
            byte z = (result & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, h);
            registers.setFlag(flag_NAME.C, c);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.Z, z);

            registers.setRegisterSimple(reg_NAMES.A, (byte)(result & 0xFF));

            clock_cycles += cycles;
            registers.programCounter += length;
        }


        /// <summary>
        /// ADD value of register to value of a plus value of flag C (carry)
        /// </summary>
        /// <param name="register">The name of register</param>
        public void ADC_A_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;

            switch (register) {
                case reg_NAMES.INMEDIATE:
                    value = getInmediateValue8bit();
                    cycles = 8;
                    length = 2;
                    break;
                case reg_NAMES.HL:
                    value = memory.readByte(registers.getRegisterDouble(reg_NAMES.HL));
                    cycles = 8;
                    break;
                default:
                    value = registers.getRegisterSimple(register);
                    break;
            }

            byte a = registers.getRegisterSimple(reg_NAMES.A);
            int result = a + value + registers.getFlag(flag_NAME.C);

            byte h = ((a & 0x0F) + (value & 0x0F) + registers.getFlag(flag_NAME.C)) > 0x0F ? (byte)1 : (byte)0;
            byte c = result > 0xFF ? (byte)1 : (byte)0;
            byte z = (result & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, h);
            registers.setFlag(flag_NAME.C, c);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.Z, z);

            registers.setRegisterSimple(reg_NAMES.A, (byte)(result & 0xFF));

            clock_cycles += cycles;
            registers.programCounter += length;
        }


        /// <summary>
        /// Subtract register from A.
        /// </summary>
        /// <param name="register">The name of register</param>
        public void SUB_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;

            switch (register) {
                case reg_NAMES.INMEDIATE:
                    value = getInmediateValue8bit();
                    cycles = 8;
                    length = 2;
                    break;
                case reg_NAMES.HL:
                    value = memory.readByte(registers.getRegisterDouble(reg_NAMES.HL));
                    cycles = 8;
                    break;
                default:
                    value = registers.getRegisterSimple(register);
                    break;
            }

            byte a = registers.getRegisterSimple(reg_NAMES.A);
            int result = a - value;

            byte h = ((a & 0x0F) < (value & 0x0F)) ? (byte)1 : (byte)0;
            byte c = a < value ? (byte)1 : (byte)0;
            byte z = (result & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, h);
            registers.setFlag(flag_NAME.C, c);
            registers.setFlag(flag_NAME.N, 1);
            registers.setFlag(flag_NAME.Z, z);

            registers.setRegisterSimple(reg_NAMES.A, (byte)(result & 0xFF));

            clock_cycles += cycles;
            registers.programCounter += length;
        }

        /// <summary>
        /// Subtract register plus flag C (carry) from A.
        /// </summary>
        /// <param name="register">The name of register</param>
        public void SBC_A_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;

            switch (register) {
                case reg_NAMES.INMEDIATE:
                    value = getInmediateValue8bit();
                    cycles = 8;
                    length = 2;
                    break;
                case reg_NAMES.HL:
                    value = memory.readByte(registers.getRegisterDouble(reg_NAMES.HL));
                    cycles = 8;
                    break;
                default:
                    value = registers.getRegisterSimple(register);
                    break;
            }

            byte a = registers.getRegisterSimple(reg_NAMES.A);
            int accum = (value + registers.getFlag(flag_NAME.C));
            int result = a - accum;

            byte h;
            if ((a & 0x0F) < (value & 0x0F))
                h = 1;
            else if ((a & 0x0F) < (accum & 0x0F))
                h = 1;
            else if (((a & 0x0F) == (value & 0x0F)) && ((value & 0x0F) == 0x0F) && (registers.getFlag(flag_NAME.C) == 1))
                h = 1;
            else
                h = 0;
            byte c;
            if (a < value) {
                c = 1;
            } else if (a < accum) {
                c = 1;
            } else if ((a == value) && ((value & 0xFF) == 0xFF) && (registers.getFlag(flag_NAME.C) == 1)) {
                c = 1;
            } else {
                c = 0;
            }
            //c = a < value ? (byte)1 : (byte)0;
            byte z = (result & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, h);
            registers.setFlag(flag_NAME.C, c);
            registers.setFlag(flag_NAME.N, 1);
            registers.setFlag(flag_NAME.Z, z);

            registers.setRegisterSimple(reg_NAMES.A, (byte)(result & 0xFF));

            clock_cycles += cycles;
            registers.programCounter += length;
        }


        /// <summary>
        /// Logically AND n with A, result in A.
        /// </summary>
        /// <param name="register">The name of register</param>
        public void AND_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;

            switch (register) {
                case reg_NAMES.INMEDIATE:
                    value = getInmediateValue8bit();
                    cycles = 8;
                    length = 2;
                    break;
                case reg_NAMES.HL:
                    value = memory.readByte(registers.getRegisterDouble(reg_NAMES.HL));
                    cycles = 8;
                    break;
                default:
                    value = registers.getRegisterSimple(register);
                    break;
            }

            byte result = (byte)((registers.getRegisterSimple(reg_NAMES.A) & value) & 0xFF);

            byte z = (result & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, 1);
            registers.setFlag(flag_NAME.C, 0);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.Z, z);

            registers.setRegisterSimple(reg_NAMES.A, result);

            clock_cycles += cycles;
            registers.programCounter += length;
        }

        /// <summary>
        /// Logically OR n with A, result in A.
        /// </summary>
        /// <param name="register">The name of register</param>
        public void OR_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;

            switch (register) {
                case reg_NAMES.INMEDIATE:
                    value = getInmediateValue8bit();
                    cycles = 8;
                    length = 2;
                    break;
                case reg_NAMES.HL:
                    value = memory.readByte(registers.getRegisterDouble(reg_NAMES.HL));
                    cycles = 8;
                    break;
                default:
                    value = registers.getRegisterSimple(register);
                    break;
            }

            byte result = (byte)((registers.getRegisterSimple(reg_NAMES.A) | value) & 0xFF);

            byte z = (result & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, 0);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.Z, z);

            registers.setRegisterSimple(reg_NAMES.A, result);

            clock_cycles += cycles;
            registers.programCounter += length;
        }

        /// <summary>
        /// Logically XOR n with A, result in A.
        /// </summary>
        /// <param name="register">The name of register</param>
        public void XOR_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;

            switch (register) {
                case reg_NAMES.INMEDIATE:
                    value = getInmediateValue8bit();
                    cycles = 8;
                    length = 2;
                    break;
                case reg_NAMES.HL:
                    value = memory.readByte(registers.getRegisterDouble(reg_NAMES.HL));
                    cycles = 8;
                    break;
                default:
                    value = registers.getRegisterSimple(register);
                    break;
            }

            byte result = (byte)((registers.getRegisterSimple(reg_NAMES.A) ^ value) & 0xFF);

            byte z = (result & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, 0);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.Z, z);

            registers.setRegisterSimple(reg_NAMES.A, result);

            clock_cycles += cycles;
            registers.programCounter += length;
        }

        /// <summary>
        /// Compare A with n. This is basically an A - n subtraction instruction but the results are thrown away.
        /// </summary>
        /// <param name="register">The name of register</param>
        public void CP_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;

            switch (register) {
                case reg_NAMES.INMEDIATE:
                    value = getInmediateValue8bit();
                    cycles = 8;
                    length = 2;
                    break;
                case reg_NAMES.HL:
                    value = memory.readByte(registers.getRegisterDouble(reg_NAMES.HL));
                    cycles = 8;
                    //length = 2;
                    break;
                default:
                    value = registers.getRegisterSimple(register);
                    break;
            }

            byte a = registers.getRegisterSimple(reg_NAMES.A);
            int result = a - value;

            byte h = ((a & 0x0F) < (value & 0x0F)) ? (byte)1 : (byte)0;
            byte c = a < value ? (byte)1 : (byte)0;
            byte z = (result & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, h);
            registers.setFlag(flag_NAME.C, c);
            registers.setFlag(flag_NAME.N, 1);
            registers.setFlag(flag_NAME.Z, z);

            clock_cycles += cycles;
            registers.programCounter += length;
        }

        /// <summary>
        /// Increment register n.
        /// </summary>
        /// <param name="register">The register name</param>
        public void INC_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;

            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                value += 1;
                memory.writeByte(address, value);
                cycles = 12;
            } else {
                value = registers.getRegisterSimple(register);
                value += 1;
                registers.setRegisterSimple(register, value);
            }

            byte h = ((value-1) & 0x0F) == 0x0F ? (byte)1 : (byte)0;
            byte z = (value & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, h);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.Z, z);

            clock_cycles += cycles;
            registers.programCounter += length;
        }


        /// <summary>
        /// Decrement register n.
        /// </summary>
        /// <param name="register">The register name</param>
        public void DEC_n(reg_NAMES register) {
            byte value, cycles = 4, length = 1;

            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                value -= 1;
                memory.writeByte(address, value);
                cycles = 12;
            } else {
                value = registers.getRegisterSimple(register);
                value -= 1;
                registers.setRegisterSimple(register, value);
            }

            byte h = (((value+1) > 0x0F) && (value == 0x0F)) ? (byte)1 : (byte)0;
            byte z = (value & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, h);
            registers.setFlag(flag_NAME.N, 1);
            registers.setFlag(flag_NAME.Z, z);

            clock_cycles += cycles;
            registers.programCounter += length;
        }

        // 16 bit Arithmetic

        /// <summary>
        /// Add n (16 bits register) to HL.
        /// </summary>
        /// <param name="register">The register name</param>
        public void ADD_HL_n(reg_NAMES register) {
            ushort value, hl;
            int result;

            value = registers.getRegisterDouble(register);
            hl = registers.getRegisterDouble(reg_NAMES.HL);

            result = value + hl;

            byte h = ((hl & 0x0FFF) + (value & 0x0FFF)) > 0x0FFF ? (byte)1 : (byte)0;
            byte c = result > 0xFFFF ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.H, h);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.C, c);

            registers.setRegisterDouble(reg_NAMES.HL, (ushort)(result & 0xFFFF));

            clock_cycles += 8;
            registers.programCounter += 1;
        }


        /// <summary>
        /// Add n to Stack Pointer (SP).
        /// </summary>
        public void ADD_SP_n() {

            byte inmediate = getInmediateValue8bit();
            ushort sp = registers.stackPointer;

            byte h = ((sp & 0x0F) + (inmediate & 0x0F)) > 0x0F ? (byte)1 : (byte)0;
            byte c = ((sp & 0xFF) + (inmediate & 0xFF)) > 0xFF ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.Z, 0);
            registers.setFlag(flag_NAME.H, h);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.C, c);

            int aux = registers.stackPointer + (sbyte)inmediate;

            registers.stackPointer = (ushort)aux;

            clock_cycles += 16;
            registers.programCounter += 2;
        }


        /// <summary>
        /// Increment register nn (16 bit register).
        /// </summary>
        /// <param name="register">The register name</param>
        public void INC_nn(reg_NAMES register) {
            ushort value;

            value = registers.getRegisterDouble(register);
            value += 1;
            registers.setRegisterDouble(register, value);

            clock_cycles += 8;
            registers.programCounter += 1;
        }

        /// <summary>
        /// Decrement register nn (16 bit register).
        /// </summary>
        /// <param name="register">The register name</param>
        public void DEC_nn(reg_NAMES register) {
            ushort value;

            value = registers.getRegisterDouble(register);
            value -= 1;
            registers.setRegisterDouble(register, value);

            clock_cycles += 8;
            registers.programCounter += 1;
        }

        // Others


        /// <summary>
        /// Swap upper & lower nibles of n.
        /// </summary>
        /// <param name="register">The register name</param>
        public void SWAP_n(reg_NAMES register) {
            byte value, cycles = 8;

            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                value = (byte)(((value & 0x0F) << 4 )| ((value & 0xF0) >> 4));
                memory.writeByte(address, value);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
                value = (byte)(((value & 0x0F) << 4) | ((value & 0xF0) >> 4));
                registers.setRegisterSimple(register, value);
            }
            byte z = (value & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.Z, z);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.C, 0);

            clock_cycles += cycles;
            if (currentOP == 0xCB)
                registers.programCounter += 2;
            else
                registers.programCounter += 1;
        }


        /// <summary>
        /// Decimal adjust register A. This instruction adjusts register A so that the correct
        /// representation of Binary Coded Decimal(BCD) is obtained.
        /// </summary>
        public void DAA() {
            /*
	 http://www.emutalk.net/showthread.php?t=41525&page=108
	 
	 Detailed info DAA
	 Instruction Format:
	 OPCODE                    CYCLES
	 --------------------------------
	 27h                       4
	 
	 
	 Description:
	 This instruction conditionally adjusts the accumulator for BCD addition
	 and subtraction operations. For addition (ADD, ADC, INC) or subtraction
	 (SUB, SBC, DEC, NEC), the following table indicates the operation performed:
	 
	 --------------------------------------------------------------------------------
	 |           | C Flag  | HEX value in | H Flag | HEX value in | Number  | C flag|
	 | Operation | Before  | upper digit  | Before | lower digit  | added   | After |
	 |           | DAA     | (bit 7-4)    | DAA    | (bit 3-0)    | to byte | DAA   |
	 |------------------------------------------------------------------------------|
	 |           |    0    |     0-9      |   0    |     0-9      |   00    |   0   |
	 |   ADD     |    0    |     0-8      |   0    |     A-F      |   06    |   0   |
	 |           |    0    |     0-9      |   1    |     0-3      |   06    |   0   |
	 |   ADC     |    0    |     A-F      |   0    |     0-9      |   60    |   1   |
	 |           |    0    |     9-F      |   0    |     A-F      |   66    |   1   |
	 |   INC     |    0    |     A-F      |   1    |     0-3      |   66    |   1   |
	 |           |    1    |     0-2      |   0    |     0-9      |   60    |   1   |
	 |           |    1    |     0-2      |   0    |     A-F      |   66    |   1   |
	 |           |    1    |     0-3      |   1    |     0-3      |   66    |   1   |
	 |------------------------------------------------------------------------------|
	 |   SUB     |    0    |     0-9      |   0    |     0-9      |   00    |   0   |
	 |   SBC     |    0    |     0-8      |   1    |     6-F      |   FA    |   0   |
	 |   DEC     |    1    |     7-F      |   0    |     0-9      |   A0    |   1   |
	 |   NEG     |    1    |     6-F      |   1    |     6-F      |   9A    |   1   |
	 |------------------------------------------------------------------------------|
	 
	 
	 Flags:
	 C:   See instruction.
	 N:   Unaffected.
	 P/V: Set if Acc. is even parity after operation, reset otherwise.
	 H:   See instruction.
	 Z:   Set if Acc. is Zero after operation, reset otherwise.
	 S:   Set if most significant bit of Acc. is 1 after operation, reset otherwise.
	 
	 Example:
	 
	 If an addition operation is performed between 15 (BCD) and 27 (BCD), simple decimal
	 arithmetic gives this result:
	 
	 15
	 +27
	 ----
	 42
	 
	 But when the binary representations are added in the Accumulator according to
	 standard binary arithmetic:
	 
	 0001 0101  15
	 +0010 0111  27
	 ---------------
	 0011 1100  3C
	 
	 The sum is ambiguous. The DAA instruction adjusts this result so that correct
	 BCD representation is obtained:
	 
	 0011 1100  3C result
	 +0000 0110  06 +error
	 ---------------
	 0100 0010  42 Correct BCD!
	*/

            int a = registers.getRegisterSimple(reg_NAMES.A);
            int upperNibble = a >> 4;
            int lowerNibble = a & 0x0F;

            byte c = 0;
            if (registers.getFlag(flag_NAME.N) == 0) {
                if (registers.getFlag(flag_NAME.C) == 0) {
                    if (lowerNibble > 9) {
                        if (upperNibble >= 9) {
                            c = 1;
                        } 
                    } else {
                        if (upperNibble > 9) {
                            c = 1;
                        }                     }
                } else {
                    c = 1;
                }
            } else {
                if (registers.getFlag(flag_NAME.C) != 0) {
                    c = 1;
                }
            }

            if (registers.getFlag(flag_NAME.N) == 0) {
                if (registers.getFlag(flag_NAME.H) != 0 || ((a & 0xF) > 9))
                    a += 0x06;
                if (registers.getFlag(flag_NAME.C) != 0 || (a > 0x9F))
                    a += 0x60;
            } else {
                if (registers.getFlag(flag_NAME.H) != 0)
                    a = (a - 6) & 0xFF;
                if (registers.getFlag(flag_NAME.C) != 0)
                    a -= 0x60;
            }

            byte z = (a & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.Z, z);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, c);

            registers.setRegisterSimple(reg_NAMES.A, (byte)(a & 0xFF));

            clock_cycles += 4;
            registers.programCounter += 1;
        }


        /// <summary>
        /// Complement A register. (Flip all bits.)
        /// </summary>
        public void CPL() {

            byte value = registers.getRegisterSimple(reg_NAMES.A);
            registers.setRegisterSimple(reg_NAMES.A, (byte)~value);

            registers.setFlag(flag_NAME.N, 1);
            registers.setFlag(flag_NAME.H, 1);

            clock_cycles += 4;
            registers.programCounter += 1;
        }

        /// <summary>
        /// Complement carry flag. If C flag is set, then reset it.
        /// If C flag is reset, then set it.
        /// </summary>
        public void CCF() {
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            byte c = registers.getFlag(flag_NAME.C);
            c = c == 0 ? (byte)1 : (byte)0;
            registers.setFlag(flag_NAME.C, c);
            clock_cycles += 4;
            registers.programCounter += 1;
        }

        /// <summary>
        /// Set Carry flag.
        /// </summary>
        public void SFC() {
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, 1);
            clock_cycles += 4;
            registers.programCounter += 1;
        }

        /// <summary>
        /// Power down CPU until an interrupt occurs. Use this when ever possible to reduce energy consumption.
        /// </summary>
        public void HALT() {
            registers.halt = true;
            clock_cycles += 4;
            registers.programCounter += 1;
        }

        /// <summary>
        /// Halt CPU & LCD display until button pressed.
        /// (No necesario en un emulador)
        /// </summary>
        public void STOP() {
            //Descomentar para activar
            //registers.stop = true;
            clock_cycles += 4;
            registers.programCounter += 2;
        }

        /// <summary>
        /// This instruction disables interrupts but not immediately.Interrupts are disabled after
        /// instruction after DI is executed.
        /// </summary>
        public void DI() {
            registers.IME = false;
            clock_cycles += 4;
            registers.programCounter += 1;
        }

        /// <summary>
        /// Enable interrupts. This intruction enables interrupts but not immediately.Interrupts are
        /// enabled after instruction after EI is executed.
        /// </summary>
        public void EI() {
            registers.IME = true;
            firstAfterEI = true;
            clock_cycles += 4;
            registers.programCounter += 1;
        }

        //Rotate and Shifts

        /// <summary>
        /// Rotate A left. Old bit 7 to Carry flag.
        /// </summary>
        public void RLCA() {

            byte a = registers.getRegisterSimple(reg_NAMES.A);

            byte old = (byte)((a & 0x80) >> 7);
            a = (byte)(a << 1);
            a = (byte)((a & 0xFE) | (old & 0x01));
            registers.setRegisterSimple(reg_NAMES.A, a);

            byte z = a == 0 ? (byte)1 : (byte)0;

            //OPS
            registers.setFlag(flag_NAME.Z, 0);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += 4;
            if (currentOP == 0xCB)
                registers.programCounter += 2;
            else
                registers.programCounter += 1;
        }

        /// <summary>
        /// Rotate A left through Carry flag.
        /// </summary>
        public void RLA() {
            byte a = registers.getRegisterSimple(reg_NAMES.A);
            byte carry = registers.getFlag(flag_NAME.C);
            byte old = (byte)((a & 0x80) >> 7);
            a = (byte)(a << 1);
            a = (byte)((a & 0xFE) | (carry & 0x01));
            registers.setRegisterSimple(reg_NAMES.A, a);

            byte z = a == 0 ? (byte)1 : (byte)0;

            //OPS
            registers.setFlag(flag_NAME.Z, 0);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += 4;
            if (currentOP == 0xCB)
                registers.programCounter += 2;
            else
                registers.programCounter += 1;
        }

        /// <summary>
        /// Rotate A right. Old bit 0 to Carry flag.
        /// </summary>
        public void RRCA() {
            byte a = registers.getRegisterSimple(reg_NAMES.A);
            byte old = (byte)(a & 0x01);
            a = (byte)(a >> 1);
            a = (byte)((a & 0x7F) | ((old << 7) & 0x80));
            registers.setRegisterSimple(reg_NAMES.A, a);

            byte z = a == 0 ? (byte)1 : (byte)0;

            //OPS
            registers.setFlag(flag_NAME.Z, 0);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += 4;
            if (currentOP == 0xCB)
                registers.programCounter += 2;
            else
                registers.programCounter += 1;
        }

        /// <summary>
        /// Rotate A right through Carry flag.
        /// </summary>
        public void RRA() {
            byte a = registers.getRegisterSimple(reg_NAMES.A);
            byte carry = registers.getFlag(flag_NAME.C);
            byte old = (byte)(a & 0x01);
            a = (byte)(a >> 1);
            a = (byte)((a & 0x7F) | ((carry << 7) & 0x80));
            registers.setRegisterSimple(reg_NAMES.A, a);

            byte z = a == 0 ? (byte)1 : (byte)0;

            //OPS
            registers.setFlag(flag_NAME.Z, 0);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += 4;
            if (currentOP == 0xCB)
                registers.programCounter += 2;
            else
                registers.programCounter += 1;
        }

        /// <summary>
        /// Rotate n left. Old bit 7 to Carry flag.
        /// </summary>
        /// <param name="register">The register name</param>
        public void RLC_n(reg_NAMES register) {
            byte value, cycles = 8;

            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
            }

            byte old = (value & 0x80) != 0 ? (byte) 1 :(byte)0 ;
            value = (byte)((value << 1) & 0xFF);
            value = (byte)((value & 0xFE) | (old & 0x01));


            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                memory.writeByte(address, value);
            } else {
                registers.setRegisterSimple(register, value);
            }

            byte z = (value & 0xFF) == 0 ? (byte)1 : (byte)0;

            
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += cycles;
            if (currentOP == 0xCB) {
                registers.setFlag(flag_NAME.Z, z);
                registers.programCounter += 2;
            } else {
                registers.setFlag(flag_NAME.Z, 0);
                registers.programCounter += 1;
            }
        }

        /// <summary>
        /// Rotate n left through Carry flag.
        /// </summary>
        /// <param name="register">The register name</param>
        public void RL_n(reg_NAMES register) {
            byte value, cycles = 8;
            byte carry = registers.getFlag(flag_NAME.C);
            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
            }

            byte old = (byte)((value & 0x80) >> 7);
            value = (byte)(value << 1);
            value = (byte)((value & 0xFE) | (carry & 0x01));


            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                memory.writeByte(address, value);
            } else {
                registers.setRegisterSimple(register, value);
            }

            byte z = (value & 0xFF) == 0 ? (byte)1 : (byte)0;

            //registers.setFlag(flag_NAME.Z, z);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += cycles;
            if (currentOP == 0xCB) {
                registers.setFlag(flag_NAME.Z, z);
                registers.programCounter += 2;
            } else {
                registers.setFlag(flag_NAME.Z, 0);
                registers.programCounter += 1;
            }

        }


        /// <summary>
        /// Rotate n right. Old bit 0 to Carry flag.
        /// </summary>
        /// <param name="register">The register name</param>
        public void RRC_n(reg_NAMES register) {
            byte value, cycles = 8;

            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
            }

            byte old = (byte)(value & 0x01);
            value = (byte)(value >> 1);
            value = (byte)((value & 0x7F) | ((old << 7) & 0x80));

            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                memory.writeByte(address, value);
            } else {
                registers.setRegisterSimple(register, value);
            }

            byte z = (value & 0xFF) == 0 ? (byte)1 : (byte)0;

            //registers.setFlag(flag_NAME.Z, z);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += cycles;
            if (currentOP == 0xCB) {
                registers.setFlag(flag_NAME.Z, z);
                registers.programCounter += 2;
            } else {
                registers.setFlag(flag_NAME.Z, 0);
                registers.programCounter += 1;
            }
        }

        /// <summary>
        /// Rotate n right through Carry flag.
        /// </summary>
        /// <param name="register">The register name</param>
        public void RR_n(reg_NAMES register) {
            byte value, cycles = 8;
            byte carry = registers.getFlag(flag_NAME.C);
            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
            }

            byte old = (byte)(value & 0x01);
            value = (byte)(value >> 1);
            value = (byte)((value & 0x7F) | ((carry << 7) & 0x80));

            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                memory.writeByte(address, value);
            } else {
                registers.setRegisterSimple(register, value);
            }

            byte z = (value & 0xFF) == 0 ? (byte)1 : (byte)0;

            //registers.setFlag(flag_NAME.Z, z);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += cycles;
            if (currentOP == 0xCB) {
                registers.setFlag(flag_NAME.Z, z);
                registers.programCounter += 2;
            } else {
                registers.setFlag(flag_NAME.Z, 0);
                registers.programCounter += 1;
            }
        }

        /// <summary>
        /// Shift n left into Carry. LSB of n set to 0.
        /// </summary>
        /// <param name="register"></param>
        public void SLA_n(reg_NAMES register) {
            byte value, cycles = 8;
            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
            }

            byte old = (byte)((value & 0x80) >> 7);
            value = (byte)(value << 1);

            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                memory.writeByte(address, value);
            } else {
                registers.setRegisterSimple(register, value);
            }

            byte z = (value & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.Z, z);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += cycles;
            if (currentOP == 0xCB)
                registers.programCounter += 2;
            else
                registers.programCounter += 1;
        }

        /// <summary>
        /// Shift n right into Carry. MSB doesn't change.
        /// </summary>
        /// <param name="register"></param>
        public void SRA_n(reg_NAMES register) {
            byte value, cycles = 8, msb;
            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
            }

            msb = (byte)((value & 0x80));
            byte old = (byte)(value & 0x01);
            value = (byte)(msb | ((value >> 1) & 0x7F));

            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                memory.writeByte(address, value);
            } else {
                registers.setRegisterSimple(register, value);
            }

            byte z = (value & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.Z, z);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += cycles;
            if (currentOP == 0xCB)
                registers.programCounter += 2;
            else
                registers.programCounter += 1;
        }

        /// <summary>
        /// Shift n right into Carry. MSB set to 0.
        /// </summary>
        /// <param name="register"></param>
        public void SRL_n(reg_NAMES register) {
            byte value, cycles = 8;
            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
            }

            byte old = (byte)(value & 0x01);
            value = (byte)((value >> 1) & 0x7F);

            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                memory.writeByte(address, value);
            } else {
                registers.setRegisterSimple(register, value);
            }

            byte z = (value & 0xFF) == 0 ? (byte)1 : (byte)0;

            registers.setFlag(flag_NAME.Z, z);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 0);
            registers.setFlag(flag_NAME.C, old);

            clock_cycles += cycles;
            if (currentOP == 0xCB)
                registers.programCounter += 2;
            else
                registers.programCounter += 1;
        }

        //BIT Opcodes


        /// <summary>
        /// Test bit b in register r.
        /// </summary>
        /// <param name="bit">Possition to test 0 - 7</param>
        /// <param name="register">The register name</param>
        public void BIT_b_r(byte bit, reg_NAMES register) {
            byte value, cycles = 8, z;
            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
            }

            if ((value & (1 << bit)) != 0)
                z = 0;
            else
                z = 1;

            registers.setFlag(flag_NAME.Z, z);
            registers.setFlag(flag_NAME.N, 0);
            registers.setFlag(flag_NAME.H, 1);

            if (DEBUG)
                Console.WriteLine("BIT - " + bit + " " + register + " result: " + z);

            clock_cycles += cycles;
            registers.programCounter += 2;
        }

        /// <summary>
        /// Set bit b in register r.
        /// </summary>
        /// <param name="bit">Possition to test 0 - 7</param>
        /// <param name="register">The register name</param>
        public void SET_b_r(byte bit, reg_NAMES register) {
            byte value, cycles = 8;
            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                value = (byte)(value | (1 << bit));
                memory.writeByte(address, value);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
                value = (byte)(value | (1 << bit));
                registers.setRegisterSimple(register, value);
            }

            clock_cycles += cycles;
            registers.programCounter += 2;
        }

        /// <summary>
        /// Reset bit b in register r.
        /// </summary>
        /// <param name="bit">Possition to test 0 - 7</param>
        /// <param name="register">The register name</param>
        public void RES_b_r(byte bit, reg_NAMES register) {
            byte value, cycles = 8;
            if (register == reg_NAMES.HL) {
                ushort address = registers.getRegisterDouble(reg_NAMES.HL);
                value = memory.readByte(address);
                value = (byte)(value & ~(1 << bit));
                memory.writeByte(address, value);
                cycles = 16;
            } else {
                value = registers.getRegisterSimple(register);
                value = (byte)(value & ~(1 << bit));
                registers.setRegisterSimple(register, value);
            }

            clock_cycles += cycles;
            registers.programCounter += 2;
        }

        //JUMPS

        /// <summary>
        /// Jump to address nn (16 bit inmediate value).
        /// </summary>
        public void JP_nn() {
            clock_cycles += 12;
            registers.programCounter = getInmediateValue16bit();
        }

        /// <summary>
        /// Jump to address n if following condition is true:
        /// cc = NZ, Jump if Z flag is reset.
        /// cc = Z, Jump if Z flag is set.
        /// cc = NC, Jump if C flag is reset.
        /// cc = C, Jump if C flag is set.
        /// </summary>
        /// <param name="flag">The name of flag</param>
        /// <param name="valueCheck">The value to do true</param>
        public void JP_cc_nn(flag_NAME flag, byte valueCheck) {
            if (registers.getFlag(flag) == valueCheck) {
                ushort inmediate = getInmediateValue16bit();
                registers.programCounter = inmediate;
                registers.conditionalTaken = true;
            } else {
                registers.programCounter += 3;
            }
            clock_cycles += 12;
        }

        /// <summary>
        /// Jump to address contained in HL.
        /// </summary>
        public void JP_HL() {
            clock_cycles += 4;
            registers.programCounter = registers.getRegisterDouble(reg_NAMES.HL);
        }

        /// <summary>
        /// Add n to current address and jump to it.
        /// </summary>
        public void JR_n() {
            byte inmediate = getInmediateValue8bit();
            clock_cycles += 8;
            //es 2 porque ya hemos ledio 2 op codes con respecto a la instruccion inicial,
            //la de la propia instruccion JR_n y el valor inmediato
            registers.programCounter += (ushort)((sbyte)inmediate + 2);
        }

        /// <summary>
        /// If following condition is true then add n to current address and jump to it:
        /// cc = NZ, Jump if Z flag is reset
        /// cc = Z, Jump if Z flag is set.
        /// cc = NC, Jump if C flag is reset.
        /// cc = C, Jump if C flag is set.
        /// </summary>
        /// <param name="flag">The name of flag</param>
        /// <param name="valueCheck">The value to do true</param>
        public void JR_CC_n(flag_NAME flag, byte valueCheck) {
            if (registers.getFlag(flag) == valueCheck) {
                if (DEBUG)
                    Console.WriteLine("JR_CC tomado");
                JR_n();
                registers.conditionalTaken = true;
            } else {
                registers.programCounter += 2;
                clock_cycles += 8;
            }
        }

        /// <summary>
        /// Push address of next instruction onto stack and then jump to address nn.
        /// </summary>
        public void CALL_nn() {
            registers.stackPointer -= 1;
            memory.writeByte(registers.stackPointer, (byte)(((registers.programCounter + 3) & 0xFF00) >> 8));
            registers.stackPointer -= 1;
            memory.writeByte(registers.stackPointer, (byte)((registers.programCounter + 3) & 0x00FF));
            registers.programCounter = getInmediateValue16bit();
            clock_cycles += 12;
        }

        /// <summary>
        /// Call address n if following condition is true:
        /// cc = NZ, Call if Z flag is reset.
        /// cc = Z, Call if Z flag is set.
        /// cc = NC, Call if C flag is reset.
        /// cc = C, Call if C flag is set.
        /// </summary>
        /// <param name="flag">The name of flag</param>
        /// <param name="valueCheck">The value to do true</param>
        public void CALL_cc_nn(flag_NAME flag, byte valueCheck) {
            if (DEBUG)
                Console.WriteLine("DEBUG - CALL_cc_nn {0} : {1:X}", flag, valueCheck);
            if (registers.getFlag(flag) == valueCheck) {
                CALL_nn();
                registers.conditionalTaken = true;
            } else {
                registers.programCounter += 3;
                clock_cycles += 12;
            }
        }

        /// <summary>
        /// Push present address onto stack.
        /// Jump to address $0000 + n.
        /// Desplazamiento, Opcode
        /// 00H C7
        /// 08H CF
        /// 10H D7
        /// 18H DF
        /// 20H E7
        /// 28H EF
        /// 30H F7
        /// 38H FF
        /// </summary>
        /// <param name="desplazamiento"></param>
        public void RST_n(byte desplazamiento) {
            if (DEBUG)
                Console.WriteLine("DEBUG - RST_n {0:X}", desplazamiento);
            registers.programCounter += 1;
            PUSH_PC();
            registers.programCounter = (ushort)(0x0000 + desplazamiento);
            clock_cycles += 32;
        }

        /// <summary>
        /// Push PC onto stack. (No es una instrunccion porpia de la GB pero viene bien)
        /// </summary>
        public void PUSH_PC() {
            if (DEBUG)
                Console.WriteLine("DEBUG - PUSH_PC");
            registers.stackPointer -= 1;
            memory.writeByte(registers.stackPointer, (byte)((registers.programCounter & 0xFF00) >> 8));
            registers.stackPointer -= 1;
            memory.writeByte(registers.stackPointer, (byte)(registers.programCounter & 0x00FF));
        }

        /// <summary>
        /// Pop two bytes from stack & jump to that address.
        /// </summary>
        public void RET() {
            if (DEBUG)
                Console.WriteLine("DEBUG - RET");
            registers.programCounter = memory.readWord(registers.stackPointer);
            registers.stackPointer += 2;
            clock_cycles += 8;
        }

        /// <summary>
        /// Return if following condition is true:
        /// cc = NZ, Return if Z flag is reset.
        /// cc = Z, Return if Z flag is set.
        /// cc = NC, Return if C flag is reset.
        /// cc = C, Return if C flag is set.
        /// </summary>
        /// <param name="flag">The name of flag</param>
        /// <param name="valueCheck">The value to do true</param>
        public void RET_cc(flag_NAME flag, byte valueCheck) {
            if (DEBUG)
                Console.WriteLine("DEBUG - RET_cc");
            if (registers.getFlag(flag) == valueCheck) {
                RET();
                registers.conditionalTaken = true;
            } else {
                registers.programCounter += 1;
                clock_cycles += 8;
            }
        }

        /// <summary>
        /// Pop two bytes from stack & jump to that address then enable interrupts.
        /// </summary>
        public void RETI() {
            if (DEBUG)
                Console.WriteLine("DEBUG - RETI");
            EI();
            RET();
            clock_cycles += (8 - 12); //Por exceso de sumas en EI y RET
        }
    }
}
