using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Google.Protobuf;
using Grpc.Core;
using MyTorrent.gRPC;

namespace TestClient
{
    class Program
    {
        private const string File = @"C:\Users\Dominik.LIN-NET\Pictures\Test.png";
        private const string Address = "127.0.0.1:50052";

        static string GetHashString(byte[] data, HashAlgorithm hashAlgorithm)
        {
            byte[] hash = hashAlgorithm.ComputeHash(data);
            string hashString = "";

            foreach (byte b in hash)
            {
                hashString += b.ToString("X2");
            }

            return hashString;
        }

        static void Main(string[] args)
        {
            byte[] data;

            using (FileStream readStream = new FileStream(File, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    readStream.CopyTo(memoryStream);
                    data = memoryStream.ToArray();
                }
            }

            using FileStream writeStream = new FileStream("DOWNLOAD.png", FileMode.CreateNew, FileAccess.Write, FileShare.None);

            Channel channel = new Channel(Address, ChannelCredentials.Insecure);
            TrackerService.TrackerServiceClient trackerserverclient = new TrackerService.TrackerServiceClient(channel);
            NetworkInfoResponse networkinforesponse = trackerserverclient.GetNetworkInfo(new NetworkInfoRequest());
            
            Console.WriteLine("FragmentSize: " + networkinforesponse.FragmentSize);
            Console.WriteLine("HashAlgorithm: " + networkinforesponse.HashAlgorithm);

            Console.WriteLine("Servers: ");
            foreach (string address in networkinforesponse.TorrentServer)
            {
                Console.WriteLine("[-] " + address);
            }

            var hashAlgorithm = HashAlgorithm.Create(networkinforesponse.HashAlgorithm);
            string fileHash = GetHashString(data, hashAlgorithm);

            FileUploadInitiationRequest fileUploadInitiationRequest = new FileUploadInitiationRequest()
            {
                FileHash = fileHash,
                FileSize = data.LongLength
            };

            trackerserverclient.InitiateUpload(fileUploadInitiationRequest);

            var uploadStream = trackerserverclient.UploadFileFragments();

            long fragmentSize = networkinforesponse.FragmentSize;

            string hashString;

            long length = data.LongLength;

            long offset = 0;

            List<string> sequence = new List<string>();
            
            while (length > 0)
            {
                byte[] fragment = new byte[length >= fragmentSize ? fragmentSize : length];

                Array.Copy(data, offset, fragment, 0, fragment.LongLength);
                offset += fragment.LongLength;
                length -= fragment.LongLength;
                
                hashString = GetHashString(fragment, hashAlgorithm);
                sequence.Add(hashString);

                FileFragment fileFragment = new FileFragment()
                {
                    FileHash = fileHash,
                    FragmentHash = hashString,
                    Data = ByteString.CopyFrom(fragment)
                };

                uploadStream.RequestStream.WriteAsync(fileFragment).GetAwaiter().GetResult();
                uploadStream.RequestStream.CompleteAsync().GetAwaiter().GetResult();
            }

            var response = uploadStream.ResponseAsync.GetAwaiter().GetResult();



            foreach (string fragmentHash in sequence)
            {
                if (response.FragmentDistribution.TryGetValue(fragmentHash, out FragmentHolderList fragmentHolderList))
                {
                    Uri uri = new Uri(fragmentHolderList.EndPoints[0]); 

                    Channel channel2 = new Channel(uri.Host + ":" + uri.Port, ChannelCredentials.Insecure);
                    TorrentService.TorrentServiceClient torrentServiceClient = new TorrentService.TorrentServiceClient(channel2);

                    var downloadResponse = torrentServiceClient.DownloadFileFragment(new FragmentDownloadRequest()
                    {
                        FragmentHash = fragmentHash
                    });

                    downloadResponse.Data.WriteTo(writeStream);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}
