using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VoiceIdleBot;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

IConfigurationSection discordConfigSection = builder.Configuration.GetSection("Discord");
builder.Services.Configure<DiscordOptions>(discordConfigSection);

builder.Services.AddHostedService<VoiceIdleService>();

builder.Services.AddSingleton<ChannelStatusService>();

IHost app = builder.Build();
await app.RunAsync();
