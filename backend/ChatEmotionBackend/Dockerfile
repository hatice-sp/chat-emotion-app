# 1️⃣ Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# 2️⃣ Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "ChatEmotionBackend.csproj"
RUN dotnet publish "ChatEmotionBackend.csproj" -c Release -o /app/publish

# 3️⃣ Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ChatEmotionBackend.dll"]
