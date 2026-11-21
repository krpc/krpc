using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Brutal.ImGuiApi;
using KRPC.Server;
using KRPC.Server.LocalSocket;
using KRPC.Server.TCP;
using KSA;

namespace KRPC.KSA.UI
{
    /// <summary>
    /// The form for editing a server's settings, drawn in place of its details.
    /// </summary>
    sealed class EditServer
    {
        const string LocalhostText = "localhost";
        const string AnyText = "Any";
        const string ManualText = "Manual";
        const string InvalidAddressText = "Invalid IP address. Must be in dot-decimal notation, e.g. \"192.168.1.0\"";
        static readonly string InvalidPathText =
            "Socket path must not be empty, and must take at most " +
            LocalSocketServer.MaximumPathLength + " bytes";
        static readonly Protocol [] Protocols = {
            Protocol.ProtocolBuffersOverTCP,
            Protocol.ProtocolBuffersOverWebsockets,
            Protocol.ProtocolBuffersOverSerialIO,
            Protocol.ProtocolBuffersOverLocalSocket,
            ConfigurationFile.GrpcProtocol
        };
        static readonly string [] ProtocolNames = Protocols.Select (MainWindow.ProtocolName).ToArray ();
        static readonly string [] ParityOptions = { "None", "Odd", "Even", "Mark", "Space" };
        static readonly string [] StopBitsOptions = { "None", "One", "Two", "OnePointFive" };

        readonly Guid id;
        readonly List<string> availableAddresses;
        Protocol protocol;
        string name;
        bool manualAddress;
        readonly Dictionary<string, string> settings;

        public EditServer (Configuration.Server server)
        {
            id = server.Id;
            name = server.Name;
            protocol = server.Protocol;
            settings = new Dictionary<string, string> (server.Settings);

            var interfaceAddresses = NetworkInformation.LocalIPAddresses.Select (x => x.ToString ()).ToList ();
            interfaceAddresses.Remove (IPAddress.Loopback.ToString ());
            interfaceAddresses.Remove (IPAddress.Any.ToString ());
            availableAddresses = new List<string> { LocalhostText, AnyText };
            availableAddresses.AddRange (interfaceAddresses);
            availableAddresses.Add (ManualText);
            manualAddress = IsManualAddress ();
        }

        // Whether the address of the protocol is one the address combo box does not list
        bool IsManualAddress ()
        {
            var address = settings.GetValueOrDefault (Prefix + "address", IPAddress.Loopback.ToString ());
            return !availableAddresses.Contains (address) && address != IPAddress.Loopback.ToString () &&
                address != IPAddress.Any.ToString ();
        }

        public void Draw ()
        {
            DrawTextRow ("Name", "name", ref name, true);
            var selected = Array.IndexOf (Protocols, protocol);
            if (DrawComboRow ("Protocol", "protocol", ProtocolNames, ref selected)) {
                protocol = Protocols [selected];
                // Each protocol has its own address setting
                manualAddress = IsManualAddress ();
            }

            if (protocol == Protocol.ProtocolBuffersOverTCP || protocol == Protocol.ProtocolBuffersOverWebsockets) {
                DrawAddress ();
                DrawSettingRow ("Port", Prefix + "port", "50000", ValidPort, true);
            } else if (protocol == ConfigurationFile.GrpcProtocol) {
                DrawAddress ();
                DrawSettingRow ("Port", Prefix + "port", "50002", ValidPort, true);
            } else if (protocol == Protocol.ProtocolBuffersOverLocalSocket) {
                DrawSettingRow ("Socket", Prefix + "path", LocalSocketServer.DefaultPath (), ValidPath);
            } else {
                DrawSettingRow ("Port", Prefix + "port", new System.IO.Ports.SerialPort ().PortName, x => x.Length > 0);
                DrawSettingRow ("Baud rate", Prefix + "baud_rate", "9600", ValidBaudRate, true);
                DrawSettingRow ("Data bits", Prefix + "data_bits", "8", ValidDataBits, true);
                DrawOptionRow ("Parity", Prefix + "parity", "None", ParityOptions);
                DrawOptionRow ("Stop bits", Prefix + "stop_bits", "One", StopBitsOptions);
            }
        }

        // The prefix of the protocol's setting keys
        string Prefix {
            get {
                switch (protocol) {
                case Protocol.ProtocolBuffersOverTCP:
                    return "tcpip_";
                case Protocol.ProtocolBuffersOverWebsockets:
                    return "ws_";
                case Protocol.ProtocolBuffersOverLocalSocket:
                    return "localsocket_";
                case ConfigurationFile.GrpcProtocol:
                    return "grpc_";
                default:
                    return "serialio_";
                }
            }
        }

        void DrawAddress ()
        {
            var address = settings.GetValueOrDefault (Prefix + "address", IPAddress.Loopback.ToString ());
            int selected;
            if (!manualAddress && address == IPAddress.Loopback.ToString ())
                selected = 0;
            else if (!manualAddress && address == IPAddress.Any.ToString ())
                selected = 1;
            else if (!manualAddress && availableAddresses.Contains (address))
                selected = availableAddresses.IndexOf (address);
            else
                selected = availableAddresses.Count - 1;
            if (DrawComboRow ("Address", "address", availableAddresses, ref selected)) {
                manualAddress = selected == availableAddresses.Count - 1;
                if (selected == 0)
                    address = IPAddress.Loopback.ToString ();
                else if (selected == 1)
                    address = IPAddress.Any.ToString ();
                else if (!manualAddress)
                    address = availableAddresses [selected];
            }
            if (manualAddress)
                DrawTextRow ("Manual address", "manual_address", ref address, ValidAddress (address));
            settings [Prefix + "address"] = address;
        }

        void DrawSettingRow (string label, string key, string defaultValue, Func<string, bool> valid, bool number = false)
        {
            if (!settings.TryGetValue (key, out var value))
                value = defaultValue;
            DrawTextRow (label, key, ref value, valid (value), number);
            settings [key] = value;
        }

        void DrawOptionRow (string label, string key, string defaultValue, IList<string> options)
        {
            var selected = Math.Max (0, options.IndexOf (settings.GetValueOrDefault (key, defaultValue)));
            DrawComboRow (label, key, options, ref selected);
            settings [key] = options [selected];
        }

        static void DrawTextRow (string label, string id, ref string value, bool valid, bool number = false)
        {
            ConsoleWindow.DrawTextRow (label, id, ref value, !valid, number);
        }

        static bool DrawComboRow (string label, string id, IList<string> options, ref int selected)
        {
            var changed = false;
            ConsoleWidgets.BeginRow (label);
            if (ConsoleWidgets.BeginComboControl (id, options [selected], false)) {
                for (var i = 0; i < options.Count; i++) {
                    if (ImGui.Selectable (options [i], i == selected)) {
                        changed = i != selected;
                        selected = i;
                    }
                }
                ConsoleWidgets.EndComboControl ();
            }
            ConsoleWidgets.EndRow ();
            return changed;
        }

        static bool ValidAddress (string value)
        {
            return IPAddress.TryParse (value, out _);
        }

        static bool ValidPort (string value)
        {
            return ushort.TryParse (value, NumberStyles.None, CultureInfo.InvariantCulture, out _);
        }

        static bool ValidPath (string value)
        {
            // The limit is on the bytes the path takes in a socket address
            return value.Length > 0 && LocalSocketServer.PathLength (value) <= LocalSocketServer.MaximumPathLength;
        }

        static bool ValidBaudRate (string value)
        {
            return uint.TryParse (value, NumberStyles.None, CultureInfo.InvariantCulture, out _);
        }

        static bool ValidDataBits (string value)
        {
            return ushort.TryParse (value, NumberStyles.None, CultureInfo.InvariantCulture, out var dataBits) &&
                dataBits >= 5 && dataBits <= 8;
        }

        /// <summary>
        /// The edited server, or <c>null</c> after adding its errors to <paramref name="errors"/>.
        /// </summary>
        public Configuration.Server Save (IList<string> errors)
        {
            errors.Clear ();
            var prefix = Prefix;
            string [] keys;
            if (protocol == Protocol.ProtocolBuffersOverTCP || protocol == Protocol.ProtocolBuffersOverWebsockets ||
                protocol == ConfigurationFile.GrpcProtocol) {
                if (!ValidAddress (settings [prefix + "address"]))
                    errors.Add (InvalidAddressText);
                if (!ValidPort (settings [prefix + "port"]))
                    errors.Add ("Port must be between 0 and 65535");
                keys = new [] { "address", "port" };
            } else if (protocol == Protocol.ProtocolBuffersOverLocalSocket) {
                if (!ValidPath (settings [prefix + "path"]))
                    errors.Add (InvalidPathText);
                keys = new [] { "path" };
            } else {
                if (settings [prefix + "port"].Length == 0)
                    errors.Add ("Port name is empty");
                if (!ValidBaudRate (settings [prefix + "baud_rate"]))
                    errors.Add ("Baud rate is not an integer");
                if (!ValidDataBits (settings [prefix + "data_bits"]))
                    errors.Add ("Data bits must be between 5 and 8 inclusive");
                keys = new [] { "port", "baud_rate", "data_bits", "parity", "stop_bits" };
            }
            if (errors.Count > 0)
                return null;
            return new Configuration.Server {
                Id = id,
                Protocol = protocol,
                Name = name,
                Settings = keys.ToDictionary (x => prefix + x, x => settings [prefix + x])
            };
        }
    }
}
