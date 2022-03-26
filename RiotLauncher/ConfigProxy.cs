using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EmbedIO;
using EmbedIO.Actions;

namespace RiotLauncher;

public class ConfigProxy
{
    private HttpClient Client { get; } = new();
    private int Option { get; }
    public int ConfigPort { get; }

    /**
     * Starts a new client configuration proxy at a random port. The proxy will modify any responses
     * to point the chat servers to our local setup. This function returns the random port that the HTTP
     * server is listening on.
     */
    public ConfigProxy(string configUrl = "https://clientconfig.rpg.riotgames.com", int option = 0)
    {
        Option = option;
        // Find a free port.
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();

        // Start a web server that sends everything to ProxyAndRewriteResponse
        var server = new WebServer(o => o
                .WithUrlPrefix("http://localhost:" + port)
                .WithMode(HttpListenerMode.EmbedIO))
            .WithModule(new ActionModule("/", HttpVerbs.Get,
                ctx => ProxyAndRewriteResponse(configUrl, ctx)));

        // Run this on a new thread, just for the sake of it.
        // It seemed to be buggy if run on the same thread.
        var thread = new Thread(() => { server.RunAsync().Wait(); }) { IsBackground = true };
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
        // url = url.Replace("KeystoneFoundationLiveWin", "KeystoneFoundationBetaWin");
        url = url.Replace("KeystoneFoundationBetaWin", "KeystoneFoundationLiveWin");
        Console.WriteLine(url);

        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        // Cloudflare bitches at us without a user agent.
        message.Headers.TryAddWithoutValidation("User-Agent", ctx.Request.Headers["user-agent"]);

        // Add authorization headers for player config.
        if (ctx.Request.Headers["x-riot-entitlements-jwt"] != null)
            message.Headers.TryAddWithoutValidation("X-Riot-Entitlements-JWT",
                ctx.Request.Headers["x-riot-entitlements-jwt"]);

        if (ctx.Request.Headers["authorization"] != null)
            message.Headers.TryAddWithoutValidation("Authorization", ctx.Request.Headers["authorization"]);

        var result = await Client.SendAsync(message);
        var content = await result.Content.ReadAsStringAsync();
        var modifiedContent = content;

        if (!result.IsSuccessStatusCode) goto RESPOND;

        try
        {
            var configObject = JsonSerializer.Deserialize<JsonNode>(content);
            if (configObject == null) goto RESPOND;

            if (configObject["keystone.client.feature_flags.autoLaunch.disabled"] != null)
                configObject["keystone.client.feature_flags.autoLaunch.disabled"] = true;
            if (configObject["keystone.client.feature_flags.autoPatch.disabled"] != null)
                configObject["keystone.client.feature_flags.autoPatch.disabled"] = true;
            if (configObject["keystone.client.feature_flags.games_library.special_events.enabled"] != null)
                configObject["keystone.client.feature_flags.games_library.special_events.enabled"] = true;
            if (configObject["keystone.client.feature_flags.login.disabled"] != null)
                configObject["keystone.client.feature_flags.login.disabled"] = false;
            if (configObject["keystone.client.feature_flags.playerReportingMailboxIntegration.enabled"] != null)
                configObject["keystone.client.feature_flags.playerReportingMailboxIntegration.enabled"] = true;
            if (configObject["keystone.client.feature_flags.playerReportingPasIntegration.enabled"] != null)
                configObject["keystone.client.feature_flags.playerReportingPasIntegration.enabled"] = true;
            if (configObject["keystone.client.feature_flags.playerReportingReporterFeedback.enabled"] != null)
                configObject["keystone.client.feature_flags.playerReportingReporterFeedback.enabled"] = true;
            if (configObject["keystone.client.feature_flags.regionlessLoginInfoTooltip.enabled"] != null)
                configObject["keystone.client.feature_flags.regionlessLoginInfoTooltip.enabled"] = false;
            // if (configObject["keystone.client.feature_flags.product_settings.keep_updated.enabled"] != null)
            // configObject["keystone.client.feature_flags.product_settings.keep_updated.enabled"] = true;
            if (configObject["keystone.client.feature_flags.staySignedIn.disabled"] != null)
                configObject["keystone.client.feature_flags.staySignedIn.disabled"] = true;
            if (configObject["keystone.client.feature_flags.socialSignOn.enabled"] != null)
                configObject["keystone.client.feature_flags.socialSignOn.enabled"] = false;
            if (configObject["keystone.client.feature_flags.system_tray.enabled"] != null)
                configObject["keystone.client.feature_flags.system_tray.enabled"] = true;
            if (configObject["keystone.client.feature_flags.restart_required.disabled"] != null)
                configObject["keystone.client.feature_flags.restart_required.disabled"] = true;
            if (configObject["keystone.client_config.diagnostics_enabled"] != null)
                configObject["keystone.client_config.diagnostics_enabled"] = false;
            if (configObject["keystone.client_config.total_configs_to_save"] != null)
                configObject["keystone.client_config.total_configs_to_save"] = 5;

            if (configObject["keystone.publishing_content.enabledByProductId"]?["arcane"] != null)
                configObject["keystone.publishing_content.enabledByProductId"]!["arcane"] = true;

            if (configObject["lol.client_settings.lobby.tft_esports_spectator.enabled"] != null)
                configObject["lol.client_settings.lobby.tft_esports_spectator.enabled"] = true;
            if (configObject["lol.game_client_settings.leagues.enabled"] != null)
                configObject["lol.game_client_settings.leagues.enabled"] = false;
            if (configObject["lol.game_client_settings.missions.enabled"] != null)
                configObject["lol.game_client_settings.missions.enabled"] = false;
            if (configObject["lol.client_settings.team_builder.pass_summoner_account_id_with_afk_readiness"] != null)
                configObject["lol.client_settings.team_builder.pass_summoner_account_id_with_afk_readiness"] = true;
            if (configObject["lol.game_client_settings.purchasing_enabled"] != null)
                configObject["lol.game_client_settings.purchasing_enabled"] = true;
            if (configObject["lol.game_client_settings.specialoffer_enabled"] != null)
                configObject["lol.game_client_settings.specialoffer_enabled"] = true;
            if (configObject["lol.game_client_settings.starshards_purchase_enabled"] != null)
                configObject["lol.game_client_settings.starshards_purchase_enabled"] = true;
            if (configObject["lol.game_client_settings.starshards_services_enabled"] != null)
                configObject["lol.game_client_settings.starshards_services_enabled"] = true;
            if (configObject["lol.game_client_settings.store_enabled"] != null)
                configObject["lol.game_client_settings.store_enabled"] = true;

            if (configObject["chat.aggressive_scan.enabled"] != null)
                configObject["chat.aggressive_scan.enabled"] = true;
            if (configObject["chat.auto_query_msg_history.enabled"] != null)
                configObject["chat.auto_query_msg_history.enabled"] = true;
            if (configObject["chat.force_filter.enabled"] != null)
                configObject["chat.force_filter.enabled"] = false;
            if (configObject["chat.game_name_tag_line.enabled"] != null)
                configObject["chat.game_name_tag_line.enabled"] = true;
            // if (configObject["chat.replace_rich_messages"] != null)
            //     configObject["chat.replace_rich_messages"] = true;
            // if (configObject["chat.require_jwt_presence.league_of_legends.enabled"] != null)
            //     configObject["chat.require_jwt_presence.league_of_legends.enabled"] = true;
            // if (configObject["chat.require_keystone_presence.enabled"] != null)
            //     configObject["chat.require_keystone_presence.enabled"] = true;
            // if (configObject["chat.require_multi_game_presence_rxep.enabled"] != null)
            //     configObject["chat.require_multi_game_presence_rxep.enabled"] = true;

            if (configObject["keystone.client.feature_flags.chrome_devtools.enabled"] != null)
                configObject["keystone.client.feature_flags.chrome_devtools.enabled"] = true;
            if (configObject["keystone.client.feature_flags.playerReporting.enabled"] != null)
                configObject["keystone.client.feature_flags.playerReporting.enabled"] = true;
            if (configObject["keystone.client.feature_flags.restriction.enabled"] != null)
                configObject["keystone.client.feature_flags.restriction.enabled"] = true;
            if (configObject["keystone.self_update.patchline_override"] != null)
                configObject["keystone.self_update.patchline_override"] = "KeystoneFoundationLiveWin";
            if (configObject["keystone.telemetry.metrics_enabled"] != null)
                configObject["keystone.telemetry.metrics_enabled"] = false;
            if (configObject["keystone.telemetry.newrelic_events_v2_enabled"] != null)
                configObject["keystone.telemetry.newrelic_events_v2_enabled"] = false;
            if (configObject["keystone.telemetry.newrelic_metrics_v1_enabled"] != null)
                configObject["keystone.telemetry.newrelic_metrics_v1_enabled"] = false;

            if (configObject["lol.client_settings.chat.allow_group_by_game"] != null)
                configObject["lol.client_settings.chat.allow_group_by_game"] = true;
            if (configObject["lol.client_settings.chat.query_sumid_batched_ledge"] != null)
                configObject["lol.client_settings.chat.query_sumid_batched_ledge"] = true;
            if (configObject["lol.client_settings.chat.query_sumid_blocked"] != null)
                configObject["lol.client_settings.chat.query_sumid_blocked"] = true;
            if (configObject["lol.client_settings.chat.query_sumid_friend_requests"] != null)
                configObject["lol.client_settings.chat.query_sumid_friend_requests"] = true;
            if (configObject["lol.client_settings.chat.query_sumid_muc_participants"] != null)
                configObject["lol.client_settings.chat.query_sumid_muc_participants"] = true;
            if (configObject["lol.client_settings.chat.query_sumid_offline_friends"] != null)
                configObject["lol.client_settings.chat.query_sumid_offline_friends"] = true;
            if (configObject["lol.client_settings.chat.query_sumid_online_friends"] != null)
                configObject["lol.client_settings.chat.query_sumid_online_friends"] = true;
            if (configObject["lol.client_settings.chat.scoped_conversations.enabled"] != null)
                configObject["lol.client_settings.chat.scoped_conversations.enabled"] = true;
            if (configObject["lol.client_settings.chat.update_session_platform_id"] != null)
                configObject["lol.client_settings.chat.update_session_platform_id"] = true;
            if (configObject["lol.game_client_settings.logging.enable_http_public_logs"] != null)
                configObject["lol.game_client_settings.logging.enable_http_public_logs"] = false;
            if (configObject["lol.game_client_settings.logging.enable_rms_public_logs"] != null)
                configObject["lol.game_client_settings.logging.enable_rms_public_logs"] = false;

            if (configObject["keystone.products.league_of_legends.patchlines.live"]?["platforms"]?["win"]?["configurations"] != null)
                foreach (var node in configObject["keystone.products.league_of_legends.patchlines.live"]!["platforms"]!["win"]!["configurations"]!.AsArray())
                {
                    if (node == null) continue;

                    if (Option == 17)
                        node["patch_url"] = configObject["keystone.products.league_of_legends.patchlines.pbe"]?["platforms"]?["win"]?["configurations"]?[0]?["patch_url"]?.GetValue<string>();

                    node["launcher"]?["arguments"]?.AsArray().Add("--system-yaml-override=\"Config/system.yaml\"");

                    if (node["locale_data"] == null) continue;
                    node["locale_data"]!["available_locales"] = new JsonArray("ar_AE", "cs_CZ", "de_DE", "el_GR", "en_AU", "en_GB", "en_PH", "en_SG", "en_US", "es_AR", "es_ES", "es_MX", "fr_FR",
                        "hu_HU", "id_ID", "it_IT", "ja_JP", "ko_KR", "ms_MY", "pl_PL", "pt_BR", "ro_RO", "ru_RU", "th_TH", "tr_TR", "vn_VN", "zh_CN", "zh_MY", "zh_TW");
                    node["locale_data"]!["default_locale"] = "en_GB";
                }

            // configObject["keystone.self_update.patchline_override"] = "KeystoneFoundationBetaWin";
            // configObject["keystone.self_update.patchline_override"] = "KeystoneFoundationLiveWin";

            modifiedContent = JsonSerializer.Serialize(configObject);
            Console.WriteLine(modifiedContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        // Using the builtin EmbedIO methods for sending the response adds some garbage in the front of it.
        // This seems to do the trick.
        RESPOND:
        var responseBytes = Encoding.UTF8.GetBytes(modifiedContent);

        ctx.Response.StatusCode = (int)result.StatusCode;
        ctx.Response.SendChunked = false;
        ctx.Response.ContentLength64 = responseBytes.Length;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.OutputStream.WriteAsync(responseBytes);
        ctx.Response.OutputStream.Close();
    }
}