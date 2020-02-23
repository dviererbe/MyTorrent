using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Grpc.Core;
using Microsoft.Win32;
using System.Security.Cryptography;
using Google.Protobuf;
using Google.Protobuf.Collections;
using System.Xml;
using System.Xml.Serialization;
using UserClient.Properties;
using System.Collections.ObjectModel;
using MyTorrent.gRPC;

namespace UserClient
{
  /// <summary>
  /// Interaktionslogik für MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
	/// <summary>
	/// Implemented Eventhandler aus dem <see cref="INotifyPropertyChanged"/>
	/// </summary>
	public event PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	/// Random number generator
	/// </summary>
	static private Random random = new Random();

	/// <summary>
	/// Private list for the items which are displayeds.
	/// </summary>
	private ObservableCollection<FileTableItem> fileTable = null;

	/// <summary>
	/// Public list for the items which are displayed.
	/// </summary>
	public ObservableCollection<FileTableItem> FileTable
	{
	  get => this.fileTable;
	  set
	  {
		if (this.fileTable?.Equals(value) != true)
		{
		  this.fileTable = value;
		  this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.FileTable)));
		}
	  }
	}

	/// <summary>
	/// Determines wether the Image for th upload is visible or not
	/// </summary>
	public Visibility ImageVisibility { get; set; } = Visibility.Visible;

	/// <summary>
	/// Determines wether the DataGrid for th download is visible or not
	/// </summary>
	public Visibility DataGridVisibility { get; set; } = Visibility.Collapsed;

	/// <summary>
	/// Boolean setter which toggles between Image and DataGrid visibility
	/// </summary>
	public bool IsDataGridVisible
	{
	  set
	  {
		this.ImageVisibility = value ? Visibility.Collapsed : Visibility.Visible;
		this.DataGridVisibility = value ? Visibility.Visible : Visibility.Collapsed;
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ImageVisibility)));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.DataGridVisibility)));
	  }
	}


	/// <summary>
	/// Commmand to load e picture from a file.
	/// </summary>
	public MyCommand LoadPictureFromFile { get; set; } = null;
	
	/// <summary>
	/// Private image to show on upload site.
	/// </summary>
	private BitmapImage source = null;

	/// <summary>
	/// Public image to show on upload site.
	/// </summary>
	public BitmapImage Source
	{
	  get
	  {
		return this.source;
	  }
	  set
	  {
		if (this.source?.Equals(value) != true)
		{
		  this.source = value;
		  this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Source)));
		}
	  }
	}
	   
	/// <summary>
	/// List of all pictures that are uploaded in the torren.
	/// </summary>
	private MapField<string, MapField<string, FragmentHolderList>> myUploads;

	/// <summary>
	/// Constructor
	/// </summary>
	public MainWindow()
	{
	  InitializeComponent();
	  this.DataContext = this;
	  this.LoadPictureFromFile = new MyCommand(this.LoadPicture);
	}

	/// <summary>
	/// Saves the choosen file to the network.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private async void SaveFileToNetworkExecuteAsync(object sender, RoutedEventArgs e)
	{
	  NetworkInfo networkinfo = this.LoadNetworkState();
	  //TODO: Das Laden noch richtig einbinden
	  string dominiksVerbindung = this.IP_TEXT.Text;
	  Channel channel = new Channel(dominiksVerbindung, ChannelCredentials.Insecure);
	  TrackerService.TrackerServiceClient trackerserverclient = new TrackerService.TrackerServiceClient(channel);

	  //NetworkInfoResponse networkinforesponse = trackerserverclient.GetNetworkInfo(new NetworkInfoRequest());
	  //Console.WriteLine("\n\n\nNetzwerkinformationen anfragen:");
	  //Console.WriteLine("Fragmentgröße: " + networkinfo.FragmentSize);
	  //Console.WriteLine("Hashalgorithmus: " + networkinfo.HashAlgorithm);
	  //Console.WriteLine("Torrents:");
	  foreach (string item in networkinfo.TorrentList) { Console.WriteLine(item); }

	  //this.SaveCurrentNetworkState(networkinforesponse);

	  //Console.WriteLine("\n\n\nInitialisiere upload:");
	  HashAlgorithm hashAlgorithm = HashAlgorithm.Create(networkinfo.HashAlgorithm);
	  string fileHash;
	  using (FileStream fileStream = new FileStream(this.Source.UriSource.OriginalString, FileMode.Open, FileAccess.Read))
	  {
		fileHash = Helper.GetHashString(hashAlgorithm.ComputeHash(fileStream));
		FileInfo fileInfo = new FileInfo(this.Source.UriSource.OriginalString);

		FileUploadInitiationRequest fileUpInitReq = new FileUploadInitiationRequest();
		fileUpInitReq.FileHash = fileHash;
		fileUpInitReq.FileSize = fileInfo.Length;
		FileUploadInitiationResponse fuires = trackerserverclient.InitiateUpload(fileUpInitReq);
		//Console.WriteLine("FileHash: " + fileHash);
		//Console.WriteLine("FileSize: " + fileInfo.Length);
		//Console.WriteLine("Response ist void!");

		AsyncClientStreamingCall<FileFragment, FileUploadResponse> asyncClientStreamingCall = trackerserverclient.UploadFileFragments();
		FileFragment fileFragment = new FileFragment();
		byte[] buffer = new byte[networkinfo.FragmentSize];
		long packetnumber = (fileInfo.Length + networkinfo.FragmentSize - 1) / networkinfo.FragmentSize;

		int counter = 0;
		fileStream.Seek(0, SeekOrigin.Begin);
		for (int i = 0; i < packetnumber; i++)
		{
		  if (i == packetnumber - 1)
		  {
			buffer = new byte[fileInfo.Length % networkinfo.FragmentSize];
		  }
		  fileStream.Read(buffer, 0, buffer.Length);
		  fileFragment.FileHash = fileHash;
		  fileFragment.Data = ByteString.CopyFrom(buffer);
		  fileFragment.FragmentHash = Helper.GetHashString(hashAlgorithm.ComputeHash(buffer));
		  await asyncClientStreamingCall.RequestStream.WriteAsync(fileFragment);
		  Console.WriteLine(counter++);
		}
		this.SaveNewFileInfoToList(new SavedFileInfo()
		{
		  CreateTime = DateTime.Now,
		  FileHash = fileHash,
		  FileName = System.IO.Path.GetFileName(this.Source.UriSource.LocalPath),
		  FileSize = fileInfo.Length
		});
		FileUploadResponse fur = await asyncClientStreamingCall.ResponseAsync;
		this.myUploads.Add(fileFragment.FileHash, fur.FragmentDistribution);
	  }
	}

	/// <summary>
	/// Loads a picture from the filesystem which is selected to be uploaded to the torrent network
	/// </summary>
	/// <param name="obj"></param>
	private void LoadPicture(object obj)
	{
	  OpenFileDialog dialog = new OpenFileDialog()
	  {
		DefaultExt = ClientResources.ExtensionPNG,
		Multiselect = false,
	  };

	  if (dialog.ShowDialog() == true)
	  {
		try
		{
		  Uri uri = new Uri(dialog.FileName);
		  BitmapImage bmi = new BitmapImage(uri);
		  this.Source = bmi;
		  this.IsDataGridVisible = false;
		}
		catch (Exception e)
		{
		  MessageBox.Show(e.Message.ToString());
		  return;
		}
	  }
	}


	#region Save and Load NetworkInfo
	/// <summary>
	/// Loads the last version of the networkinfo from the filesystem.
	/// </summary>
	/// <returns>Last saved version of Networkinfo from the filesystem</returns>
	private NetworkInfo LoadNetworkState()
	{
	  NetworkInfo networkInfo;
	  DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
	  FileInfo fi = new FileInfo(ClientResources.NetworkInfoFileName);
	  XmlSerializer serializer = new XmlSerializer(typeof(NetworkInfo));
	  FileInfo[] d = di.GetFiles();
	  if (d.Where(x => x.FullName.Equals(fi.FullName, StringComparison.OrdinalIgnoreCase)).Count() == 0)
	  {
		this.UpdateNetworkInfo(null, null);
	  }
	  using (FileStream stream = File.OpenRead(ClientResources.NetworkInfoFileName))
	  {
		networkInfo = (NetworkInfo)serializer.Deserialize(stream);
	  }
	  return networkInfo;
	}

	/// <summary>
	/// Requests the current state of the networkinfo from the given Trackerserver and saves it to the local filesystem.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void UpdateNetworkInfo(object sender, RoutedEventArgs e)
	{
	  string dominiksVerbindung = this.IP_TEXT.Text;
	  Channel channel = new Channel(dominiksVerbindung, ChannelCredentials.Insecure);
	  TrackerService.TrackerServiceClient trackerserverclient = new TrackerService.TrackerServiceClient(channel);
	  NetworkInfoResponse networkinforesponse = trackerserverclient.GetNetworkInfo(new NetworkInfoRequest());
	  NetworkInfo networkInfo = new NetworkInfo()
	  {
		FragmentSize = networkinforesponse.FragmentSize,
		HashAlgorithm = networkinforesponse.HashAlgorithm,
		TorrentList = new HashSet<string>(networkinforesponse.TorrentServer)
	  };
	  XmlSerializer serializer = new XmlSerializer(typeof(NetworkInfo));
	  using (FileStream stream = new FileStream(Directory.GetCurrentDirectory() + ClientResources.Slash + ClientResources.NetworkInfoFileName, FileMode.Create, FileAccess.Write))
	  {
		serializer.Serialize(stream, networkInfo);
	  }
	}
	#endregion

	#region Save and Load files that are saved in torrent network
	/// <summary>
	/// Loads the list of all files that are stored in the torrent network. This list is localy saved.
	/// </summary>
	/// <returns>List of saved files</returns>
	private List<SavedFileInfo> LoadSavedFileInfoList()
	{
	  DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
	  FileInfo fi = new FileInfo(ClientResources.SavedFilesInfoFileName);
	  XmlSerializer serializer = new XmlSerializer(typeof(List<SavedFileInfo>));
	  if (di.EnumerateFiles().Where(x => x.FullName.Equals(fi.FullName, StringComparison.OrdinalIgnoreCase)).Count() == 0)
	  {
		throw new FileNotFoundException();
	  }
	  else
	  {
		using (FileStream stream = File.OpenRead(ClientResources.SavedFilesInfoFileName))
		{
		  return (List<SavedFileInfo>)serializer.Deserialize(stream);
		}
	  }
	}

	/// <summary>
	/// Saves a new entry to the list of the files which are already stored in the torrent netnetwork
	/// </summary>
	/// <param name="savedFileInfo"></param>
	private void SaveNewFileInfoToList(SavedFileInfo savedFileInfo)
	{
	  if (savedFileInfo == null) { throw new ArgumentNullException(); }
	  List<SavedFileInfo> list;
	  try
	  {
		list = this.LoadSavedFileInfoList();
	  }
	  catch (FileNotFoundException)
	  {
		list = new List<SavedFileInfo>();
	  }
	  list.Add(savedFileInfo);
	  FileInfo fi = new FileInfo(ClientResources.File_SavedFilesXML);
	  if (fi.Exists) { fi.Delete(); }
	  using (FileStream stream = new FileStream(ClientResources.SavedFilesInfoFileName, FileMode.CreateNew, FileAccess.Write))
	  {
		new XmlSerializer(typeof(List<SavedFileInfo>)).Serialize(stream, list);
	  }
	}
	#endregion

	/// <summary>
	/// Loads the list of all uploaded files and fills the data grid.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void LoadLocalNetworkFileInformation(object sender, RoutedEventArgs e)
	{
	  ObservableCollection<FileTableItem> fileTable = new ObservableCollection<FileTableItem>();
	  List<SavedFileInfo> savedFiles = null;
	  try
	  {
		savedFiles = this.LoadSavedFileInfoList();
	  }
	  catch
	  {
		MessageBox.Show(ClientResources.Output_NoSavedFiles);
		return;
	  }
	  foreach (SavedFileInfo sfi in savedFiles)
	  {
		fileTable.Add(new FileTableItem()
		{
		  FileSize = sfi.FileSize,
		  FileHash = sfi.FileHash,
		  CreateTime = sfi.CreateTime,
		  FileName = sfi.FileName,
		  DeleteFromNetwork = false,
		  Download = false
		});
	  }
	  this.FileTable = fileTable;
	  this.IsDataGridVisible = true;
	  this.Source = null;
	}

	/// <summary>
	/// Downloads the in the data grid selected files and saves the in the local download directory.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void DownloadFilesFromTorrentNetwork(object sender, RoutedEventArgs e)
	{
	  foreach (FileTableItem filetableitem in this.FileTable)
	  {
		if (filetableitem.Download)
		{
		  FileInfo fi = new FileInfo(Directory.GetCurrentDirectory() + ClientResources.Directory_Downloads + ClientResources.Slash + filetableitem.FileName);
		  if (fi.Exists)
		  {
			string message = ClientResources.Output_ReplaceFile1 + filetableitem.FileName + ClientResources.Output_ReplaceFile2;
			string title = ClientResources.Output_ReplaceFileTitle;
			MessageBoxResult res = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (res == MessageBoxResult.No || res == MessageBoxResult.Cancel) { continue; }
		  }
		  this.DownloadSingleFileFromTorrentNetwork(filetableitem);
		}
	  }
	}

	/// <summary>
	/// Downloads a single file from the torrent network.
	/// </summary>
	/// <param name="filetableitem"></param>
	private void DownloadSingleFileFromTorrentNetwork(FileTableItem filetableitem)
	{
	  NetworkInfo networkinfo = this.LoadNetworkState();

	  for (int i = random.Next(0, networkinfo.TorrentList.Count), j = 0; j < networkinfo.TorrentList.Count; j++, i++)
	  {
		//Get connection
		string choosenTorrent = networkinfo.TorrentList.ElementAt(i % networkinfo.TorrentList.Count);
		if (!Helper.TryUriParse(choosenTorrent, out (string host, int port) channelHostPort)) { continue; }
		//Create connection
		string channelConnection = channelHostPort.host + ClientResources.Colon + channelHostPort.port;
		Channel channel = new Channel(channelConnection, ChannelCredentials.Insecure);
		TorrentService.TorrentServiceClient torrentServiceClient = new TorrentService.TorrentServiceClient(channel);
		//get fragment distribution
		FileDistributionRequest fileDistReq = new FileDistributionRequest() { FileHash = filetableitem.FileHash };
		FileDistributionResponse fileDistRes;
		try
		{
		  fileDistRes = torrentServiceClient.GetFileDistribution(fileDistReq);
		}
		catch (RpcException rpcexception)
		{
		  //server was not able to send me the distribution
		  switch (rpcexception.StatusCode)
		  {
			case StatusCode.NotFound:
			  {
				MessageBox.Show(ClientResources.ReqFileNotFound);
				break;
			  }
			case StatusCode.OutOfRange:
			  {
				MessageBox.Show(ClientResources.ReqFragOneNotFound);
				break;
			  }
			default:
			  {
				MessageBox.Show(ClientResources.OtherError + rpcexception.StatusCode + ClientResources.SemiColon + rpcexception.Message);
				break;
			  }
		  }
		  continue;
		}
		this.DownloadFragments(filetableitem, fileDistRes, networkinfo);
	  }
	}
	/// <summary>
	/// Downloads the file fragments and puts them together
	/// </summary>
	/// <param name="filetableitem">File that gets downloaded.</param>
	/// <param name="fileDistRes">List of Fragments and their holders.</param>
	/// <param name="networkinfo">NetworkInfo of the network.</param>
	private void DownloadFragments(FileTableItem filetableitem, FileDistributionResponse fileDistRes, NetworkInfo networkinfo)
	{
	  DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory() + ClientResources.Directory_Downloads);
	  if (!di.Exists) { di.Create(); }
	  //now i have a valid file distribution
	  using (FileStream fileStream = new FileStream(Directory.GetCurrentDirectory() + ClientResources.Directory_Downloads + ClientResources.Slash + filetableitem.FileName, FileMode.Create, FileAccess.Write))
	  {
		long packetnumber = (filetableitem.FileSize + networkinfo.FragmentSize - 1) / networkinfo.FragmentSize;
		//für jedes Fragment in meiner Verteilung
		foreach (string fragmenthash in fileDistRes.FragmentOrder)
		{
		  //Liste der Torrentserver, die dieses Fragment besitzen
		  FragmentHolderList verteilung = fileDistRes.FragmentDistribution[fragmenthash];
		  RepeatedField<string> torrents = verteilung.EndPoints;
		  FragmentDownloadResponse fragDownRes = null;

		  for (int torrentCounter = 0; torrentCounter < torrents.Count; torrentCounter++)
		  {
			if (!Helper.TryUriParse(torrents[torrentCounter], out (string host, int port) endpointHostPort)) { continue; }
			string channelConnection = endpointHostPort.host + ClientResources.Colon + endpointHostPort.port;
			Channel channel = new Channel(channelConnection, ChannelCredentials.Insecure);
			TorrentService.TorrentServiceClient endpoint = new TorrentService.TorrentServiceClient(channel);
			try
			{
			  fragDownRes = endpoint.DownloadFileFragment(new FragmentDownloadRequest() { FragmentHash = fragmenthash });
			}
			catch (RpcException rpcexception)
			{
			  switch (rpcexception.StatusCode)
			  {
				case StatusCode.NotFound:
				  {
					MessageBox.Show(ClientResources.ReqFragNotFound);
					break;
				  }
				case StatusCode.InvalidArgument:
				  {
					MessageBox.Show(ClientResources.ReqFragInvalidHash);
					break;
				  }
				case StatusCode.Internal:
				  {
					MessageBox.Show(ClientResources.ReqInternalError);
					break;
				  }
				default:
				  {
					MessageBox.Show(ClientResources.OtherError + rpcexception.StatusCode + ClientResources.SemiColon + rpcexception.Message);
					break;
				  }
			  }
			  continue;
			}
			if(fragDownRes != null)
			{
			  break;
			}
		  }

		  if (fragDownRes == null)
		  {
			//keiner der Torrents konnte mir dieses Fragment richtig überreichen. Ich mus den Download abbrechen
			MessageBox.Show(ClientResources.ErrorWhileDownload);
			return;
		  }
		  fragDownRes.Data.WriteTo(fileStream);

		}
	  }
	}

	/// <summary>
	/// Close
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void Close(object sender, RoutedEventArgs e)
	{
	  this.Close();
	}
  }
}




//FileFragment fileFragment = new FileFragment();

// }

//{
//  FileFragment fileFragment = new FileFragment();
//  for (int i = 0; i < packetnumber; i++)
//  {
//	downStream.RequestStream.WriteAsync(new FragmentDownloadRequest() { FragmentHash = fragmenthash })
//	fileStream.Read(buffer, 0, buffer.Length);

//	fileFragment.FileHash = fileHash;
//	fileFragment.Data = ByteString.CopyFrom(buffer);
//	fileFragment.FragmentHash = Helper.GetHashString(hashAlgorithm.ComputeHash(buffer));
//	await asyncClientStreamingCall.RequestStream.WriteAsync(fileFragment);
//	Console.WriteLine(counter++);

//}