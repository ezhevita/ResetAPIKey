using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ArchiSteamFarm;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Plugins;
using JetBrains.Annotations;

namespace ResetAPIKey {
	[Export(typeof(IPlugin))]
	[UsedImplicitly]
	public class ResetAPIKey : IBotCommand {
		public void OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo(nameof(ResetAPIKey) + " by Vital7 | Support & source code: https://github.com/Vital7/ResetAPIKey");
		}

		public string Name => nameof(ResetAPIKey);
		public Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? throw new InvalidOperationException(nameof(Version));

		public async Task<string?> OnBotCommand(Bot bot, ulong steamID, string message, string[] args) {
			return args[0].ToUpperInvariant() switch {
				"RESETAPIKEY" when args.Length > 1 => await ResponseResetAPIKey(steamID, Utilities.GetArgsAsText(args, 1, ",")),
				"RESETAPIKEY" => await ResponseResetAPIKey(bot, steamID).ConfigureAwait(false),
				_ => null
			};
		}

		private async Task<string?> ResponseResetAPIKey(Bot bot, ulong steamID) {
			if (!bot.HasAccess(steamID, BotConfig.EAccess.Master)) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
			}

			const string request = "/dev/revokekey";
			bool result = await bot.ArchiWebHandler.UrlPostWithSession(ArchiWebHandler.SteamCommunityURL, request, new Dictionary<string, string>(1)).ConfigureAwait(false);
			return Commands.FormatBotResponse(result ? Strings.Success : Strings.WarningFailed, bot.BotName);
		}

		private async Task<string?> ResponseResetAPIKey(ulong steamID, string botNames) {
			HashSet<Bot>? bots = Bot.GetBots(botNames);
			if ((bots == null) || (bots.Count == 0)) {
				return ASF.IsOwner(steamID) ? Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseResetAPIKey(bot, steamID))).ConfigureAwait(false);

			List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);
			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}
	}
}
