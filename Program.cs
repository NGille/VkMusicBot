using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Net.Http.Headers;

namespace vmb
{
    internal class Program
    {
        static async Task Main2()
        {

        }
        static async Task Main()
        {
            string pubKey;
            string token;
            string appID;
            using (FileStream fs = File.OpenRead("props"))
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes);
                string props = Encoding.UTF8.GetString(bytes);
                pubKey = Regex.Match(props, @"pubKey=(.*)").Groups[1].Value;
                token = Regex.Match(props, @"botToken=(\S+)").Groups[1].Value;
                appID = Regex.Match(props, @"appID=(.*)").Groups[1].Value;
            }
            try
            {
                await GetCommands(token, appID);
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }
            X509Certificate cert = X509Certificate.CreateFromCertFile("cert.pfx");
            var server = new TcpListener(IPAddress.Any, 443);
            server.Start();
            Console.WriteLine("Strated");
            byte[] msgBuf = new byte[1024];
            string response = "HTTP/1.1 200 OK\r\nHost: vkmusicbot.ru\r\nConnection: Close\r\n\r\n";
            while (true)
            {
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
                using var client = await server.AcceptTcpClientAsync();
                using SslStream sslStream = new SslStream(client.GetStream(), false);
                try
                {
                    sslStream.AuthenticateAsServer(cert, clientCertificateRequired: false, SslProtocols.Tls13, checkCertificateRevocation: true);
                    sslStream.ReadTimeout = 5000;
                    sslStream.WriteTimeout = 5000;
                    sslStream.Read(msgBuf);
                    string msg = Encoding.UTF8.GetString(msgBuf);
                    Console.WriteLine(msg);
                    if (Regex.IsMatch(msg, "/interactions"))
                    {
                        Regex bodyLen = new Regex(@"content-length: (\d*)");
                        Regex bodyRegx = new Regex(@"\r\n\r\n(.{" + Int32.Parse(bodyLen.Match(msg).Groups[1].Value) + "})");
                        Regex credsRegex = new Regex(@"x-signature-timestamp: (\d*)\r\nx-signature-ed25519: (.{128})");
                        string body = bodyRegx.Match(msg).Groups[1].Value;
                        var creds = credsRegex.Match(msg);
                        byte[] signature = HexStrToByteArr(creds.Groups[2].Value);
                        byte[] message = new byte[creds.Groups[1].Length + body.Length];
                        for (int i = 0; i < creds.Groups[1].Length; i++)
                        {
                            message[i] = (byte)creds.Groups[1].Value[i];
                        }
                        int pointer = creds.Groups[1].Length;
                        foreach (char c in body)
                        {
                            message[pointer] = (byte)c;
                            pointer++;
                        }
                        if (!Chaos.NaCl.Ed25519.Verify(signature, message, HexStrToByteArr(pubKey)))
                        {
                            Console.WriteLine("not ok");
                            await sslStream.WriteAsync(Encoding.UTF8.GetBytes("HTTP/1.1 401 Unauthorized\r\nContent-Type: application/json\r\n\r\nBad request signature"));
                        }
                        else
                        {
                            Console.WriteLine("ok");
                            ApplicationCommandObject interactionMsg = JsonSerializer.Deserialize<ApplicationCommandObject>(msg);
                            if (interactionMsg.type == 1)
                            {
                                await sslStream.WriteAsync(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\ncontent-type: application/json\r\ncontent-length: 10\r\n\r\n" +
                                    JsonSerializer.Serialize<ApplicationCommandObject>(new ApplicationCommandObject { type = 1 })));
                            }
                        }
                    }
                    else if (Regex.IsMatch(msg, " /")) { }                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    Console.WriteLine("Authentication failed - closing the connection.");
                    sslStream.Close();
                    client.Close();
                    return;
                }
            }
        }
        static async Task GetCommands(string token, string appID)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            var response = await client.GetAsync($"https://discord.com/api/v10/applications/{appID}/commands");
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("все ок");
                var responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseText);
            }
            else
            {
                Console.WriteLine(response.StatusCode.ToString());
                var responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseText);
            }
        }
        static byte[] HexStrToByteArr(string hexStr)
        {
            byte[] b = new byte[hexStr.Length >> 1];
            for (int i = 0; i < hexStr.Length - 1; i += 2)
            {
                int val1 = (int)hexStr[i];
                val1 -= (val1 < 58 ? 48 : (val1 < 97 ? 55 : 87));
                int val2 = (int)hexStr[i + 1];
                val2 -= (val2 < 58 ? 48 : (val2 < 97 ? 55 : 87));
                b[i >> 1] = (byte)((val1 << 4) + val2);
            }
            return b;
        }
    }
}