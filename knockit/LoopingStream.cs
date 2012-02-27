using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace knockit
{
    /// <summary>
    /// Stream for looping playback
    /// </summary>
    public class LoopingStream : WaveStream
    {
        WaveStream sourceStream;

        /// <summary>
        /// Creates a new Loop stream
        /// </summary>
        /// <param name="sourceStream">The stream to read from. Note: the Read method of this stream should return 0 when it reaches the end
        /// or else we will not loop to the start again.</param>
        public LoopingStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
        }

        /// <summary>
        /// Return source stream's wave format
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        /// <summary>
        /// LoopStream simply returns
        /// </summary>
        public override long Length
        {
            get { return sourceStream.Length; }
        }

        /// <summary>
        /// LoopStream simply passes on positioning to source stream
        /// </summary>
        public override long Position
        {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
                int bytesRequired = (int)Math.Min(count, Length - Position);
                int bytesRead = sourceStream.Read(buffer, offset, bytesRequired);

                if (bytesRequired < count)
                {
                    sourceStream.Position = 0;
                    sourceStream.Read(buffer, offset + bytesRead, count - bytesRequired);
                }
                return count;
        }
    }
}
