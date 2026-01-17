using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;

namespace SwissKnifeApp.ViewModels
{
    public partial class NetworkToolsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pingHost = "google.com";

        [ObservableProperty]
        private string _pingResult = "";

        [ObservableProperty]
        private bool _isPinging = false;

        [ObservableProperty]
        private string _portScanHost = "localhost";

        [ObservableProperty]
        private int _startPort = 1;

        [ObservableProperty]
        private int _endPort = 1000;

        [ObservableProperty]
        private bool _isScanning = false;

        [ObservableProperty]
        private ObservableCollection<string> _openPorts = new();

        [ObservableProperty]
        private string _ipLookupAddress = "";

        [ObservableProperty]
        private string _ipLookupResult = "";

        [ObservableProperty]
        private string _dnsHost = "google.com";

        [ObservableProperty]
        private string _dnsResult = "";

        public NetworkToolsViewModel()
        {
        }

        [RelayCommand]
        private async Task PingHostAsync()
        {
            if (string.IsNullOrWhiteSpace(PingHost))
            {
                MessageBox.Show("Lütfen bir host adresi girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsPinging = true;
            PingResult = "Ping gönderiliyor...\n";

            try
            {
                using var ping = new Ping();
                var stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        var reply = await ping.SendPingAsync(PingHost, 3000);
                        stopwatch.Stop();

                        if (reply.Status == IPStatus.Success)
                        {
                            PingResult += $"✅ {PingHost} [{reply.Address}]: bytes={reply.Buffer.Length} time={reply.RoundtripTime}ms TTL={reply.Options?.Ttl}\n";
                        }
                        else
                        {
                            PingResult += $"❌ {PingHost}: {reply.Status}\n";
                        }

                        stopwatch.Restart();
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        PingResult += $"❌ Hata: {ex.Message}\n";
                    }
                }

                PingResult += "\nPing tamamlandı.";
            }
            catch (Exception ex)
            {
                PingResult = $"❌ Ping hatası: {ex.Message}";
            }
            finally
            {
                IsPinging = false;
            }
        }

        [RelayCommand]
        private async Task ScanPortsAsync()
        {
            if (string.IsNullOrWhiteSpace(PortScanHost))
            {
                MessageBox.Show("Lütfen bir host adresi girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StartPort < 1 || EndPort > 65535 || StartPort > EndPort)
            {
                MessageBox.Show("Port aralığı geçersiz! (1-65535)", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsScanning = true;
            OpenPorts.Clear();

            try
            {
                OpenPorts.Add($"🔍 {PortScanHost} taranıyor ({StartPort}-{EndPort})...");

                var tasks = new System.Collections.Generic.List<Task<(int port, bool isOpen)>>();

                for (int port = StartPort; port <= EndPort; port++)
                {
                    int currentPort = port;
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            using var client = new System.Net.Sockets.TcpClient();
                            await client.ConnectAsync(PortScanHost, currentPort).WaitAsync(TimeSpan.FromMilliseconds(100));
                            return (currentPort, true);
                        }
                        catch
                        {
                            return (currentPort, false);
                        }
                    }));

                    // Process in batches of 100
                    if (tasks.Count >= 100)
                    {
                        var results = await Task.WhenAll(tasks);
                        foreach (var (p, isOpen) in results)
                        {
                            if (isOpen)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    OpenPorts.Add($"✅ Port {p} - AÇIK");
                                });
                            }
                        }
                        tasks.Clear();
                    }
                }

                // Process remaining
                if (tasks.Count > 0)
                {
                    var results = await Task.WhenAll(tasks);
                    foreach (var (p, isOpen) in results)
                    {
                        if (isOpen)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                OpenPorts.Add($"✅ Port {p} - AÇIK");
                            });
                        }
                    }
                }

                OpenPorts.Add("\n🎯 Tarama tamamlandı!");
            }
            catch (Exception ex)
            {
                OpenPorts.Add($"❌ Hata: {ex.Message}");
            }
            finally
            {
                IsScanning = false;
            }
        }

        [RelayCommand]
        private async Task LookupIPAsync()
        {
            if (string.IsNullOrWhiteSpace(IpLookupAddress))
            {
                MessageBox.Show("Lütfen bir IP adresi veya domain girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IpLookupResult = "IP bilgisi alınıyor...\n";

            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(IpLookupAddress);

                IpLookupResult = $"🌐 Host: {hostEntry.HostName}\n\n";
                IpLookupResult += "📍 IP Adresleri:\n";

                foreach (var ip in hostEntry.AddressList)
                {
                    IpLookupResult += $"  • {ip} ({ip.AddressFamily})\n";
                }

                IpLookupResult += $"\n🏷️ Aliases: {string.Join(", ", hostEntry.Aliases)}\n";
            }
            catch (Exception ex)
            {
                IpLookupResult = $"❌ IP lookup hatası: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DnsLookupAsync()
        {
            if (string.IsNullOrWhiteSpace(DnsHost))
            {
                MessageBox.Show("Lütfen bir domain girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DnsResult = "DNS kayıtları alınıyor...\n";

            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(DnsHost);

                DnsResult = $"🌐 Domain: {DnsHost}\n";
                DnsResult += $"📝 Host: {hostEntry.HostName}\n\n";
                DnsResult += "📍 A Kayıtları (IP Adresleri):\n";

                foreach (var ip in hostEntry.AddressList)
                {
                    DnsResult += $"  ✅ {ip} ({ip.AddressFamily})\n";
                }

                if (hostEntry.Aliases.Length > 0)
                {
                    DnsResult += "\n🔗 Aliaslar:\n";
                    foreach (var alias in hostEntry.Aliases)
                    {
                        DnsResult += $"  • {alias}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                DnsResult = $"❌ DNS lookup hatası: {ex.Message}";
            }
        }
    }
}
