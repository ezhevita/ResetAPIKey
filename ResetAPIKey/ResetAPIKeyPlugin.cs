using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using ArchiSteamFarm.Steam.Interaction;
using JetBrains.Annotations;

namespace ResetAPIKey;

[Export(typeof(IPlugin))]
[UsedImplicitly]
internal sealed class ResetAPIKeyPlugin : IBotCommand, IGitHubPluginUpdates
{
	public Task OnLoaded(CancellationToken cancellationToken)
	{
		ASF.ArchiLogger.LogGenericInfo(Name + " by ezhevita | Support & source code: https://github.com/ezhevita/ResetAPIKey");

		return Task.CompletedTask;
	}

	public string Name => nameof(ResetAPIKey);
	public string RepositoryName => "ezhevita/ResetAPIKey";
	public Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public Task<string?> OnBotCommand(Bot bot, EBotUserAccess access, string message, string[] args, ulong steamID = 0, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(args);
		ArgumentNullException.ThrowIfNull(bot);

		return args[0].ToUpperInvariant() switch
		{
			"RESETAPIKEY" when args.Length > 1 => ResponseResetAPIKey(access, Utilities.GetArgsAsText(args, 1, ","), steamID),
			"RESETAPIKEY" => ResponseResetAPIKey(access, bot),
			_ => Task.FromResult<string?>(null)
		};
	}

	private static async Task<string?> ResponseResetAPIKey(EBotUserAccess access, Bot bot)
	{
		if (access < EBotUserAccess.Master)
		{
			return null;
		}

		if (!bot.IsConnectedAndLoggedOn)
		{
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		const string request = "/dev/revokekey";
		var result = await bot.ArchiWebHandler.UrlPostWithSession(new Uri(ArchiWebHandler.SteamCommunityURL, request), new Dictionary<string, string>(1)).ConfigureAwait(false);

		return Commands.FormatBotResponse(result ? Strings.Success : Strings.WarningFailed, bot.BotName);
	}

	private static async Task<string?> ResponseResetAPIKey(EBotUserAccess access, string botNames, ulong steamID)
	{
		var bots = Bot.GetBots(botNames);
		if ((bots == null) || (bots.Count == 0))
		{
			return ASF.IsOwner(steamID) ? Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		var results = await Utilities.InParallel(bots.Select(bot => ResponseResetAPIKey(Commands.GetProxyAccess(bot, access, steamID), bot))).ConfigureAwait(false);

		List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}
}
