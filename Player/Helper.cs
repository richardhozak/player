using System;
using System.Linq;
using System.Text;

namespace DriftPlayer
{
    public static class Helper
    {
        public static double ToUnixTimestamp(this DateTime dateTime)
        {
            string f = (dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds.ToString();
            if (f.Contains(','))
                return double.Parse(f.Split(',')[0]);
            else
                return double.Parse(f);
        }

        public static DateTime ToDateTime(this double unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string Reverse(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static bool IsNotNullOrEmpty(this string s)
        {
            return !String.IsNullOrEmpty(s);
        }

        public static string ExceptBlanks(this string str)
        {
            StringBuilder sb = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                switch (c)
                {
                    case '\r':
                    case '\n':
                    case '\t':
                    case ' ':
                        continue;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
