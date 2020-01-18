using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserClient
{
  class Helper
  {
    internal static string GetHashString(byte[] hash)
    {
      StringBuilder sb = new StringBuilder();
      foreach (byte item in hash)
      {
        sb.Append(item.ToString("X2"));
      }
      return sb.ToString();
    }
  }
}
