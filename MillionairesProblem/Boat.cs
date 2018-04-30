using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Boat
{
	class Program
	{
		private static Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		private static List<Socket> clientSockets = new List<Socket>();
		private static Dictionary<string, int> clients = new Dictionary<string, int>();
		private static string name;
		private static byte[] buffer = new byte[1024];
		private static object locky = new object();
		static void Main(string[] args)
		{
			name = "Wonderful exercise in networks  ";
			Console.Title = name;
			while (true)
			{
				Thread t = new Thread(() => SetupServer());
				t.Start();
				Console.ReadLine(); // When we press enter close everything
				CloseAllSockets();
				t.Abort();
			}

		}

		private static void SetupServer()
		{
			serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			byte[] massegearray = new byte[45];

			string messege = "IntroToNets" + name;
			byte[] messegebyte = Encoding.ASCII.GetBytes(messege);

			for (int i = 0; i < 43; i++)
			{
				massegearray[i] = messegebyte[i];
			}
			UdpClient client = new UdpClient();
			IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, 4759);
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

			serverSocket.Bind(ep);
			everymin(massegearray, name, client, ip, serverSocket, ep);
			List<Socket> serversocketInbal = new List<Socket>();
			serversocketInbal.Add(serverSocket);
			serverSocket.Listen(1000);
			while (true)
			{
				Socket.Select(serversocketInbal, null, null, 1000);
				if (serversocketInbal.Count == 0)
				{
					serversocketInbal.Add(serverSocket);
				}
				else
				{
					Socket handler = serverSocket.Accept();
					Thread t = new Thread(() => talkWithClient(handler));
					t.Start();
				}
			}
		}

		private static void talkWithClient(Socket handler)
		{
			lock (locky)
			{
				clientSockets.Add(handler);
			}
			byte[] recBuf = new byte[1024];

			handler.Send(Encoding.ASCII.GetBytes(String.Format("Welcome to the {0}! what is your name?\r\n", name)));
			Thread.Sleep(1000);
			int received = handler.Receive(recBuf);

			string clientName = Encoding.ASCII.GetString(recBuf, 0, received);
			Console.WriteLine("Received Text: " + clientName);
			clients.Add(clientName, 0); // Getting the name
			string ms = String.Format("A Millionaire named {0} has joined the boat. " +
									  "The richest person on the boat right now is {1}\r\n"
				, clientName, maxIncome());
			sendToAll(ms);

			while (true)
			{
				try
				{
					received = handler.Receive(recBuf);
					string incomeText = Encoding.ASCII.GetString(recBuf, 0, received);
					Console.WriteLine("Received Text: " + incomeText);
					int income;
					if (int.TryParse(incomeText, out income))
					{
						clients[clientName] = income;
						sendToAll(String.Format("{0} has updated his/her income. the richest person on the boat right now is {1}\r\n", clientName, maxIncome()));
					}
					else if (incomeText.Length == 2)
					{
						clients.Remove(clientName);
						lock (locky)
						{
							handler.Close();
							clientSockets.Remove(handler);
							if (clientSockets.Count > 0)
								sendToAll(String.Format("{0} has left the boat, the richest person on the boat right now is {1}\r\n", clientName, maxIncome()));
						}
						break;
					}
				}
				catch (Exception e)
				{
					clients.Remove(clientName);
					clientSockets.Remove(handler);
					if (clientSockets.Count > 0)
						sendToAll(String.Format("{0} has left the boat, the richest person on the boat right now is {1}\r\n", clientName, maxIncome()));
					break;
				}

			}
		}

		private static void sendToAll(string massege)
		{
			lock (locky)
			{
				for (int i = 0; i < clientSockets.Count; i++)
				{
					clientSockets[i].Send(Encoding.ASCII.GetBytes(massege));
				}
			}

		}

		private static void CloseAllSockets()
		{
			lock (locky)
			{
				foreach (Socket socket in clientSockets)
				{
					socket.Close();
				}
				clientSockets.Clear();
				// serverSocket.Close();
			}

			clients.Clear();
		}



		public static void everymin(byte[] massegearray, string name, UdpClient client, IPEndPoint ip, Socket socket, IPEndPoint ep)
		{
			new System.Threading.Timer((e) => { Send(massegearray, name, client, ip, socket, ep); },
				null,
				TimeSpan.Zero,
				TimeSpan.FromMinutes(0.25)
			);
		}


		public static void Send(byte[] massegearray, string name, UdpClient client, IPEndPoint ip, Socket socket, IPEndPoint ep)
		{
			try
			{
				int port = ((IPEndPoint)socket.LocalEndPoint).Port;
				socket.Listen(1000);
				//byte[] portbyte = BitConverter.GetBytes(port);
				byte[] portbyte = BitConverter.GetBytes((ushort)port);
				if (BitConverter.IsLittleEndian)
				{
					Array.Reverse(portbyte);
				}
				massegearray[43] = portbyte[0];
				massegearray[44] = portbyte[1];
				client.Send(massegearray, massegearray.Length, ip);
			}
			catch (Exception e)
			{
				Send(massegearray, name, client, ip, socket, ep);
			}

		}


		public static string maxIncome()
		{
			var myList = clients.ToList();
			myList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
			return myList[0].Key;
		}
	}
}