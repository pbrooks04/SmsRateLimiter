FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY SmsRateLimiter/SmsRateLimiter.csproj SmsRateLimiter/
RUN dotnet restore SmsRateLimiter/SmsRateLimiter.csproj
COPY . .
RUN dotnet publish SmsRateLimiter/SmsRateLimiter.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SmsRateLimiter.dll"]
