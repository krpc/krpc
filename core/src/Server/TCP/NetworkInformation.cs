using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace KRPC.Server.TCP
{
    /// <summary>
    /// Utilities for getting information about the TCP/IP network.
    /// </summary>
    public static class NetworkInformation
    {
        static IEnumerable<NetworkInterface> Interfaces {
            get {
                try {
                    return NetworkInterface.GetAllNetworkInterfaces ().ToList ();
                } catch (NetworkInformationException) {
                    return new List<NetworkInterface> ();
                }
            }
        }

        /// <summary>
        /// Returns the IPv4 address of all local network interfaces.
        /// </summary>
        public static IEnumerable<IPAddress> LocalIPAddresses {
            get {
                foreach (var adapter in Interfaces) {
                    foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses) {
                        if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork) {
                            yield return unicastIPAddressInformation.Address;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the IPv4 subnet mask of the network interface with the given IPv4 address.
        /// </summary>
        public static IPAddress GetSubnetMask (IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces()) {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses) {
                    var unicastAddress = unicastIPAddressInformation.Address;
                    if (unicastAddress.AddressFamily == AddressFamily.InterNetwork) {
                        if (address.Equals (unicastAddress)) {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException ("Network interface with IPv4 address " + address + " does not exist.");
        }
    }
}
