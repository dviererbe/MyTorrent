﻿using System;
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
using MyTorrent;
using System.Security.Cryptography;
using Google.Protobuf;
using Google.Protobuf.Collections;
using System.Xml;
using System.Xml.Serialization;
using UserClient.Properties;

namespace UserClient
{
  /// <summary>
  /// Interaktionslogik für MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
	public event PropertyChangedEventHandler PropertyChanged;

	public MyCommand LoadPictureFromFile { get; set; } = null;
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
	Bitmap OriginalBitmap { get; set; } = null;

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
	  Predicate<object> pictureLoaded = new Predicate<object>((object obj) => this.OriginalBitmap != null);

	  this.LoadPictureFromFile = new MyCommand(this.LoadPicture);
	  //this.SavePictureToTorrentNetwork = new MyCommand(this.SaveNetworkExecuteAsync, pictureLoaded);
	  this.SplitTest = new MyCommand(this.split, pictureLoaded);

	}

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

	private async void SaveNetworkExecuteAsync(object sender, RoutedEventArgs e)
	{
	  NetworkInfo networkinfo = this.LoadNetworkState();
	  //TODO: Das Laden noch richtig einbinden
	  string dominiksVerbindung = "172.20.87.92:50051";
	  Channel channel = new Channel(dominiksVerbindung, ChannelCredentials.Insecure);
	  MyTorrent.TrackerService.TrackerServiceClient trackerserverclient = new TrackerService.TrackerServiceClient(channel);

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
		  fileStream.Read(buffer, 0, buffer.Length);
		  fileFragment.FileHash = fileHash;
		  fileFragment.Data = ByteString.CopyFrom(buffer);
		  fileFragment.FragmentHash = Helper.GetHashString(hashAlgorithm.ComputeHash(buffer));
		  await asyncClientStreamingCall.RequestStream.WriteAsync(fileFragment);
		  Console.WriteLine(counter++);
		}
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
		  if (this.OriginalBitmap != null)
		  {
			this.OriginalBitmap.Dispose();
			this.OriginalBitmap = null;
		  }
		  Uri uri = new Uri(dialog.FileName);
		  BitmapImage bmi = new BitmapImage(uri);
		  this.OriginalBitmap = new Bitmap(dialog.FileName);
		  this.Source = bmi;
		}
		catch (Exception e)
		{
		  MessageBox.Show(e.Message.ToString());
		  return;
		}
	  }
	}
	


	private NetworkInfo LoadNetworkState()
	{
	  NetworkInfo networkInfo;
	  DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
	  FileInfo fi = new FileInfo(ClientResources.NetworkInfoFileName);
	  XmlSerializer serializer = new XmlSerializer(typeof(NetworkInfo));
	  FileInfo[] d = di.GetFiles();
	  if (d.Where(x => x.FullName.Equals(fi.FullName,StringComparison.OrdinalIgnoreCase)).Count() == 0)
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
	  string dominiksVerbindung = "172.20.87.92:50051";
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
		using (FileStream stream = new FileStream(ClientResources.NetworkInfoFileName, FileMode.OpenOrCreate, FileAccess.Write))
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

  }
}

/*
	  //TODO: Netzwerkinfo abspeichern, Torrentliste vorhalten falls Trackerserver ausfällt.
	  DirectoryInfo di = new DirectoryInfo("");
	  FileInfo fi = new FileInfo("");
	  XmlDocument xml = new XmlDocument();

	  // Save the document to a file and auto-indent the output.
	  XmlTextWriter writer = new XmlTextWriter("networkinfo.xml", null);
	  writer.Formatting = Formatting.Indented;
	  if (di.GetFiles().Where(x => x.FullName.Equals(fi.FullName)).Count() == 0)
	  {
		//neu anlegen
		StringBuilder sb = new StringBuilder("");
		sb.Append("<networkinfo>");
		sb.Append("<fragmentsize>");
		sb.Append(networkinforesponse.FragmentSize);
		sb.Append("</fragmentsize>");
		sb.Append("<hashalgorithm>");
		sb.Append(networkinforesponse.HashAlgorithm);
		sb.Append("</hashalgorithm>");
		sb.Append("<torrentlist>");
		foreach (string torrent in networkinforesponse.TorrentServer)
		{
		  sb.Append("<torrent>");
		  sb.Append(torrent);
		  sb.Append("</torrent>");
		}
		sb.Append("</torrentlist>");
		sb.Append("<networkinfo>");
		xml.LoadXml(sb.ToString());
		xml.Save(writer);
	  }
	  else
	  {
		//laden und aktualisieren
		xml.Load("networkinfo.xml");
		xml.DocumentElement.GetAttribute("torrentlist");

	  }
 
  */
