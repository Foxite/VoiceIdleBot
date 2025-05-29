using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VoiceIdleBot;

public class VoiceIdleService : IHostedService, IAsyncDisposable {
	private readonly IOptions<DiscordOptions> _discordOptions;
	private readonly ILogger<VoiceIdleService> _logger;

	private readonly DiscordSocketClient _discordClient;
	private IAudioClient? _audioClient;

	public VoiceIdleService(IOptions<DiscordOptions> discordOptions, ILogger<DiscordSocketClient> discordLogger, ILogger<VoiceIdleService> logger) {
		_discordOptions = discordOptions;
		_logger = logger;

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
		
		SocketGuild guild = _discordClient.GetGuild(_discordOptions.Value.GuildId) ?? throw new Exception($"Guild with ID {_discordOptions.Value.GuildId} is not found");

		SocketVoiceChannel channel = guild.GetVoiceChannel(_discordOptions.Value.ChannelId) ?? throw new Exception($"Channel with ID {_discordOptions.Value.ChannelId} is not found");
		_audioClient = await channel.ConnectAsync();

		_discordClient.UserVoiceStateUpdated += (user, oldState, newState) => {
			if (oldState.VoiceChannel?.Id != _discordOptions.Value.ChannelId && newState.VoiceChannel?.Id == _discordOptions.Value.ChannelId) {
				_ = OnUserJoin(_audioClient);
			}
			return Task.CompletedTask;
		};
	}
	
	private async Task OnUserJoin(IAudioClient audioClient) {
		_logger.LogInformation("Playing join sound");
		try {
			await using AudioOutStream soundOut = audioClient.CreatePCMStream(AudioApplication.Music);
			await using FileStream soundIn = File.OpenRead(_discordOptions.Value.SoundPath);
			await soundIn.CopyToAsync(soundOut);
		} catch (Exception e) {
			_logger.LogError(e, "Failed to play sound on user join");
		}
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
