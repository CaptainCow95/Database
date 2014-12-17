using System;
using System.Linq;

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
    }
}