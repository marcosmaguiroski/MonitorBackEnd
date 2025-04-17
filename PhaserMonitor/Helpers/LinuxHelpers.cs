using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PhaserMonitor.Model;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static PhaserMonitor.Helpers.LinuxHelpers;

namespace PhaserMonitor.Helpers
{

    public static class LinuxHelpers
    {//


        public static HardwareModel hardwareModel = CheckModel();
        public static string ChangeMacByCPUSerialNumber()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {//
                string bashCommand = "";
                switch (hardwareModel)
                {
                    case HardwareModel.orangepi4:
                        bashCommand += "SERIAL=$(cat /proc/device-tree/serial-number); ";
                        break;
                    case HardwareModel.orangepi5:
                        bashCommand += "SERIAL=$(cat_serial.sh); ";
                        break;
                        //case HardwareModel.raspberrypi:
                        //    bashCommand += "SERIAL=$(cat /proc/cpuinfo | grep Serial | cut -d ' ' -f 2); ";
                        //    break;
                }
                bashCommand += "MAC=$(echo $SERIAL | md5sum | sed 's/^\\(..\\)\\(..\\)\\(..\\)\\(..\\)\\(..\\)\\(..\\).*$/02:\\1:\\2:\\3:\\4:\\5/'); ";
                bashCommand += "echo $SERIAL; echo $MAC";

                string result = bashCommand.Bash(true);
                string[] output = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                if (output.Length >= 2)
                {
                    string serial = output[0];
                    string mac = output[1];

                    Console.WriteLine("CPU Serial Number is: " + serial);
                    Console.WriteLine("New MAC is: " + mac);

                    "sudo ip link set dev eth0 down".Bash();
                    $"sudo ip link set dev eth0 address {mac}".Bash();
                    "sudo ip link set dev eth0 up".Bash();
                    return mac;

                }



            }
            return "AA-19-52-F6-75-4A";
        }
        public static bool Is24LC64(int i2cNumber)
        {
            // Tentativa de leitura de um endereço alto
            try
            {
                int highAddress = 0x100; // Um endereço além da capacidade da 24LC02
                byte highAddressHigh = (byte)(highAddress >> 8);
                byte highAddressLow = (byte)(highAddress & 0xFF);

                // Comando para ler o endereço alto
                var result = string.Format("i2cget -y {0} 0x50 0x{1:X2} w", i2cNumber, highAddressLow).Bash();

                // Se a leitura for bem-sucedida, assume-se que é uma 24LC64
                return true;
            }
            catch (Exception)
            {
                // Se a leitura falhar, assume-se que é uma 24LC02
                return false;
            }
        }

        public static void SetSerialNumber(List<byte> serial)
        {
            if (serial.Count > 10)
                throw new Exception("Wrong serial number size.");

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                ushort check = 0;
                for (int i = 0; i < serial.Count; i++)
                    check += serial[i];

                check = (ushort)(0x100 - check);

                var i2cNumber = 1;

                if (hardwareModel == HardwareModel.orangepi4) // OrangePi4 has a different i2c bus
                {
                    i2cNumber = 3;
                }

                bool is24LC64 = Is24LC64(i2cNumber);

                for (int i = 0; i < 10; i++)
                {
                    int address = 0x10 + i;
                    if (is24LC64)
                    {
                        byte addressHigh = (byte)(address >> 8);
                        byte addressLow = (byte)(address & 0xFF);

                        string.Format("i2cset -y {2} 0x50 0x{0:X2} 0x{1:X2} i", addressHigh, addressLow, i2cNumber).Bash();
                        string.Format("i2cset -y {2} 0x50 0x{0:X2} 0x{1:X2}", addressLow, serial[i], i2cNumber).Bash();
                    }
                    else
                    {
                        string.Format("i2cset -y {2} 0x50 0x{0:X2} 0x{1:X2}", 0x10 + i, serial[i], i2cNumber).Bash();
                    }
                }

                if (is24LC64)
                {
                    int checkAddress1 = 0x1A;
                    byte checkAddress1High = (byte)(checkAddress1 >> 8);
                    byte checkAddress1Low = (byte)(checkAddress1 & 0xFF);
                    string.Format("i2cset -y {2} 0x50 0x{0:X2} 0x{1:X2} i", checkAddress1High, checkAddress1Low, i2cNumber).Bash();
                    string.Format("i2cset -y {2} 0x50 0x{0:X2} 0x{1:X2}", checkAddress1Low, BitConverter.GetBytes(check)[0], i2cNumber).Bash();

                    int checkAddress2 = 0x1B;
                    byte checkAddress2High = (byte)(checkAddress2 >> 8);
                    byte checkAddress2Low = (byte)(checkAddress2 & 0xFF);
                    string.Format("i2cset -y {2} 0x50 0x{0:X2} 0x{1:X2} i", checkAddress2High, checkAddress2Low, i2cNumber).Bash();
                    string.Format("i2cset -y {2} 0x50 0x{0:X2} 0x{1:X2}", checkAddress2Low, BitConverter.GetBytes(check)[1], i2cNumber).Bash();
                }
                else
                {
                    string.Format("i2cset -y {0} 0x50 0x1A 0x{1}", i2cNumber, BitConverter.GetBytes(check)[0].ToString("X2")).Bash();
                    string.Format("i2cset -y {0} 0x50 0x1B 0x{1}", i2cNumber, BitConverter.GetBytes(check)[1].ToString("X2")).Bash();
                }
            }
        }
        public static string GetSerialNumber()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                try
                {
                    string serial = "";

                    var i2cNumber = 1;

                    if (hardwareModel == HardwareModel.orangepi4) // OrangePi4 has a different i2c bus
                    {
                        i2cNumber = 3;
                    }

                    bool is24LC64 = Is24LC64(i2cNumber);

                    List<byte> Serial = new List<byte>();
                    for (int i = 0; i < 12; i++)
                    {
                        int address = 0x10 + i;
                        if (is24LC64)
                        {
                            byte addressHigh = (byte)(address >> 8);
                            byte addressLow = (byte)(address & 0xFF);

                            string.Format("i2cget -y {0} 0x50 0x{1:X2} w", i2cNumber, addressLow).Bash();
                            Serial.Add(Convert.ToByte(string.Format("i2cget -y {0} 0x50 0x{1:X2}", i2cNumber, addressLow).Bash().Replace("\n", ""), 16));
                        }
                        else
                        {
                            Serial.Add(Convert.ToByte(string.Format("i2cget -y {0} 0x50 0x{1:X2}", i2cNumber, (0x10 + i).ToString("X2")).Bash().Replace("\n", ""), 16));
                        }
                    }

                    ushort check = 0;
                    for (int i = 0; i < Serial.Count - 2; i++)
                        check += Serial[i];

                    check = (ushort)(0x100 - check);

                    ushort chekc2 = (ushort)(Serial[Serial.Count - 1] << 8 | Serial[Serial.Count - 2]);
                    if (chekc2 == check)
                    {
                        serial = Encoding.UTF8.GetString(Serial.GetRange(0, 10).ToArray());
                        serial = serial.Replace("\0", string.Empty);
                    }
                    return serial;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    List<byte> bts = new List<byte>("7D123456".Select(c => (byte)c).ToArray());
                    while (bts.Count < 10)
                        bts.Add(0);

                    SetSerialNumber(bts);
                }
            }
            return "7D123456";
        }




        //public static string GetSerialNumber()
        //{
        //    if (Environment.OSVersion.Platform == PlatformID.Unix)
        //    {
        //        try
        //        {
        //            string serial = "";

        //            //List<byte> Serial = new List<byte>
        //            //{
        //            //	Convert.ToByte("i2cget -y 1 0x50 0x10".Bash().Replace("\n", ""), 16),
        //            //	Convert.ToByte("i2cget -y 1 0x50 0x11".Bash().Replace("\n", ""), 16),
        //            //	Convert.ToByte("i2cget -y 1 0x50 0x12".Bash().Replace("\n", ""), 16),
        //            //	Convert.ToByte("i2cget -y 1 0x50 0x13".Bash().Replace("\n", ""), 16),
        //            //	Convert.ToByte("i2cget -y 1 0x50 0x14".Bash().Replace("\n", ""), 16),
        //            //	Convert.ToByte("i2cget -y 1 0x50 0x15".Bash().Replace("\n", ""), 16)
        //            //};

        //            var i2cNumber = 1;

        //            if (hardwareModel == HardwareModel.orangepi4)//OrangePi4 has a diferent i2c bus
        //            {
        //                i2cNumber = 3;
        //            }


        //            List<byte> Serial = new List<byte>();
        //            for (int i = 0; i < 12; i++)
        //                Serial.Add(Convert.ToByte(string.Format("i2cget -y {0} 0x50 0x{1}", i2cNumber, (0x10 + i).ToString("X2")).Bash().Replace("\n", ""), 16));

        //            UInt16 check = 0;
        //            for (int i = 0; i < Serial.Count - 2; i++)
        //                check += (UInt16)Serial[i];

        //            check = (UInt16)(0x100 - check);

        //            UInt16 chekc2 = (UInt16)((UInt16)Serial[Serial.Count - 1] << 8 | (UInt16)Serial[Serial.Count - 2]);
        //            if (chekc2 == check)
        //            {
        //                serial = System.Text.Encoding.UTF8.GetString(Serial.GetRange(0, 10).ToArray()); //= BitConverter.ToString(Serial.GetRange(0, 10).ToArray()).Replace("-", "").ToUpper();
        //                serial = serial.Replace("\0", string.Empty);
        //            }
        //            //$"SERIAL_NUMBER = \"{serial}\" \n export SERIAL_NUMBER ".Bash();
        //            return serial;
        //        }
        //        catch (Exception ex)
        //        {

        //            Console.WriteLine(ex.Message);
        //            Console.WriteLine(ex.StackTrace);
        //            List<byte> bts = new List<byte>("7D123456".Select(c => (byte)c).ToArray());
        //            while (bts.Count < 10)
        //                bts.Add(0);

        //            LinuxHelpers.SetSerialNumber(bts);

        //        }

        //    }
        //    return "7D123456";
        //}




        //public static void SetSerialNumber(List<byte> serial)
        //{
        //    if (serial.Count > 10)
        //        throw new Exception("Wrong serial number size.");

        //    if (Environment.OSVersion.Platform == PlatformID.Unix)
        //    {
        //        UInt16 check = 0;
        //        for (int i = 0; i < serial.Count; i++)
        //            check += (UInt16)serial[i];

        //        check = (UInt16)(0x100 - check);

        //        var i2cNumber = 1;

        //        if (hardwareModel == HardwareModel.orangepi4)//OrangePi4 has a diferent i2c bus
        //        {
        //            i2cNumber = 3;
        //        }

        //        for (int i = 0; i < 10; i++)
        //        {
        //            string.Format("i2cset -y {2} 0x50 0x{0} 0x{1}", (0x10 + i).ToString("X2"), serial[i].ToString("X2"), i2cNumber).Bash();
        //        }

        //        string.Format("i2cset -y {0} 0x50 0x1A 0x{1}", i2cNumber, BitConverter.GetBytes(check)[0].ToString("X2")).Bash();
        //        string.Format("i2cset -y {0} 0x50 0x1B 0x{1}", i2cNumber, BitConverter.GetBytes(check)[1].ToString("X2")).Bash();
        //    }
        //}


        public static PhaserConfigurationModel PhaserConfiguration { get; set; } = new PhaserConfigurationModel();
        public static PhaserNetworkConfigurationModel GetNetworkConfiguration()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                using var fs = File.Open(@"/etc/dhcpcd.conf", FileMode.OpenOrCreate, FileAccess.ReadWrite);

                byte[] buffer = new byte[1024];
                string file = "";
                while (fs.Read(buffer, 0, buffer.Length) > 0)
                    file += Encoding.UTF8.GetString(buffer);

                var lines = file.Split(
                    new[] { "\r\n", "\r", "\n" },
                    StringSplitOptions.None
                );


                PhaserNetworkConfigurationModel iPConfiguration = new PhaserNetworkConfigurationModel();
                if (!lines.Any(x => x.StartsWith("static ip_address=")))
                {
                    iPConfiguration.DHCP = true;
                }
                else
                if (lines.Length > 0)
                {
                    try
                    {
                        var addrStr = lines.FirstOrDefault(x => x.StartsWith("static ip_address="));
                        if (!string.IsNullOrEmpty(addrStr))
                        {
                            var ips = addrStr.Split('=');
                            var ipm = ips[1].Split('/');
                            iPConfiguration.IPAddress = ipm[0];

                            uint mask = 0;
                            int nb = int.Parse(ipm[1]);
                            for (int i = 0; i < 32; i++)
                            {
                                if (nb > i)
                                    mask = mask << 1 | 1;
                                else
                                    mask <<= 1;
                            }

                            var str = new IPAddress(mask).ToString();
                            var strArray = str.Split('.');

                            iPConfiguration.NetMask = strArray[3] + "." + strArray[2] + "." + strArray[1] + "." + strArray[0];

                        }
                    }
                    catch { }

                    try
                    {
                        var addrStr = lines.FirstOrDefault(x => x.Contains("static routers="));
                        if (!string.IsNullOrEmpty(addrStr))
                        {
                            var ips = addrStr.Split('=');
                            iPConfiguration.Gateway = ips[1];
                        }
                    }
                    catch { }

                    try
                    {
                        var addrStr = lines.FirstOrDefault(x => x.Contains("static domain_name_servers="));
                        if (!string.IsNullOrEmpty(addrStr))
                        {
                            var ips = addrStr.Split('=');
                            var dn = ips[1].Split(' ');
                            iPConfiguration.DNSPrimary = dn[0];

                            iPConfiguration.DNSSecondary = dn[1];
                        }
                    }
                    catch { }
                }
                return iPConfiguration;
            }
            else
                return null;
        }

        private static int NumberOfSetBits(int i)
        {
            i -= i >> 1 & 0x55555555;
            i = (i & 0x33333333) + (i >> 2 & 0x33333333);
            return (i + (i >> 4) & 0x0F0F0F0F) * 0x01010101 >> 24;
        }


        public static void SetNetworkConfiguration(PhaserNetworkConfigurationModel iPConfiguration)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                if (iPConfiguration.DHCP != true)
                {
                    try
                    {
                        
                        if (string.IsNullOrEmpty(iPConfiguration.IPAddress))
                            throw new ArgumentException("Endereço IP é obrigatório para configuração estática");

                        if (string.IsNullOrEmpty(iPConfiguration.NetMask))
                            throw new ArgumentException("Máscara de rede é obrigatória para configuração estática");

                        if (string.IsNullOrEmpty(iPConfiguration.Gateway))
                            throw new ArgumentException("Gateway é obrigatório para configuração estática");

                        // Validação existente da máscara de rede
                        if (!IPAddress.TryParse(iPConfiguration.NetMask, out var netMask) || netMask.AddressFamily != AddressFamily.InterNetwork)
                            throw new ArgumentException("NetMask inválida.");

                        // Cálculo do CIDR
                        byte[] maskBytes = netMask.GetAddressBytes();
                        int addressAsInt = BitConverter.ToInt32(maskBytes.Reverse().ToArray(), 0);
                        int cidrMask = NumberOfSetBits(addressAsInt);

                        // Criação do conteúdo do arquivo
                        string fileContent = $"interface eth0\n" +
                                            $"noipv6rs\n" +
                                            $"static ip_address={iPConfiguration.IPAddress}/{cidrMask}\n" +
                                            $"static routers={iPConfiguration.Gateway}\n";

                        if (!string.IsNullOrEmpty(iPConfiguration.DNSPrimary) || !string.IsNullOrEmpty(iPConfiguration.DNSSecondary))
                        {
                            fileContent += $"static domain_name_servers={iPConfiguration.DNSPrimary} {iPConfiguration.DNSSecondary}\n";
                        }

                        // Escrita do arquivo
                        File.WriteAllText(@"/etc/dhcpcd.conf", fileContent);
                        PhaserConfiguration.Network = iPConfiguration;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao configurar rede: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    // Código existente para DHCP
                    File.WriteAllText(@"/etc/dhcpcd.conf", string.Empty);
                    PhaserConfiguration.Network.DHCP = true;
                }

                // Reinício do serviço mantido
                "systemctl restart dhcpcd.service".Bash();
            }
        }
        public static string GetSerialString()
        {
            return hardwareModel switch
            {
                HardwareModel.raspberrypi => "/dev/ttyAMA0",
                HardwareModel.orangepi4 => "/dev/ttyS4",
                HardwareModel.orangepi5 => "/dev/ttyS1",
                HardwareModel.orangepicm4 => "/dev/ttyS3",
                _ => "/dev/ttyAMA0",
            };
        }
        public static string GeneratePassword(string PhaserSerialNumber)
        {
            try
            {
                string RPiSerialNumber = "";
                string MermoryCardSrial = "";
                try
                {
                    switch (hardwareModel)
                    {
                        case HardwareModel.raspberrypi:
                            RPiSerialNumber = "cat /proc/cpuinfo | grep Serial | cut -d ' ' -f 2".Bash();
                            MermoryCardSrial = $"udevadm info --query=all --name=/dev/mmcblk0p1 | grep ID_SERIAL | cut -d '=' -f 2".Bash(true).Remove(0, 2);
                            break;
                        case HardwareModel.orangepi4:
                            RPiSerialNumber = "cat /proc/device-tree/serial-number".Bash();
                            MermoryCardSrial = $"udevadm info --query=all --name=/dev/mmcblk2p1 | grep ID_SERIAL | cut -d '=' -f 2".Bash(true).Remove(0, 2);
                            if (MermoryCardSrial.Contains("known device"))//Unknowm device
                            {
                                Console.WriteLine("/dev/mmcblk2p1 doesn't exists, apparently i'm running in the emmc");
                                MermoryCardSrial = $"udevadm info --query=all --name=/dev/mmcblk0p1 | grep ID_SERIAL | cut -d '=' -f 2".Bash().Remove(0, 2);

                                if (MermoryCardSrial.Contains("known device"))
                                { //Unknowm device
                                    Console.WriteLine("I'm neither runing in the emmc.");
                                }
                            }

                            break;
                        case HardwareModel.orangepi5:

                            RPiSerialNumber = "cat_serial.sh".Bash().Replace("Serial\t\t: ", "").Replace("\n", ""); ;
                            MermoryCardSrial = $"udevadm info --query=all --name=/dev/mmcblk1p1 | grep ID_SERIAL | cut -d '=' -f 2".Bash(true).Remove(0, 2);
                            if (MermoryCardSrial.Contains("known device"))//Unknowm device
                            {
                                Console.WriteLine("/dev/mmcblk1p1 doesn't exists, apparently i'm running in the emmc");
                                MermoryCardSrial = $"udevadm info --query=all --name=/dev/mmcblk0p1 | grep ID_SERIAL | cut -d '=' -f 2".Bash(true).Remove(0, 2);

                                if (MermoryCardSrial.Contains("known device"))
                                { //Unknowm device
                                    Console.WriteLine("I'm neither runing in the emmc.");
                                    Console.WriteLine("Apparently i'm running in the nvme");
                                    MermoryCardSrial = $"udevadm info --query=all --name=/dev/nvme0n1p1 | grep ID_SERIAL | cut -d '=' -f 2".Bash(true).Remove(0, 2);

                                    if (MermoryCardSrial.Contains("known device"))
                                    { //Unknowm device
                                        Console.WriteLine("I'm neither runing in the nvme. Help!");
                                    }
                                }
                            }

                            break;

                    }


                }
                catch (Exception ex)
                {

                    Console.WriteLine("GeneratePassword error: " + ex.Message);
                }

                var MacAddress = "cat /sys/class/net/eth0/address".Bash().Replace(":", "");

                var iRPiSerialNumber = long.Parse(RPiSerialNumber, System.Globalization.NumberStyles.HexNumber);

                // filter out the non-hex characters
                MermoryCardSrial = Regex.Replace(MermoryCardSrial, "[^0-9A-Fa-f]", "");
                //limit lenght to int64
                MermoryCardSrial = MermoryCardSrial.Substring(0, Math.Min(MermoryCardSrial.Length, 16));

                var iMermoryCardSrial = long.Parse(MermoryCardSrial, System.Globalization.NumberStyles.HexNumber);
                var iMacAddress = long.Parse(MacAddress, System.Globalization.NumberStyles.HexNumber);

                if (long.TryParse(PhaserSerialNumber, System.Globalization.NumberStyles.HexNumber, null, out var iPhaserSerialNumber))
                {
                    var hashRPiSerialNumber = SHA256.Create().ComputeHash(BitConverter.GetBytes(iRPiSerialNumber));
                    var hashMermoryCardSrial = SHA256.Create().ComputeHash(BitConverter.GetBytes(iMermoryCardSrial));
                    var hashMacAddress = SHA256.Create().ComputeHash(BitConverter.GetBytes(iMacAddress));
                    var hashPhaserSerialNumber = SHA256.Create().ComputeHash(BitConverter.GetBytes(iPhaserSerialNumber));

                    List<byte> pwdList = new List<byte>();
                    for (int i = 0; i < hashRPiSerialNumber.Length; i++)
                    {
                        switch (i % 4)
                        {
                            case 0:
                                pwdList.Add(hashRPiSerialNumber[i]);
                                break;

                            case 1:
                                pwdList.Add(hashMermoryCardSrial[i]);
                                break;

                            case 2:
                                pwdList.Add(hashMacAddress[i]);
                                break;

                            case 3:
                                pwdList.Add(hashPhaserSerialNumber[i]);
                                break;
                        }
                    }
                    var pwdBytes = SHA256.Create().ComputeHash(pwdList.ToArray());
                    var pwdString = BitConverter.ToString(pwdBytes).Replace("-", "").ToLower();

                    return pwdString.Substring(0, 10);
                }
                else
                    return "";
            }
            catch (Exception ex)
            {

                Console.WriteLine("GeneratePassword error: " + ex.Message);
                return "";
            }

        }

        public static void SetDateTime(DateTime dateTime, string TimeZone)
        {
            string.Format("date -s '{0}'", dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")).Bash();
            //To do
            //Sanitizar TimeZone
            string.Format("timedatectl set-timezone {0}", TimeZone).Bash();


            switch (hardwareModel)
            {
                case HardwareModel.raspberrypi:
                    "hwclock --systohc".Bash();//Acerta hora do sistem pela hora do hardware
                    break;
                case HardwareModel.orangepi4:
                    "hwclock --systohc -D -f /dev/rtc1".Bash();//Acerta hora do hardware pela hora do sistema
                    break;
                case HardwareModel.orangepi5:

                    "hwclock --systohc -D --localtime -f /dev/rtc1".Bash();//Acerta hora do hardware pela hora do sistema
                    break;

            }
        }

        static private bool StatusLed = false;

        public static void BlinkStatusLed()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {

                //switch case with all hardware models
                switch (hardwareModel)
                {
                    case HardwareModel.raspberrypi:
                        if (StatusLed)
                        {
                            StatusLed = false;
                            "raspi-gpio set 6 op dl".Bash();
                        }
                        else
                        {
                            StatusLed = true;
                            "raspi-gpio set 6 op dh".Bash();
                        }
                        break;
                    case HardwareModel.orangepi4:
                        "gpio toggle 5".Bash();
                        break;
                    case HardwareModel.orangepi5:
                        "gpio toggle 3".Bash();
                        break;

                    default:
                        break;

                }


            }
        }
        public static void ControlReadyRelay(bool state)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                if (hardwareModel is HardwareModel.raspberrypi)
                {
                    //if (state)
                    //{
                    //    StatusLed = false;
                    //    "raspi-gpio set 6 op dl".Bash();
                    //}
                    //else
                    //{
                    //    StatusLed = true;
                    //    "raspi-gpio set 6 op dh".Bash();
                    //}
                    Console.WriteLine("ControlReadyRelay  not implemented on Raspberry");
                }
                else
                {
                    $"gpio write {(int)OutputsPortsPi4.Ready} {(state ? 1 : 0)}".Bash();
                }
                switch (hardwareModel)
                {
                    case HardwareModel.orangepi4:
                        $"gpio write {(int)OutputsPortsPi4.Ready} {(state ? 1 : 0)}".Bash();
                        break;
                    case HardwareModel.orangepi5:
                        $"gpio write {(int)OutputsPortsPi5.Ready} {(state ? 1 : 0)}".Bash();
                        break;
                    default:
                        Console.WriteLine("ControlReadyRelay  not implemented on Raspberry");
                        throw new NotImplementedException();
                        break;
                }

            }
        }
        public static void ControlOutputOnRelay(bool state)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                if (hardwareModel is HardwareModel.raspberrypi)
                {
                    //if (state)
                    //{
                    //    StatusLed = false;
                    //    "raspi-gpio set 6 op dl".Bash();
                    //}
                    //else
                    //{
                    //    StatusLed = true;
                    //    "raspi-gpio set 6 op dh".Bash();
                    //}
                    Console.WriteLine("ControlOutputRelay  not implemented on Raspberry");
                }
                else
                {
                    $"gpio write {(int)OutputsPortsPi4.OutputOn} {(state ? 1 : 0)}".Bash();
                }
                switch (hardwareModel)
                {
                    case HardwareModel.raspberrypi:
                        Console.WriteLine("ControlOutputRelay  not implemented on Raspberry");
                        throw new NotImplementedException();
                        break;
                    case HardwareModel.orangepi4:
                        $"gpio write {(int)OutputsPortsPi4.OutputOn} {(state ? 1 : 0)}".Bash();
                        break;
                    case HardwareModel.orangepi5:
                        $"gpio write {(int)OutputsPortsPi5.OutputOn} {(state ? 1 : 0)}".Bash();
                        break;

                }
                switch (hardwareModel)
                {
                    case HardwareModel.raspberrypi:
                        Console.WriteLine("ControlOutputRelay  not implemented on Raspberry");
                        throw new NotImplementedException();
                        break;
                    case HardwareModel.orangepi4:
                        $"gpio write {(int)OutputsPortsPi4.OutputOn} {(state ? 1 : 0)}".Bash();
                        break;
                    case HardwareModel.orangepi5:
                        $"gpio write {(int)OutputsPortsPi5.OutputOn} {(state ? 1 : 0)}".Bash();
                        break;

                }

            }
        }
        static private bool CommLed = false;

        public static void BlinkCommLed()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {

                switch (hardwareModel)
                {
                    case HardwareModel.raspberrypi:
                        if (CommLed)
                        {
                            CommLed = false;
                            "raspi-gpio set 13 op dl".Bash();
                        }
                        else
                        {
                            CommLed = true;
                            "raspi-gpio set 13 op dh".Bash();
                        }
                        break;
                    case HardwareModel.orangepi4:
                        "gpio toggle 13".Bash();
                        break;
                    case HardwareModel.orangepi5:
                        "gpio toggle 11".Bash();
                        break;

                }

            }
        }
        public static void InitializationGPIO()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {

                switch (hardwareModel)
                {
                    case HardwareModel.raspberrypi:
                        break;
                    case HardwareModel.orangepi4:
                        "gpio mode 2 out".Bash();
                        "gpio mode 5 out".Bash();
                        "gpio mode 6 out".Bash();
                        "gpio mode 7 out".Bash();
                        "gpio mode 8 out".Bash();
                        "gpio mode 9 out".Bash();
                        "gpio mode 10 out".Bash();
                        "gpio mode 13 out".Bash();
                        "gpio mode 16 out".Bash();
                        "gpio write 2 1".Bash(); //Porta RX/TX em high
                        break;

                    case HardwareModel.orangepi5:


                        //GreenLight = 6,
                        "gpio mode 6 out".Bash();
                        //YellowLight = 5,
                        "gpio mode 5 out".Bash();
                        //RedLight = 4,
                        "gpio mode 4 out".Bash();
                        //Sound = 7,
                        "gpio mode 7 out".Bash();
                        //Comm = 11,
                        "gpio mode 11 out".Bash();
                        //Status = 3,
                        "gpio mode 3 out".Bash();
                        //Ready = 13,
                        "gpio mode 13 out".Bash();

                        //OutputOn = 12,
                        "gpio mode 12 out".Bash();
                        //RXTX = 2,
                        "gpio mode 2 out".Bash();
                        "gpio write 2 1".Bash(); //Porta RX/TX em high
                        break;

                }

            }
        }

        public enum OutputsPortsPi4
        {
            GreenLight = 7,
            YellowLight = 10,
            RedLight = 6,
            Sound = 8,
            Comm = 13,
            Status = 5,
            Ready = 16,
            OutputOn = 9,
        };

        public enum OutputsPortsPi5
        {
            GreenLight = 6,
            YellowLight = 5,
            RedLight = 4,
            Sound = 7,
            Comm = 11,
            Status = 3,
            Ready = 13,
            OutputOn = 12,
            RXTX = 2,


        };

        public static HardwareModel CheckModel()
        {

            try
            {
                string str = "uname -n".Bash();
                Console.WriteLine("Model: " + str);
                var model = str.Contains("orangepi5b") ? HardwareModel.orangepi5 : Enum.Parse<HardwareModel>(str, true);

                return model;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return HardwareModel.raspberrypi;
            }

        }

        public static float GetTemperature()
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {

                    switch (hardwareModel)
                    {
                        case HardwareModel.raspberrypi:
                            {
                                //TODO checar como é enviado no raspberry
                            }

                            break;
                        case HardwareModel.orangepi4:
                            {
                                string str = "sensors -j".Bash();

                                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(str); //Deserializei como dynamic por preguiça criar uma classe

                                return json?["cpu_thermal-virtual-0"]["temp1"]["temp1_input"] ?? 0;


                            }
                            break;

                        case HardwareModel.orangepi5:
                            {
                                string str = "sensors -j".Bash();

                                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(str); //Deserializei como dynamic por preguiça criar uma classe

                                return json?["center_thermal-virtual-0"]["temp1"]["temp1_input"] ?? 0;
                            }
                        default:


                            break;
                    }

                }
            }
            catch (Exception ex)
            {

                return 0;
            }



            return 0;
        }
        public static void LoadDateTimeFromRTC()
        {
            if (hardwareModel == HardwareModel.orangepi5)
            {
                "hwclock --hctosys -f /dev/rtc1".Bash();
                "hwclock -s -f /dev/rtc1".Bash();
            }
        }
        public static bool CheckRTCAndFix()
        {

            if (hardwareModel == HardwareModel.raspberrypi)
                return false;
            var str = "hwclock --show  -f /dev/rtc1".Bash(true);



            Console.WriteLine(str);
            var result = str.Contains("failed") || str.EndsWith("Invalid argument") || str.Contains("Cannot access the Hardware Clock");


            if (result)
            {
                if (hardwareModel == HardwareModel.orangepi4)
                {
                    "echo 0x68 > /sys/class/i2c-adapter/i2c-3/delete_device".Bash();//remove o device
                    "echo \"ds1307 0x68\" > /sys/class/i2c-adapter/i2c-3/new_device".Bash();//adiciona novamente
                }
                else
                {
                    "echo 0x68 > /sys/class/i2c-adapter/i2c-1/delete_device".Bash();//remove o device
                    "echo \"ds1307 0x68\" > /sys/class/i2c-adapter/i2c-1/new_device".Bash();//adiciona novamente
                }

                "hwclock --systohc -D --noadjfile --localtime -f /dev/rtc1".Bash();//define format
                $"hwclock --set --date {DateTime.Now.ToString("MM/dd/yy HH:mm:ss")} -f /dev/rtc1".Bash(true);//seta a hora

                "hwclock --systohc -f  /dev/rtc1".Bash();
                "hwclock --show  -f /dev/rtc1".Bash();


            }


            return result;
        }
        public static DateTime GetRTCDate()
        {
            try
            {
                return DateTime.Parse($"hwclock --show {(hardwareModel == HardwareModel.raspberrypi ? "" : "-f /dev/rtc1")} ".Bash(true));
            }
            catch
            {

                return DateTime.Now;
            }

        }
        public static void SetCurrentDateToRTC()
        {

            if (hardwareModel == HardwareModel.raspberrypi)
                $"hwclock --systohc".Bash(true);
            else
                $"hwclock --systohc -f /dev/rtc1".Bash(true);
        }
        public enum HardwareModel
        {
            orangepi4 = 0,
            raspberrypi,
            orangepi5,
            orangepicm4,

        }

    }
}
