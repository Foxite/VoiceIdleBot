using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceIdleBot;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

IConfigurationSection discordConfigSection = builder.Configuration.GetSection("Discord");
builder.Services.Configure<DiscordOptions>(discordConfigSection);

builder.Services.AddSingleton<DiscordConfiguration>(isp => {
	var options = isp.GetRequiredService<IOptions<DiscordOptions>>();
	return new DiscordConfiguration() {
		Intents = DiscordIntents.GuildVoiceStates | DiscordIntents.Guilds,
		LoggerFactory = isp.GetRequiredService<ILoggerFactory>(),
		Token = options.Value.Token,
	};
});

//builder.Services.AddScoped<DiscordClient>();

builder.Services.AddHostedService<VoiceIdleService>();

IHost app = builder.Build();
await app.RunAsync();
