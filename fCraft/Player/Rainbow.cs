using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    class Rainbow
    {
        // &4, &c, &e, &a, &b, &9, &5
        static readonly char[] RainbowChars = new char[] { 'c', '4', '6', 'e', 'a', '2', 'b', '3', '9', '1', 'd', '5' };

        // Adds rainbow colors to given input string.
        // Note that there is no trailing color code! Add one if
        // you don't want color to "leak" past the end of this string.
        public static string Rainbowize(string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (char c in Color.StripColors(str))
            {
                if (!Char.IsWhiteSpace(c))
                {
                    sb.Append('&')
                    .Append(RainbowChars[i % RainbowChars.Length]);
                    i++;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

		static readonly char[] BWRainbowChars = new char[] { '0', '8', '7', 'f', '7', '8' };

		// Adds rainbow colors to given input string.
		// Note that there is no trailing color code! Add one if
		// you don't want color to "leak" past the end of this string.
		public static string BWRainbowize(string str) {
			if (str == null)
				throw new ArgumentNullException("str");
			StringBuilder sb = new StringBuilder();
			int i = 0;
			foreach (char c in Color.StripColors(str)) {
				if (!Char.IsWhiteSpace(c)) {
					sb.Append('&')
					.Append(BWRainbowChars[i % BWRainbowChars.Length]);
					i++;
				}
				sb.Append(c);
			}
			return sb.ToString();
		}
    }
}
