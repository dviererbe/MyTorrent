package TrackerServer;


/**
* TrackerServer/FileInfo.java .
* Generated by the IDL-to-Java compiler (portable), version "3.2"
* from MyTorrent.idl
* Mittwoch, 8. Januar 2020 15:41 Uhr MEZ
*/

public final class FileInfo implements org.omg.CORBA.portable.IDLEntity
{
  public int size = (int)0;
  public String hash = null;

  public FileInfo ()
  {
  } // ctor

  public FileInfo (int _size, String _hash)
  {
    size = _size;
    hash = _hash;
  } // ctor

} // class FileInfo
