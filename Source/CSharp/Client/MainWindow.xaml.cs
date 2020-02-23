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
	public event PropertyChangedEventHandler PropertyChanged;

	static private Random rnd = new Random();

	private ObservableCollection<FileTableItem> fileTable = null;
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

	public Visibility ImageVisibility { get; set; } = Visibility.Visible;
	public Visibility DataGridVisibility { get; set; } = Visibility.Collapsed;
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



	public MyCommand LoadPictureFromFile { get; set; } = null;
	public MyCommand LoadPictureFromFile2 { get; set; } = null;
	public MyCommand SavePictureToTorrentNetwork { get; set; } = null;
	public MyCommand FindPictureInTorrentNetwork { get; set; } = null;
	public MyCommand LoadAndComposePicture { get; set; } = null;
	public MyCommand SavePictureToFileSystem { get; set; } = null;
	public MyCommand SplitTest { get; set; } = null;

	BitmapImage source = null;
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

	private MapField<string, MapField<string, FragmentHolderList>> myUploads;

	public MainWindow()
	{
	  InitializeComponent();
	  this.DataContext = this;
	  this.InitCommands();
	  Console.WriteLine("START");
	}

	private void InitCommands()
	{
	  Predicate<object> pictureLoaded = new Predicate<object>((object obj) => this.Source != null);

	  this.LoadPictureFromFile = new MyCommand(this.LoadPicture);
	  //this.SavePictureToTorrentNetwork = new MyCommand(this.SaveNetworkExecuteAsync, pictureLoaded);
	  this.SplitTest = new MyCommand(this.split, pictureLoaded);


	  this.LoadPictureFromFile2 = new MyCommand(this.LoadPicture, pictureLoaded);

	}

	#region Test zum Datei splitten. Nicht wichtig für das eigentliche Programm
	private void split(object obj)
	{
	  SplitFile(this.Source.UriSource.OriginalString, 1024, @"C:\Users\Dirk Neumann\Desktop\Splits");
	}
	public static void SplitFile(string inputFile, int chunkSize, string path)
	{
	  const int BUFFER_SIZE = 20 * 1024;
	  byte[] buffer = new byte[BUFFER_SIZE];

	  using (Stream input = File.OpenRead(inputFile))
	  {
		int index = 0;
		while (input.Position < input.Length)
		{
		  using (Stream output = File.Create(path + "\\" + index))
		  {
			int remaining = chunkSize, bytesRead;
			while (remaining > 0 && (bytesRead = input.Read(buffer, 0,
					Math.Min(remaining, BUFFER_SIZE))) > 0)
			{
			  output.Write(buffer, 0, bytesRead);
			  remaining -= bytesRead;
			}
		  }
		  index++;
		  Thread.Sleep(50); // experimental; perhaps try it
		}
	  }
	}
	#endregion

	private async void SaveFileToNetworkExecuteAsync(object sender, RoutedEventArgs e)
	{
	  NetworkInfo networkinfo = this.LoadNetworkState();
	  //TODO: Das Laden noch richtig einbinden
	  string dominiksVerbindung = this.IP_TEXT.Text;
	  Channel channel = new Channel(dominiksVerbindung, ChannelCredentials.Insecure);
	  TrackerService.TrackerServiceClient trackerserverclient = new TrackerService.TrackerServiceClient(channel);

	  //NetworkInfoResponse networkinforesponse = trackerserverclient.GetNetworkInfo(new NetworkInfoRequest());
	  Console.WriteLine("\n\n\nNetzwerkinformationen anfragen:");
	  Console.WriteLine("Fragmentgröße: " + networkinfo.FragmentSize);
	  Console.WriteLine("Hashalgorithmus: " + networkinfo.HashAlgorithm);
	  Console.WriteLine("Torrents:");
	  foreach (string item in networkinfo.TorrentList) { Console.WriteLine(item); }

	  //this.SaveCurrentNetworkState(networkinforesponse);

	  Console.WriteLine("\n\n\nInitialisiere upload:");
	  HashAlgorithm hashAlgorithm = HashAlgorithm.Create(networkinfo.HashAlgorithm);
	  string fileHash = "";
	  using (FileStream fileStream = new FileStream(this.Source.UriSource.OriginalString, FileMode.Open, FileAccess.Read))
	  {
		fileHash = Helper.GetHashString(hashAlgorithm.ComputeHash(fileStream));
		FileInfo fileInfo = new FileInfo(this.Source.UriSource.OriginalString);

		FileUploadInitiationRequest fileUpInitReq = new FileUploadInitiationRequest();
		fileUpInitReq.FileHash = fileHash;
		fileUpInitReq.FileSize = fileInfo.Length;
		FileUploadInitiationResponse fuires = trackerserverclient.InitiateUpload(fileUpInitReq);
		Console.WriteLine("FileHash: " + fileHash);
		Console.WriteLine("FileSize: " + fileInfo.Length);
		Console.WriteLine("Response ist void!");

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

	private void LoadPicture(object obj)
	{
	  OpenFileDialog dialog = new OpenFileDialog()
	  {
		DefaultExt = ".png",
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
	  this.SaveCurrentNetworkState(networkInfo);
	}
	private void SaveCurrentNetworkState(NetworkInfo networkInfo)
	{
	  DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
	  FileInfo fi = new FileInfo(ClientResources.NetworkInfoFileName);
	  XmlSerializer serializer = new XmlSerializer(typeof(NetworkInfo));
	  FileInfo[] d = di.GetFiles();
	  if (d.Where(x => x.FullName.Equals(fi.FullName, StringComparison.OrdinalIgnoreCase)).Count() == 0)
	  {
		using (FileStream stream = new FileStream(ClientResources.NetworkInfoFileName, FileMode.CreateNew, FileAccess.Write))
		{
		  serializer.Serialize(stream, networkInfo);
		}
	  }
	  else
	  {
		HashSet<string> dezerialized;
		using (FileStream stream = File.OpenRead(ClientResources.NetworkInfoFileName))
		{
		  dezerialized = ((NetworkInfo)serializer.Deserialize(stream)).TorrentList;
		}
		bool changed = false;
		foreach (string item in dezerialized)
		{
		  changed |= networkInfo.TorrentList.Add(item);
		}
		if (changed)
		{
		  using (FileStream stream = new FileStream(ClientResources.NetworkInfoFileName, FileMode.CreateNew, FileAccess.Write))
		  {
			serializer.Serialize(stream, networkInfo);
		  }
		}
	  }
	}
	#endregion

	#region Save and Load files that are saved in torrent network
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
	private void SaveNewFileInfoToList(SavedFileInfo sfi)
	{
	  if (sfi == null) { throw new ArgumentNullException(); }
	  List<SavedFileInfo> list;
	  try
	  {
		list = this.LoadSavedFileInfoList();
	  }
	  catch (FileNotFoundException)
	  {
		list = new List<SavedFileInfo>();
	  }
	  list.Add(sfi);
	  FileInfo fi = new FileInfo("savedfiles.xml");
	  if (fi.Exists) { fi.Delete(); }
	  using (FileStream stream = new FileStream(ClientResources.SavedFilesInfoFileName, FileMode.CreateNew, FileAccess.Write))
	  {
		new XmlSerializer(typeof(List<SavedFileInfo>)).Serialize(stream, list);
	  }
	}
	#endregion

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
		MessageBox.Show("Sie haben noch keine Datein im Netzwerkgespeichert.");
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


	private void DownloadFilesFromTorrentNetwork(object sender, RoutedEventArgs e)
	{
	  foreach (FileTableItem filetableitem in this.FileTable)
	  {
		if (filetableitem.Download)
		{
		  this.DownloadSingleFileFromTorrentNetwork(filetableitem);
		}
	  }
	}

	private async void DownloadSingleFileFromTorrentNetwork(FileTableItem filetableitem)
	{
	  NetworkInfo networkinfo = this.LoadNetworkState();
	  string choosenTorrent = Helper.GetRandomElement(networkinfo.TorrentList);
	  if (!Helper.TryUriParse(choosenTorrent, out (string host, int port) channelHostPort))
	  {
		MessageBox.Show("Invalid torrent uri");
		return;
	  }
	  string channelConnection = channelHostPort.host + ":" + channelHostPort.port;
	  Channel channel = new Channel(channelConnection, ChannelCredentials.Insecure);
	  TorrentService.TorrentServiceClient torrentServiceClient = new TorrentService.TorrentServiceClient(channel);

	  FileDistributionRequest fileDistReq = new FileDistributionRequest() { FileHash = filetableitem.FileHash };
	  FileDistributionResponse fileDistRes;
	  try
	  {
		fileDistRes = torrentServiceClient.GetFileDistribution(fileDistReq);
	  }
	  catch (Exception e)
	  {
		MessageBox.Show(e.Message);
		return;
	  }
	  AsyncDuplexStreamingCall<FragmentDownloadRequest, FragmentDownloadResponse> downStream = torrentServiceClient.DownloadFileFragment();

	  DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory() + "/Downloads");
	  if (!di.Exists) { di.Create(); }
	  using (FileStream fileStream = new FileStream(di.FullName + "/" + filetableitem.FileName, FileMode.CreateNew, FileAccess.Write))
	  {
		long packetnumber = (filetableitem.FileSize + networkinfo.FragmentSize - 1) / networkinfo.FragmentSize;
		int counter = 0;
		fileStream.Seek(0, SeekOrigin.Begin);
		foreach (string fragmenthash in fileDistRes.FragmentOrder)
		{
		  string choosenEndpoint = Helper.GetRandomElement(fileDistRes.FragmentDistribution[fragmenthash].EndPoints);
		  if (!Helper.TryUriParse(choosenTorrent, out (string host, int port) endpointHostPort))
		  {
			MessageBox.Show("Invalid torrent uri");
			return;
		  }
		  await downStream.RequestStream.WriteAsync(new FragmentDownloadRequest() { FragmentHash = fragmenthash });
		  if (await downStream.ResponseStream.MoveNext())
		  {
			byte[] data = downStream.ResponseStream.Current.Data.ToArray();
			if (data.Length != networkinfo.FragmentSize) { /*Fehler*/}
			else
			{
			  fileStream.Write(data, 0, data.Length);
			}
		  }
		  else {/*Fehler*/}

		  //bool x = await downStream.ResponseStream.MoveNext();


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
	  }
	}
  }
}