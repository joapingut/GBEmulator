using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {
    enum RTCREG {
        RTC_S, RTC_M, RTC_H, RTC_DL, RTC_DH
    }

    class RTC {

        byte mask_HALT = 0x40;

        byte[] rtc_regs;
        byte[] rtc_fixed_regs;
        RTCREG select_reg;
        bool regsFixed = false;

        DateTime lastMeasure;

        public RTC() {
            rtc_fixed_regs = new byte[5];
            rtc_regs = new byte[5];
            select_reg = RTCREG.RTC_S;
        }

        public void update() {
            TimeSpan elapsed = new TimeSpan();
            if ((rtc_regs[(int)RTCREG.RTC_DH] & mask_HALT) == 0) {
                elapsed = DateTime.Now - lastMeasure;
            }
            add(elapsed);
        }

        private void add(TimeSpan elapsed) {
            int days = elapsed.Days;
            int hours = elapsed.Hours;
            int minutes = elapsed.Minutes;
            int seconds = elapsed.Seconds;

            rtc_fixed_regs[(int)RTCREG.RTC_S] = (byte)(rtc_regs[(int)RTCREG.RTC_S] + seconds);
            while (rtc_fixed_regs[(int)RTCREG.RTC_S] >= 60) {
                rtc_fixed_regs[(int)RTCREG.RTC_S] -= 60;
                minutes += 1;
            }

            rtc_fixed_regs[(int)RTCREG.RTC_M] = (byte)(rtc_regs[(int)RTCREG.RTC_M] + minutes);
            while (rtc_fixed_regs[(int)RTCREG.RTC_M] >= 60) {
                rtc_fixed_regs[(int)RTCREG.RTC_M] -= 60;
                hours += 1;
            }

            rtc_fixed_regs[(int)RTCREG.RTC_H] = (byte)(rtc_regs[(int)RTCREG.RTC_H] + hours);
            while (rtc_fixed_regs[(int)RTCREG.RTC_H] >= 60) {
                rtc_fixed_regs[(int)RTCREG.RTC_H] -= 60;
                days += 1;
            }

            int daysInReg = ((rtc_regs[(int)RTCREG.RTC_DH] & 0x01) << 8) | rtc_regs[(int)RTCREG.RTC_DH];
            days += daysInReg;
            if (days > 511)
                rtc_fixed_regs[(int)RTCREG.RTC_DH] |= 0x80;
            days = days % 512;
            rtc_fixed_regs[(int)RTCREG.RTC_DL] = (byte)(days & 0xFF);
            rtc_fixed_regs[(int)RTCREG.RTC_DH] = (byte)(rtc_fixed_regs[(int)RTCREG.RTC_DH] | ((days & 0x100) >> 8));
        }

        public byte readSelected() {
            return rtc_fixed_regs[(int)select_reg];
        }

        public void writeSelected(byte value) {
            update();
            rtc_regs[(int)select_reg] = value;
            lastMeasure = DateTime.Now;
        }

        public void changeSelected(byte value) {
            switch (value) {
                case (int)RTCREG.RTC_DH:
                    select_reg = RTCREG.RTC_DH;
                    break;
                case (int)RTCREG.RTC_DL:
                    select_reg = RTCREG.RTC_DL;
                    break;
                case (int)RTCREG.RTC_H:
                    select_reg = RTCREG.RTC_H;
                    break;
                case (int)RTCREG.RTC_M:
                    select_reg = RTCREG.RTC_M;
                    break;
                case (int)RTCREG.RTC_S:
                    select_reg = RTCREG.RTC_S;
                    break;
                default:
                    select_reg = RTCREG.RTC_S;
                    break;
            }
        }

        public void updateFixedRegs(int value) {
            if (!regsFixed && value == 1)
                update();
            if (value == 0)
                regsFixed = false;
            else
                regsFixed = true;
        }

        public void saveRTC(string fileName) {
            using (BinaryWriter writer = new BinaryWriter(File.Open(Memory.BATTERYPATH + fileName + ".rtc", FileMode.OpenOrCreate))) {
                long pos = 0;
                for (int i = 0; i < rtc_regs.Length; i++) {
                    writer.Write(rtc_regs[i]);
                }
                for (int i = 0; i < rtc_fixed_regs.Length; i++) {
                    writer.Write(rtc_fixed_regs[i]);
                }
                writer.Write((int)select_reg);
                writer.Write(regsFixed);
                writer.Write(lastMeasure.ToBinary());
            }
        }

        public void loadRTC(string fileName) {
            using (BinaryReader reader = new BinaryReader(File.Open(Memory.BATTERYPATH + fileName + ".rtc", FileMode.OpenOrCreate))) {
                for (int i = 0; i < rtc_regs.Length; i++) {
                    rtc_regs[i] = reader.ReadByte();
                }
                for (int i = 0; i < rtc_fixed_regs.Length; i++) {
                    byte leido = reader.ReadByte();
                    rtc_fixed_regs[i] = leido;
                }
                select_reg = (RTCREG) Enum.ToObject(typeof(RTCREG),reader.ReadInt32());
                //                regsFixed = reader.ReadBoolean();
                reader.ReadBoolean();
                lastMeasure = DateTime.FromBinary(reader.ReadInt64());
            }
        }
    }
}
