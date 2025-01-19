using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Recognition
{
    private const string URI = "wss://voiceservices.yandex.net/uni.ws";
    private static readonly string[] LANGS = { "", "ru-RU", "en-US" };
    private const string CHUNKS_FILE_PATH = "chunks_output.bin"; // Path to save binary chunks

    private ClientWebSocket _webSocket;
    private readonly byte[] _binaryData;
    private readonly IStatusMessage _statusMsg;

    public Recognition(byte[] binaryData, IStatusMessage statusMsg)
    {
        _binaryData = binaryData;
        _statusMsg = statusMsg;
        _webSocket = new ClientWebSocket();
    }

    public async Task<string> GetTrackIdAsync()
    {
        Debug.WriteLine("GetTrackIdAsync started");

        foreach (var lang in LANGS)
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseSent || _webSocket.State == WebSocketState.CloseReceived)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reset for next attempt", CancellationToken.None);
                }

                _webSocket = new ClientWebSocket();  // Reinitialize the WebSocket
                var asrData = GetAsrData();
                var eventData = (Dictionary<string, object>)asrData["event"];
                var payload = (Dictionary<string, object>)eventData["payload"];
                payload["lang"] = lang;

                if (!string.IsNullOrEmpty(lang))
                {
                    _statusMsg.EditText($"Trying to recognize music in {lang}...");
                }
                else
                {
                    _statusMsg.EditText("Trying to recognize music without language binding...");
                }

                await _webSocket.ConnectAsync(new Uri(URI), CancellationToken.None);

                if (_webSocket.State == WebSocketState.Open)
                {
                    await SendJsonAsync(GetAuthData());
                    await SendJsonAsync(asrData);

                    var binaryArray = GetChunksAndReplaceEncoder(_binaryData);

                    foreach (var chunk in binaryArray)
                    {
                        _statusMsg.EditText("Sending chunk...");
                        await _webSocket.SendAsync(new ArraySegment<byte>(chunk), WebSocketMessageType.Binary, true, CancellationToken.None);

                        WriteChunkToFile(chunk);
                    }

                    await ReceiveResponseAsync();

                    var secondResponse = await ReceiveResponseAsync();

                    var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(secondResponse);

                    string match = jsonResponse?["directive"]?["payload"]?["data"]?["match"].ToString();

                    if (match != null)
                    {
                        Debug.WriteLine("NOT NILLLLLLLLL");
                        return ParseTrackId(match);
                    }
                }
                else
                {
                    Debug.WriteLine($"WebSocket state is not open: {_webSocket.State}");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        Debug.WriteLine("No track ID found");
        return null;
    }

    private Dictionary<string, object> GetAuthData()
    {
        return new Dictionary<string, object>
        {
            { "event", new Dictionary<string, object>
                {
                    { "header", new Dictionary<string, string>
                        {
                            { "messageId", Guid.NewGuid().ToString() },
                            { "name", "SynchronizeState" },
                            { "namespace", "System" }
                        }
                    },
                    { "payload", new Dictionary<string, object>
                        {
                            { "accept_invalid_auth", true },
                            { "auth_token", "5983ba91-339e-443c-8452-390fe7d9d308" },
                            { "uuid", Guid.NewGuid().ToString("N") }
                        }
                    }
                }
            }
        };
    }

    private Dictionary<string, object> GetAsrData()
    {
        return new Dictionary<string, object>
        {
            { "event", new Dictionary<string, object>
                {
                    { "header", new Dictionary<string, object>
                        {
                            { "messageId", Guid.NewGuid().ToString() },
                            { "name", "Recognize" },
                            { "namespace", "ASR" },
                            { "streamId", 1 }
                        }
                    },
                    { "payload", new Dictionary<string, object>
                        {
                            { "advancedASROptions", new Dictionary<string, bool>
                                {
                                    { "manual_punctuation", false },
                                    { "partial_results", false }
                                }
                            },
                            { "disableAntimatNormalizer", false },
                            { "format", "audio/opus" },
                            { "music_request2", new Dictionary<string, object>
                                {
                                    { "headers", new Dictionary<string, string>
                                        {
                                            { "Content-Type", "audio/opus" }
                                        }
                                    }
                                }
                            },
                            { "punctuation", false },
                            { "tags", "PASS_AUDIO;" },
                            { "topic", "queries" }
                        }
                    }
                }
            }
        };
    }

    private async Task SendJsonAsync(Dictionary<string, object> data)
    {
        var jsonData = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(jsonData);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        Debug.WriteLine("Sent JSON data: " + jsonData);
    }

    private async Task<string> ReceiveResponseAsync()
    {
        Debug.WriteLine("Receiving response...");
        var buffer = new byte[4096];
        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Text)
        {
            var jsonResponse = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Debug.WriteLine("Received response: " + jsonResponse);
            return JsonSerializer.Serialize(jsonResponse);
        }

        return null;
    }

    private string ParseTrackId(string response)
    {
        try
        {
            // Deserialize JSON response
            var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(response);

            // Safely extract "match" value
            var match = jsonResponse?["directive"]?["payload"]?["data"]?["match"];

            // If match is not null and has the "realId" property, return it
            if (match != null && match.Type == Newtonsoft.Json.Linq.JTokenType.Object)
            {
                var trackId = jsonResponse?["directive"]?["payload"]?["data"]?["match"]?["realId"]?.ToString();
                Debug.WriteLine("RETURNING TRACK ID: " + trackId);
                return trackId ?? "";
            }
            else
            {
                Debug.WriteLine("Match data is missing or invalid");
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error parsing track ID: " + ex.ToString());
            return string.Empty;
        }
    }


    public List<byte[]> GetChunksAndReplaceEncoder(byte[] binaryData)
    {
        List<byte[]> chunks = new List<byte[]>();

        // Split the binary data on "OggS" bytes, equivalent to Python's split(b'OggS')
        byte[] separator = new byte[] { 0x4F, 0x67, 0x67, 0x53 }; // "OggS" in bytes
        List<byte[]> splitChunks = SplitBySeparator(binaryData, separator);
        Debug.WriteLine($"Number of chunks after split: {splitChunks.Count}");

        // Use a standard for loop to allow modification of `chunk`
        for (int i = 1; i < splitChunks.Count; i++) // Skip the first element, as per `[1:]` in Python
        {
            byte[] chunk = splitChunks[i];

            if (ContainsByteSequence(chunk, new byte[] { 0x4F, 0x70, 0x75, 0x73, 0x54, 0x61, 0x67, 0x73 })) // "OpusTags" in bytes
            {
                int pos = IndexOfByteSequence(chunk, new byte[] { 0x4F, 0x70, 0x75, 0x73, 0x54, 0x61, 0x67, 0x73 }) + 12;
                int size = chunk.Length;

                // Replace with encoder string
                byte[] encoder = Encoding.UTF8.GetBytes("#\x00\x00\x00\x00" + "ENCODER=SpeechKit Mobile SDK v3.28.0");
                chunk = chunk.Take(pos).Concat(encoder).ToArray();
                chunk = chunk.Concat(new byte[size - chunk.Length]).ToArray(); // Pad with zeros
            }

            // Prepend the "OggS" marker
            byte[] oggS = new byte[] { 0x00, 0x00, 0x00, 0x01, 0x4F, 0x67, 0x67, 0x53 }; // "\x00\x00\x00\x01OggS"
            chunk = oggS.Concat(chunk).ToArray();

            chunks.Add(chunk);
        }

        Console.WriteLine($"Generated {chunks.Count} chunks.");
        return chunks;
    }

    private List<byte[]> SplitBySeparator(byte[] data, byte[] separator)
    {
        List<byte[]> result = new List<byte[]>();
        int startIndex = 0;

        for (int i = 0; i < data.Length - separator.Length; i++)
        {
            if (data.Skip(i).Take(separator.Length).SequenceEqual(separator))
            {
                result.Add(data.Skip(startIndex).Take(i - startIndex).ToArray());
                startIndex = i + separator.Length;
                i += separator.Length - 1; // Skip over the separator
            }
        }

        if (startIndex < data.Length) // Add remaining data
            result.Add(data.Skip(startIndex).ToArray());

        return result;
    }

    private bool ContainsByteSequence(byte[] data, byte[] sequence)
    {
        // Use a sliding window approach to check if the sequence exists in the data
        for (int i = 0; i <= data.Length - sequence.Length; i++)
        {
            if (data.Skip(i).Take(sequence.Length).SequenceEqual(sequence))
            {
                return true;
            }
        }
        return false; // Sequence not found
    }

    private int IndexOfByteSequence(byte[] data, byte[] sequence)
    {
        for (int i = 0; i <= data.Length - sequence.Length; i++)
        {
            if (data.Skip(i).Take(sequence.Length).SequenceEqual(sequence))
            {
                return i;
            }
        }
        return -1; // Not found
    }

    private List<string> SplitDataIntoSegments(byte[] binaryData)
    {
        var segments = new List<string>();
        string binaryString = Encoding.UTF8.GetString(binaryData);
        string[] splitSegments = binaryString.Split(new[] { "OggS" }, StringSplitOptions.None);

        for (int i = 1; i < splitSegments.Length; i++)
        {
            segments.Add(splitSegments[i]);
        }

        return segments;
    }

    // New method to write the binary chunk to the file
    private void WriteChunkToFile(byte[] chunk)
    {
        try
        {
            using (FileStream fs = new FileStream(CHUNKS_FILE_PATH, FileMode.Append, FileAccess.Write))
            {
                fs.Write(chunk, 0, chunk.Length); // Write the chunk as binary data
                Debug.WriteLine("Written chunk to binary file.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing chunk to file: {ex}");
        }
    }
}

public interface IStatusMessage
{
    void EditText(string message);
}
