using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using KRPC.Server;
using KRPC.Server.TCP;
using UnityEngine;

namespace KRPC.UI
{
    sealed class EditServer
    {
        readonly MainWindow window;
        readonly Guid id;

        const int nameMaxLength = 30;
        const int addressMaxLength = 15;
        const int portMaxLength = 5;
        const int portNameMaxLength = 255;
        const int baudRateMaxLength = 16;
        const int dataBitsMaxLength = 1;
        const string protocolLabelText = "Protocol:";
        const string addressLabelText = "Address:";
        const string rpcPortLabelText = "RPC port:";
        const string streamPortLabelText = "Stream port:";
        const string portLabelText = "Port:";
        const string baudRateLabelText = "Baud rate:";
        const string dataBitsLabelText = "Data bits:";
        const string parityLabelText = "Parity:";
        const string stopBitsLabelText = "Stop bits:";
        const string localhostText = "localhost";
        const string manualText = "Manual";
        const string anyText = "Any";
        const string invalidAddressText = "Invalid IP address. Must be in dot-decimal notation, e.g. \"192.168.1.0\"";
        const string invalidRPCPortText = "RPC port must be between 0 and 65535";
        const string invalidStreamPortText = "Stream port must be between 0 and 65535";
        const string invalidPortNameText = "Port name is empty";
        const string invalidBaudRateText = "Baud rate is not an integer";
        const string invalidDataBitsText = "Data bits must be between 5 and 8 inclusive";
        string[] availableProtocols = {
            MainWindow.protobufOverTcpText,
            MainWindow.protobufOverWebSocketsText,
            MainWindow.protobufOverSerialIOText
        };
        string[] parityOptions = {
            "None", "Odd", "Even", "Mark", "Space"
        };
        string[] stopBitsOptions = {
            "None", "One", "Two", "OnePointFive"
        };

        // Editable fields
        Protocol protocol;
        string name;
        IDictionary<string, string> settings;
        bool manualAddress;
        List<string> availableAddresses;

        public EditServer (MainWindow mainWindow, Configuration.Server server)
        {
            window = mainWindow;
            id = server.Id;

            name = server.Name;
            protocol = server.Protocol;
            settings = new Dictionary<string,string>(server.Settings);

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

        public void Draw ()
        {
            GUILayout.BeginHorizontal ();
            GUILayout.Label (protocolLabelText, window.labelStyle);
            protocol = (Protocol)GUILayoutExtensions.ComboBox ("protocol", (int)protocol, availableProtocols, window.buttonStyle, window.comboOptionsStyle, window.comboOptionStyle);
            GUILayout.EndHorizontal ();

            if (protocol == Protocol.ProtocolBuffersOverTCP ||
                protocol == Protocol.ProtocolBuffersOverWebsockets) {
                GUILayout.BeginHorizontal();
                GUILayout.Label(addressLabelText, window.labelStyle);
                if (!settings.ContainsKey("address"))
                    settings["address"] = IPAddress.Loopback.ToString();
                var address = settings["address"];
                // Get the index of the address in the combo box
                int addressSelected;
                if (!manualAddress && address == IPAddress.Loopback.ToString())
                    addressSelected = 0;
                else if (!manualAddress && address == IPAddress.Any.ToString())
                    addressSelected = 1;
                else if (!manualAddress && availableAddresses.Contains(address))
                    addressSelected = availableAddresses.IndexOf(address);
                else
                    addressSelected = availableAddresses.Count - 1;
                // Display the combo box
                addressSelected = GUILayoutExtensions.ComboBox("address", addressSelected, availableAddresses, window.buttonStyle, window.comboOptionsStyle, window.comboOptionStyle);
                // Get the address from the combo box selection
                if (addressSelected == 0) {
                    address = IPAddress.Loopback.ToString();
                    manualAddress = false;
                } else if (addressSelected == 1) {
                    address = IPAddress.Any.ToString();
                    manualAddress = false;
                } else if (addressSelected < availableAddresses.Count - 1) {
                    address = availableAddresses[addressSelected];
                    manualAddress = false;
                } else {
                    // Display a text field when "Manual" is selected
                    address = GUILayout.TextField(address, addressMaxLength, window.stretchyTextFieldStyle);
                    manualAddress = true;
                }
                settings["address"] = address;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(rpcPortLabelText, window.labelStyle);
                if (!settings.ContainsKey("rpc_port"))
                    settings["rpc_port"] = "50000";
                settings["rpc_port"] = GUILayout.TextField(settings["rpc_port"], portMaxLength, window.longTextFieldStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(streamPortLabelText, window.labelStyle);
                if (!settings.ContainsKey("stream_port"))
                    settings["stream_port"] = "50000";
                settings["stream_port"] = GUILayout.TextField(settings["stream_port"], portMaxLength, window.longTextFieldStyle);
                GUILayout.EndHorizontal();
            } else {
                if (!settings.ContainsKey("baud_rate"))
                    settings["baud_rate"] = "9600";
                if (!settings.ContainsKey("data_bits"))
                    settings["data_bits"] = "8";

                GUILayout.BeginHorizontal();
                GUILayout.Label(portLabelText, window.labelStyle);
                if (!settings.ContainsKey("port"))
                    settings["port"] = new KRPC.IO.Ports.SerialPort ().PortName;
                settings["port"] = GUILayout.TextField(
                    settings["port"], portNameMaxLength, window.longTextFieldStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(baudRateLabelText, window.labelStyle);
                if (!settings.ContainsKey("baud_rate"))
                    settings["baud_rate"] = "9600";
                settings["baud_rate"] = GUILayout.TextField(
                    settings["baud_rate"], baudRateMaxLength, window.longTextFieldStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(dataBitsLabelText, window.labelStyle);
                if (!settings.ContainsKey("data_bits"))
                    settings["data_bits"] = "8";
                settings["data_bits"] = GUILayout.TextField(
                    settings["data_bits"], dataBitsMaxLength, window.longTextFieldStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(parityLabelText, window.labelStyle);
                if (!settings.ContainsKey("parity"))
                    settings["parity"] = "None";
                settings["parity"] = parityOptions [GUILayoutExtensions.ComboBox (
                        "parity", parityOptions.IndexOf(settings["parity"]), parityOptions,
                        window.buttonStyle, window.comboOptionsStyle, window.comboOptionStyle)];
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(stopBitsLabelText, window.labelStyle);
                if (!settings.ContainsKey("stop_bits"))
                    settings["stop_bits"] = "One";
                settings["stop_bits"] = stopBitsOptions [GUILayoutExtensions.ComboBox (
                        "stop_bits", stopBitsOptions.IndexOf(settings["stop_bits"]), stopBitsOptions,
                        window.buttonStyle, window.comboOptionsStyle, window.comboOptionStyle)];
                GUILayout.EndHorizontal();
            }
        }

        public Configuration.Server Save ()
        {
            window.Errors.Clear();
            IList<string> allowedKeys = null;
            if (protocol == Protocol.ProtocolBuffersOverTCP ||
                protocol == Protocol.ProtocolBuffersOverWebsockets)
            {
                IPAddress ipAddress;
                ushort rpcPortInt;
                ushort streamPortInt;
                bool validAddress = IPAddress.TryParse(settings["address"], out ipAddress);
                bool validRPCPort = ushort.TryParse(settings["rpc_port"], out rpcPortInt);
                bool validStreamPort = ushort.TryParse(settings["stream_port"], out streamPortInt);
                if (!validAddress)
                    window.Errors.Add(invalidAddressText);
                if (!validRPCPort)
                    window.Errors.Add(invalidRPCPortText);
                if (!validStreamPort)
                    window.Errors.Add(invalidStreamPortText);
                allowedKeys = new List<string> { "address", "rpc_port", "stream_port" };
            } else {
                uint baudRateInt;
                ushort dataBitsInt;
                bool validPort = (settings["port"].Length > 0);
                bool validBaudRate = uint.TryParse(settings["baud_rate"], out baudRateInt);
                bool validDataBits = ushort.TryParse(settings["data_bits"], out dataBitsInt);
                if (!validPort)
                    window.Errors.Add(invalidPortNameText);
                if (!validBaudRate)
                    window.Errors.Add(invalidBaudRateText);
                if (!validDataBits)
                    window.Errors.Add(invalidDataBitsText);
                allowedKeys = new List<string> {
                    "port", "baud_rate", "data_bits", "parity", "stop_bits" };
            }
            settings = settings
                .Where(x => allowedKeys.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
            if (window.Errors.Any ())
                return null;
            return new Configuration.Server {
                Id = id,
                Protocol = protocol,
                Name = name,
                Settings = new Dictionary<string, string>(settings)
            };
        }
    }
}
