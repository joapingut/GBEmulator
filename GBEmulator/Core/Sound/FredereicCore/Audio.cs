/*
 * Copyright (C) 2014 - 2015 Frederic Meyer
 * 
 * This file is part of nanoboy.
 *
 * nanoboy is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *   
 * nanoboy is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with nanoboy.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace nanoboy.Core.Audio
{
    public sealed class AudioAvailableEventArgs : EventArgs
    {
        public float[] Buffer { get; set; }

        public AudioAvailableEventArgs(float[] buffer)
        {
            Buffer = buffer;
        }
    }

    public sealed class Audio
    {
        public event EventHandler<AudioAvailableEventArgs> AudioAvailable;
        public QuadChannel Channel1 { get; set; }
        public QuadChannel Channel2 { get; set; }
        public WaveChannel Channel3 { get; set; }
        public NoiseChannel Channel4 { get; set; }
        public bool Enabled { get; set; }
        private int ticks;
        private int samples;
        private List<float> samplebuffer;

        public Audio()
        {
            Channel1 = new QuadChannel();
            Channel2 = new QuadChannel();
            Channel3 = new WaveChannel();
            Channel4 = new NoiseChannel();
            Enabled = true;
            ticks = 0;
            samples = 0;
            samplebuffer = new List<float>();
        }

        public void Tick()
        {
            // Schedule all channels
            Channel1.Tick();
            Channel2.Tick();
            Channel3.Tick();
            Channel4.Tick();

            // At a frequency of 44,1khz read samples from enabled channels
            if (ticks++ == 12) {
                if (Enabled) {
                    float sample = (Channel1.Enabled ? Channel1.Next(44100) : 0) +
                                   (Channel2.Enabled ? Channel2.Next(44100) : 0) +
                                   (Channel3.Enabled ? Channel3.Next(44100) : 0) +
                                   (Channel4.Enabled ? Channel4.Next(44100) : 0);
                    samplebuffer.Add(sample);
                    if (samples++ == 2428)//2428
                    {//3528
                        if (AudioAvailable != null) {
                            AudioAvailable(this, new AudioAvailableEventArgs(samplebuffer.ToArray()));
                        }
                        samplebuffer.Clear();
                        samples = 0;
                    }
                }
                ticks = 0;
            }
        }

        internal byte memoryRead(ushort address){
            int value = 0;
            switch (address - 0xFF00){
                case 0x10: // NR10 Channel 1 Sweep register
                    value = this.Channel1.SweepShift |
                            ((int)this.Channel1.SweepDirection << 3) |
                            (this.Channel1.SweepTime << 4);
                    return (byte)value;
                case 0x11: // NR11 Channel 1 Sound length/Wave pattern duty
                    value = this.Channel1.SoundLengthData |
                            (this.Channel1.WavePatternDuty << 6);
                    return (byte)value;
                case 0x12: // NR12 Channel 1 Volume Envelope
                    value = this.Channel1.EnvelopeSweep |
                            ((int)this.Channel1.EnvelopeDirection << 3) |
                            (this.Channel1.Volume << 4);
                    return (byte)value;
                case 0x14: // NR14 Channel 1 Frequency hi
                    return (byte)(this.Channel1.StopOnLengthExpired ? 0x40 : 0x00);
                case 0x16: // NR21 Channel 2 Sound length/Wave pattern duty
                    value = this.Channel2.SoundLengthData |
                            (this.Channel2.WavePatternDuty << 6);
                    return (byte)value;
                case 0x17: // NR22 Channel 2 Volume Envelope
                    value = this.Channel2.EnvelopeSweep |
                            ((int)this.Channel2.EnvelopeDirection << 3) |
                            (this.Channel2.Volume << 4);
                    return (byte)value;
                case 0x19: // NR24 Channel 2 Frequency hi
                    return (byte)(this.Channel2.StopOnLengthExpired ? 0x40 : 0x00);
                case 0x1A: // NR30 Channel 3 Sound on/off
                    return (byte)(this.Channel3.On ? 0x80 : 0x00);
                case 0x1B: // NR31 Channel 3 Sound Length
                    return (byte)this.Channel3.SoundLengthData;
                case 0x1C: // NR32 Channel 3 Select output level
                    return (byte)(this.Channel3.OutputLevel << 5);
                case 0x1E: // NR33 Channel 3 Frequency hi
                    return (byte)(this.Channel3.StopOnLengthExpired ? 0x40 : 0x00);
                case 0x20:
                case 0x21:
                case 0x22:
                //case 0x23: throw new NotImplementedException();
                // FF30-FF3F Wave Pattern RAM
                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                case 0x38:
                case 0x39:
                case 0x3A:
                case 0x3B:
                case 0x3C:
                case 0x3D:
                case 0x3E:
                case 0x3F:
                    value = this.Channel3.WaveRAM[(address & 0xF) * 2 + 1] |
                            (this.Channel3.WaveRAM[(address & 0xF) * 2] << 4);
                    return (byte)value;
                default:
                    //throw new Exception(string.Format("Unknown IO port at 0x{0:X} (READ)", address));
                    return 0;
            }
        }

        internal void memoryWrite(ushort address, byte value){
                switch (address - 0xFF00){
                    case 0x10: // NR10 Channel 1 Sweep register
                        this.Channel1.SweepShift = value & 7;
                        this.Channel1.SweepDirection = (QuadChannel.SweepMode)((value >> 3) & 1);
                        this.Channel1.SweepTime = (value >> 4) & 7;
                        break;
                    case 0x11: // NR11 Channel 1 Sound length/Wave pattern duty
                        this.Channel1.SoundLengthData = value & 0x3F;
                        this.Channel1.WavePatternDuty = (value >> 6) & 3;
                        break;
                    case 0x12: // NR12 Channel 1 Volume Envelope
                        this.Channel1.EnvelopeSweep = value & 7;
                        this.Channel1.EnvelopeDirection = (QuadChannel.EnvelopeMode)((value >> 3) & 1);
                        this.Channel1.Volume = (value >> 4) & 0xF;
                        break;
                    case 0x13: // NR13 Channel 1 Frequency lo
                        this.Channel1.Frequency = (this.Channel1.Frequency & 0x700) | value;
                        break;
                    case 0x14: // NR14 Channel 1 Frequency hi
                        this.Channel1.Frequency = (this.Channel1.Frequency & 0xFF) | ((value & 7) << 8);
                        this.Channel1.StopOnLengthExpired = (value & 0x40) == 0x40;
                        if ((value & 0x80) == 0x80)
                        {
                            this.Channel1.Restart();
                        }
                        break;
                    case 0x16: // NR16 Channel 2 Sound length/Wave pattern duty
                        this.Channel2.SoundLengthData = value & 0x3F;
                        this.Channel2.WavePatternDuty = (value >> 6) & 3;
                        break;
                    case 0x17: // NR17 Channel 2 Volume Envelope
                        this.Channel2.EnvelopeSweep = value & 7;
                        this.Channel2.EnvelopeDirection = (QuadChannel.EnvelopeMode)((value >> 3) & 1);
                        this.Channel2.Volume = (value >> 4) & 0xF;
                        break;
                    case 0x18: // NR18 Channel 2 Frequency lo
                        this.Channel2.Frequency = (this.Channel2.Frequency & 0x700) | value;
                        break;
                    case 0x19: // NR19 Channel 2 Frequency hi
                        this.Channel2.Frequency = (this.Channel2.Frequency & 0xFF) | ((value & 7) << 8);
                        this.Channel2.StopOnLengthExpired = (value & 0x40) == 0x40;
                        if ((value & 0x80) == 0x80)
                        {
                            this.Channel2.Restart();
                        }
                        break;
                    case 0x1A: // NR30 Channel 3 Sound on / off
                        this.Channel3.On = (value & 0x80) == 0x80;
                        break;
                    case 0x1B: // NR31 Channel 3 Sound Length
                        this.Channel3.SoundLengthData = value;
                        break;
                    case 0x1c: // NR32 Channel 3 Select output level
                        this.Channel3.OutputLevel = (value >> 5) & 3;
                        break;
                    case 0x1D: // NR33 Channel 3 Frequency lo
                        this.Channel3.Frequency = (this.Channel3.Frequency & 0x700) | value;
                        break;
                    case 0x1E: // NR34 Channel 3 Frequency hi
                        this.Channel3.Frequency = (this.Channel3.Frequency & 0xFF) | ((value & 7) << 8);
                        this.Channel3.StopOnLengthExpired = (value & 0x40) == 0x40;
                        if ((value & 0x80) == 0x80)
                        {
                            this.Channel3.Restart();
                        }
                        break;
                    case 0x20: // NR41 Channel 4 Sound Length
                        this.Channel4.SoundLengthData = value;
                        break;
                    case 0x21: // NR42 Channel 4 Volume Envelope
                        this.Channel4.EnvelopeSweep = value & 7;
                        this.Channel4.EnvelopeDirection = (NoiseChannel.EnvelopeMode)((value >> 3) & 1);
                        this.Channel4.Volume = (value >> 4) & 0xF;
                        break;
                    case 0x22: // NR43 Channel 4 Polynomial Counter
                        this.Channel4.ClockFrequency = value >> 4;
                        this.Channel4.CounterStep = (value & 8) == 8;
                        this.Channel4.Counter = this.Channel4.CounterStep ? 0x7F : 0x7FFF;
                        this.Channel4.DividingRatio = value & 7;
                        break;
                    case 0x23: // NR44 Channel 4 Counter/consecutive; Initial
                        this.Channel4.StopOnLengthExpired = (value & 0x40) == 0x40;
                        if ((value & 0x80) == 0x80)
                        {
                            this.Channel4.Restart();
                        }
                        break;
                    // FF30-FF3F Wave Pattern RAM
                    case 0x30:
                    case 0x31:
                    case 0x32:
                    case 0x33:
                    case 0x34:
                    case 0x35:
                    case 0x36:
                    case 0x37:
                    case 0x38:
                    case 0x39:
                    case 0x3A:
                    case 0x3B:
                    case 0x3C:
                    case 0x3D:
                    case 0x3E:
                    case 0x3F:
                        this.Channel3.WaveRAM[(address & 0xF) * 2] = (byte)(value >> 4);
                        this.Channel3.WaveRAM[(address & 0xF) * 2 + 1] = (byte)(value & 0xF);
                        break;
                    default:
                        //throw new Exception(string.Format("Unknown IO port at 0x{0:X} (WRITE)", address));
                        break;
                }
            }
    }
}
