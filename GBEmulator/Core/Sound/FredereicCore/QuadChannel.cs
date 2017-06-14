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
using nanoboy.Core.Audio.Backend;

namespace nanoboy.Core.Audio
{
    public sealed class QuadChannel : ISoundChannel
    {
        public enum SweepMode
        {
            Addition = 0,
            Substraction = 1
        }

        public enum EnvelopeMode
        {
            Decrease = 0,
            Increase = 1
        }

        // Enables or disables the channel
        public bool Enabled { get; set; }

        // Sweep
        public int SweepTime { get; set; }
        public SweepMode SweepDirection { get; set; }
        public int SweepShift { get; set; }
        private int[] sweepclocktable = new int[] { 0, 32768, 65536, 98304, 131072, 163840, 196608, 229376 };
        private int lastfrequency;
        private int currentfrequency;
        private int sweepcycles;

        // Frequency
        public int Frequency {
            get {
                return initialfrequency;
            }
            set {
                initialfrequency = value;
                currentfrequency = value; // this is the one used to generate the sound
            }
        }
        private int initialfrequency;

        // Amplitude / Volumen sweep
        public EnvelopeMode EnvelopeDirection { get; set; }
        public int EnvelopeSweep { 
            get {
                return envelopesweep;
            }
            set {
                envelopesweep = value;
                envelopecycles = 0;
            }
        }
        private int envelopesweep;
        public int Volume {
            get {
                return lastwrittenvolume;
            }
            set {
                lastwrittenvolume = value;
                currentvolume = value;
            }
        }
        private int lastwrittenvolume;
        private int currentvolume;
        private int envelopecycles;

        // Sound length
        public int SoundLengthData { get; set; }
        public bool StopOnLengthExpired { get; set; }
        private int soundlengthcycles;

        // Wave pattern duty (implement this)
        public int WavePatternDuty { get; set; }

        // Sound generation
        private int sample;

        public QuadChannel()
        {
            lastfrequency = 0;
            currentfrequency = 0;
            sweepcycles = 0;
            Enabled = true;
        }

        public float Next(int samplerate)
        {
            int soundlengthclock = (64 - SoundLengthData) * (1 / 256) * 4194304;
            if (!StopOnLengthExpired || soundlengthcycles <= soundlengthclock) {
                float amplitude = (float)currentvolume * (1f / 16f);
                float value;
                float duty;
                switch (WavePatternDuty) {
                    case 0: duty = 0.125f; break;
                    case 1: duty = 0.25f; break;
                    case 2: duty = 0.5f; break;
                    case 3: duty = 0.75f; break;
                    default:
                        throw new Exception("Unknown wave duty");
                }
                value = (float)(amplitude * Generate((float)((2 * Math.PI * sample * ConvertFrequency(currentfrequency)) / samplerate), duty));
                if (++sample >= samplerate) {
                    sample = 0;
                }
                return value;
            } else {
                return 0f;
            }
        }

        public void Tick()
        {
            int sweepclock = sweepclocktable[SweepTime];
            int envelopeclock = (int)(EnvelopeSweep * (1f / 64f) * 4194304f);
            // recalculate frequency
            if (sweepclock != 0) {
                sweepcycles++;
                if (sweepcycles >= sweepclock) {
                    sweepcycles = 0;
                    lastfrequency = currentfrequency;
                    if (SweepDirection == SweepMode.Addition) {
                        currentfrequency = lastfrequency + lastfrequency / (2 << SweepShift);
                    } else {
                        currentfrequency = lastfrequency - lastfrequency / (2 << SweepShift);
                    }
                }
            }
            // recalculate volume
            if (EnvelopeSweep != 0) {
                envelopecycles++;
                if (envelopecycles >= envelopeclock) {
                    envelopecycles = 0;
                    if (EnvelopeDirection == EnvelopeMode.Increase) {
                        if (currentvolume != 15) {
                            currentvolume++;
                        }
                    } else {
                        if (currentvolume != 0) {
                            currentvolume--;
                        }
                    }
                }
            }
            if (StopOnLengthExpired) {
                soundlengthcycles++;
            }
        }

        public void Restart()
        {
            currentfrequency = initialfrequency;
            sweepcycles = 0;
            soundlengthcycles = 0;
            envelopecycles = 0;
        }

        private float ConvertFrequency(int frequency)
        {
            return 131072 / (2048 - frequency);
        }

        private float Generate(float x, float duty)
        {
            float realx = x % (float)(2 * Math.PI);
            if (realx <= 2 * Math.PI * duty) {
                return 1f;
            }
            return 0f;
        }
    }
}
