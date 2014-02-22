using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace KRPC.Server.Net
{
    static class NetworkInformation
    {
        /// <summary>
        /// Returns the IPv4 address of all local network interfaces.
        /// </summary>
        public static IEnumerable<IPAddress> GetLocalIPAddresses ()
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces()) {
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
            // TODO: is this an appropriate type of exception?
            throw new ArgumentException ("Network interface with IPv4 address " + address + " does not exist.");
        }
    }
}
