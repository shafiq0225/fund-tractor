FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["FundTrackr.sln", "./"]
COPY ["API/API.csproj", "API/"]
COPY ["Core/Core.csproj", "Core/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

# Restore dependencies
RUN dotnet restore "FundTrackr.sln"

# Copy everything else
COPY . .

# Build and publish
WORKDIR "/src/API"
RUN dotnet build "API.csproj" -c Release -o /app/build
RUN dotnet publish "API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]