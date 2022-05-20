using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RiotLauncher;

public class ConfigProxy
{
    private const string ConfigUrl = "https://clientconfig.rpg.riotgames.com"; // https://clientconfig.esports.rpg.riotgames.com
    private readonly HttpClient _client = new();
    private readonly HttpListener _httpListener;
    private readonly int _option;

    public ConfigProxy(int option = 0)
    {
        _option = option;

        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        ConfigPort = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();

        _httpListener = new HttpListener { Prefixes = { $"http://localhost:{ConfigPort}/" } };
        _httpListener.Start();

        Task.Run(async () =>
        {
            while (true)
                await ProxyAndRewriteResponseAsync();
            // ReSharper disable once FunctionNeverReturns
        });
    }

    public int ConfigPort { get; }

    private async Task ProxyAndRewriteResponseAsync()
    {
        var ctx = await _httpListener.GetContextAsync();

        var url = ConfigUrl + ctx.Request.RawUrl;
        url = url.Replace("KeystoneFoundationLiveWin", "KeystoneFoundationBetaWin");
        Console.WriteLine(url);

        using var message = new HttpRequestMessage(HttpMethod.Get, url);

        message.Headers.TryAddWithoutValidation("User-Agent", "RiotLauncher/0.1 (https://github.com/aPinat/RiotLauncher)");
        message.Headers.TryAddWithoutValidation("Accept", "application/json");

        if (ctx.Request.Headers["X-Riot-Entitlements-JWT"] is { })
            message.Headers.TryAddWithoutValidation("X-Riot-Entitlements-JWT", ctx.Request.Headers["X-Riot-Entitlements-JWT"]);

        if (ctx.Request.Headers["Authorization"] is { })
            message.Headers.TryAddWithoutValidation("Authorization", ctx.Request.Headers["Authorization"]);

        if (ctx.Request.Headers["X-Riot-RSO-Identity-JWT"] is { })
            message.Headers.TryAddWithoutValidation("X-Riot-RSO-Identity-JWT", ctx.Request.Headers["X-Riot-RSO-Identity-JWT"]);

        var result = await _client.SendAsync(message);
        var content = await result.Content.ReadAsStringAsync();

        if (!result.IsSuccessStatusCode)
            goto RESPOND;

        var config = JsonSerializer.Deserialize<JsonNode>(content);
        if (config is null)
            goto RESPOND;

        if (config["keystone.client.feature_flags.autoLaunch.disabled"] is JsonValue)
            config["keystone.client.feature_flags.autoLaunch.disabled"] = false;
        if (config["keystone.client.feature_flags.autoPatch.disabled"] is JsonValue)
            config["keystone.client.feature_flags.autoPatch.disabled"] = false;
        if (config["keystone.client.feature_flags.games_library.special_events.enabled"] is JsonValue)
            config["keystone.client.feature_flags.games_library.special_events.enabled"] = false;
        if (config["keystone.client.feature_flags.login.disabled"] is JsonValue)
            config["keystone.client.feature_flags.login.disabled"] = false;
        if (config["keystone.client.feature_flags.playerReportingMailboxIntegration.enabled"] is JsonValue)
            config["keystone.client.feature_flags.playerReportingMailboxIntegration.enabled"] = true;
        if (config["keystone.client.feature_flags.playerReportingPasIntegration.enabled"] is JsonValue)
            config["keystone.client.feature_flags.playerReportingPasIntegration.enabled"] = true;
        if (config["keystone.client.feature_flags.playerReportingReporterFeedback.enabled"] is JsonValue)
            config["keystone.client.feature_flags.playerReportingReporterFeedback.enabled"] = true;
        if (config["keystone.client.feature_flags.regionlessLoginInfoTooltip.enabled"] is JsonValue)
            config["keystone.client.feature_flags.regionlessLoginInfoTooltip.enabled"] = false;
        // if (config["keystone.client.feature_flags.product_settings.keep_updated.enabled"] is JsonValue)
        // config["keystone.client.feature_flags.product_settings.keep_updated.enabled"] = true;
        if (config["keystone.client.feature_flags.staySignedIn.disabled"] is JsonValue)
            config["keystone.client.feature_flags.staySignedIn.disabled"] = true;
        if (config["keystone.client.feature_flags.socialSignOn.enabled"] is JsonValue)
            config["keystone.client.feature_flags.socialSignOn.enabled"] = false;
        if (config["keystone.client.feature_flags.system_tray.enabled"] is JsonValue)
            config["keystone.client.feature_flags.system_tray.enabled"] = true;
        if (config["keystone.client.feature_flags.restart_required.disabled"] is JsonValue)
            config["keystone.client.feature_flags.restart_required.disabled"] = true;

        if (config["keystone.client_config.diagnostics_enabled"] is JsonValue)
            config["keystone.client_config.diagnostics_enabled"] = false;
        if (config["keystone.client_config.total_configs_to_save"] is JsonValue)
            config["keystone.client_config.total_configs_to_save"] = 5;

        if (config["lol.client_settings.lobby.tft_esports_spectator.enabled"] is JsonValue)
            config["lol.client_settings.lobby.tft_esports_spectator.enabled"] = true;
        // Use new league edge instead of ACS, which is now shutdown
        // if (config["lol.client_settings.match_history.gamhs.enabled"] is JsonValue)
        //     config["lol.client_settings.match_history.gamhs.enabled"] = true;
        // Use player platform instead of league edge, which returns 403 Forbidden now
        // if (config["lol.client_settings.match_history.player_platform_edge.enabled"] is JsonValue)
        //     config["lol.client_settings.match_history.player_platform_edge.enabled"] = true;
        if (config["lol.game_client_settings.leagues.enabled"] is JsonValue)
            config["lol.game_client_settings.leagues.enabled"] = true;
        if (config["lol.game_client_settings.missions.enabled"] is JsonValue)
            config["lol.game_client_settings.missions.enabled"] = true;
        if (config["lol.client_settings.team_builder.pass_summoner_account_id_with_afk_readiness"] is JsonValue)
            config["lol.client_settings.team_builder.pass_summoner_account_id_with_afk_readiness"] = true;
        if (config["lol.game_client_settings.purchasing_enabled"] is JsonValue)
            config["lol.game_client_settings.purchasing_enabled"] = true;
        if (config["lol.game_client_settings.specialoffer_enabled"] is JsonValue)
            config["lol.game_client_settings.specialoffer_enabled"] = true;
        if (config["lol.game_client_settings.starshards_purchase_enabled"] is JsonValue)
            config["lol.game_client_settings.starshards_purchase_enabled"] = true;
        if (config["lol.game_client_settings.starshards_services_enabled"] is JsonValue)
            config["lol.game_client_settings.starshards_services_enabled"] = true;
        if (config["lol.game_client_settings.store_enabled"] is JsonValue)
            config["lol.game_client_settings.store_enabled"] = true;

        if (config["chat.aggressive_scan.enabled"] is JsonValue)
            config["chat.aggressive_scan.enabled"] = true;
        if (config["chat.auto_query_msg_history.enabled"] is JsonValue)
            config["chat.auto_query_msg_history.enabled"] = true;
        if (config["chat.force_filter.enabled"] is JsonValue)
            config["chat.force_filter.enabled"] = false;
        if (config["chat.game_name_tag_line.enabled"] is JsonValue)
            config["chat.game_name_tag_line.enabled"] = true;
        // if (config["chat.replace_rich_messages"] is JsonValue)
        //     config["chat.replace_rich_messages"] = true;
        // if (config["chat.require_jwt_presence.league_of_legends.enabled"] is JsonValue)
        //     config["chat.require_jwt_presence.league_of_legends.enabled"] = true;
        // if (config["chat.require_keystone_presence.enabled"] is JsonValue)
        //     config["chat.require_keystone_presence.enabled"] = true;
        // if (config["chat.require_multi_game_presence_rxep.enabled"] is JsonValue)
        //     config["chat.require_multi_game_presence_rxep.enabled"] = true;

        if (config["keystone.client.feature_flags.chrome_devtools.enabled"] is JsonValue)
            config["keystone.client.feature_flags.chrome_devtools.enabled"] = true;
        if (config["keystone.client.feature_flags.playerReporting.enabled"] is JsonValue)
            config["keystone.client.feature_flags.playerReporting.enabled"] = true;
        if (config["keystone.client.feature_flags.restriction.enabled"] is JsonValue)
            config["keystone.client.feature_flags.restriction.enabled"] = true;
        if (config["keystone.self_update.patchline_override"] is JsonValue || config["keystone.self_update.http_cookie"] is JsonValue)
            config["keystone.self_update.patchline_override"] = "KeystoneFoundationBetaWin"; // KeystoneFoundationLiveWin
        if (config["keystone.telemetry.metrics_enabled"] is JsonValue)
            config["keystone.telemetry.metrics_enabled"] = false;
        if (config["keystone.telemetry.newrelic_events_v2_enabled"] is JsonValue)
            config["keystone.telemetry.newrelic_events_v2_enabled"] = false;
        if (config["keystone.telemetry.newrelic_metrics_v1_enabled"] is JsonValue)
            config["keystone.telemetry.newrelic_metrics_v1_enabled"] = false;
        if (config["keystone.telemetry.newrelic_schemaless_events_v2_enabled"] is JsonValue)
            config["keystone.telemetry.newrelic_schemaless_events_v2_enabled"] = false;

        if (config["lol.client_settings.chat.allow_group_by_game"] is JsonValue)
            config["lol.client_settings.chat.allow_group_by_game"] = true;
        if (config["lol.client_settings.chat.query_sumid_batched_ledge"] is JsonValue)
            config["lol.client_settings.chat.query_sumid_batched_ledge"] = true;
        if (config["lol.client_settings.chat.query_sumid_blocked"] is JsonValue)
            config["lol.client_settings.chat.query_sumid_blocked"] = true;
        if (config["lol.client_settings.chat.query_sumid_friend_requests"] is JsonValue)
            config["lol.client_settings.chat.query_sumid_friend_requests"] = true;
        if (config["lol.client_settings.chat.query_sumid_muc_participants"] is JsonValue)
            config["lol.client_settings.chat.query_sumid_muc_participants"] = true;
        if (config["lol.client_settings.chat.query_sumid_offline_friends"] is JsonValue)
            config["lol.client_settings.chat.query_sumid_offline_friends"] = true;
        if (config["lol.client_settings.chat.query_sumid_online_friends"] is JsonValue)
            config["lol.client_settings.chat.query_sumid_online_friends"] = true;
        if (config["lol.client_settings.chat.scoped_conversations.enabled"] is JsonValue)
            config["lol.client_settings.chat.scoped_conversations.enabled"] = true;
        if (config["lol.client_settings.chat.update_session_platform_id"] is JsonValue)
            config["lol.client_settings.chat.update_session_platform_id"] = true;
        if (config["lol.game_client_settings.logging.enable_http_public_logs"] is JsonValue)
            config["lol.game_client_settings.logging.enable_http_public_logs"] = false;
        if (config["lol.game_client_settings.logging.enable_rms_public_logs"] is JsonValue)
            config["lol.game_client_settings.logging.enable_rms_public_logs"] = false;


        //// Custom League of Legends live patchline configuration

        if (config["keystone.products.league_of_legends.patchlines.live"]?["platforms"]?["win"]?["configurations"] is JsonArray configurations)
            foreach (var node in configurations)
            {
                if (node is null)
                    continue;

                // PBE client
                if (_option == 17 && config["keystone.products.league_of_legends.patchlines.pbe"]?["platforms"]?["win"]?["configurations"]?[0]?["patch_url"] is JsonValue patchUrl)
                    node["patch_url"] = patchUrl.GetValue<string>();

                // Use custom system.yaml, also required for PBE client, because live regions are obviously missing
                node["launcher"]?["arguments"]?.AsArray().Add("--system-yaml-override=\"Config/system.yaml\"");

                if (node["locale_data"] is not JsonObject localeData)
                    continue;

                // Replace region locked locales with all possible locales (also includes completely unsupported locales)
                localeData["available_locales"] = new JsonArray("ar_AE", "cs_CZ", "de_DE", "el_GR", "en_AU", "en_GB", "en_PH", "en_SG", "en_US", "es_AR", "es_ES", "es_MX", "fr_FR",
                    "hu_HU", "id_ID", "it_IT", "ja_JP", "ko_KR", "ms_MY", "pl_PL", "pt_BR", "ro_RO", "ru_RU", "th_TH", "tr_TR", "vn_VN", "zh_CN", "zh_MY", "zh_TW");
                localeData["default_locale"] = "en_GB";
            }


        //// Use and setup ChatProxy

        if (config["chat.host"] is JsonValue host)
        {
            ChatProxy.Hostname = host.GetValue<string>();
            config["chat.host"] = "127.0.0.1";
        }

        // Running our ChatProxy on the same port, no need to change.
        // if (config["chat.port"] is JsonValue)
        //     config["chat.port"] = 5223;

        if (config["chat.use_tls.enabled"] is JsonValue)
            config["chat.use_tls.enabled"] = false;

        if (config["chat.affinity.enabled"] is JsonValue enabled && enabled.GetValue<bool>() && config["chat.affinities"] is JsonObject affinities)
        {
            var pasRequest = new HttpRequestMessage(HttpMethod.Get, "https://riot-geo.pas.si.riotgames.com/pas/v1/service/chat"); // https://pas.esports.rpg.riotgames.com/pas/v1/service/chat
            pasRequest.Headers.TryAddWithoutValidation("Authorization", ctx.Request.Headers["authorization"]);
            try
            {
                var pasJwt = await (await _client.SendAsync(pasRequest)).Content.ReadAsStringAsync();
                Console.WriteLine("PAS JWT:" + pasJwt);
                var pasJwtContent = pasJwt.Split('.')[1];
                var validBase64 = pasJwtContent.PadRight((pasJwtContent.Length / 4 * 4) + (pasJwtContent.Length % 4 == 0 ? 0 : 4), '=');
                var pasJwtString = Encoding.UTF8.GetString(Convert.FromBase64String(validBase64));
                var pasJwtJson = JsonSerializer.Deserialize<JsonNode>(pasJwtString);
                var affinity = pasJwtJson?["affinity"]?.GetValue<string>();
                if (affinity is not null)
                {
                    ChatProxy.Hostname = affinities[affinity]?.GetValue<string>() ?? ChatProxy.Hostname;
                    Console.WriteLine($"AFFINITY: {affinity} -> {ChatProxy.Hostname}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting player affinity token, using default chat server.");
                Console.WriteLine(e);
            }

            affinities.Select(pair => pair.Key).ToList().ForEach(s => affinities[s] = "127.0.0.1");
        }

        content = config.ToJsonString();
        // Console.WriteLine(JsonSerializer.Deserialize<JsonNode>(content)?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        RESPOND:
        var responseBytes = Encoding.UTF8.GetBytes(content);
        ctx.Response.StatusCode = (int)result.StatusCode;
        ctx.Response.ContentLength64 = responseBytes.Length;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.OutputStream.WriteAsync(responseBytes);
        ctx.Response.OutputStream.Close();
    }
}
