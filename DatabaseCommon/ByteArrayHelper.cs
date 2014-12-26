using System;
using System.Linq;
using System.Text;

namespace Database.Common
{
    public static class ByteArrayHelper
    {
        public static byte[] Combine(params byte[][] toCombine)
        {
            byte[] result = new byte[toCombine.Sum(e => e.Length)];
            int index = 0;
            foreach (var array in toCombine)
            {
                Buffer.BlockCopy(array, 0, result, index, array.Length);
                index += array.Length;
            }

            return result;
        }

        public static bool ToBoolean(byte[] data, ref int index)
        {
            bool returnValue = BitConverter.ToBoolean(data, index);
            index += sizeof(bool);
            return returnValue;
        }

        public static byte[] ToBytes(int value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] ToBytes(uint value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] ToBytes(bool value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] ToBytes(string value)
        {
            byte[] stringData = Encoding.UTF8.GetBytes(value);
            return Combine(BitConverter.GetBytes(stringData.Length), stringData);
        }

        public static int ToInt32(byte[] data, ref int index)
        {
            int returnValue = BitConverter.ToInt32(data, index);
            index += sizeof(int);
            return returnValue;
        }

        public static string ToString(byte[] data, ref int index)
        {
            int length = ToInt32(data, ref index);
            string returnValue = Encoding.UTF8.GetString(data, index, length);
            index += length;
            return returnValue;
        }

        public static uint ToUInt32(byte[] data, ref int index)
        {
            uint returnValue = BitConverter.ToUInt32(data, index);
            index += sizeof(uint);
            return returnValue;
        }
    }
}