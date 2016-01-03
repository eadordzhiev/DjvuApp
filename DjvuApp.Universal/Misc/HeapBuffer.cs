using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Storage.Streams;

namespace DjvuApp.Misc
{
    public class HeapBuffer : IBuffer, HeapBuffer.IBufferByteAccess, IDisposable
    {
        [ComImport]
        [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IBufferByteAccess
        {
            IntPtr GetBuffer();
        }

        public uint Capacity { get; private set; }

        public uint Length { get; set; }

        private IntPtr _pointer;

        public HeapBuffer(uint length)
        {
            Length = Capacity = length;
            _pointer = Marshal.AllocHGlobal((int) length);
        }

        ~HeapBuffer()
        {
            Dispose();    
        }

        public IntPtr GetBuffer()
        {
            return _pointer;
        }

        public void Dispose()
        {
            if (_pointer == IntPtr.Zero)
                return;

            Capacity = Length = 0;

            var ptr = _pointer;
            _pointer = IntPtr.Zero;
            Marshal.FreeHGlobal(ptr);

            GC.SuppressFinalize(this);
        }
    }
}