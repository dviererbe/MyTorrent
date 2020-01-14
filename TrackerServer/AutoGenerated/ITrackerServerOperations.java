package TrackerServer;


/**
* TrackerServer/ITrackerServerOperations.java .
* Generated by the IDL-to-Java compiler (portable), version "3.2"
* from MyTorrent.idl
* Mittwoch, 8. Januar 2020 15:41 Uhr MEZ
*/

public interface ITrackerServerOperations 
{
  TrackerServer.NetworkInfo GetNetworkInfo ();
  TrackerServer.UploadStatus GetUploadStatus (String fileHash);
  void InitiateUpload (int size, String hash);
  void UploadFragment (TrackerServer.FragmentInfo info, char[] fragment);
} // interface ITrackerServerOperations