using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace VoiceIdleBot;

public class VoiceIdleService : IHostedService, IAsyncDisposable {
	private readonly IOptions<DiscordOptions> _discordOptions;
	
	private readonly DiscordClient _discordClient;
	private VoiceNextConnection? _voiceConnection;

	public VoiceIdleService(IOptions<DiscordOptions> discordOptions, DiscordConfiguration discordConfiguration) {
		_discordOptions = discordOptions;

		_discordClient = new DiscordClient(discordConfiguration);
	}

	public async Task StartAsync(CancellationToken cancellationToken) {
		_discordClient.UseVoiceNext(new VoiceNextConfiguration() {
			EnableIncoming = false,
		});
		await _discordClient.ConnectAsync();

		var guild = await _discordClient.GetGuildAsync(_discordOptions.Value.GuildId);
		
		DiscordChannel channel = guild.GetChannel(_discordOptions.Value.ChannelId) ?? throw new Exception($"Specified channel with ID {_discordOptions.Value.ChannelId} is not found");

		Console.WriteLine("YYY");
		_voiceConnection = await channel.ConnectAsync();
		Console.WriteLine("ZZZ");

		_voiceConnection.UserJoined += OnUserJoin;
		
		Console.WriteLine("AAA");
	}
	
	private Task OnUserJoin(VoiceNextConnection sender, VoiceUserJoinEventArgs args) {
		Console.WriteLine("BBB");
		// TODO play sound
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) {
		Console.WriteLine("CCC");
		try {
			Console.WriteLine("DDD");
			_voiceConnection?.Disconnect();
			Console.WriteLine("EEE");
		}
		finally {
			Console.WriteLine("FFF");
			_voiceConnection?.Dispose();
			Console.WriteLine("GGG");
			_voiceConnection = null;
			Console.WriteLine("HHH");
		}
		
		Console.WriteLine("III");
		
		return Task.CompletedTask;
	}
	
	public async ValueTask DisposeAsync() {
		Console.WriteLine("JJJ");
		await _discordClient.DisconnectAsync();
		Console.WriteLine("KKK");
		_discordClient.Dispose();
		Console.WriteLine("LLL");
	}
}
