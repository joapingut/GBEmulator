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
using nanoboy.Core.Audio.Backend;

namespace nanoboy.Core.Audio
{
    public sealed class NoiseChannel : ISoundChannel
    {
        public enum EnvelopeMode
        {
            Decrease = 0,
            Increase = 1
        }

        // Enables or disables the channel
        public bool Enabled { get; set; }

        // Noise generation
        public int ClockFrequency { get; set; } // s
        public bool CounterStep { get; set; } // 0 = 15 bits, 1 = 7 bits
        public int DividingRatio { get; set; } // r
        public int Counter { get; set; }
        private float frequency
        {
            get
            {
                float r = DividingRatio == 0 ? 0.5f : (float)DividingRatio;
                return 524288f / r / (float)Math.Pow(2, (double)(ClockFrequency + 1));
            }
        }

        // Amplitude / Volumen sweep
        public EnvelopeMode EnvelopeDirection { get; set; }
        public int EnvelopeSweep
        {
            get
            {
                return envelopesweep;
            }
            set
            {
                envelopesweep = value;
                envelopecycles = 0;
            }
        }
        private int envelopesweep;
        public int Volume
        {
            get
            {
                return lastwrittenvolume;
            }
            set
            {
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

        // Sound generation
        private int steps;
        private List<int> buffer;
        private int sample;

        public NoiseChannel()
        {
            buffer = new List<int>();
            Enabled = true;
        }

        public float Next(int samplerate)
        {
            int value = 0;
            int soundlengthclock = (64 - SoundLengthData) * (1 / 256) * 4194304;
            float amplitude = (float)currentvolume * (1f / 16f);
            if (!StopOnLengthExpired || soundlengthcycles <= soundlengthclock) {
                if (buffer.Count != 0 && buffer.Count > sample) {
                    float index = sample;
                    value = buffer[sample];
                    if (++sample >= samplerate) {
                        sample = 0;
                        buffer.Clear();
                    }
                }
            }
            return (value & 1) == 1 ? amplitude : 0f;
        }

        public void Tick()
        {
            int envelopeclock = (int)(EnvelopeSweep * (1f / 64f) * 4194304f);
            steps++;
            // update counter
            if ((int)(4194304f / frequency) >= steps) {
                int msb = (Counter & 1) ^ ((Counter >> 1) & 1);
                Counter = (Counter >> 1) | (msb << (CounterStep ? 6 : 14));
                buffer.Add(Counter);
                steps = 0;
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
        }

        public void Restart()
        {
            soundlengthcycles = 0;
            envelopecycles = 0;
        }
    }
}