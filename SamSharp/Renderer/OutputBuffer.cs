using System;

namespace SamSharp.Renderer
{
    public class OutputBuffer
    {
        // TODO: Can allocate HUGE amounts of memory with long inputs, use streams instead?
        private byte[] buffer;

        private int bufferPos = 0;
        private int oldTimeTableIndex = 0;

        public OutputBuffer(int bufferSize)
        {
            buffer = new byte[bufferSize];
        }

        public void Write(int index, int a)
        {
            int scaled = ((a & 15) * 16) & 0xFF;
            Ary(index, new[] { scaled, scaled, scaled, scaled, scaled });
        }

        public void Ary(int index, int[] array)
        {
            // Timetable for more accurate C64 simulation
            var timetable = new[]
            {
                new[] { 162, 167, 167, 127, 128 }, // Formants synth
                new[] { 226, 60, 60, 0, 0 }, // Unvoiced sample 0
                new[] { 225, 60, 59, 0, 0 }, // Unvoiced sample 1
                new[] { 200, 0, 0, 54, 55 }, // Voiced sample 0
                new[] { 199, 0, 0, 54, 54 }, // Voiced sample 1
            };

            bufferPos += timetable[oldTimeTableIndex][index];

            if (bufferPos / 50 > buffer.Length)
                throw new Exception($"Buffer overflow, want {bufferPos / 50} but buffer size is {buffer.Length}");

            oldTimeTableIndex = index;
            
            // Write a little bit in advance
            for (int k = 0; k < 5; k++)
                buffer[bufferPos / 50 + k] = (byte)array[k];
        }

        public byte[] Get()
        {
            byte[] bytes = new byte[bufferPos / 50];
            for (int i = 0; i < bufferPos / 50; i++)
                bytes[i] = (byte)(buffer[i]);
            return bytes;
        }
    }
}