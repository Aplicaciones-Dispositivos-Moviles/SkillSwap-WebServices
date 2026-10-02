FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first: the packages layer is reused until the project file changes.
COPY SkillSwap.Platform/SkillSwap.Platform.csproj SkillSwap.Platform/
RUN dotnet restore SkillSwap.Platform/SkillSwap.Platform.csproj

COPY SkillSwap.Platform/ SkillSwap.Platform/
RUN dotnet publish SkillSwap.Platform/SkillSwap.Platform.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000

# Render provides the port to listen on in PORT (10000 by default). exec lets the app receive the stop signal.
ENTRYPOINT ["sh", "-c", "exec dotnet SkillSwap.Platform.dll --urls http://0.0.0.0:${PORT:-10000}"]