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
using Microsoft.VisualBasic;
using System.Text.Json.Serialization;
using vmb;

internal class Program
{
    private static async Task Main(string[] args)
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
        X509Certificate cert = X509Certificate.CreateFromCertFile("cert.pfx");
        var server = new TcpListener(IPAddress.Any, 443);
        server.Start();
        Console.WriteLine("Strated"); ;
        string httpHeader = "HTTP/1.1 200 OK\r\ncontent-type: application/json\r\n";
        byte[] msgBuff = new byte[2048];
        while (true)
        {
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
            using var client = await server.AcceptTcpClientAsync();
            using SslStream sslStream = new SslStream(client.GetStream(), false);
            sslStream.AuthenticateAsServer(cert, clientCertificateRequired: false, SslProtocols.Tls13, checkCertificateRevocation: true);
            sslStream.ReadTimeout = 5000;
            sslStream.WriteTimeout = 5000;
            sslStream.Read(msgBuff);
            string msg = Encoding.UTF8.GetString(msgBuff);
            Console.WriteLine(msg);
            if (Regex.IsMatch(msg, "/interactions"))
            {
                int bodyLength = int.Parse(Regex.Match(msg, @"content-length: (\d*)").Groups[1].Value);
                Regex credsRegex = new Regex(@"x-signature-timestamp: (\d*)\r\nx-signature-ed25519: (.{128})");
                byte[] body = new byte[bodyLength];
                for (int i = Array.FindIndex(msgBuff, (b) => { return b == 123; }), j = 0; j < body.Length; i++, j++)
                {
                    if (msgBuff[i] == 0) break;
                    body[j] = msgBuff[i];
                }
                var creds = credsRegex.Match(msg);
                byte[] signature = HexStrToByteArr(creds.Groups[2].Value);
                byte[] message = new byte[creds.Groups[1].Length + body.Length];
                Array.Copy(Encoding.UTF8.GetBytes(creds.Groups[1].Value), message, creds.Groups[1].Length);
                Array.Copy(body, 0, message, creds.Groups[1].Length, body.Length);
                if (!Chaos.NaCl.Ed25519.Verify(signature, message, HexStrToByteArr(pubKey)))
                {
                    Console.WriteLine("not ok");
                    await sslStream.WriteAsync(Encoding.UTF8.GetBytes("HTTP/1.1 401 Unauthorized\r\nContent-Type: application/json\r\n\r\nBad request signature"));
                }
                else
                {
                    Console.WriteLine("ok");
                    InteractionObject interactionMsg = JsonSerializer.Deserialize<InteractionObject>(Encoding.UTF8.GetString(body));
                    if (interactionMsg.type == 1)
                    {
                        string response = JsonSerializer.Serialize(
                                new InteractionObject { type = 1 }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });
                        await sslStream.WriteAsync(Encoding.UTF8.GetBytes(httpHeader + $"content-length: {response.Length}\r\n\r\n" + response));
                    }
                    if (interactionMsg.type == 2)
                    {
                        if (interactionMsg.data.name == "test")
                        {
                            Console.WriteLine("initializing test command");
                            string response = JsonSerializer.Serialize(new InteractionResponse { type = 4, data = new InteractionCallbackData { content = "it works" } },
                                new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });
                            await sslStream.WriteAsync(Encoding.UTF8.GetBytes(httpHeader + $"content-length: {response.Length}" + "\r\n\r\n" + response));
                        }
                    }
                }
            }
        }
        async Task GetCommands(string token, string appID)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bot " + token);
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

        async Task DeleteCommand(string token, string appID, string commandID)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bot " + token);
            var response = await client.DeleteAsync($"https://discord.com/api/v10/applications/{appID}/commands/{commandID}");
            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.Content.ReadAsStringAsync());
        }
        byte[] HexStrToByteArr(string hexStr)
        {
            byte[] b = new byte[hexStr.Length >> 1];
            for (int i = 0; i < hexStr.Length - 1; i += 2)
            {
                int val1 = hexStr[i];
                val1 -= val1 < 58 ? 48 : val1 < 97 ? 55 : 87;
                int val2 = hexStr[i + 1];
                val2 -= val2 < 58 ? 48 : val2 < 97 ? 55 : 87;
                b[i >> 1] = (byte)((val1 << 4) + val2);
            }
            return b;
        }
    }
}