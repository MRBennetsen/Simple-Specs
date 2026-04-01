using System;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Linq;

namespace Simple_Specs
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadHardwareSpecsAsync();
        }

        private async void LoadHardwareSpecsAsync()
        {
            await Task.Run(() =>
            {
                string sysInfo = GetSystemInfo();
                string cpu = GetWmiData("Win32_Processor", "Name");
                string gpu = GetWmiData("Win32_VideoController", "Name");
                string ram = GetTotalRam();
                string storage = GetStorageInfo();
                string battery = GetBatteryHealth();

                // Opdater UI på hovedtråden
                Dispatcher.Invoke(() =>
                {
                    // Dynamisk SYSTEM kort
                    if (!string.IsNullOrWhiteSpace(sysInfo))
                    {
                        SystemText.Text = sysInfo;
                        SystemCard.Visibility = Visibility.Visible;
                    }

                    CpuText.Text = string.IsNullOrEmpty(cpu) ? "Kunne ikke finde CPU" : cpu;
                    GpuText.Text = string.IsNullOrEmpty(gpu) ? "Kunne ikke finde GPU" : gpu;
                    RamText.Text = ram;
                    StorageText.Text = storage;

                    // Dynamisk BATTERI kort
                    if (!string.IsNullOrWhiteSpace(battery))
                    {
                        BatteryText.Text = battery;
                        BatteryCard.Visibility = Visibility.Visible;
                    }
                });
            });
        }

        private string GetWmiData(string wmiClass, string property)
        {
            try
            {
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj[property]?.ToString()?.Trim() ?? "";
                }
            }
            catch { }
            return "";
        }

        private string? GetSystemInfo()
        {
            try
            {
                string manufacturer = GetWmiData("Win32_ComputerSystem", "Manufacturer");
                string model = GetWmiData("Win32_ComputerSystem", "Model");
                string serial = GetWmiData("Win32_BIOS", "SerialNumber");

                // Filtrer "skralde-data" som nogle producenter lader stå
                if (serial.Contains("O.E.M.") || serial.Contains("Default")) serial = "";
                if (manufacturer.Contains("O.E.M.")) manufacturer = "";

                if (string.IsNullOrEmpty(manufacturer) && string.IsNullOrEmpty(model)) 
                    return null;

                string output = $"{manufacturer} {model}".Trim();
                if (!string.IsNullOrEmpty(serial))
                    output += $"\nSerienummer: {serial}";

                return output;
            }
            catch { return null; }
        }

        private string? GetBatteryHealth()
        {
            try
            {
                uint fullCharge = 0;
                uint designCapacity = 0;

                // Nogle WMI namespaces kræver admin, vi bruger try/catch til at håndtere det
                try
                {
                    using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity"))
                    {
                        foreach (ManagementObject mo in searcher.Get()) { fullCharge = Convert.ToUInt32(mo["FullChargedCapacity"]); break; }
                    }
                    using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT DesignedCapacity FROM BatteryStaticData"))
                    {
                        foreach (ManagementObject mo in searcher.Get()) { designCapacity = Convert.ToUInt32(mo["DesignedCapacity"]); break; }
                    }
                }
                catch { /* Ignorer og gå til fallback */ }

                if (fullCharge > 0 && designCapacity > 0)
                {
                    double health = Math.Round(((double)fullCharge / designCapacity) * 100);
                    if (health > 100) health = 100; // Sikkerhed for underlige sensor-aflæsninger
                    return $"Batteritilstand: {health}%\nDesign: {designCapacity} mWh\nNu: {fullCharge} mWh";
                }

                // Fallback hvis vi ikke kan få adgang til det dybe batteri-helbred
                using (var searcher = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining FROM Win32_Battery"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        if (mo["EstimatedChargeRemaining"] != null)
                            return $"Batteri: {mo["EstimatedChargeRemaining"]}% tilbage";
                    }
                }
            }
            catch { }
            return null; // Returnerer null hvis intet batteri findes
        }

        private string GetTotalRam()
        {
            string capacity = "Kunne ikke beregne RAM";
            string speed = "";
            string modelInfo = "";

            try
            {
                // 1. Samlet kapacitet
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["TotalPhysicalMemory"] != null)
                    {
                        double bytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                        double gb = bytes / (1024 * 1024 * 1024);
                        capacity = $"{Math.Round(gb)} GB System RAM";
                    }
                }

                // 2. Hastighed og Model
                using ManagementObjectSearcher memSearcher = new ManagementObjectSearcher("SELECT Speed, Manufacturer, PartNumber FROM Win32_PhysicalMemory");
                foreach (ManagementObject obj in memSearcher.Get())
                {
                    if (obj["Speed"] != null && obj["Speed"].ToString() != "0")
                    {
                        speed = $"\nHastighed: {obj["Speed"]} MT/s";
                    }

                    string mfg = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                    string part = obj["PartNumber"]?.ToString()?.Trim() ?? "";

                    // Filtrer "skralde-data" fra dovne producenter
                    if (!string.IsNullOrEmpty(part) && part != "Unknown" && part != "00000000")
                    {
                        if (!string.IsNullOrEmpty(mfg) && mfg != "Unknown" && mfg != "0000")
                        {
                            modelInfo = $"\nModel: {mfg} {part}";
                        }
                        else
                        {
                            modelInfo = $"\nModel: {part}";
                        }
                    }
                    
                    break; // Vi trækker bare info fra den første RAM-blok for at holde det simpelt
                }
            }
            catch { }
            
            return capacity + speed + modelInfo;
        }

        private string GetStorageInfo()
        {
            string storageDetails = "";
            try
            {
                // 1. Find Fysiske drev (Hardware modeller)
                using ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive");
                foreach (ManagementObject obj in diskSearcher.Get())
                {
                    if (obj["Model"] != null && obj["Size"] != null)
                    {
                        double totalGb = Convert.ToDouble(obj["Size"]) / (1024 * 1024 * 1024);
                        storageDetails += $"💾 {obj["Model"]} ({Math.Round(totalGb)} GB)\n";
                    }
                }

                storageDetails += "\n";

                // 2. Find Logiske partitioner (C:, D: ledig plads)
                using ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher("SELECT DeviceID, FreeSpace, Size FROM Win32_LogicalDisk WHERE DriveType = 3");
                foreach (ManagementObject obj in partitionSearcher.Get())
                {
                    if (obj["Size"] != null && obj["FreeSpace"] != null)
                    {
                        double totalGb = Convert.ToDouble(obj["Size"]) / (1024 * 1024 * 1024);
                        double freeGb = Convert.ToDouble(obj["FreeSpace"]) / (1024 * 1024 * 1024);
                        storageDetails += $"{obj["DeviceID"]} {Math.Round(freeGb)} GB ledig af {Math.Round(totalGb)} GB\n";
                    }
                }
            }
            catch { }
            
            return string.IsNullOrEmpty(storageDetails.Trim()) ? "Kunne ikke finde lagerdrev" : storageDetails.Trim();
        }

        private async Task<string> GetNetworkInfoAsync()
        {
            string info = "";
            try 
            {
                var ni = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(i => i.OperationalStatus == OperationalStatus.Up && 
                                         i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                         i.NetworkInterfaceType != NetworkInterfaceType.Tunnel);
                
                if (ni != null) 
                {
                    var props = ni.GetIPProperties();
                    var addr = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    string macAddress = string.Join("-", ni.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
                    
                    info += $"Kort: {ni.Description}\n";
                    info += $"Privat IP: {addr?.Address}\n";
                    info += $"MAC: {macAddress}\n";
                }
                else { info += "Ingen aktiv lokal netværksforbindelse fundet.\n"; }

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string publicIp = await client.GetStringAsync("https://api.ipify.org");
                info += $"Offentlig IP: {publicIp}";
            }
            catch { info += "Kunne ikke hente offentlig IP (Tjek internetforbindelse)."; }
            
            return info;
        }

        private async void UpdateNetwork_Click(object sender, RoutedEventArgs e) 
        {
            NetworkText.Text = "Henter netværksinfo...\nVent venligst.";
            NetworkText.Text = await GetNetworkInfoAsync();
        }

        private void CopySpecs_Click(object sender, RoutedEventArgs e) 
        {
            // Vi bygger dynamisk strengen, så den kun kopierer de kort der rent faktisk er vist
            string allSpecs = "--- SIMPLE-SPECS RAPPORT ---\n\n";
            
            if (SystemCard.Visibility == Visibility.Visible)
                allSpecs += $"SYSTEM:\n{SystemText.Text}\n\n";
                
            allSpecs += $"CPU:\n{CpuText.Text}\n\n" +
                        $"GPU:\n{GpuText.Text}\n\n" +
                        $"RAM:\n{RamText.Text}\n\n" +
                        $"LAGER:\n{StorageText.Text}\n\n";

            if (BatteryCard.Visibility == Visibility.Visible)
                allSpecs += $"BATTERI:\n{BatteryText.Text}\n\n";
                
            allSpecs += $"NETVÆRK:\n{NetworkText.Text}\n\n" +
                        "----------------------------";
                              
            Clipboard.SetText(allSpecs);
            MessageBox.Show("Specifikationer er kopieret til udklipsholderen!", "Kopieret", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}