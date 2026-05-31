using System;
namespace DarkMultiPlayerCommon
{
    public class ByteArray
    {
        public int size;
        public readonly byte[] data;
        public bool temporary;

        public ByteArray(int size)
        {
            data = new byte[size];
            this.size = size;
            temporary = false;
        }

        public int Length
        {
            get
            {
                return size;
            }
        }
    }
}
