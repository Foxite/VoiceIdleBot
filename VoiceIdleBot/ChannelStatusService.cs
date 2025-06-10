using Microsoft.Extensions.Options;

namespace VoiceIdleBot;

public class ChannelStatusService {
	private readonly IOptions<DiscordOptions> _discordOptions;
	
	public ChannelStatusService(IOptions<DiscordOptions> discordOptions) {
		_discordOptions = discordOptions;
	}

	public async Task<string> GetStatus() {
		string status = _discordOptions.Value.Status;
		try {
			status = await File.ReadAllTextAsync(_discordOptions.Value.StatusFile);
		} catch (FileNotFoundException) {}
		
		return status;
	}

	public async Task SaveStatus(string status) {
		await File.WriteAllTextAsync(_discordOptions.Value.StatusFile, status);
	}
}
