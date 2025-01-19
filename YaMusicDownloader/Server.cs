using System.Diagnostics;
using System.Net;
using System.Text;

namespace YaMusicDownloader
{
    class Server
    {
        private static HttpListener listener;
        private static string url = "http://localhost:9999/";
        public static string token = "";
        public delegate void TokenReceivedHandler(string token);
        public static event TokenReceivedHandler OnTokenReceived;

        public static async Task HandleIncomingConnections()
        {
            while (true)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                if (req.Url.AbsolutePath == "/token")
                {
                    string html = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Extracting token</title>
                        <script>
                            window.onload = function () {
                                // Extract the fragment from the URL
                                const fragment = window.location.hash.substring(1);
                                const params = new URLSearchParams(fragment);
                                const accessToken = params.get('access_token');

                                if (accessToken) {
                                    // Send the access token to the server
                                    fetch('http://localhost:9999/store-token', {
                                        method: 'POST',
                                        headers: { 'Content-Type': 'application/json' },
                                        body: JSON.stringify({ access_token: accessToken })
                                    })
                                    .then(() => {
                                        document.body.innerHTML = 'Access token received successfully! You can close this window now.';
                                    })
                                    .catch(() => {
                                        document.body.innerHTML = 'Failed to send access token to the server.';
                                    });
                                } else {
                                    document.body.innerHTML = 'No access token found in the URL.';
                                }
                            };
                        </script>
                    </head>
                    <body>
                        <h1>Processing Access Token... (if you see this for too long, you may be using a mobile device. If that's the case, then please use Windows version)</h1>
                    </body>
                    </html>";
                    byte[] data = Encoding.UTF8.GetBytes(html);
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
                else if (req.Url.AbsolutePath == "/store-token" && req.HttpMethod == "POST")
                {
                    using (StreamReader reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        string body = await reader.ReadToEndAsync();
                        var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(body);

                        if (tokenData != null && tokenData.TryGetValue("access_token", out string accessToken))
                        {
                            Debug.WriteLine($"Access Token: {accessToken}");
                            token = accessToken;

                            File.WriteAllText(".env", $"TOKEN={accessToken}");
                            OnTokenReceived?.Invoke(accessToken);
                        }
                    }

                    string responseMessage = "Access token received successfully! You can close this page";
                    byte[] data = Encoding.UTF8.GetBytes(responseMessage);
                    resp.ContentType = "text/plain";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
                else
                {
                    resp.StatusCode = 404;
                    byte[] data = Encoding.UTF8.GetBytes("Not Found");
                    resp.ContentType = "text/plain";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
            }
        }

        public async Task Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            while (listener.IsListening)
            {
                await HandleIncomingConnections();
            }

            listener.Close();
        }
    }
}
