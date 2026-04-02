using System;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Linq;
using System.Windows.Controls;

namespace Simple_Specs
{
    public partial class MainWindow : Window
    {
        private string _gpuBrand = ""; // Gemmer mærket (AMD/Nvidia) til opdateringsknappen

        public MainWindow()
        {
            InitializeComponent();
            LoadHardwareSpecsAsync();
            LoadSupportSpecsAsync();
        }

        // --- VINDUES KONTROL & FANER ---
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SwitchTab_Click(object sender, RoutedEventArgs e)
        {
            Button clickedBtn = (Button)sender;
            
            // Nulstil farver
            BtnTabSystem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
            BtnTabSupport.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
            
            // Sæt valgt farve og skift fane
            clickedBtn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00D2FF"));

            if (clickedBtn.Name == "BtnTabSystem")
            {
                TabSystemInfo.Visibility = Visibility.Visible;
                TabSupport.Visibility = Visibility.Collapsed;
                BtnCopy.Visibility = Visibility.Visible;
            }
            else
            {
                TabSystemInfo.Visibility = Visibility.Collapsed;
                TabSupport.Visibility = Visibility.Visible;
                BtnCopy.Visibility = Visibility.Collapsed; // Gemmer kopier knappen på support siden
            }
        }

        // --- INDLÆSNING AF DATA ---
        private async void LoadHardwareSpecsAsync()
        {
            await Task.Run(() =>
            {
                string sysInfo = GetSystemInfo();
                string cpu = GetWmiData("Win32_Processor", "Name");
                string gpu = GetGpuInfo(); 
                string ram = GetTotalRam();
                string storage = GetStorageInfo();

                Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrWhiteSpace(sysInfo))
                    {
                        SystemText.Text = sysInfo;
                        SystemCard.Visibility = Visibility.Visible;
                    }
                    CpuText.Text = string.IsNullOrEmpty(cpu) ? "Kunne ikke finde CPU" : cpu;
                    GpuText.Text = string.IsNullOrEmpty(gpu) ? "Kunne ikke finde GPU" : gpu;
                    RamText.Text = ram;
                    StorageText.Text = storage;
                });
            });
        }

        private async void LoadSupportSpecsAsync()
        {
            await Task.Run(() =>
            {
                string os = GetWmiData("Win32_OperatingSystem", "Caption");
                string build = GetWmiData("Win32_OperatingSystem", "Version");
                string uptime = GetUptime();
                string battery = GetBatteryHealth();
                string driverInfo = GetGpuDriverInfo();

                Dispatcher.Invoke(() =>
                {
                    OsText.Text = $"OS: {os}\nBuild: {build}";
                    UptimeText.Text = $"Oppetid: {uptime}";
                    BatterySupportText.Text = battery ?? "Ingen batteri fundet.";
                    
                    DriverText.Text = driverInfo;
                    if (_gpuBrand == "NVIDIA" || _gpuBrand == "AMD" || _gpuBrand == "INTEL")
                    {
                        BtnUpdateDriver.Content = $"Find opdatering hos {_gpuBrand}";
                        BtnUpdateDriver.Visibility = Visibility.Visible;
                    }
                });
            });
        }

        // --- WMI METODER ---
        private string GetUptime()
        {
            try
            {
                TimeSpan t = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return $"{t.Days} dage, {t.Hours} timer, {t.Minutes} min.";
            }
            catch { return "Ukendt"; }
        }

        private string GetGpuDriverInfo()
        {
            string info = "";
            try
            {
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, DriverDate FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString()?.Trim() ?? "";
                    string version = obj["DriverVersion"]?.ToString()?.Trim() ?? "";
                    string dateRaw = obj["DriverDate"]?.ToString()?.Trim() ?? "";
                    
                    string lowerName = name.ToLower();
                    if (!lowerName.Contains("virtual") && !lowerName.Contains("parsec") && !lowerName.Contains("citrix") && !lowerName.Contains("basic"))
                    {
                        string cleanDate = "Ukendt dato";
                        if (dateRaw.Length >= 8) 
                        {
                            cleanDate = $"{dateRaw.Substring(6, 2)}/{dateRaw.Substring(4, 2)}-{dateRaw.Substring(0, 4)}";
                        }

                        info += $"• {name}\nVersion: {version}\nDato: {cleanDate}\n\n";

                        // Gem mærket til knappen
                        if (lowerName.Contains("nvidia")) _gpuBrand = "NVIDIA";
                        else if (lowerName.Contains("amd") || lowerName.Contains("radeon")) _gpuBrand = "AMD";
                        else if (lowerName.Contains("intel")) _gpuBrand = "INTEL";
                    }
                }
            }
            catch { }
            return string.IsNullOrEmpty(info.Trim()) ? "Ingen fysisk driver fundet" : info.Trim();
        }

        private void UpdateDriver_Click(object sender, RoutedEventArgs e)
        {
            string url = "";
            if (_gpuBrand == "NVIDIA") url = "https://www.nvidia.com/Download/index.aspx";
            else if (_gpuBrand == "AMD") url = "https://www.amd.com/en/support/download/drivers.html";
            else if (_gpuBrand == "INTEL") url = "https://www.intel.com/content/www/us/en/download-center/home.html";

            if (!string.IsNullOrEmpty(url))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        private async void StartTeamViewer_Click(object sender, RoutedEventArgs e)
        {
            BtnTeamViewer.Content = "Henter TeamViewer...";
            BtnTeamViewer.IsEnabled = false;

            try
            {
                // Dette er det direkte link til TeamViewers standard QuickSupport
                // Udskift evt. med jeres virksomheds eget Custom TeamViewer QS link
                string downloadUrl = "https://download.teamviewer.com/download/TeamViewerQS.exe";
                string tempPath = Path.Combine(Path.GetTempPath(), "SimpleSpecs_TeamViewerQS.exe");

                using (HttpClient client = new HttpClient())
                {
                    byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(tempPath, fileBytes);
                }

                BtnTeamViewer.Content = "Starter...";
                
                // Åbn programmet
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                
                BtnTeamViewer.Content = "Download & Start TeamViewer";
                BtnTeamViewer.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunne ikke hente TeamViewer. Tjek forbindelsen.\n\nFejl: {ex.Message}", "Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnTeamViewer.Content = "Download & Start TeamViewer";
                BtnTeamViewer.IsEnabled = true;
            }
        }

        // --- RESTEN ER DINE EKSISTERENDE METODER (GpuInfo, Ram, Storage osv.) ---
        private string GetGpuInfo()
        {
            string gpuDetails = "";
            try
            {
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string gpuName = obj["Name"]?.ToString()?.Trim() ?? "";
                    string lowerName = gpuName.ToLower();

                    if (!string.IsNullOrEmpty(gpuName) && !lowerName.Contains("virtual") && !lowerName.Contains("parsec") && !lowerName.Contains("citrix") && !lowerName.Contains("basic"))
                    {
                        gpuDetails += $"• {gpuName}\n";
                    }
                }
            }
            catch { }
            return string.IsNullOrEmpty(gpuDetails.Trim()) ? "Kunne ikke finde GPU" : gpuDetails.Trim();
        }

        private string GetWmiData(string wmiClass, string property)
        {
            try
            {
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (ManagementObject obj in searcher.Get()) { return obj[property]?.ToString()?.Trim() ?? ""; }
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

                if (serial.Contains("O.E.M.") || serial.Contains("Default")) serial = "";
                if (manufacturer.Contains("O.E.M.")) manufacturer = "";

                if (string.IsNullOrEmpty(manufacturer) && string.IsNullOrEmpty(model)) return null;

                string output = $"{manufacturer} {model}".Trim();
                if (!string.IsNullOrEmpty(serial)) output += $"\nSerienummer: {serial}";
                return output;
            }
            catch { return null; }
        }

        private string? GetBatteryHealth()
        {
            try
            {
                uint fullCharge = 0; uint designCapacity = 0;
                try
                {
                    using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity")) { foreach (ManagementObject mo in searcher.Get()) { fullCharge = Convert.ToUInt32(mo["FullChargedCapacity"]); break; } }
                    using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT DesignedCapacity FROM BatteryStaticData")) { foreach (ManagementObject mo in searcher.Get()) { designCapacity = Convert.ToUInt32(mo["DesignedCapacity"]); break; } }
                }
                catch { }

                if (fullCharge > 0 && designCapacity > 0)
                {
                    double health = Math.Round(((double)fullCharge / designCapacity) * 100);
                    if (health > 100) health = 100; 
                    return $"Batteritilstand: {health}%\nDesign: {designCapacity} mWh\nNu: {fullCharge} mWh";
                }

                using (var searcher = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining FROM Win32_Battery"))
                {
                    foreach (ManagementObject mo in searcher.Get()) { if (mo["EstimatedChargeRemaining"] != null) return $"Batteri: {mo["EstimatedChargeRemaining"]}% tilbage"; }
                }
            }
            catch { }
            return null; 
        }

        private string GetTotalRam()
        {
            string capacity = "Kunne ikke beregne RAM";
            string speed = "";
            string modelInfo = "";
            try
            {
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get()) { if (obj["TotalPhysicalMemory"] != null) { double bytes = Convert.ToDouble(obj["TotalPhysicalMemory"]); double gb = bytes / (1024 * 1024 * 1024); capacity = $"{Math.Round(gb)} GB System RAM"; } }

                using ManagementObjectSearcher memSearcher = new ManagementObjectSearcher("SELECT Speed, Manufacturer, PartNumber FROM Win32_PhysicalMemory");
                foreach (ManagementObject obj in memSearcher.Get())
                {
                    if (obj["Speed"] != null && obj["Speed"].ToString() != "0") speed = $"\nHastighed: {obj["Speed"]} MT/s";
                    string mfg = obj["Manufacturer"]?.ToString()?.Trim() ?? ""; string part = obj["PartNumber"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(part) && part != "Unknown" && part != "00000000") { if (!string.IsNullOrEmpty(mfg) && mfg != "Unknown" && mfg != "0000") { modelInfo = $"\nModel: {mfg} {part}"; } else { modelInfo = $"\nModel: {part}"; } }
                    break; 
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
                using ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive");
                foreach (ManagementObject obj in diskSearcher.Get()) { if (obj["Model"] != null && obj["Size"] != null) { double totalGb = Convert.ToDouble(obj["Size"]) / (1024 * 1024 * 1024); storageDetails += $"💾 {obj["Model"]} ({Math.Round(totalGb)} GB)\n"; } }
                storageDetails += "\n";
                using ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher("SELECT DeviceID, FreeSpace, Size FROM Win32_LogicalDisk WHERE DriveType = 3");
                foreach (ManagementObject obj in partitionSearcher.Get()) { if (obj["Size"] != null && obj["FreeSpace"] != null) { double totalGb = Convert.ToDouble(obj["Size"]) / (1024 * 1024 * 1024); double freeGb = Convert.ToDouble(obj["FreeSpace"]) / (1024 * 1024 * 1024); storageDetails += $"{obj["DeviceID"]} {Math.Round(freeGb)} GB ledig af {Math.Round(totalGb)} GB\n"; } }
            }
            catch { }
            return string.IsNullOrEmpty(storageDetails.Trim()) ? "Kunne ikke finde lagerdrev" : storageDetails.Trim();
        }

        private async Task<string> GetNetworkInfoAsync()
        {
            string info = "";
            try 
            {
                var ni = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.OperationalStatus == OperationalStatus.Up && i.NetworkInterfaceType != NetworkInterfaceType.Loopback && i.NetworkInterfaceType != NetworkInterfaceType.Tunnel);
                if (ni != null) { var props = ni.GetIPProperties(); var addr = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork); string macAddress = string.Join("-", ni.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2"))); info += $"Kort: {ni.Description}\nPrivat IP: {addr?.Address}\nMAC: {macAddress}\n"; }
                else { info += "Ingen aktiv lokal netværksforbindelse fundet.\n"; }

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string publicIp = await client.GetStringAsync("https://api.ipify.org"); info += $"Offentlig IP: {publicIp}";
            }
            catch { info += "Kunne ikke hente offentlig IP (Tjek internetforbindelse)."; }
            return info;
        }

        private async void UpdateNetwork_Click(object sender, RoutedEventArgs e) { NetworkText.Text = "Henter netværksinfo...\nVent venligst."; NetworkText.Text = await GetNetworkInfoAsync(); }

        private void CopySpecs_Click(object sender, RoutedEventArgs e) 
        {
            string allSpecs = "--- SIMPLE-SPECS RAPPORT ---\n\n";
            
            if (SystemCard.Visibility == Visibility.Visible)
                allSpecs += $"SYSTEM:\n{SystemText.Text}\n\n";
                
            allSpecs += $"CPU:\n{CpuText.Text}\n\n" +
                        $"GPU:\n{GpuText.Text}\n\n" +
                        $"RAM:\n{RamText.Text}\n\n" +
                        $"LAGER:\n{StorageText.Text}\n\n";

            // FIKS: Vi kigger nu på den nye batteri-tekst på support fanen
            if (!string.IsNullOrWhiteSpace(BatterySupportText.Text) && BatterySupportText.Text != "Ingen batteri fundet.")
                allSpecs += $"BATTERI:\n{BatterySupportText.Text}\n\n";
                
            allSpecs += $"NETVÆRK:\n{NetworkText.Text}\n\n" +
                        "----------------------------";
                              
            Clipboard.SetText(allSpecs);
            MessageBox.Show("Specifikationer er kopieret til udklipsholderen!", "Kopieret", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}