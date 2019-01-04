// ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace fCraft {
    internal sealed class LevelChunkStream : Stream {
        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        
        static Exception ex = new NotSupportedException("Stream does not support length/seeking.");
        public override void Flush() { }
        public override long Length { get { throw ex; } }
        public override long Position { get { throw ex; } set { throw ex; } }
        public override int Read(byte[] buffer, int offset, int count) { throw ex; }
        public override long Seek(long offset, SeekOrigin origin) { throw ex; }
        public override void SetLength(long length) { throw ex; }
        
        int index;
        internal byte chunkValue;
        Player player;
        byte[] data = new byte[chunkSize + 4];        
        const int chunkSize = 1024;
        
        public LevelChunkStream(Player player) {
            this.player = player;
            data[0] = (byte)OpCode.MapChunk;
        }
        
        public override void Close() {
            if (index > 0) WritePacket();
            player = null;
            base.Close();
        }
        
        public override void Write(byte[] buffer, int offset, int count) {
            while (count > 0) {
                int copy = Math.Min(chunkSize - index, count);
                if (copy <= 8) {
                    for (int i = 0; i < copy; i++)
                        data[index + i + 3] = buffer[offset + i];
                } else {
                    Buffer.BlockCopy(buffer, offset, data, index + 3, copy);
                }
                offset += copy; index += copy; count -= copy;
                
                if (index != chunkSize) continue;
                WritePacket();
            }
        }
        
        public override void WriteByte(byte value) {
            data[index + 3] = value;
            index++;
            
            if (index != chunkSize) return;
            WritePacket();
        }
        
        void WritePacket() {
            Packet.WriteI16((short)index, data, 1);
            data[1027] = chunkValue;
            player.SendNow(new Packet(data));
            index = 0;
        }
    }
}
