using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace GBEmulator.Core {
    enum pad_NAMES {
        RIGHT, LEFT, UP, DOWN, A, B, SELECT, START
    }

    class Pad {
        public Pad() {
        }

        public static bool[] buttonStates = new bool[8];

        public static void changekey(pad_NAMES key, bool down) {
            lock (buttonStates) {
                buttonStates[(int)key] = down;
            }
        }

        public byte update(byte newValue) {
            //throw new NotImplementedException();

            byte value;
            lock(buttonStates){
            if ((newValue & 0x20) == 0) {
                int start = (buttonStates[(int)pad_NAMES.START] ? 0 : 1) << 3;
                int select = (buttonStates[(int)pad_NAMES.SELECT] ? 0 : 1) << 2;
                int b = (buttonStates[(int)pad_NAMES.B] ? 0 : 1) << 1;
                int a = (buttonStates[(int)pad_NAMES.A] ? 0 : 1);
                value = (byte)(start | select | b | a);
            } else if ((newValue & 0x10) == 0) {
                int down = (buttonStates[(int)pad_NAMES.DOWN] ? 0 : 1) << 3;
                int up = (buttonStates[(int)pad_NAMES.UP] ? 0 : 1) << 2;
                int left = (buttonStates[(int)pad_NAMES.LEFT] ? 0 : 1) << 1;
                int right = (buttonStates[(int)pad_NAMES.RIGHT] ? 0 : 1);
                value = (byte)(down | up | left | right);
            } else {
                value = 0xF;
            }
            }
            return (byte)((newValue & 0xF0) | value);
        }
    }
}
