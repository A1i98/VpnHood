﻿using System;
using System.Net;
using System.Net.Sockets;

namespace VpnHood.Common
{
    public static class Util
    {
        public const int SocketStackSize_Datagram = 65536;
        public const int SocketStackSize_Stream = 65536 * 2;
        public const int TlsHandshakeLength = 5000;

        public static ulong RandomLong()
        {
            Random random = new Random();
            byte[] bytes = new byte[8];
            random.NextBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public static  bool TryParseIpEndPoint(string value, out IPEndPoint ipEndPoint)
        {
            ipEndPoint = null;
            var addr = value.Split(':');
            if (addr.Length != 2) return false;
            if (!IPAddress.TryParse(addr[0], out IPAddress ipAddress)) return false;
            if (!int.TryParse(addr[1], out int port)) return false;
            ipEndPoint = new IPEndPoint(ipAddress, port);
            return true;
        }

        public static IPEndPoint ParseIpEndPoint(string value)
        {
            if (!TryParseIpEndPoint(value, out IPEndPoint ipEndPoint))
                throw new ArgumentException($"Could not parse {value} to an IpEndPoint");
            return ipEndPoint;
        }

        public static IPAddress GetLocalIpAddress()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 0);
            var endPoint = (IPEndPoint)socket.LocalEndPoint;
            return endPoint.Address;
        }
    }
}
