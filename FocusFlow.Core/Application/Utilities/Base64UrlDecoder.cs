using System;
using System.Text;

namespace FocusFlow.Core.Application.Utilities
{
    public static class Base64UrlDecoder
    {
        public static string Decode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var base64String = input.Replace('-', '+').Replace('_', '/');

            switch (base64String.Length % 4)
            {
                case 2: base64String += "=="; break;
                case 3: base64String += "="; break;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64String);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}

