package TrackerServer;

/**
* TrackerServer/NetworkInfoHolder.java .
* Generated by the IDL-to-Java compiler (portable), version "3.2"
* from MyTorrent.idl
* Mittwoch, 8. Januar 2020 15:41 Uhr MEZ
*/

public final class NetworkInfoHolder implements org.omg.CORBA.portable.Streamable
{
  public TrackerServer.NetworkInfo value = null;

  public NetworkInfoHolder ()
  {
  }

  public NetworkInfoHolder (TrackerServer.NetworkInfo initialValue)
  {
    value = initialValue;
  }

  public void _read (org.omg.CORBA.portable.InputStream i)
  {
    value = TrackerServer.NetworkInfoHelper.read (i);
  }

  public void _write (org.omg.CORBA.portable.OutputStream o)
  {
    TrackerServer.NetworkInfoHelper.write (o, value);
  }

  public org.omg.CORBA.TypeCode _type ()
  {
    return TrackerServer.NetworkInfoHelper.type ();
  }

}