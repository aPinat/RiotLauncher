using System;
using System.Diagnostics;
using System.Linq;
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

        internal int ConfigPort { get; }

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

                try
                {
                    var configObject = JObject.Parse(content);

                    // configObject["keystone.client.feature_flags.autoLaunch.disabled"]?.Replace(true);
                    // configObject["keystone.client.feature_flags.autoPatch.disabled"]?.Replace(true);
                    configObject["keystone.client.feature_flags.login.disabled"]?.Replace(false);
                    configObject["keystone.client.feature_flags.privacyPolicy.enabled"]?.Replace(false);
                    configObject["keystone.client.feature_flags.regionlessLoginInfoTooltip.enabled"]?.Replace(false);
                    // configObject["keystone.client.feature_flags.system_tray.enabled"]?.Replace(true);
                    configObject["keystone.client_config.diagnostics_enabled"]?.Replace(false);
                    configObject["keystone.client_config.total_configs_to_save"]?.Replace(5);

                    configObject["lol.client_settings.chat.use_proxy_to_riot_client"]?.Replace(true);
                    configObject["lol.client_settings.rms.use_proxy_to_riot_client"]?.Replace(true);
                    configObject["lol.client_settings.voice.use_proxy_to_riot_client"]?.Replace(true);
                    configObject["lol.game_client_settings.leagues.enabled"]?.Replace(true);
                    configObject["lol.game_client_settings.missions.enabled"]?.Replace(true);

                    configObject["chat.aggressive_scan.enabled"]?.Replace(true);
                    configObject["chat.auto_query_msg_history.enabled"]?.Replace(true);
                    configObject["chat.force_filter.enabled"]?.Replace(true);
                    configObject["chat.game_name_tag_line.enabled"]?.Replace(true);
                    configObject["chat.replace_rich_messages"]?.Replace(true);
                    //configObject["chat.require_jwt_presence.league_of_legends.enabled"]?.Replace(true);
                    configObject["chat.require_keystone_presence.enabled"]?.Replace(true);
                    // configObject["chat.require_multi_game_presence_rxep.enabled"]?.Replace(true);

                    configObject["keystone.client.feature_flags.playerReporting.enabled"]?.Replace(true);
                    configObject["keystone.client.feature_flags.restriction.enabled"]?.Replace(true);
                    configObject["keystone.self_update.patchline_override"]?.Replace("KeystoneFoundationBetaWin");
                    configObject["keystone.telemetry.newrelic_events_v2_enabled"]?.Replace(false);
                    configObject["keystone.telemetry.newrelic_metrics_v1_enabled"]?.Replace(false);
                    configObject["keystone.telemetry.metrics_enabled"]?.Replace(false);

                    configObject["lol.client_settings.chat.allow_group_by_game"]?.Replace(true);
                    configObject["lol.client_settings.chat.query_sumid_batched_ledge"]?.Replace(true);
                    configObject["lol.client_settings.chat.query_sumid_blocked"]?.Replace(true);
                    configObject["lol.client_settings.chat.query_sumid_friend_requests"]?.Replace(true);
                    configObject["lol.client_settings.chat.query_sumid_muc_participants"]?.Replace(true);
                    configObject["lol.client_settings.chat.query_sumid_offline_friends"]?.Replace(true);
                    configObject["lol.client_settings.chat.query_sumid_online_friends"]?.Replace(true);
                    configObject["lol.client_settings.chat.scoped_conversations.enabled"]?.Replace(true);
                    configObject["lol.client_settings.chat.update_session_platform_id"]?.Replace(true);
                    configObject["lol.game_client_settings.logging.enable_http_public_logs"]?.Replace(false);
                    configObject["lol.game_client_settings.logging.enable_rms_public_logs"]?.Replace(false);

                    if (configObject["keystone.products.league_of_legends.patchlines.live"]?["platforms"]?["win"]?["configurations"] != null)
                        foreach (var jToken in configObject["keystone.products.league_of_legends.patchlines.live"]?["platforms"]?["win"]?["configurations"]!)
                        {
                            //jToken["patch_url"] = configObject["keystone.products.league_of_legends.patchlines.pbe"]?["platforms"]?["win"]?["configurations"]?[0]?["patch_url"];
                            //((JArray) jToken["launcher"]?["arguments"])?.Add("--system-yaml-override=\"Config/system.yaml\"");

                            if (jToken["locale_data"]?["available_locales"] is JArray availableLocales && availableLocales.Values<string>().All(s => s != "en_GB")) availableLocales.Add("en_GB");
                            jToken["locale_data"]?["default_locale"]?.Replace("en_GB");
                        }

                    modifiedContent = JsonConvert.SerializeObject(configObject);
                    Console.WriteLine(modifiedContent);
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
                await ctx.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                ctx.Response.OutputStream.Close();
            }
        }
    }
}