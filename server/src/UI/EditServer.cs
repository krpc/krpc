using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using KRPC.Server;
using KRPC.Server.LocalSocket;
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
        const float labelWidth = 90f;
        const string protocolLabelText = "Protocol:";
        const string addressLabelText = "Address:";
        const string portLabelText = "Port:";
        const string baudRateLabelText = "Baud rate:";
        const string dataBitsLabelText = "Data bits:";
        const string parityLabelText = "Parity:";
        const string stopBitsLabelText = "Stop bits:";
        const string localhostText = "localhost";
        const string manualText = "Manual";
        const string anyText = "Any";
        const string invalidAddressText = "Invalid IP address. Must be in dot-decimal notation, e.g. \"192.168.1.0\"";
        const string invalidPortText = "Port must be between 0 and 65535";
        const string invalidPortNameText = "Port name is empty";
        const string invalidBaudRateText = "Baud rate is not an integer";
        const string invalidDataBitsText = "Data bits must be between 5 and 8 inclusive";
        const string pathLabelText = "Socket:";
        static readonly string invalidPathText =
            "Socket path must not be empty, and must take at most " +
            LocalSocketServer.MaximumPathLength + " bytes";
        string[] availableProtocols = {
            MainWindow.protobufOverTcpText,
            MainWindow.protobufOverWebSocketsText,
            MainWindow.protobufOverSerialIOText,
            MainWindow.protobufOverLocalSocketText
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

        // Unique identifiers for the combo boxes, so that they do not collide with
        // the combo boxes of other servers being edited at the same time. These must
        // be stable across frames, so they cannot be constructed inline in Draw ().
        readonly object protocolComboId = new object ();
        readonly object addressComboId = new object ();
        readonly object parityComboId = new object ();
        readonly object stopBitsComboId = new object ();

        // Names for the fields whose value is too long for the field it is edited in, unique
        // in the same way, and how far each has been scrolled sideways. The field is a
        // window onto its value that follows the caret
        readonly string pathFieldName;
        readonly string portNameFieldName;
        float pathScroll;
        float portNameScroll;

        public EditServer (MainWindow mainWindow, Configuration.Server server)
        {
            window = mainWindow;
            id = server.Id;
            pathFieldName = "path " + id;
            portNameFieldName = "port " + id;

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
            name = GUILayout.TextField (name, nameMaxLength, window.stretchyTextFieldStyle);
        }

        // A fixed-width field label, so every field in the form starts at the same
        // column and the (stretchy) fields all end up the same width.
        void DrawFieldLabel (string text)
        {
            GUILayout.Label (text, window.labelStyle, GUILayout.Width (labelWidth * GameSettings.UI_SCALE));
        }

        // The prefix of the address and port keys, which differs between TCP/IP and WebSockets
        string SettingsPrefix {
            get { return protocol == Protocol.ProtocolBuffersOverWebsockets ? "ws_" : "tcpip_"; }
        }

        public void Draw ()
        {
            GUILayout.BeginHorizontal ();
            DrawFieldLabel (protocolLabelText);
            protocol = (Protocol)GUILayoutExtensions.ComboBox (protocolComboId, (int)protocol, availableProtocols, window.buttonStyle, window.comboOptionsStyle, window.comboOptionStyle);
            GUILayout.EndHorizontal ();

            if (protocol == Protocol.ProtocolBuffersOverTCP ||
                protocol == Protocol.ProtocolBuffersOverWebsockets) {
                var addressKey = SettingsPrefix + "address";
                var portKey = SettingsPrefix + "port";
                GUILayout.BeginHorizontal();
                DrawFieldLabel (addressLabelText);
                if (!settings.ContainsKey(addressKey))
                    settings[addressKey] = IPAddress.Loopback.ToString();
                var address = settings[addressKey];
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
                addressSelected = GUILayoutExtensions.ComboBox(addressComboId, addressSelected, availableAddresses, window.buttonStyle, window.comboOptionsStyle, window.comboOptionStyle);
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
                    address = GUILayoutExtensions.FilterDigitsAndPeriods (
                        GUILayoutExtensions.ValidatedTextField (address, addressMaxLength, window.stretchyTextFieldStyle,
                            ValidAddress (address), window.errorColor));
                    manualAddress = true;
                }
                settings[addressKey] = address;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawFieldLabel (portLabelText);
                if (!settings.ContainsKey(portKey))
                    settings[portKey] = "50000";
                settings[portKey] = GUILayoutExtensions.FilterDigits (
                    GUILayoutExtensions.ValidatedTextField (settings[portKey], portMaxLength, window.stretchyTextFieldStyle,
                        ValidPort (settings[portKey]), window.errorColor));
                GUILayout.EndHorizontal();
            } else if (protocol == Protocol.ProtocolBuffersOverLocalSocket) {
                GUILayout.BeginHorizontal();
                DrawFieldLabel (pathLabelText);
                if (!settings.ContainsKey("localsocket_path"))
                    settings["localsocket_path"] = LocalSocketServer.DefaultPath ();
                settings["localsocket_path"] = GUILayoutExtensions.ValidatedScrollingTextField (
                    pathFieldName, settings["localsocket_path"], ref pathScroll,
                    LocalSocketServer.MaximumPathLength, window.stretchyTextFieldStyle,
                    ValidPath (settings["localsocket_path"]), window.errorColor);
                GUILayout.EndHorizontal();
            } else {
                if (!settings.ContainsKey("serialio_baud_rate"))
                    settings["serialio_baud_rate"] = "9600";
                if (!settings.ContainsKey("serialio_data_bits"))
                    settings["serialio_data_bits"] = "8";

                GUILayout.BeginHorizontal();
                DrawFieldLabel (portLabelText);
                if (!settings.ContainsKey("serialio_port"))
                    settings["serialio_port"] = new KRPC.IO.Ports.SerialPort ().PortName;
                settings["serialio_port"] = GUILayoutExtensions.ValidatedScrollingTextField (
                    portNameFieldName, settings["serialio_port"], ref portNameScroll,
                    portNameMaxLength, window.stretchyTextFieldStyle,
                    settings["serialio_port"].Length > 0, window.errorColor);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawFieldLabel (baudRateLabelText);
                if (!settings.ContainsKey("serialio_baud_rate"))
                    settings["serialio_baud_rate"] = "9600";
                settings["serialio_baud_rate"] = GUILayoutExtensions.FilterDigits (
                    GUILayoutExtensions.ValidatedTextField (settings["serialio_baud_rate"], baudRateMaxLength, window.stretchyTextFieldStyle,
                        ValidBaudRate (settings["serialio_baud_rate"]), window.errorColor));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawFieldLabel (dataBitsLabelText);
                if (!settings.ContainsKey("serialio_data_bits"))
                    settings["serialio_data_bits"] = "8";
                settings["serialio_data_bits"] = GUILayoutExtensions.FilterDigits (
                    GUILayoutExtensions.ValidatedTextField (settings["serialio_data_bits"], dataBitsMaxLength, window.stretchyTextFieldStyle,
                        ValidDataBits (settings["serialio_data_bits"]), window.errorColor));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawFieldLabel (parityLabelText);
                if (!settings.ContainsKey("serialio_parity"))
                    settings["serialio_parity"] = "None";
                settings["serialio_parity"] = parityOptions [GUILayoutExtensions.ComboBox (
                        parityComboId, parityOptions.IndexOf(settings["serialio_parity"]), parityOptions,
                        window.buttonStyle, window.comboOptionsStyle, window.comboOptionStyle)];
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawFieldLabel (stopBitsLabelText);
                if (!settings.ContainsKey("serialio_stop_bits"))
                    settings["serialio_stop_bits"] = "One";
                settings["serialio_stop_bits"] = stopBitsOptions [GUILayoutExtensions.ComboBox (
                        stopBitsComboId, stopBitsOptions.IndexOf(settings["serialio_stop_bits"]), stopBitsOptions,
                        window.buttonStyle, window.comboOptionsStyle, window.comboOptionStyle)];
                GUILayout.EndHorizontal();
            }
        }

        static bool ValidAddress (string value)
        {
            IPAddress address;
            return IPAddress.TryParse (value, out address);
        }

        static bool ValidPort (string value)
        {
            ushort port;
            return ushort.TryParse (value, out port);
        }

        static bool ValidPath (string value)
        {
            // The limit is on the bytes the path takes in a socket address, not on the
            // characters it is written with
            return value.Length > 0 &&
                LocalSocketServer.PathLength (value) <= LocalSocketServer.MaximumPathLength;
        }

        static bool ValidBaudRate (string value)
        {
            uint baudRate;
            return uint.TryParse (value, out baudRate);
        }

        static bool ValidDataBits (string value)
        {
            ushort dataBits;
            return ushort.TryParse (value, out dataBits) && dataBits >= 5 && dataBits <= 8;
        }

        public Configuration.Server Save ()
        {
            window.Errors.Clear();
            IList<string> allowedKeys = null;
            if (protocol == Protocol.ProtocolBuffersOverTCP ||
                protocol == Protocol.ProtocolBuffersOverWebsockets)
            {
                var addressKey = SettingsPrefix + "address";
                var portKey = SettingsPrefix + "port";
                if (!ValidAddress (settings[addressKey]))
                    window.Errors.Add(invalidAddressText);
                if (!ValidPort (settings[portKey]))
                    window.Errors.Add(invalidPortText);
                allowedKeys = new List<string> { addressKey, portKey };
            } else if (protocol == Protocol.ProtocolBuffersOverLocalSocket) {
                if (!ValidPath (settings["localsocket_path"]))
                    window.Errors.Add(invalidPathText);
                allowedKeys = new List<string> { "localsocket_path" };
            } else {
                if (settings["serialio_port"].Length == 0)
                    window.Errors.Add(invalidPortNameText);
                if (!ValidBaudRate (settings["serialio_baud_rate"]))
                    window.Errors.Add(invalidBaudRateText);
                if (!ValidDataBits (settings["serialio_data_bits"]))
                    window.Errors.Add(invalidDataBitsText);
                allowedKeys = new List<string> {
                    "serialio_port", "serialio_baud_rate", "serialio_data_bits",
                    "serialio_parity", "serialio_stop_bits" };
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
