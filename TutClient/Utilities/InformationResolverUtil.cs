using System;
using System.Management;
using System.Net;
using System.Net.Sockets;

namespace TutClient.Utilities
{
    public interface IInformationResolverUtil
    {
        string GetLocalIpAddress();
        string AvName();
    }

    public class InformationResolverUtil : IInformationResolverUtil
    {
        private string _localIpCache = string.Empty;
        private string _localAvCache = string.Empty;

        /// <summary>
        /// Get the IPv4 address of the local machine
        /// </summary>
        /// <returns>The IPv4 address of the machine</returns>
        public string GetLocalIpAddress()
        {
            if (_localIpCache != string.Empty) return _localIpCache;
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName()); //Get our ip addresses
            foreach (IPAddress ip in host.AddressList) //Go through the addresses
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) //If address is inet
                {
                    _localIpCache = ip.ToString();
                    return ip.ToString(); //Return the ip of the machine
                }
            }
            return "N/A"; //IP not found at this point
        }

        /// <summary>
        /// Get the Anti-Virus product name of the machine
        /// </summary>
        /// <returns>The name of the installed AV product</returns>
        public string AvName()
        {
            if (_localAvCache != string.Empty) return _localAvCache;
            var wmiPathStr = @"\\" + Environment.MachineName + @"\root\SecurityCenter2"; //Create the WMI path
            var searcher = new ManagementObjectSearcher(wmiPathStr, "SELECT * FROM AntivirusProduct"); //Create a search query
            var instances = searcher.Get(); //Search the database
            var av = ""; //The name of the AV product
            foreach (var instance in instances) //Go through the results
            {
                //Console.WriteLine(instance.GetPropertyValue("displayName"));
                av = instance.GetPropertyValue("displayName").ToString(); //Get the name of the AV
            }

            if (av == "") av = "N/A"; //If AV name isn't found return this

            _localAvCache = av;

            return av; //Return the name of the installed AV Product
        }
    }
}