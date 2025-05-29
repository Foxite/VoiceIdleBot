using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VoiceIdleBot;

public class VoiceIdleService : IHostedService, IAsyncDisposable {
	private readonly IOptions<DiscordOptions> _discordOptions;
	
	private readonly DiscordSocketClient _discordClient;
	private IAudioClient? _audioClient;

	public VoiceIdleService(IOptions<DiscordOptions> discordOptions, ILogger<DiscordSocketClient> discordLogger) {
		_discordOptions = discordOptions;

		_discordClient = new DiscordSocketClient(new DiscordSocketConfig() {
			GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates,
		});

		_discordClient.Log += message => {
			discordLogger.Log(message.Severity switch {
				LogSeverity.Critical => LogLevel.Critical,
				LogSeverity.Error    => LogLevel.Error,
				LogSeverity.Warning  => LogLevel.Warning,
				LogSeverity.Info     => LogLevel.Information,
				LogSeverity.Debug    => LogLevel.Debug,
				LogSeverity.Verbose  => LogLevel.Trace,
			}, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
			return Task.CompletedTask;
		};
	}

	public async Task StartAsync(CancellationToken cancellationToken) {
		var readyTcs = new TaskCompletionSource();
		_discordClient.Ready += () => {
			readyTcs.SetResult();
			return Task.CompletedTask;
		};
		await _discordClient.LoginAsync(TokenType.Bot, _discordOptions.Value.Token);
		await _discordClient.StartAsync();
		await readyTcs.Task;
		
		var guild = _discordClient.GetGuild(_discordOptions.Value.GuildId);

		var channel = guild.GetVoiceChannel(_discordOptions.Value.ChannelId);
		_audioClient = await channel.ConnectAsync();

		_discordClient.UserVoiceStateUpdated += (user, oldState, newState) => {
			if (oldState.VoiceChannel?.Id != _discordOptions.Value.ChannelId && newState.VoiceChannel?.Id == _discordOptions.Value.ChannelId) {
				return OnUserJoin();
			} else {
				return Task.CompletedTask;
			}
		};
	}
	
	private Task OnUserJoin() {
		Console.WriteLine("BBB");
		// TODO play sound
		return Task.CompletedTask;
	}

	public async Task StopAsync(CancellationToken cancellationToken) {
		try {
			if (_audioClient != null) {
				await _audioClient.StopAsync();
			}
		} finally {
			_audioClient?.Dispose();
			_audioClient = null;
		}
	}
	
	public async ValueTask DisposeAsync() {
		try {
			if (_audioClient != null) {
				await _audioClient.StopAsync();
			}
		} finally {
			_audioClient?.Dispose();
			_audioClient = null;
		}
	}
}
