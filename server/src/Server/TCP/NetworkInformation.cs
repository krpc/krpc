using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace KRPC.Server.TCP
{
    static class NetworkInformation
    {
        static IEnumerable<NetworkInterface> GetInterfaces ()
        {
            try {
                return NetworkInterface.GetAllNetworkInterfaces ().ToList ();
            } catch (NetworkInformationException) {
                return new List<NetworkInterface> ();
            }
        }

        /// <summary>
        /// Returns the IPv4 address of all local network interfaces.
        /// </summary>
        public static IEnumerable<IPAddress> GetLocalIPAddresses ()
        {
            foreach (var adapter in GetInterfaces()) {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses) {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork) {
                        yield return unicastIPAddressInformation.Address;
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
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork) {
                        if (address.Equals (unicastIPAddressInformation.Address)) {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException ("Network interface with IPv4 address " + address + " does not exist.");
        }
    }
}
