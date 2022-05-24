using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace RiotLauncher;

internal class ChatProxy : TcpProxy
{
    public ChatProxy(string hostname, int port) : base(hostname, port) => Hostname = hostname;

    internal static string Hostname { get; set; } = "rnet-stable.chat.si.riotgames.com";

    private protected override async Task AcceptClientLoopAsync()
    {
        while (true)
        {
            var incoming = await _listener.AcceptTcpClientAsync();
            var ipEndPoint = incoming.Client.RemoteEndPoint as IPEndPoint;
            Console.WriteLine($"Incoming connection from {ipEndPoint}");

            var outgoing = new TcpClient(Hostname, _port);
            var outgoingSslStream = new SslStream(outgoing.GetStream());
            await outgoingSslStream.AuthenticateAsClientAsync(Hostname);
            new ChatProxyThread(incoming, outgoing, outgoingSslStream).StartThreads();
            Console.WriteLine($"Finished setting up proxy for {ipEndPoint} to {Hostname}:{_port}");
        }
        // ReSharper disable once FunctionNeverReturns
    }

    /// <summary>
    ///     Handles a client connection from ChatProxy.
    ///     Most of this class is modified code from https://github.com/aPinat/Deceive/blob/ci/Deceive/MainController.cs / https://github.com/molenzwiebel/Deceive/blob/master/Deceive/MainController.cs
    /// </summary>
    private class ChatProxyThread : TcpProxyThread
    {
        private bool _connectToMuc = true;
        private bool _enabled = true;
        private bool _insertedFakePlayer;
        private string? _lastPresence;
        private bool _sentFakePlayerPresence;
        private string _status = "offline";
        private string? _valorantVersion;

        public ChatProxyThread(TcpClient incoming, TcpClient outgoing, SslStream outgoingSslStream) : base(incoming, outgoing, outgoingSslStream)
        {
        }

        protected override async Task IncomingAsync()
        {
            try
            {
                int byteCount;
                var bytes = new byte[8192];

                do
                {
                    byteCount = await _incoming.GetStream().ReadAsync(bytes);

                    var content = Encoding.UTF8.GetString(bytes, 0, byteCount);

                    // If this is possibly a presence stanza, rewrite it.
                    if (content.Contains("<presence") && _enabled)
                    {
                        Console.WriteLine("<!--RC TO SERVER ORIGINAL-->" + content);
                        await PossiblyRewriteAndResendPresenceAsync(content, _status);
                    }
                    else if (content.Contains("41c322a1-b328-495b-a004-5ccd3e45eae8@eu1.pvp.net"))
                    {
                        if (content.Contains("offline", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (!_enabled)
                                await SendMessageFromFakePlayerAsync("Now enabled.");
                            _enabled = true;
                            await UpdateStatusAsync(_status = "offline");
                        }
                        else if (content.Contains("mobile", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (!_enabled)
                                await SendMessageFromFakePlayerAsync("Now enabled.");
                            _enabled = true;
                            await UpdateStatusAsync(_status = "mobile");
                        }
                        else if (content.Contains("online", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (!_enabled)
                                await SendMessageFromFakePlayerAsync("Now enabled.");
                            _enabled = true;
                            await UpdateStatusAsync(_status = "chat");
                        }
                        else if (content.Contains("enable", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (_enabled)
                            {
                                await SendMessageFromFakePlayerAsync("Already enabled.");
                            }
                            else
                            {
                                _enabled = true;
                                await UpdateStatusAsync(_status);
                                await SendMessageFromFakePlayerAsync("Now enabled.");
                            }
                        }
                        else if (content.Contains("disable", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (!_enabled)
                            {
                                await SendMessageFromFakePlayerAsync("Already disabled.");
                            }
                            else
                            {
                                _enabled = false;
                                await UpdateStatusAsync("chat");
                                await SendMessageFromFakePlayerAsync("Now disabled.");
                            }
                        }
                        else if (content.Contains("status", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (_status == "chat")
                                await SendMessageFromFakePlayerAsync("You are appearing online.");
                            else
                                await SendMessageFromFakePlayerAsync("You are appearing " + _status + ".");
                        }
                        else if (content.Contains("muc", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _connectToMuc = !_connectToMuc;
                            if (_connectToMuc)
                                await SendMessageFromFakePlayerAsync("Enabled connecting to group chats.");
                            else
                                await SendMessageFromFakePlayerAsync("Disabled connecting to group chats.");
                        }

                        // Don't send anything involving our fake user to chat servers
                        Console.WriteLine("<!--RC TO SERVER REMOVED-->" + content);
                    }
                    else
                    {
                        await _outgoingSslStream.WriteAsync(bytes.AsMemory(0, byteCount));
                        Console.WriteLine("<!--RC TO SERVER-->" + content);
                    }

                    if (_insertedFakePlayer && !_sentFakePlayerPresence)
                        await SendFakePlayerPresenceAsync();
                } while (byteCount != 0 && _connected);
            }
            catch (Exception e)
            {
                Console.WriteLine("Incoming errored.");
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("Incoming closed.");
                if (_connected)
                    Dispose();
            }
        }

        protected override async Task OutgoingAsync()
        {
            try
            {
                int byteCount;
                var bytes = new byte[8192];

                do
                {
                    byteCount = await _outgoingSslStream.ReadAsync(bytes);
                    var content = Encoding.UTF8.GetString(bytes, 0, byteCount);

                    // Insert fake player into roster
                    const string roster = "<query xmlns='jabber:iq:riotgames:roster'>";
                    if (!_insertedFakePlayer && content.Contains(roster))
                    {
                        _insertedFakePlayer = true;
                        Console.WriteLine("<!--SERVER TO RC ORIGINAL-->" + content);
                        content = content.Insert(content.IndexOf(roster, StringComparison.Ordinal) + roster.Length,
                            "<item jid='41c322a1-b328-495b-a004-5ccd3e45eae8@eu1.pvp.net' name='PinatBot' subscription='both' puuid='41c322a1-b328-495b-a004-5ccd3e45eae8'>" +
                            "<group priority='99'>Pinat</group>" +
                            "<id name='PinatBot' tagline='Pinat'/><lol name='PinatBot'/>" +
                            "</item>");
                        await _incoming.GetStream().WriteAsync(Encoding.UTF8.GetBytes(content));
                        Console.WriteLine("<!--ChatProxy TO RC-->" + content);
                    }
                    else
                    {
                        await _incoming.GetStream().WriteAsync(bytes.AsMemory(0, byteCount));
                        Console.WriteLine("<!--SERVER TO RC-->" + Encoding.UTF8.GetString(bytes, 0, byteCount));
                    }
                } while (byteCount != 0 && _connected);
            }
            catch (Exception e)
            {
                Console.WriteLine("Outgoing errored.");
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("Outgoing closed.");
                if (_connected)
                    Dispose();
            }
        }

        private async Task PossiblyRewriteAndResendPresenceAsync(string content, string targetStatus)
        {
            try
            {
                _lastPresence = content;
                var wrappedContent = "<xml>" + content + "</xml>";
                var xml = XDocument.Load(new StringReader(wrappedContent));

                if (xml.Root is null)
                    return;
                if (xml.Root.HasElements is false)
                    return;

                foreach (var presence in xml.Root.Elements())
                {
                    if (presence.Name != "presence")
                        continue;
                    if (presence.Attribute("to") is not null)
                    {
                        if (_connectToMuc)
                            continue;
                        presence.Remove();
                    }

                    if (targetStatus != "chat" || presence.Element("games")?.Element("league_of_legends")?.Element("st")?.Value != "dnd")
                    {
                        presence.Element("show")?.ReplaceNodes(targetStatus);
                        presence.Element("games")?.Element("league_of_legends")?.Element("st")?.ReplaceNodes(targetStatus);
                    }

                    if (targetStatus == "chat")
                        continue;
                    presence.Element("status")?.Remove();

                    if (targetStatus == "mobile")
                    {
                        presence.Element("games")?.Element("league_of_legends")?.Element("p")?.Remove();
                        presence.Element("games")?.Element("league_of_legends")?.Element("m")?.Remove();
                    }
                    else
                    {
                        presence.Element("games")?.Element("league_of_legends")?.Remove();
                    }

                    // Remove Legends of Runeterra presence
                    presence.Element("games")?.Element("bacon")?.Remove();

                    if (_valorantVersion is null)
                    {
                        var valorantBase64 = presence.Element("games")?.Element("valorant")?.Element("p")?.Value;
                        if (valorantBase64 is not null)
                        {
                            var valorantPresence = Encoding.UTF8.GetString(Convert.FromBase64String(valorantBase64));
                            var valorantJson = JsonSerializer.Deserialize<JsonNode>(valorantPresence);
                            _valorantVersion = valorantJson?["partyClientVersion"]?.GetValue<string>();
                            Console.WriteLine("Found VALORANT version: " + _valorantVersion);
                            // only resend
                            if (_insertedFakePlayer && _valorantVersion is not null)
                                await SendFakePlayerPresenceAsync();
                        }
                    }

                    // Remove VALORANT presence
                    presence.Element("games")?.Element("valorant")?.Remove();
                }

                var sb = new StringBuilder();
                var xws = new XmlWriterSettings { OmitXmlDeclaration = true, Encoding = Encoding.UTF8, ConformanceLevel = ConformanceLevel.Fragment, Async = true };
                await using (var xw = XmlWriter.Create(sb, xws))
                {
                    foreach (var xElement in xml.Root.Elements())
                        xElement.WriteTo(xw);
                }

                await _outgoingSslStream.WriteAsync(Encoding.UTF8.GetBytes(sb.ToString()));
                Console.WriteLine("<!--ChatProxy TO SERVER-->" + sb);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Error rewriting presence.");
            }
        }

        private async Task UpdateStatusAsync(string newStatus)
        {
            if (string.IsNullOrEmpty(_lastPresence))
                return;

            await PossiblyRewriteAndResendPresenceAsync(_lastPresence, newStatus);

            if (newStatus == "chat")
                await SendMessageFromFakePlayerAsync("You are now appearing online.");
            else
                await SendMessageFromFakePlayerAsync("You are now appearing " + newStatus + ".");
        }

        private async Task SendFakePlayerPresenceAsync()
        {
            _sentFakePlayerPresence = true;
            // VALORANT requires a recent version to not display "Version Mismatch"
            var valorantPresence = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{{\"isValid\":true,\"partyId\":\"00000000-0000-0000-0000-000000000000\",\"partyClientVersion\":\"{_valorantVersion ?? "unknown"}\"}}")
            );

            var randomStanzaId = Guid.NewGuid();
            var unixTimeMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var presenceMessage =
                $"<presence from='41c322a1-b328-495b-a004-5ccd3e45eae8@eu1.pvp.net/RC-PinatBot' id='b-{randomStanzaId}'>" +
                "<games>" +
                $"<keystone><st>chat</st><s.t>{unixTimeMilliseconds}</s.t><s.p>keystone</s.p></keystone>" +
                $"<league_of_legends><st>chat</st><s.t>{unixTimeMilliseconds}</s.t><s.p>league_of_legends</s.p><p>{{&quot;pty&quot;:true}}</p></league_of_legends>" + // No Region s.r keeps it in the main "League" category rather than "Other Servers" in every region with "Group Games & Servers" active
                $"<valorant><st>chat</st><s.t>{unixTimeMilliseconds}</s.t><s.p>valorant</s.p><p>{valorantPresence}</p></valorant>" +
                $"<bacon><st>chat</st><s.t>{unixTimeMilliseconds}</s.t><s.l>bacon_availability_online</s.l><s.p>bacon</s.p></bacon>" +
                "</games>" +
                "<show>chat</show>" +
                "</presence>";

            var bytes = Encoding.UTF8.GetBytes(presenceMessage);
            await _incoming.GetStream().WriteAsync(bytes);
            Console.WriteLine("<!--ChatProxy TO RC-->" + presenceMessage);
        }

        private async Task SendMessageFromFakePlayerAsync(string message)
        {
            var stamp = DateTime.UtcNow.AddSeconds(1).ToString("yyyy-MM-dd HH:mm:ss.fff");

            var chatMessage =
                $"<message from='41c322a1-b328-495b-a004-5ccd3e45eae8@eu1.pvp.net/RC-PinatBot' stamp='{stamp}' id='fake-{stamp}' type='chat'><body>{message}</body></message>";

            var bytes = Encoding.UTF8.GetBytes(chatMessage);
            await _incoming.GetStream().WriteAsync(bytes);
            Console.WriteLine("<!--ChatProxy TO RC-->" + chatMessage);
        }
    }
}
