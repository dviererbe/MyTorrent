package TrackerServer;


/**
* TrackerServer/ITrackerServerHelper.java .
* Generated by the IDL-to-Java compiler (portable), version "3.2"
* from MyTorrent.idl
* Mittwoch, 8. Januar 2020 15:41 Uhr MEZ
*/

abstract public class ITrackerServerHelper
{
  private static String  _id = "IDL:TrackerServer/ITrackerServer:1.0";

  public static void insert (org.omg.CORBA.Any a, TrackerServer.ITrackerServer that)
  {
    org.omg.CORBA.portable.OutputStream out = a.create_output_stream ();
    a.type (type ());
    write (out, that);
    a.read_value (out.create_input_stream (), type ());
  }

  public static TrackerServer.ITrackerServer extract (org.omg.CORBA.Any a)
  {
    return read (a.create_input_stream ());
  }

  private static org.omg.CORBA.TypeCode __typeCode = null;
  synchronized public static org.omg.CORBA.TypeCode type ()
  {
    if (__typeCode == null)
    {
      __typeCode = org.omg.CORBA.ORB.init ().create_interface_tc (TrackerServer.ITrackerServerHelper.id (), "ITrackerServer");
    }
    return __typeCode;
  }

  public static String id ()
  {
    return _id;
  }

  public static TrackerServer.ITrackerServer read (org.omg.CORBA.portable.InputStream istream)
  {
    return narrow (istream.read_Object (_ITrackerServerStub.class));
  }

  public static void write (org.omg.CORBA.portable.OutputStream ostream, TrackerServer.ITrackerServer value)
  {
    ostream.write_Object ((org.omg.CORBA.Object) value);
  }

  public static TrackerServer.ITrackerServer narrow (org.omg.CORBA.Object obj)
  {
    if (obj == null)
      return null;
    else if (obj instanceof TrackerServer.ITrackerServer)
      return (TrackerServer.ITrackerServer)obj;
    else if (!obj._is_a (id ()))
      throw new org.omg.CORBA.BAD_PARAM ();
    else
    {
      org.omg.CORBA.portable.Delegate delegate = ((org.omg.CORBA.portable.ObjectImpl)obj)._get_delegate ();
      TrackerServer._ITrackerServerStub stub = new TrackerServer._ITrackerServerStub ();
      stub._set_delegate(delegate);
      return stub;
    }
  }

  public static TrackerServer.ITrackerServer unchecked_narrow (org.omg.CORBA.Object obj)
  {
    if (obj == null)
      return null;
    else if (obj instanceof TrackerServer.ITrackerServer)
      return (TrackerServer.ITrackerServer)obj;
    else
    {
      org.omg.CORBA.portable.Delegate delegate = ((org.omg.CORBA.portable.ObjectImpl)obj)._get_delegate ();
      TrackerServer._ITrackerServerStub stub = new TrackerServer._ITrackerServerStub ();
      stub._set_delegate(delegate);
      return stub;
    }
  }

}
