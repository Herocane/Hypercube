﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using System.Web.Services;
using System.IO;
using System.Threading;
using System.Security.Cryptography;

using Hypercube_Classic.Client;

namespace Hypercube_Classic.Core {
    public class Heartbeat {
        public string Salt;//, MCSalt;
        Hypercube ServerCore;
        Thread HeartbeatThread;//, MinecraftThread;

        /// <summary>
        /// Generates a new salt and starts heartbeating.
        /// </summary>
        /// <param name="Core"></param>
        public Heartbeat(Hypercube Core) {
            ServerCore = Core;
            CreateSalt();

            //if (Core.nh.DualHeartbeat) {
            //    MinecraftThread = new Thread(DoHeartbeatMinecraft);
            //    MinecraftThread.Start();
            //}

            HeartbeatThread = new Thread(DoHeartbeatClassicube);
            HeartbeatThread.Start();

        }

        /// <summary>
        /// Aborts the heartbeat thread.
        /// </summary>
        public void Shutdown() {
            if (HeartbeatThread != null)
                HeartbeatThread.Abort();

            //if (MinecraftThread != null)
            //    MinecraftThread.Abort();
        }

        /// <summary>
        /// Creates a random 32-character salt for verification.
        /// </summary>
        public void CreateSalt() {
            Salt = "";
            var Random = new Random();

            for (int i = 1; i < 33; i++)
                Salt += (char)(65 + Random.Next(25));

            //MCSalt = "";

            //for (int i = 1; i < 33; i++)
            //    MCSalt += (char)(65 + Random.Next(25));
        }

        public string GetIPv4Address(string site) {
            IPAddress[] Addresses = Dns.GetHostAddresses(site);
            IPAddress v4 = Addresses.First(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            return v4.ToString();
        }
        /// <summary>
        /// Sends a heartbeat to Classicube.net for this server.
        /// </summary>
        public void DoHeartbeatClassicube() {
            var Request = new WebClient();
            Request.Proxy = new WebProxy("http://" + GetIPv4Address("classicube.net") + ":80/"); // -- Makes sure we're using an IPv4 Address and not IPv6.
            
            while (ServerCore.Running) {
                try {
                    string Response = Request.DownloadString("http://www.classicube.net/heartbeat.jsp?port=" + ServerCore.nh.Port.ToString() + "&users=" + ServerCore.OnlinePlayers.ToString() + "&max=" + ServerCore.nh.MaxPlayers.ToString() + "&name=" + HttpUtility.UrlEncode(ServerCore.ServerName) + "&public=" + ServerCore.nh.Public.ToString() + "&salt=" + HttpUtility.UrlEncode(Salt));
                    ServerCore.Logger._Log("Info", "Heartbeat", "Heartbeat sent.");
                    File.WriteAllText("ServerURL.txt", Response);
                } catch (Exception e) {
                    ServerCore.Logger._Log("Error", "Heartbeate", "Failed to send heartbeat.");
                    ServerCore.Logger._Log("Error", "Classicube", e.Message);
                }

                Thread.Sleep(45000);
            }
        }

        //public void DoHeartbeatMinecraft() {
        //    var Request = new WebClient();

        //    while (ServerCore.Running) {
        //        try {
        //            string Response = Request.DownloadString("https://www.minecraft.net/heartbeat.jsp?port=" + ServerCore.nh.Port.ToString() + "&users=" + ServerCore.OnlinePlayers.ToString() + "&max=" + ServerCore.nh.MaxPlayers.ToString() + "&name=" + HttpUtility.UrlEncode(ServerCore.ServerName) + "&public=" + ServerCore.nh.Public.ToString() + "&salt=" + HttpUtility.UrlEncode(Salt));
        //            ServerCore.Logger._Log("Info", "Heartbeat_Minecraft", "Heartbeat sent.");
        //            File.WriteAllText("ServerURL_MC.txt", Response);
        //        } catch {
        //            ServerCore.Logger._Log("Error", "Heartbeat_Minecraft", "Failed to send heartbeat.");
        //        }

        //        Thread.Sleep(45000);
        //    }
        //}

        /// <summary>
        /// Verifys the authenticity of this user.
        /// </summary>
        /// <param name="Client"></param>
        /// <returns></returns>
        public bool VerifyClientName(NetworkClient Client) {
            if (Client.CS.IP == "127.0.0.1" || Client.CS.IP.Substring(0, 7) == "192.168" || ServerCore.nh.VerifyNames == false)
                return true;

            var MD5Creator = MD5.Create();
            string Correct = BitConverter.ToString(MD5Creator.ComputeHash(Encoding.ASCII.GetBytes(Salt + Client.CS.LoginName))).Replace("-", "");
            //string CorrectMC = BitConverter.ToString(MD5Creator.ComputeHash(Encoding.ASCII.GetBytes(MCSalt + Client.CS.LoginName))).Replace("-", "");

            if (Correct.Trim().ToLower() == Client.CS.MPPass.Trim().ToLower()) 
                return true;
            // } else if (CorrectMC.Trim().ToLower() == Client.CS.MPPass.Trim().ToLower()) {
            //    Client.CS.Service = "Minecraft";
            //    return true;
            else {
                ServerCore.Logger._Log("Info", "Heartbeat", Correct.Trim() + " != " + Client.CS.MPPass.Trim());
                //ServerCore.Logger._Log("Info", "Heartbeat", CorrectMC.Trim() + " != " + Client.CS.MPPass.Trim());
                return false;
            }
        }
    }
}
