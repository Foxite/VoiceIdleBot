FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VoiceIdleBot/VoiceIdleBot.csproj", "VoiceIdleBot/"]
RUN dotnet restore "VoiceIdleBot/VoiceIdleBot.csproj"
COPY . .
WORKDIR "/src/VoiceIdleBot"
RUN dotnet build "VoiceIdleBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "VoiceIdleBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final

#RUN apt-get update && apt-get install libopus0 libsodium23

USER $APP_UID
WORKDIR /app
COPY --from=build /app/publish .
CMD ["dotnet", "VoiceIdleBot.dll"]
