namespace VoiceIdleBot;

public class DiscordOptions {
	public string Token { get; set; }
	public ulong GuildId { get; set; }
	public ulong ChannelId { get; set; }
	public string? Status { get; set; }
	public string SoundPath { get; set; }
}
