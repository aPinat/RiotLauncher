using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RiotLauncher
{
    internal class ConfigProxy
    {
        private readonly HttpClient _client = new HttpClient();
        internal int ConfigPort { get; }

        /**
         * Starts a new client configuration proxy at a random port. The proxy will modify any responses
         * to point the chat servers to our local setup. This function returns the random port that the HTTP
         * server is listening on.
         */
        internal ConfigProxy(string configUrl)
        {
            // Find a free port.
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint) l.LocalEndpoint).Port;
            l.Stop();

            // Start a web server that sends everything to ProxyAndRewriteResponse
            var server = new WebServer(o => o
                    .WithUrlPrefix("http://localhost:" + port)
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithModule(new ActionModule("/", HttpVerbs.Get,
                    ctx => ProxyAndRewriteResponse(configUrl, ctx)));

            // Run this on a new thread, just for the sake of it.
            // It seemed to be buggy if run on the same thread.
            var thread = new Thread(() => { server.RunAsync().Wait(); }) {IsBackground = true};
            thread.Start();

            ConfigPort = port;
        }

        /**
         * Proxies any request made to this web server to the clientconfig service. Rewrites the response
         * to have any chat servers point to localhost at the specified port.
         */
        private async Task ProxyAndRewriteResponse(string configUrl, IHttpContext ctx)
        {
            var url = configUrl + ctx.Request.RawUrl;
            Console.WriteLine(url);

            using (var message = new HttpRequestMessage(HttpMethod.Get, url))
            {
                // Cloudflare bitches at us without a user agent.
                message.Headers.TryAddWithoutValidation("User-Agent", ctx.Request.Headers["user-agent"]);

                // Add authorization headers for player config.
                if (ctx.Request.Headers["x-riot-entitlements-jwt"] != null)
                    message.Headers.TryAddWithoutValidation("X-Riot-Entitlements-JWT",
                        ctx.Request.Headers["x-riot-entitlements-jwt"]);

                if (ctx.Request.Headers["authorization"] != null) message.Headers.TryAddWithoutValidation("Authorization", ctx.Request.Headers["authorization"]);

                var result = await _client.SendAsync(message);
                var content = await result.Content.ReadAsStringAsync();
                var modifiedContent = content;
                Console.WriteLine(modifiedContent);

                try
                {
                    var configObject = JObject.Parse(content);

                    //if (configObject.ContainsKey("keystone.client.feature_flags.flaggedNameModal.disabled")) configObject["keystone.client.feature_flags.flaggedNameModal.disabled"] = false;
                    //if (configObject.ContainsKey("keystone.client.feature_flags.lifecycle.leagueRegionElection.enabled")) configObject["keystone.client.feature_flags.lifecycle.leagueRegionElection.enabled"] = true;
                    if (configObject.ContainsKey("keystone.client.feature_flags.login.disabled")) configObject["keystone.client.feature_flags.login.disabled"] = false;
                    //if (configObject.ContainsKey("keystone.client.feature_flags.regionlessLogin.enabled")) configObject["keystone.client.feature_flags.regionlessLogin.enabled"] = true;
                    if (configObject.ContainsKey("keystone.client.feature_flags.regionlessLoginInfoTooltip.enabled"))
                        configObject["keystone.client.feature_flags.regionlessLoginInfoTooltip.enabled"] = false;
                    //if (configObject.ContainsKey("keystone.client.feature_flags.system_tray.enabled")) configObject["keystone.client.feature_flags.system_tray.enabled"] = true;
                    if (configObject.ContainsKey("keystone.client_config.total_configs_to_save")) configObject["keystone.client_config.total_configs_to_save"] = "5";

                    if (configObject.ContainsKey("chat.aggressive_scan.enabled")) configObject["chat.aggressive_scan.enabled"] = true;
                    if (configObject.ContainsKey("chat.auto_query_msg_history.enabled")) configObject["chat.auto_query_msg_history.enabled"] = true;
                    if (configObject.ContainsKey("chat.game_name_tag_line.enabled")) configObject["chat.game_name_tag_line.enabled"] = true;
                    if (configObject.ContainsKey("chat.replace_rich_messages")) configObject["chat.replace_rich_messages"] = true;
                    //if (configObject.ContainsKey("chat.require_jwt_presence.league_of_legends.enabled")) configObject["chat.require_jwt_presence.league_of_legends.enabled"] = true;
                    if (configObject.ContainsKey("chat.require_keystone_presence.enabled")) configObject["chat.require_keystone_presence.enabled"] = true;
                    //if (configObject.ContainsKey("chat.require_multi_game_presence_rxep.enabled")) configObject["chat.require_multi_game_presence_rxep.enabled"] = true;

                    if (configObject.ContainsKey("lol.client_settings.chat.query_sumid_batched_ledge")) configObject["lol.client_settings.chat.query_sumid_batched_ledge"] = true;
                    if (configObject.ContainsKey("lol.client_settings.chat.query_sumid_blocked")) configObject["lol.client_settings.chat.query_sumid_blocked"] = true;
                    if (configObject.ContainsKey("lol.client_settings.chat.query_sumid_friend_requests")) configObject["lol.client_settings.chat.query_sumid_friend_requests"] = true;
                    if (configObject.ContainsKey("lol.client_settings.chat.query_sumid_muc_participants")) configObject["lol.client_settings.chat.query_sumid_muc_participants"] = true;
                    if (configObject.ContainsKey("lol.client_settings.chat.query_sumid_offline_friends")) configObject["lol.client_settings.chat.query_sumid_offline_friends"] = true;
                    if (configObject.ContainsKey("lol.client_settings.chat.query_sumid_online_friends")) configObject["lol.client_settings.chat.query_sumid_online_friends"] = true;

                    if (configObject.ContainsKey("lol.client_settings.chat.update_session_platform_id")) configObject["lol.client_settings.chat.update_session_platform_id"] = true;
                    if (configObject.ContainsKey("lol.client_settings.chat.scoped_conversations.enabled")) configObject["lol.client_settings.chat.scoped_conversations.enabled"] = true;

                    if (configObject["keystone.products.league_of_legends.patchlines.live"]?["platforms"]?["win"]?["configurations"] != null)
                        foreach (var jToken in configObject["keystone.products.league_of_legends.patchlines.live"]["platforms"]["win"]["configurations"].Children())
                        {
                            jToken["patch_url"] = configObject["keystone.products.league_of_legends.patchlines.pbe"]["platforms"]["win"]["configurations"][0]["patch_url"];
                            ((JArray) jToken["launcher"]["arguments"]).Add("--system-yaml-override=\"Config/system.yaml\"");

                            ((JArray) jToken["locale_data"]["available_locales"]).Add("en_GB");
                            jToken["locale_data"]["default_locale"] = "en_GB";
                        }

                    if (configObject.ContainsKey("lol.client_settings.anti_addiction.legacy_aas_enabled")) configObject["lol.client_settings.anti_addiction.legacy_aas_enabled"] = true;
                    if (configObject.ContainsKey("lol.client_settings.rms.use_proxy_to_riot_client")) configObject["lol.client_settings.rms.use_proxy_to_riot_client"] = true;
                    if (configObject.ContainsKey("lol.client_settings.voice.use_proxy_to_riot_client")) configObject["lol.client_settings.voice.use_proxy_to_riot_client"] = true;

                    //if (configObject.ContainsKey("lol.game_client_settings.app_config.enabled_regions")) ((JArray) configObject["lol.game_client_settings.app_config.enabled_regions"])?.Add("EUW");
                    //if (configObject.ContainsKey("lol.game_client_settings.app_config.enabled_regions")) ((JArray) configObject["lol.game_client_settings.app_config.enabled_regions"])?.Add("EUW1");

                    modifiedContent = JsonConvert.SerializeObject(configObject);
                    //Console.WriteLine(modifiedContent);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }

                // Using the builtin EmbedIO methods for sending the response adds some garbage in the front of it.
                // This seems to do the trick.
                var responseBytes = Encoding.UTF8.GetBytes(modifiedContent);

                ctx.Response.StatusCode = (int) result.StatusCode;
                ctx.Response.SendChunked = false;
                ctx.Response.ContentLength64 = responseBytes.Length;
                ctx.Response.ContentType = "application/json";
                ctx.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                ctx.Response.OutputStream.Close();
            }
        }
    }
}