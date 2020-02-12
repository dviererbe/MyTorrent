using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public static class StringEqualsIgnoreCaseExtension
    {
        public static bool EqualsIgnoreCase(this string value1, string value2)
        {
            return value1.Equals(value2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
