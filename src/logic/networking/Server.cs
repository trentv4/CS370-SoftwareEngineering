using Project.Levels;
using Network;
using System;
using Network.Enums;
using Network.Extensions;

namespace Project.Networking {
    public class Server {

        private Level level = null;

		private ServerConnectionContainer connection;

        public Server(Level gameLevel) {
            level = gameLevel;

            //Configure server connection
            connection = ConnectionFactory.CreateServerConnectionContainer(100);
            connection.ConnectionLost += (a, b, c) => Console.WriteLine($"{connection.Count} {b.ToString()} Connection lost {a.IPRemoteEndPoint.Port}. Reason {c.ToString()}");
            connection.ConnectionEstablished += OnNewConnection;
            connection.AllowUDPConnections = true;
            connection.UDPConnectionLimit = 2;

            //Open server connection
            connection.Start();
			Console.WriteLine($"Server IP address: {connection.IPAddress}");
        }

        private void InitializeServer() {

        }

        ///<summary>Called whenever the server has a new connection. Registers packet handlers.</summary>
        private void OnNewConnection(Connection connection, ConnectionType type)
        {
            Console.WriteLine($"{this.connection.Count} {connection.GetType()} connected on port {connection.IPRemoteEndPoint.Port}");

            //Register packet handlers
            connection.RegisterRawDataHandler("Command", (rawData, con) => { //General "command" packet for testing purposes
				string command = rawData.ToUTF8String();
				switch (command) {
                    case "r":
						uint numAdded = level.Player.Inventory.AddRandomItems(5);
						Console.WriteLine($"Added {numAdded} to player inventory due to network command!");
                        break;
                    case "d":
						level.Player.Health -= 2;
						Console.WriteLine($"Player health lowered by 2 via network command! Health: {level.Player.Health}/{level.Player.MaxHealth}");
                        break;
                    case "h":
						level.Player.Health = level.Player.MaxHealth;
						Console.WriteLine($"Player health fully healed to {level.Player.Health}/{level.Player.MaxHealth} via network command!");
                        break;
                    default:
                        Console.WriteLine($"Unknown network command '{command}'");
                        break;
                }
			});
        }
    }
}