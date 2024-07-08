using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Wacom.Kiosk.Integrator;
using Wacom.Kiosk.Message.Shared;
using System.Net.Sockets;
using System.Net;
using System.Runtime.CompilerServices;
using System.Configuration;

namespace Wacom.Kiosk.IntegratorUI
{
    public static class KioskServer
    {

        /// <summary>
        /// The messaging queue server instance
        /// </summary>
        public static Server ServerInstance { get; } = new ServerFactory().Create(certificate: "MyServer.pfx", certificatePassword: "password", port: 5555, local: true, useNamedPipeServer: ConfigurationManager.AppSettings.Get("useNamedPipes"));
        //public static MQServer ServerInstance { get; } = new MQServer(certificate: "MyServer.pfx", certificatePassword: "password", port: 5555, local: true);
        //public static NamedPipeServer ServerInstance { get; } = new NamedPipeServer("MyServer.pfx","password");

        /// <summary>
        /// The kiosk client that the server uses to communicate with the tablet client.
        /// </summary>
        public static KioskClient Sender { get; } = new KioskClient("IntegratorUI");

        /// <summary>
        /// List of client name strings.
        /// </summary>
        public static List<string> ClientNames { get => ServerInstance.ActiveClients.Select(client => client.Name).ToList(); }
        /// <summary>
        /// Obtains the <see cref="ActiveClient"/> instance by name.
        /// </summary>
        /// <param name="name">The name of the client</param>
        /// <returns>The <see cref="ActiveClient"/> instance. Null if none found.</returns>
        public static ActiveClient GetActiveClient(string name)
        {
            return ServerInstance.ActiveClients.FirstOrDefault(x => x.Name == name);
        }
        /// <summary>
        /// Returns a boolean flag to determine if a client is connected.
        /// </summary>
        /// <param name="clientName">The client name</param>
        /// <returns>True if connected.</returns>
        public static bool IsClientRegistered(string clientName)
        {
            if (!ClientNames.Contains(clientName))
            {
                return true;
            }

            return false;
        }
        /// <summary>Sends the message.</summary>
        /// <param name="clientName">Name of the client.</param>
        /// <param name="bytes">The message bytes.</param>
        public static void SendMessage(string clientName, byte[] bytes)
        {
            ServerInstance.SendMessage(clientName, bytes);
        }
        ///// <summary>Broadcasts the message.</summary>
        ///// <param name="bytes">The message bytes.</param>
        //public static void BroadcastMessage(byte[] bytes)
        //{
        //    Mq.BroadcastMessage(bytes);
        //}
        /// <summary>
        /// Closes the MQ server instance and closes all active client connections.
        /// </summary>
        public static void Close()
        {
            ServerInstance.Dispose();
        }
    }
}
