using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;

namespace ConsoleApplication
{
    class Program
    {
        const string STRING_TERMINATOR = ";";

        public static Hashtable clientList = new Hashtable();
        private static int userCnt = 0;
        //private static Mutex mtx = new Mutex();
        private static object lockSocket = new object();

        static void Main(string[] args)
        {
            try
            {
                TcpListener serverSocket = new TcpListener(IPAddress.Any, 8888);
                TcpClient clientSocket = default(TcpClient);
                int counter = 0;
                byte[] bytesFrom = new byte[1024];
                string bytesFromClient = "";

                serverSocket.Start(); //Listen
                Console.WriteLine("C# Server Started...");
                counter = 0;

                while (true)
                {
                    counter += 1;
                    clientSocket = serverSocket.AcceptTcpClient();
                    bytesFromClient = "";

                    counter = userCnt;
                    userCnt++;

                    handleClient client = new handleClient();
                    clientList.Add(counter, client);

                    client.startClient(clientSocket, clientList, counter);
                }

                clientSocket.Close();
                serverSocket.Stop();
                Console.WriteLine("exit");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString);
            }
        }

        public static TcpClient GetSocket(int id)
        {
            TcpClient socket = null;
            if (clientList.ContainsKey(id))
            {
                handleClient hc = (handleClient)clientList[id];
                socket = hc.clientSocket;
            }
            return socket;
        }

        public static void UserAdd(string clientNo)
        {
            broadcast(clientNo + "Joined", "", false);
            Console.WriteLine(clientNo + "Joined Chat room");
        }

        public static void UserLeft(int userID, string clientID)
        {
            if (clientList.ContainsKey(userID))
            {
                broadcast(clientID + "$#Left#", clientID, false);
                Console.WriteLine("Client Left: " + clientID);

                TcpClient clientSocket = GetSocket(userID);

                clientList.Remove(userID);
                clientSocket.Close();
            }
        }

        public static void broadcast(string msg, string uName, bool flag)
        {
            //mtx.WaitOne();
            Byte[] broadcastBytes = null;
            List<object> deletedClients = new List<object>();

            if (flag == true) //클라이언트
            {
                broadcastBytes = Encoding.UTF8.GetBytes(uName + "$" + msg + STRING_TERMINATOR);
            }

            else //서버 
            {
                broadcastBytes = Encoding.UTF8.GetBytes(msg + STRING_TERMINATOR);
            }

            lock (lockSocket)
            {
                foreach (DictionaryEntry Item in clientList)
                {
                    TcpClient broadcastSocket;
                    handleClient hc = (handleClient)Item.Value;
                    broadcastSocket = hc.clientSocket;
                    NetworkStream broadcastStream = broadcastSocket.GetStream();
                    try
                    {
                        broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);
                        broadcastStream.Flush();
                    }
                    catch (Exception ex)
                    {
                        deletedClients.Add(Item.Key);
                    }
                }
                foreach (var item in deletedClients)
                {
                    TcpClient broadcastSocket;
                    handleClient hc = (handleClient)clientList[item];
                    broadcastSocket = hc.clientSocket;
                    broadcastSocket.Close();
                    clientList.Remove(item);
                }

            }


            //mtx.ReleaseMutex();
        } //end broadcast
    }

    public class handleClient
    {
        const string COMMAND_ENTER = "#Enter#";
        const string COMMAND_MOVE = "#Move#";
        const char CHAR_TERMINATOR = ';';



        public TcpClient clientSocket;
        public int userID;
        public string clientID;

        public float posX;
        public float posY;

        private Hashtable clientList;
        private bool noConnection = false;

        public void startClient(TcpClient inClientSocket,
            Hashtable cList, int userSerial)
        {
            userID = userSerial;
            clientList = cList;
            clientSocket = inClientSocket;

            Thread ctThread = new Thread(doChat);
            ctThread.Start();
        }

        bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
            {
                return false;
            }
            else return true;
        }

        private void doChat()
        {
            byte[] bytesFrom = new byte[1024];
            string dataFromClient = "";
            NetworkStream networkStream = clientSocket.GetStream();

            while (!noConnection)
            {
                try
                {
                    int numBytesRead;
                    if (!SocketConnected(clientSocket.Client))
                    {
                        noConnection = true;
                    }
                    else
                    {
                        if (networkStream.DataAvailable)
                        {
                            while (networkStream.DataAvailable)
                            {
                                numBytesRead = networkStream.Read(bytesFrom, 0, bytesFrom.Length);
                                dataFromClient = Encoding.UTF8.GetString(bytesFrom, 0, numBytesRead);
                            }

                            int idx = dataFromClient.IndexOf('$');

                            if (clientID == null && idx > 0) //닉네임 전송
                            {
                                clientID = dataFromClient.Substring(0, idx);
                                Program.UserAdd(clientID);
                            }
                            int pos = idx + 1;
                            if (pos < dataFromClient.Length)// 채팅 내용
                            {
                                dataFromClient = dataFromClient.Substring(pos, dataFromClient.Length - pos);
                                Console.WriteLine("From Client - " + clientID + ": " + dataFromClient);
                                ProcessCommand(clientID, dataFromClient);
                                Program.broadcast(dataFromClient, clientID, true);
                            }
                            else
                            {
                                dataFromClient = "";
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    noConnection = true;
                    Console.WriteLine(e.ToString());
                }
            }
            Program.UserLeft(userID, clientID);
        }

        private string DeleteTerminator(string remain)
        {
            int idx = remain.IndexOf(CHAR_TERMINATOR);
            if (idx > 0)
            {
                remain = remain.Substring(0, idx);
            }
            return remain;
        }

        private void ProcessMove(string clientID, string remain)
        {
            var strs = remain.Split(',');
            try
            {
                posX = float.Parse(strs[0]);
                posY = float.Parse(strs[1]);
                Console.WriteLine("User move - " + clientID + "to" + posX + "," + posY);
            }
            catch (Exception e)
            {

            }
        }

        private void ProcessCommand(string clientID, string dataFromClient)
        {
            if (dataFromClient[0] == '#')
            {
                string command;
                string remain;
                int idx = dataFromClient.IndexOf('#', 1);
                if (idx > 1)
                {
                    command = dataFromClient.Substring(0, idx + 1);
                    if (command == COMMAND_MOVE)
                    {
                        remain = DeleteTerminator(dataFromClient.Substring(idx + 1));
                        ProcessMove(clientID, remain);
                    }
                }
            }
        }
    }
}