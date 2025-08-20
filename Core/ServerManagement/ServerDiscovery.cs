using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SqlServerManager.Core.ServerManagement
{
    public class ServerDiscoveryService
    {
        private const int SQL_BROWSER_PORT = 1434;
        private const int DISCOVERY_TIMEOUT = 5000;

        /// <summary>
        /// Discovers all available SQL Server instances (local and network)
        /// </summary>
        public async Task<List<SqlServerInstance>> DiscoverAllInstancesAsync(CancellationToken cancellationToken = default)
        {
            var instances = new List<SqlServerInstance>();

            try
            {
                // Discover local instances
                var localInstances = await DiscoverLocalInstancesAsync(cancellationToken);
                instances.AddRange(localInstances);

                // Discover network instances
                var networkInstances = await DiscoverNetworkInstancesAsync(cancellationToken);
                instances.AddRange(networkInstances);

                // Remove duplicates
                instances = RemoveDuplicateInstances(instances);

                // Discovered SQL Server instances
            }
            catch (Exception)
            {
                // Error during server discovery
            }

            return instances;
        }

        /// <summary>
        /// Discovers local SQL Server instances using registry and services
        /// </summary>
        public async Task<List<SqlServerInstance>> DiscoverLocalInstancesAsync(CancellationToken cancellationToken = default)
        {
            var instances = new List<SqlServerInstance>();

            try
            {
                // Method 1: Registry discovery
                var registryInstances = await Task.Run(() => DiscoverFromRegistry(), cancellationToken);
                instances.AddRange(registryInstances);

                // Method 2: SQL Server Data Sources Enumerator
                var enumeratorInstances = await Task.Run(() => DiscoverFromEnumerator(), cancellationToken);
                instances.AddRange(enumeratorInstances);

                // Discovered local SQL Server instances
            }
            catch (Exception)
            {
                // Error discovering local instances
            }

            return RemoveDuplicateInstances(instances);
        }

        /// <summary>
        /// Discovers network SQL Server instances using SQL Browser service
        /// </summary>
        public async Task<List<SqlServerInstance>> DiscoverNetworkInstancesAsync(CancellationToken cancellationToken = default)
        {
            var instances = new List<SqlServerInstance>();

            try
            {
                // Get local network addresses
                var networkAddresses = GetLocalNetworkAddresses();

                var discoveryTasks = networkAddresses.Select(async networkAddress =>
                {
                    try
                    {
                        return await DiscoverInstancesOnNetwork(networkAddress, cancellationToken);
                    }
                    catch (Exception)
                    {
                        // Error discovering instances on network
                        return new List<SqlServerInstance>();
                    }
                }).ToArray();

                var networkResults = await Task.WhenAll(discoveryTasks);
                
                foreach (var networkResult in networkResults)
                {
                    instances.AddRange(networkResult);
                }

                // Discovered network SQL Server instances
            }
            catch (Exception)
            {
                // Error discovering network instances
            }

            return RemoveDuplicateInstances(instances);
        }

        private List<SqlServerInstance> DiscoverFromRegistry()
        {
            var instances = new List<SqlServerInstance>();

            try
            {
                // Check for SQL Server instances in registry
                var registryKeys = new[]
                {
                    @"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL",
                    @"SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL"
                };

                foreach (var keyPath in registryKeys)
                {
                    try
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                        if (key != null)
                        {
                            foreach (var instanceName in key.GetValueNames())
                            {
                                var instanceValue = key.GetValue(instanceName)?.ToString();
                                if (!string.IsNullOrEmpty(instanceValue))
                                {
                                    var serverName = instanceName.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase)
                                        ? Environment.MachineName
                                        : $"{Environment.MachineName}\\{instanceName}";

                                    instances.Add(new SqlServerInstance
                                    {
                                        ServerName = serverName,
                                        InstanceName = instanceName,
                                        IsLocal = true,
                                        DiscoveryMethod = "Registry",
                                        Version = GetInstanceVersion(instanceValue)
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Error reading registry key
                    }
                }
            }
            catch (Exception)
            {
                // Error in registry discovery
            }

            return instances;
        }

        private List<SqlServerInstance> DiscoverFromEnumerator()
        {
            var instances = new List<SqlServerInstance>();

            try
            {
                // SqlDataSourceEnumerator is not available in .NET 8.0
                // Fallback to using services discovery or other methods
                // For now, we'll rely on registry and network discovery methods
                
                // Log that SQL Data Source Enumerator is not available in .NET 8.0
            }
            catch (Exception)
            {
                // Error using fallback discovery methods
            }

            return instances;
        }

        private async Task<List<SqlServerInstance>> DiscoverInstancesOnNetwork(string networkAddress, CancellationToken cancellationToken)
        {
            var instances = new List<SqlServerInstance>();

            try
            {
                // Parse network address to get base IP
                var network = IPNetwork.Parse(networkAddress);
                var hosts = network.ListIPAddress();

                var discoveryTasks = hosts.Take(254).Select(async ip =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return new List<SqlServerInstance>();

                    return await QuerySqlBrowserService(ip.ToString(), cancellationToken);
                }).ToArray();

                var results = await Task.WhenAll(discoveryTasks);
                
                foreach (var result in results)
                {
                    instances.AddRange(result);
                }
            }
            catch (Exception)
            {
                // Error discovering instances on network
            }

            return instances;
        }

        private async Task<List<SqlServerInstance>> QuerySqlBrowserService(string ipAddress, CancellationToken cancellationToken)
        {
            var instances = new List<SqlServerInstance>();

            try
            {
                using var udpClient = new UdpClient();
                udpClient.Client.ReceiveTimeout = DISCOVERY_TIMEOUT;
                udpClient.Client.SendTimeout = DISCOVERY_TIMEOUT;

                var serverEndpoint = new IPEndPoint(IPAddress.Parse(ipAddress), SQL_BROWSER_PORT);
                
                // SQL Browser service query packet for SQL Server instances
                var queryPacket = new byte[] { 0x02 };
                
                await udpClient.SendAsync(queryPacket, queryPacket.Length, serverEndpoint);
                
                var receiveTask = udpClient.ReceiveAsync();
                var timeoutTask = Task.Delay(DISCOVERY_TIMEOUT, cancellationToken);
                
                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
                
                if (completedTask == receiveTask && !cancellationToken.IsCancellationRequested)
                {
                    var result = await receiveTask;
                    var responseData = result.Buffer;
                    
                    if (responseData.Length > 3)
                    {
                        // Skip the first 3 bytes which contain response type and length info
                        var responseString = Encoding.ASCII.GetString(responseData, 3, responseData.Length - 3);
                        instances.AddRange(ParseBrowserResponse(responseString, ipAddress));
                    }
                    else if (responseData.Length > 0)
                    {
                        // Try parsing the entire response if it's shorter
                        var responseString = Encoding.ASCII.GetString(responseData);
                        instances.AddRange(ParseBrowserResponse(responseString, ipAddress));
                    }
                }
            }
            catch (SocketException)
            {
                // Network unreachable, host down, or port filtered - normal for network scanning
            }
            catch (Exception)
            {
                // Other exceptions - don't log timeouts as they're expected for non-SQL Server IPs
            }

            return instances;
        }

        private List<SqlServerInstance> ParseBrowserResponse(string response, string ipAddress)
        {
            var instances = new List<SqlServerInstance>();

            try
            {
                if (string.IsNullOrWhiteSpace(response))
                    return instances;

                // SQL Server Browser response format: ServerName;InstanceName;IsClustered;Version;tcp;Port;np;NamedPipe;...
                // Split by ;; to separate different server instances, then by ; for properties
                var serverBlocks = response.Split(new string[] { ";;" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var serverBlock in serverBlocks)
                {
                    var entries = serverBlock.Split(';');
                    
                    if (entries.Length > 1)
                    {
                        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        
                        // Parse key-value pairs
                        for (int i = 0; i < entries.Length - 1; i += 2)
                        {
                            if (i + 1 < entries.Length)
                            {
                                properties[entries[i]] = entries[i + 1];
                            }
                        }

                        // Look for required instance information
                        if (properties.TryGetValue("InstanceName", out var instanceName) ||
                            properties.TryGetValue("ServerName", out instanceName))
                        {
                            var serverName = instanceName.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase)
                                ? ipAddress
                                : $"{ipAddress}\\{instanceName}";

                            properties.TryGetValue("Version", out var version);
                            properties.TryGetValue("tcp", out var tcpPort);
                            properties.TryGetValue("IsClustered", out var clustered);
                            
                            instances.Add(new SqlServerInstance
                            {
                                ServerName = serverName,
                                InstanceName = instanceName,
                                IsLocal = false,
                                DiscoveryMethod = "SQL Browser Service",
                                Version = version ?? "Unknown",
                                TcpPort = int.TryParse(tcpPort, out var port) ? port : (int?)null,
                                IPAddress = ipAddress,
                                IsClustered = bool.TryParse(clustered, out var isCluster) && isCluster
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Error parsing browser response - create a basic instance if we got some response
                if (!string.IsNullOrWhiteSpace(response))
                {
                    instances.Add(new SqlServerInstance
                    {
                        ServerName = ipAddress,
                        InstanceName = "MSSQLSERVER",
                        IsLocal = false,
                        DiscoveryMethod = "SQL Browser Service (Basic)",
                        Version = "Unknown",
                        IPAddress = ipAddress
                    });
                }
            }

            return instances;
        }

        private string FindPropertyValue(string[] entries, string propertyName, int startIndex)
        {
            for (int i = startIndex; i < entries.Length - 1; i += 2)
            {
                if (entries[i].Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return entries[i + 1];
                }
            }
            return null;
        }

        private List<string> GetLocalNetworkAddresses()
        {
            var networkAddresses = new List<string>();

            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                               ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                foreach (var networkInterface in networkInterfaces)
                {
                    var ipProperties = networkInterface.GetIPProperties();
                    
                    foreach (var unicastAddress in ipProperties.UnicastAddresses)
                    {
                        if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            var ip = unicastAddress.Address;
                            var mask = unicastAddress.IPv4Mask;
                            
                            if (mask != null)
                            {
                                var network = GetNetworkAddress(ip, mask);
                                var cidr = GetCidrFromMask(mask);
                                networkAddresses.Add($"{network}/{cidr}");
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Error getting network addresses
            }

            return networkAddresses.Distinct().ToList();
        }

        private string GetNetworkAddress(IPAddress ip, IPAddress mask)
        {
            var ipBytes = ip.GetAddressBytes();
            var maskBytes = mask.GetAddressBytes();
            var networkBytes = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            }

            return new IPAddress(networkBytes).ToString();
        }

        private int GetCidrFromMask(IPAddress mask)
        {
            var maskBytes = mask.GetAddressBytes();
            var cidr = 0;

            foreach (var b in maskBytes)
            {
                cidr += CountSetBits(b);
            }

            return cidr;
        }

        private int CountSetBits(byte b)
        {
            int count = 0;
            while (b != 0)
            {
                count += b & 1;
                b >>= 1;
            }
            return count;
        }

        private string GetInstanceVersion(string instanceKey)
        {
            try
            {
                using var versionKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Microsoft SQL Server\{instanceKey}\Setup");
                return versionKey?.GetValue("Version")?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private List<SqlServerInstance> RemoveDuplicateInstances(List<SqlServerInstance> instances)
        {
            return instances
                .GroupBy(i => i.ServerName.ToLowerInvariant())
                .Select(g => g.First())
                .ToList();
        }
    }

    public class SqlServerInstance
    {
        public string ServerName { get; set; }
        public string InstanceName { get; set; }
        public bool IsLocal { get; set; }
        public string DiscoveryMethod { get; set; }
        public string Version { get; set; }
        public bool IsClustered { get; set; }
        public int? TcpPort { get; set; }
        public string IPAddress { get; set; }
        public DateTime DiscoveredAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return ServerName;
        }
    }

    // Simple IPNetwork implementation for network discovery
    public class IPNetwork
    {
        public IPAddress Network { get; set; }
        public int PrefixLength { get; set; }

        public static IPNetwork Parse(string networkString)
        {
            var parts = networkString.Split('/');
            return new IPNetwork
            {
                Network = IPAddress.Parse(parts[0]),
                PrefixLength = int.Parse(parts[1])
            };
        }

        public IEnumerable<IPAddress> ListIPAddress()
        {
            var networkBytes = Network.GetAddressBytes();
            var hostBits = 32 - PrefixLength;
            var hostCount = (int)Math.Pow(2, hostBits) - 2; // Exclude network and broadcast

            for (int i = 1; i <= Math.Min(hostCount, 254); i++)
            {
                var hostBytes = networkBytes.ToArray();
                hostBytes[3] = (byte)(hostBytes[3] + i);
                yield return new IPAddress(hostBytes);
            }
        }
    }
}
