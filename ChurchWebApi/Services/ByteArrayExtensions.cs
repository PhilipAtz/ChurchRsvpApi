using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace ChurchWebApi.Services
{
    internal static class ByteArrayExtensions
    {
        public static byte[] ToByteArray(this string value)
        {
            return value.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries).Select(s => byte.TryParse(s, NumberStyles.HexNumber, null, out var hex) ? hex : (byte)0).ToArray();
        }

        public static string ToHexString(this byte[] values) => BitConverter.ToString(values);
    }
}
