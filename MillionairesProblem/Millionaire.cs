using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Millionaire
{
	class Program
	{
		private const int listenPort = 4759;
		private static object locky = new object();
		static void Main(string[] args)
		{

			bool done = false;
			Console.WriteLine("Enter your name");
			string name = Console.ReadLine();
			UdpClient listener = new UdpClient();
			listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			listener.Client.Bind(new IPEndPoint(IPAddress.Any, listenPort));
			while (true)
			{
				try
				{
					while (!done)
					{
						Socket client = new Socket(AddressFamily.InterNetwork,
							SocketType.Stream, ProtocolType.Tcp);
						IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
						Console.WriteLine("Looking for a new boat..");
						//Thread.Sleep(1000);
						byte[] bytes = listener.Receive(ref groupEP);
						string boatName = getName(bytes);
						if (null == boatName)
							break;
						int boatPort = getPort(bytes);
						Console.WriteLine("Requsting to board the {0}", boatName);
						try
						{
							client.Connect(groupEP.Address, boatPort);
						}
						catch (Exception e)
						{
							continue;
						}

						Console.WriteLine("I am now aboard the {0}", boatName);
						bytes = new byte[1024];
						client.ReceiveTimeout = 500;
						int bytesRec = client.Receive(bytes);
						client.ReceiveTimeout = 0;
						string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
						Console.WriteLine(data);
						client.Send(Encoding.ASCII.GetBytes(name + "\r\n"));
						Thread t = new Thread(new ParameterizedThreadStart(talkToBoat));
						t.Start(client);
						while (bytesRec != 0)
						{
							try
							{
								bytes = new byte[1024];
								bytesRec = client.Receive(bytes);
								data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
								Console.WriteLine(data);
							}
							catch (Exception e)
							{
								t.Abort();
								break;
							}
						}
					}
				}
				catch (Exception e)
				{
					continue;
				}
			}
		}

		private static void talkToBoat(object obj)
		{
			Socket client = (Socket)obj;
			try
			{
				while (client.Connected)
				{
					string send = Console.ReadLine();
					send += "\r\n";
					client.Send(Encoding.ASCII.GetBytes(send));

					if (send.Equals(""))
					{
						client.Disconnect(false);
						return;
						//client.Shutdown(SocketShutdown.Both);
					}
				}
			}
			catch (Exception e)
			{
				return;
			}
		}

		private static int getPort(byte[] bytes)
		{
			byte[] bytePort = { bytes[bytes.Length - 2], bytes[bytes.Length - 1] };
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytePort);
			int port = (int)BitConverter.ToUInt16(bytePort, 0);
			return Convert.ToInt32(BitConverter.ToUInt16(bytePort, 0));
		}

		private static string getName(byte[] bytes)
		{
			if (bytes.Length > 45)
				return null;
			string boatName = Encoding.ASCII.GetString(bytes, 11, 32);
			return boatName;
		}
	}
}