﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MsmhTools
{
    public static class Network
    {
        public static int GetNextPort(int currentPort)
        {
            currentPort = currentPort < 65535 ? currentPort + 1 : currentPort - 1;
            return currentPort;
        }

        public static Uri? UrlToUri(string url)
        {
            try
            {
                string[] split1 = url.Split("//");
                string prefix = "https://";
                for (int n1 = 0; n1 < split1.Length; n1++)
                {
                    if (n1 > 0)
                    {
                        prefix += split1[n1];
                        if (n1 < split1.Length - 1)
                            prefix += "//";
                    }
                }
                
                Uri uri = new(prefix);
                return uri;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return null;
        }

        public static void GetUrlDetails(string url, int defaultPort, out string host, out int port, out string path, out bool isIPv6)
        {
            url = url.Trim();

            // Strip xxxx://
            if (url.Contains("//"))
            {
                string[] split = url.Split("//");
                if (!string.IsNullOrEmpty(split[1]))
                    url = split[1];
            }

            GetHostDetails(url, defaultPort, out host, out port, out path, out isIPv6);
        }

        public static void GetHostDetails(string hostIpPort, int defaultPort, out string host, out int port, out string path, out bool isIPv6)
        {
            hostIpPort = hostIpPort.Trim();
            path = string.Empty;
            isIPv6 = false;

            // Strip /xxxx (Path)
            if (!hostIpPort.Contains("//") && hostIpPort.Contains('/'))
            {
                string[] split = hostIpPort.Split('/');
                if (!string.IsNullOrEmpty(split[0]))
                    hostIpPort = split[0];

                // Get Path
                string outPath = "/";
                for (int n = 0; n < split.Length; n++)
                {
                    if (n != 0)
                        outPath += split[n];
                }
                if (!outPath.Equals("/"))
                    path = outPath;
            }

            string host0 = hostIpPort;
            port = defaultPort;

            // Split Host and Port
            if (hostIpPort.Contains('[') && hostIpPort.Contains("]:")) // IPv6 + Port
            {
                string[] split = hostIpPort.Split("]:");
                if (split.Length == 2)
                {
                    isIPv6 = true;
                    host0 = $"{split[0]}]";
                    bool isInt = int.TryParse(split[1], out int result);
                    if (isInt) port = result;
                }
            }
            else if (hostIpPort.Contains('[') && hostIpPort.Contains(']')) // IPv6
            {
                string[] split = hostIpPort.Split(']');
                if (split.Length == 2)
                {
                    isIPv6 = true;
                    host0 = $"{split[0]}]";
                }
            }
            else if (!hostIpPort.Contains('[') && !hostIpPort.Contains(']') && hostIpPort.Contains(':')) // Host + Port OR IPv4 + Port
            {
                string[] split = hostIpPort.Split(':');
                if (split.Length == 2)
                {
                    host0 = split[0];
                    bool isInt = int.TryParse(split[1], out int result);
                    if (isInt) port = result;
                }
            }

            host = host0;
        }

        public static IPAddress? HostToIP(string host, bool getIPv6 = false)
        {
            IPAddress? result = null;

            try
            {
                //IPAddress[] ipAddresses = Dns.GetHostEntry(host).AddressList;
                IPAddress[] ipAddresses = Dns.GetHostAddresses(host);

                if (ipAddresses == null || ipAddresses.Length == 0)
                    return null;

                if (!getIPv6)
                {
                    for (int n = 0; n < ipAddresses.Length; n++)
                    {
                        var addressFamily = ipAddresses[n].AddressFamily;
                        if (addressFamily == AddressFamily.InterNetwork)
                        {
                            result = ipAddresses[n];
                            break;
                        }
                    }
                }
                else
                {
                    for (int n = 0; n < ipAddresses.Length; n++)
                    {
                        var addressFamily = ipAddresses[n].AddressFamily;
                        if (addressFamily == AddressFamily.InterNetworkV6)
                        {
                            result = ipAddresses[n];
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return result;
        }

        public static List<IPAddress>? HostToIPs(string host, bool getIPv6 = false)
        {
            List<IPAddress>? result = new();

            try
            {
                //IPAddress[] ipAddresses = Dns.GetHostEntry(host).AddressList;
                IPAddress[] ipAddresses = Dns.GetHostAddresses(host);

                if (ipAddresses == null || ipAddresses.Length == 0)
                    return null;

                if (!getIPv6)
                {
                    for (int n = 0; n < ipAddresses.Length; n++)
                    {
                        var addressFamily = ipAddresses[n].AddressFamily;
                        if (addressFamily == AddressFamily.InterNetwork)
                        {
                            result.Add(ipAddresses[n]);
                        }
                    }
                }
                else
                {
                    for (int n = 0; n < ipAddresses.Length; n++)
                    {
                        var addressFamily = ipAddresses[n].AddressFamily;
                        if (addressFamily == AddressFamily.InterNetworkV6)
                        {
                            result.Add(ipAddresses[n]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return result.RemoveDuplicates();
        }

        /// <summary>
        /// Uses ipinfo.io to get result
        /// </summary>
        /// <param name="iPAddress">IP to check</param>
        /// <param name="proxyScheme">Use proxy to connect</param>
        /// <returns>Company name</returns>
        public static async Task<string?> IpToCompanyAsync(IPAddress iPAddress, string? proxyScheme = null)
        {
            string? company = null;
            try
            {
                using SocketsHttpHandler socketsHttpHandler = new();
                if (proxyScheme != null)
                    socketsHttpHandler.Proxy = new WebProxy(proxyScheme, true);
                using HttpClient httpClient2 = new(socketsHttpHandler);
                company = await httpClient2.GetStringAsync("https://ipinfo.io/" + iPAddress.ToString() + "/org");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return company;
        }

        public static IPAddress? GetLocalIPv4(string remoteHostToCheck = "8.8.8.8")
        {
            try
            {
                IPAddress? localIP;
                using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect(remoteHostToCheck, 80);
                IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint?.Address;
                return localIP;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        public static IPAddress? GetLocalIPv6(string remoteHostToCheck = "8.8.8.8")
        {
            try
            {
                IPAddress? localIP;
                using Socket socket = new(AddressFamily.InterNetworkV6, SocketType.Dgram, 0);
                socket.Connect(remoteHostToCheck, 80);
                IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint?.Address;
                return localIP;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        public static IPAddress? GetDefaultGateway()
        {
            IPAddress? gateway = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n?.GetIPProperties()?.GatewayAddresses)
                .Select(g => g?.Address)
                .Where(a => a != null)
                .Where(a => a?.AddressFamily == AddressFamily.InterNetwork)
                //.Where(a => Array.FindIndex(a.GetAddressBytes(), b => b != 0) >= 0) // Filter out 0.0.0.0
                .FirstOrDefault();
            return gateway;
        }

        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto)]
        private static extern int GetBestInterface(uint destAddr, out uint bestIfIndex);
        public static IPAddress? GetGatewayForDestination(IPAddress destinationAddress)
        {
            uint destaddr = BitConverter.ToUInt32(destinationAddress.GetAddressBytes(), 0);

            int result = GetBestInterface(destaddr, out uint interfaceIndex);
            if (result != 0)
            {
                Debug.WriteLine(new Win32Exception(result));
                return null;
            }

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var niprops = ni.GetIPProperties();
                if (niprops == null)
                    continue;

                var gateway = niprops.GatewayAddresses?.FirstOrDefault()?.Address;
                if (gateway == null)
                    continue;

                if (ni.Supports(NetworkInterfaceComponent.IPv4))
                {
                    var v4props = niprops.GetIPv4Properties();
                    if (v4props == null)
                        continue;

                    if (v4props.Index == interfaceIndex)
                        return gateway;
                }

                if (ni.Supports(NetworkInterfaceComponent.IPv6))
                {
                    var v6props = niprops.GetIPv6Properties();
                    if (v6props == null)
                        continue;

                    if (v6props.Index == interfaceIndex)
                        return gateway;
                }
            }

            return null;
        }

        public static bool IsIPv4(IPAddress iPAddress)
        {
            if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
                return true;
            else
                return false;
        }

        public static bool IsIPv4Valid(string ipString, out IPAddress? iPAddress)
        {
            iPAddress = null;
            if (string.IsNullOrWhiteSpace(ipString)) return false;
            if (!ipString.Contains('.')) return false;
            if (ipString.Count(c => c == '.') != 3) return false;
            if (ipString.StartsWith('.')) return false;
            if (ipString.EndsWith('.')) return false;
            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4) return false;

            foreach (string splitValue in splitValues)
            {
                // 0x and 0xx are not valid
                if (splitValue.Length > 1)
                {
                    bool isInt1 = int.TryParse(splitValue.AsSpan(0, 1), out int first);
                    if (isInt1 && first == 0) return false;
                }

                bool isInt2 = int.TryParse(splitValue, out int testInt);
                if (!isInt2) return false;
                if (testInt < 0 || testInt > 255) return false;
            }

            bool isIP = IPAddress.TryParse(ipString, out IPAddress? outIP);
            if (!isIP) return false;
            iPAddress = outIP;
            return true;
        }

        public static bool IsIPv6(IPAddress iPAddress)
        {
            return iPAddress.AddressFamily == AddressFamily.InterNetworkV6;
        }

        public static bool IsPortOpen(string host, int port, double timeoutSeconds)
        {
            try
            {
                using TcpClient client = new();
                var result = client.BeginConnect(host, port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(timeoutSeconds));
                client.EndConnect(result);
                return success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static List<NetworkInterface> GetNetworkInterfacesIPv4(bool upAndRunning = true)
        {
            List<NetworkInterface> nicList = new();
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int n1 = 0; n1 < networkInterfaces.Length; n1++)
            {
                NetworkInterface nic = networkInterfaces[n1];
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    var unicastAddresses = nic.GetIPProperties().UnicastAddresses;
                    for (int n2 = 0; n2 < unicastAddresses.Count; n2++)
                    {
                        var unicastAddress = unicastAddresses[n2];
                        if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (upAndRunning)
                            {
                                if (nic.OperationalStatus == OperationalStatus.Up)
                                {
                                    nicList.Add(nic);
                                    break;
                                }
                            }
                            else
                            {
                                nicList.Add(nic);
                                break;
                            }
                        }
                    }
                }
            }
            return nicList;
        }

        public static NetworkInterface? GetNICByName(string name)
        {
            NetworkInterface? nic = null;
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int n = 0; n < networkInterfaces.Length; n++)
            {
                nic = networkInterfaces[n];
                if (nic.Name.Equals(name))
                    return nic;
            }
            return nic;
        }

        public static NetworkInterface? GetNICByDescription(string description)
        {
            NetworkInterface? nic = null;
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int n = 0; n < networkInterfaces.Length; n++)
            {
                nic = networkInterfaces[n];
                if (nic.Description.Equals(description))
                    return nic;
            }
            return nic;
        }

        /// <summary>
        /// Set's the DNS Server of the local machine
        /// </summary>
        /// <param name="nic">NIC address</param>
        /// <param name="dnsServers">Comma seperated list of DNS server addresses</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        public static void SetDNS(NetworkInterface nic, string dnsServers)
        {
            // Requires Elevation
            // Only netsh can set DNS on Windows 7
            if (nic == null) return;

            try
            {
                string dnsServer1 = dnsServers;
                string dnsServer2 = string.Empty;
                if (dnsServers.Contains(','))
                {
                    string[] split = dnsServers.Split(',');
                    dnsServer1 = split[0];
                    dnsServer2 = split[1];
                }

                string processName = "netsh";
                string processArgs1 = $"interface ipv4 delete dnsservers {nic.Name} all";
                string processArgs2 = $"interface ipv4 set dnsservers {nic.Name} static {dnsServer1} primary";
                string processArgs3 = $"interface ipv4 add dnsservers {nic.Name} {dnsServer2} index=2";
                ProcessManager.Execute(out Process _, processName, processArgs1, true, true);
                ProcessManager.Execute(out Process _, processName, processArgs2, true, true);
                if (!string.IsNullOrEmpty(dnsServer2))
                    ProcessManager.Execute(out Process _, processName, processArgs3, true, true);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            Task.Delay(200).Wait();

            try
            {
                using ManagementClass managementClass = new("Win32_NetworkAdapterConfiguration");
                using ManagementObjectCollection moc = managementClass.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] && mo["Description"].Equals(nic.Description))
                    {
                        using ManagementBaseObject newDNS = mo.GetMethodParameters("SetDNSServerSearchOrder");
                        if (newDNS != null)
                        {
                            newDNS["DNSServerSearchOrder"] = dnsServers.Split(',');
                            mo.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Unset DNS to DHCP
        /// </summary>
        /// <param name="nic">Network Interface</param>
        public static void UnsetDNS(NetworkInterface nic)
        {
            // Requires Elevation - Can't Unset DNS when there is no Internet connectivity but netsh can :)
            // NetSh Command: netsh interface ip set dns "nic.Name" source=dhcp
            if (nic == null) return;

            try
            {
                string processName = "netsh";
                string processArgs1 = $"interface ipv4 delete dnsservers {nic.Name} all";
                string processArgs2 = $"interface ipv4 set dnsservers {nic.Name} source=dhcp";
                ProcessManager.Execute(out Process _, processName, processArgs1, true, true);
                ProcessManager.Execute(out Process _, processName, processArgs2, true, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            try
            {
                using ManagementClass managementClass = new("Win32_NetworkAdapterConfiguration");
                using ManagementObjectCollection moc = managementClass.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if (mo["Description"].Equals(nic.Description))
                    {
                        using ManagementBaseObject newDNS = mo.GetMethodParameters("SetDNSServerSearchOrder");
                        if (newDNS != null)
                        {
                            newDNS["DNSServerSearchOrder"] = null;
                            mo.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Unset DNS by seting DNS to Static
        /// </summary>
        /// <param name="nic">Network Interface</param>
        /// <param name="dns1">Primary</param>
        /// <param name="dns2">Secondary</param>
        public static void UnsetDNS(NetworkInterface nic, string dns1, string dns2)
        {
            string dnsServers = $"{dns1},{dns2}";
            SetDNS(nic, dnsServers);
        }

        /// <summary>
        /// Check if DNS is set to Static or DHCP
        /// </summary>
        /// <param name="nic">Network Interface</param>
        /// <param name="dnsServer1">Primary DNS Server</param>
        /// <param name="dnsServer2">Secondary DNS Server</param>
        /// <returns>True = Static, False = DHCP</returns>
        public static bool IsDnsSet(NetworkInterface nic, out string dnsServer1, out string dnsServer2)
        {
            dnsServer1 = dnsServer2 = string.Empty;
            if (nic == null) return false;

            string processName = "netsh";
            string processArgs = $"interface ipv4 show dnsservers {nic.Name}";
            string stdout = ProcessManager.Execute(out Process _, processName, processArgs, true, false);

            List<string> lines = stdout.SplitToLines();
            for (int n = 0; n < lines.Count; n++)
            {
                string line = lines[n];
                // Get Primary
                if (line.Contains(": ") && line.Contains('.') && line.Count(c => c == '.') == 3)
                {
                    string[] split = line.Split(": ");
                    if (split.Length > 1)
                    {
                        dnsServer1 = split[1].Trim();
                        Debug.WriteLine($"DNS 1: {dnsServer1}");
                    }
                }

                // Get Secondary
                if (!line.Contains(": ") && line.Contains('.') && line.Count(c => c == '.') == 3)
                {
                    dnsServer2 = line.Trim();
                    Debug.WriteLine($"DNS 2: {dnsServer2}");
                }
            }
            //Debug.WriteLine(stdout);
            return !stdout.Contains("DHCP");
        }

        public static bool IsInternetAlive(string? url = null, int timeoutMs = 5000)
        {
            // Attempt 1
            // only recognizes changes related to Internet adapters
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                // however, this will include all adapters -- filter by opstatus and activity
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                bool attempt1 = (from face in interfaces
                        where face.OperationalStatus == OperationalStatus.Up
                        where (face.NetworkInterfaceType != NetworkInterfaceType.Tunnel) && (face.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        select face.GetIPv4Statistics()).Any(statistics => (statistics.BytesReceived > 0) && (statistics.BytesSent > 0));

                return attempt1 ? true : attempt2(url, timeoutMs);
            }
            else
            {
                return attempt2(url, timeoutMs);
            }

            // Attempt 2
            static bool attempt2(string? url = null, int timeoutMs = 5000)
            {
                try
                {
                    url ??= CultureInfo.InstalledUICulture switch
                    {
                        { Name: var n } when n.StartsWith("fa") => // Iran
                            "http://www.google.com",
                        { Name: var n } when n.StartsWith("zh") => // China
                            "http://www.baidu.com",
                        _ =>
                            "http://www.gstatic.com/generate_204",
                    };
                    
                    using HttpClient httpClient = new();
                    httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
                    var req = httpClient.GetAsync(url);
                    return req.Result.IsSuccessStatusCode;
                }
                catch(Exception ex)
                {
                    Debug.WriteLine("IsInternetAlive: " + ex.Message);
                    return false;
                }
            }
        }

        public static bool IsInternetAlive2(string? url = null, int timeoutMs = 5000)
        {
            try
            {
                url ??= CultureInfo.InstalledUICulture switch
                {
                    { Name: var n } when n.StartsWith("fa") => // Iran
                        "http://www.google.com",
                    { Name: var n } when n.StartsWith("zh") => // China
                        "http://www.baidu.com",
                    _ =>
                        "http://www.gstatic.com/generate_204",
                };

                using HttpClient httpClient = new();
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
                var req = httpClient.GetAsync(url);
                return req.Result.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsProxySet(out string httpProxy, out string httpsProxy, out string ftpProxy, out string socksProxy)
        {
            bool isProxyEnable = false;
            httpProxy = httpsProxy = ftpProxy = socksProxy = string.Empty;
            RegistryKey? registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", false);
            if (registry != null)
            {
                // ProxyServer
                object? proxyServerObj = registry.GetValue("ProxyServer");
                if (proxyServerObj != null)
                {
                    string? proxyServers = proxyServerObj.ToString();
                    if (proxyServers != null)
                    {
                        if (proxyServers.Contains(';'))
                        {
                            string[] split = proxyServers.Split(';');
                            for (int n = 0; n < split.Length; n++)
                            {
                                string server = split[n];
                                if (server.StartsWith("http=")) httpProxy = server[5..];
                                else if (server.StartsWith("https=")) httpsProxy = server[6..];
                                else if (server.StartsWith("ftp=")) ftpProxy = server[4..];
                                else if (server.StartsWith("socks=")) socksProxy = server[6..];
                            }
                        }
                        else if (proxyServers.Contains('='))
                        {
                            string[] split = proxyServers.Split('=');
                            if (split[0] == "http") httpProxy = split[1];
                            else if (split[0] == "https") httpsProxy = split[1];
                            else if (split[0] == "ftp") ftpProxy = split[1];
                            else if (split[0] == "socks") socksProxy = split[1];
                        }
                        else if (proxyServers.Contains("://"))
                        {
                            string[] split = proxyServers.Split("://");
                            if (split[0] == "http") httpProxy = split[1];
                            else if (split[0] == "https") httpsProxy = split[1];
                            else if (split[0] == "ftp") ftpProxy = split[1];
                            else if (split[0] == "socks") socksProxy = split[1];
                        }
                        else if (!string.IsNullOrEmpty(proxyServers)) httpProxy = proxyServers;
                    }
                }

                // ProxyEnable
                object? proxyEnableObj = registry.GetValue("ProxyEnable");
                if (proxyEnableObj != null)
                {
                    string? proxyEnable = proxyEnableObj.ToString();
                    if (proxyEnable != null)
                    {
                        bool isInt = int.TryParse(proxyEnable, out int value);
                        if (isInt)
                            isProxyEnable = value == 1;
                    }
                }

            }
            return isProxyEnable;
        }

        
        public static void SetHttpProxy(string ip, int port)
        {
            RegistryKey? registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            if (registry != null)
            {
                string proxyServer = $"{ip}:{port}";

                try
                {
                    registry.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                    registry.SetValue("ProxyServer", proxyServer, RegistryValueKind.String);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Set Http Proxy: {ex.Message}");
                }

                RegistryTool.ApplyRegistryChanges();
            }
        }

        /// <summary>
        /// Unset Internet Options Proxy
        /// </summary>
        /// <param name="clearIpPort">Clear IP and Port</param>
        /// <param name="applyRegistryChanges">Don't apply registry changes on app exit</param>
        public static void UnsetProxy(bool clearIpPort, bool applyRegistryChanges)
        {
            RegistryKey? registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            if (registry != null)
            {
                try
                {
                    registry.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                    if (clearIpPort)
                        registry.SetValue("ProxyServer", "", RegistryValueKind.String);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unset Proxy: {ex.Message}");
                }

                if (applyRegistryChanges)
                    RegistryTool.ApplyRegistryChanges();
            }
        }

        /// <summary>
        /// Only the 'http', 'socks4', 'socks4a' and 'socks5' schemes are allowed for proxies.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> CheckProxyWorks(string websiteToCheck, string proxyScheme, int timeoutSec)
        {
            try
            {
                Uri uri = new(websiteToCheck, UriKind.Absolute);

                using SocketsHttpHandler socketsHttpHandler = new();
                socketsHttpHandler.Proxy = new WebProxy(proxyScheme);
                using HttpClient httpClientWithProxy = new(socketsHttpHandler);
                httpClientWithProxy.Timeout = new TimeSpan(0, 0, timeoutSec);

                HttpResponseMessage checkingResponse = await httpClientWithProxy.GetAsync(uri);
                Task.Delay(200).Wait();

                return checkingResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Check Proxy: {ex.Message}");
                return false;
            }
        }

        public static bool CanPing(string host, int timeoutMS)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    Ping ping = new();
                    PingReply reply = ping.Send(host, timeoutMS);
                    if (reply == null) return false;

                    return reply.Status == IPStatus.Success;
                }
                catch (PingException ex)
                {
                    Debug.WriteLine($"Ping: {ex.Message}");
                    return false;
                }
            });

            if (task.Wait(TimeSpan.FromMilliseconds(timeoutMS + 500)))
                return task.Result;
            else
                return false;
        }

        public static bool CanTcpConnect(string host, int port, int timeoutMS)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    using TcpClient client = new(host, port);
                    client.SendTimeout = timeoutMS;
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CanTcpConnect: {ex.Message}");
                    return false;
                }
            });

            if (task.Wait(TimeSpan.FromMilliseconds(timeoutMS + 500)))
                return task.Result;
            else
                return false;
        }

        public static async Task<bool> CanConnect(string host, int port, int timeoutMS)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    string url = $"https://{host}:{port}";
                    Uri uri = new(url, UriKind.Absolute);

                    using HttpClient httpClient = new();
                    httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMS);

                    await httpClient.GetAsync(uri);

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });

            if (await task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMS + 500)))
                return task.Result;
            else
                return false;
        }

    }
}
