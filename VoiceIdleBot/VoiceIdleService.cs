using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;

namespace VoiceIdleBot;

public class VoiceIdleService : IHostedService, IAsyncDisposable {
	private readonly IOptions<DiscordOptions> _discordOptions;
	private readonly ILogger<VoiceIdleService> _logger;
	private readonly ChannelStatusService _channelStatusService;

	private readonly DiscordSocketClient _discordClient;
	private readonly SemaphoreSlim _playLock = new(1, 1);
	private IAudioClient? _audioClient;

	public VoiceIdleService(IOptions<DiscordOptions> discordOptions, ILogger<DiscordSocketClient> discordLogger, ILogger<VoiceIdleService> logger, ChannelStatusService channelStatusService) {
		_discordOptions = discordOptions;
		_logger = logger;
		_channelStatusService = channelStatusService;

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

		_discordClient.Connected += () => {
			_ = ConnectAudio();
			return Task.CompletedTask;
		};
		await ConnectAudio();

		_discordClient.UserVoiceStateUpdated += (user, oldState, newState) => {
			if (oldState.VoiceChannel?.Id != _discordOptions.Value.ChannelId && newState.VoiceChannel?.Id == _discordOptions.Value.ChannelId && user.Id != _discordClient.CurrentUser.Id) {
				// _audioClient is not supposed to be null at this point, if it is when well fuck
				_ = OnUserJoin(_audioClient);
			}
			return Task.CompletedTask;
		};

		_discordClient.VoiceChannelStatusUpdated += (channel, _, newStatus) => {
			if (channel.Id != _discordOptions.Value.ChannelId) {
				return Task.CompletedTask;
			}

			return _channelStatusService.SaveStatus(newStatus);
		};
	}
	
	private async Task ConnectAudio() {
		try {
			SocketGuild guild = _discordClient.GetGuild(_discordOptions.Value.GuildId) ?? throw new Exception($"Guild with ID {_discordOptions.Value.GuildId} is not found");

			SocketVoiceChannel channel = guild.GetVoiceChannel(_discordOptions.Value.ChannelId) ?? throw new Exception($"Channel with ID {_discordOptions.Value.ChannelId} is not found");

			bool retry;
			do {
				try {
					_audioClient = await channel.ConnectAsync();
					retry = false;
				} catch (TimeoutException) { // ye idk either
					retry = true;
				}
			} while (retry);
			
			await channel.SetStatusAsync(await _channelStatusService.GetStatus());
		} catch (Exception e) {
			_logger.LogCritical(e, "Exception caught in ConnectAudio");
		}
	}

	private async Task OnUserJoin(IAudioClient audioClient) {
		_logger.LogInformation("Playing join sound");
		try {
			await PlaySound(audioClient);
		} catch (Exception e) {
			_logger.LogError(e, "Failed to play sound on user join");
		}
	}

	private async Task PlaySound(IAudioClient audioClient) {
		if (!await _playLock.WaitAsync(TimeSpan.FromMilliseconds(1))) {
			_logger.LogInformation("Already playing sound; not playing");
			return;
		}

		try {
			await using AudioOutStream soundOut = audioClient.CreatePCMStream(AudioApplication.Music);
			await using FileStream soundIn = File.OpenRead(_discordOptions.Value.SoundPath);
			await soundIn.CopyToAsync(soundOut);
		} finally {
			_playLock.Release();
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
		_playLock.Dispose();
		
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
