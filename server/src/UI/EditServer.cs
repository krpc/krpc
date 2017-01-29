using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using KRPC.Server;
using KRPC.Server.TCP;
using UnityEngine;

namespace KRPC.UI
{
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    sealed class EditServer
    {
        readonly MainWindow window;
        readonly Guid id;

        const int nameMaxLength = 30;
        const int addressMaxLength = 15;
        const int portMaxLength = 5;
        const string protocolLabelText = "Protocol:";
        const string addressLabelText = "Address:";
        const string rpcPortLabelText = "RPC port:";
        const string streamPortLabelText = "Stream port:";
        const string localhostText = "localhost";
        const string manualText = "Manual";
        const string anyText = "Any";
        const string invalidAddressText = "Invalid IP address. Must be in dot-decimal notation, e.g. \"192.168.1.0\"";
        const string invalidRPCPortText = "RPC port must be between 0 and 65535";
        const string invalidStreamPortText = "Stream port must be between 0 and 65535";
        string[] availableProtocols = { MainWindow.protobufOverTcpText, MainWindow.protobufOverWebSocketsText };

        // Editable fields
        Protocol protocol;
        string name;
        string address;
        bool manualAddress;
        List<string> availableAddresses;
        string rpcPort;
        string streamPort;

        public EditServer (MainWindow mainWindow, Configuration.Server server)
        {
            window = mainWindow;
            id = server.Id;

            name = server.Name;
            protocol = server.Protocol;
            address = server.Address.ToString ();
            rpcPort = server.RPCPort.ToString ();
            streamPort = server.StreamPort.ToString ();

            // Get list of available addresses for drop down
            var interfaceAddresses = NetworkInformation.LocalIPAddresses.Select (x => x.ToString ()).ToList ();
            interfaceAddresses.Remove (IPAddress.Loopback.ToString ());
            interfaceAddresses.Remove (IPAddress.Any.ToString ());
            availableAddresses = new List<string> (new [] { localhostText, anyText });
            availableAddresses.AddRange (interfaceAddresses);
            availableAddresses.Add (manualText);
        }

        public void DrawName ()
        {
            name = GUILayout.TextField (name, nameMaxLength, window.longTextFieldStyle);
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public void Draw ()
        {
            GUILayout.BeginHorizontal ();
            GUILayout.Label (protocolLabelText, window.labelStyle);
            int protocolSelected = protocol == Protocol.ProtocolBuffersOverTCP ? 0 : 1;
            protocolSelected = GUILayoutExtensions.ComboBox ("protocol", protocolSelected, availableProtocols, window.buttonStyle, window.comboOptionsStyle, window.comboOptionStyle);
            protocol = protocolSelected == 0 ? Protocol.ProtocolBuffersOverTCP : Protocol.ProtocolBuffersOverWebsockets;
            GUILayout.EndHorizontal ();

            GUILayout.BeginHorizontal ();
            GUILayout.Label (addressLabelText, window.labelStyle);
            // Get the index of the address in the combo box
            int addressSelected;
            if (!manualAddress && address == IPAddress.Loopback.ToString ())
                addressSelected = 0;
            else if (!manualAddress && address == IPAddress.Any.ToString ())
                addressSelected = 1;
            else if (!manualAddress && availableAddresses.Contains (address))
                addressSelected = availableAddresses.IndexOf (address);
            else
                addressSelected = availableAddresses.Count - 1;
            // Display the combo box
            addressSelected = GUILayoutExtensions.ComboBox ("address", addressSelected, availableAddresses, window.buttonStyle, window.comboOptionsStyle, window.comboOptionStyle);
            // Get the address from the combo box selection
            if (addressSelected == 0) {
                address = IPAddress.Loopback.ToString ();
                manualAddress = false;
            } else if (addressSelected == 1) {
                address = IPAddress.Any.ToString ();
                manualAddress = false;
            } else if (addressSelected < availableAddresses.Count - 1) {
                address = availableAddresses [addressSelected];
                manualAddress = false;
            } else {
                // Display a text field when "Manual" is selected
                address = GUILayout.TextField (address, addressMaxLength, window.stretchyTextFieldStyle);
                manualAddress = true;
            }
            GUILayout.EndHorizontal ();

            GUILayout.BeginHorizontal ();
            GUILayout.Label (rpcPortLabelText, window.labelStyle);
            rpcPort = GUILayout.TextField (rpcPort, portMaxLength, window.longTextFieldStyle);
            GUILayout.EndHorizontal ();

            GUILayout.BeginHorizontal ();
            GUILayout.Label (streamPortLabelText, window.labelStyle);
            streamPort = GUILayout.TextField (streamPort, portMaxLength, window.longTextFieldStyle);
            GUILayout.EndHorizontal ();
        }

        public Configuration.Server Save ()
        {
            // Validate the settings
            window.Errors.Clear ();
            IPAddress ipAddress;
            ushort rpcPortInt;
            ushort streamPortInt;
            bool validAddress = IPAddress.TryParse (address, out ipAddress);
            bool validRPCPort = ushort.TryParse (rpcPort, out rpcPortInt);
            bool validStreamPort = ushort.TryParse (streamPort, out streamPortInt);

            // Display error message if required
            if (!validAddress)
                window.Errors.Add (invalidAddressText);
            if (!validRPCPort)
                window.Errors.Add (invalidRPCPortText);
            if (!validStreamPort)
                window.Errors.Add (invalidStreamPortText);

            if (window.Errors.Any ())
                return null;

            return new Configuration.Server {
                Id = id,
                Protocol = protocol,
                Name = name,
                Address = ipAddress,
                RPCPort = rpcPortInt,
                StreamPort = streamPortInt
            };
        }
    }
}
