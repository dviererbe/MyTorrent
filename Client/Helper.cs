using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserClient
{
  class Helper
  {
    static Random random = new Random();
    internal static string GetHashString(byte[] hash)
    {
      StringBuilder sb = new StringBuilder();
      foreach (byte item in hash)
      {
        sb.Append(item.ToString("X2"));
      }
      return sb.ToString();
    }

    internal static T GetRandomElement<T>(IEnumerable<T> list)
    {
      return list.ElementAt(random.Next(list.Count()));
    }

    internal static bool TryUriParse(in string uriString, out (string host, int port) hostPort)
    {
      hostPort = ("", -1);
      try
      {
        Uri uri = new Uri(uriString);
        hostPort.host = uri.Host;
        hostPort.port = uri.Port;
        return true;
      }
      catch
      {
        return false;
      }
    }
  }
}
