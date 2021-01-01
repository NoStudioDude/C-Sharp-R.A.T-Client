using System;
using System.Net;

namespace TutClient.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Convert a connection string to an IP Address
        /// </summary>
        /// <param name="input">The connection string</param>
        /// <returns>The IP Address of the R.A.T Server if can be parsed, otherwise false</returns>
        public static string GetIpAddress(this string input)
        {
            if (input == "") return null; //Filter empty input
            var validIp = true; //True if input is a valid IP

            if (input.Contains(".")) //Input contains dots
            {
                var parts = input.Split('.'); //Get the octects
                if (parts.Length == 4) //If 4 octets present
                {
                    foreach (var ipPart in parts) //Loop throught them
                    {
                        for (var i = 0; i < ipPart.Length; i++) //Check char by char
                        {
                            if (!char.IsNumber(ipPart[i])) //If char isn't a nuber, then input isn't an IP
                            {
                                validIp = false; //Invalid for IP
                                break; //Break out
                            }
                        }

                        if (!validIp) //Invalid IP Address
                        {
                            Console.WriteLine("Invalid IP Address!\r\nInput is not an IP Address");
                            break; //Break
                        }
                    }

                    return validIp ? input : ResolveDns(input);
                }
                
                //input doesn't have 4 parts, but it can be still a hostname
                return ResolveDns(input); //Get the IP of the DNS name
            }

            return null; //All parsing failed at this point
        }

        /// <summary>
        /// Resolve a DNS name into an IP Address
        /// </summary>
        /// <param name="input">The DNS name to resolve</param>
        /// <returns>The IP Address if resolvation is successful, otherwise null</returns>
        private static string ResolveDns(string input)
        {
            try
            {
                var ipAddr = Dns.GetHostAddresses(input)[0].ToString(); //Try to get the first result
                return ipAddr; //Return the IP Address
            }
            catch (Exception ex) //Something went wrong
            {
                Console.WriteLine("Dns Resolve on input: " + input + " failed\r\n" + ex.Message);
                return null; //Return null
            }
        }
    }
}
