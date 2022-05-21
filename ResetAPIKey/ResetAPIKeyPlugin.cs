using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using ArchiSteamFarm.Steam.Interaction;
using ArchiSteamFarm.Steam.Storage;
using JetBrains.Annotations;

namespace ResetAPIKey {
	[Export(typeof(IPlugin))]
	[UsedImplicitly]
	public class ResetAPIKeyPlugin : IBotCommand {
		public void OnLoaded() {
			Assembly assembly = Assembly.GetExecutingAssembly();
			string repository = assembly
				.GetCustomAttributes<AssemblyMetadataAttribute>()
				.First(x => x.Key == "RepositoryUrl")
				.Value ?? throw new InvalidOperationException(nameof(AssemblyMetadataAttribute));

			string company = assembly
				.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? throw new InvalidOperationException(nameof(AssemblyCompanyAttribute));

			ASF.ArchiLogger.LogGenericInfo(Name + " by ezhevita | Support & source code: https://github.com/ezhevita/ResetAPIKey");
		}

		public string Name => nameof(ResetAPIKey);
		public Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? throw new InvalidOperationException(nameof(Version));

		[CLSCompliant(false)]
		public Task<string?> OnBotCommand(Bot bot, ulong steamID, string message, string[] args) {
			if (bot == null) {
				throw new ArgumentNullException(nameof(bot));
			}

			if (string.IsNullOrEmpty(message)) {
				throw new ArgumentNullException(nameof(message));
			}

			if (args == null) {
				throw new ArgumentNullException(nameof(args));
			}

			return args[0].ToUpperInvariant() switch {
				"RESETAPIKEY" when args.Length > 1 => ResponseResetAPIKey(steamID, Utilities.GetArgsAsText(args, 1, ",")),
				"RESETAPIKEY" => ResponseResetAPIKey(bot, steamID),
				_ => Task.FromResult<string?>(null)
			};
		}

		private static async Task<string?> ResponseResetAPIKey(Bot bot, ulong steamID) {
			if (!bot.HasAccess(steamID, BotConfig.EAccess.Master)) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
			}

			const string request = "/dev/revokekey";
			bool result = await bot.ArchiWebHandler.UrlPostWithSession(new Uri(ArchiWebHandler.SteamCommunityURL, request), new Dictionary<string, string>(1)).ConfigureAwait(false);
			return Commands.FormatBotResponse(result ? Strings.Success : Strings.WarningFailed, bot.BotName);
		}

		private static async Task<string?> ResponseResetAPIKey(ulong steamID, string botNames) {
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
