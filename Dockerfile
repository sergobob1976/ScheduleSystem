FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Schedule.Api/Schedule.Api.csproj", "Schedule.Api/"]
COPY ["Schedule.Application/Schedule.Application.csproj", "Schedule.Application/"]
COPY ["Schedule.Contracts/Schedule.Contracts.csproj", "Schedule.Contracts/"]
COPY ["Schedule.Core/Schedule.Core.csproj", "Schedule.Core/"]
COPY ["Schedule.Infrastructure/Schedule.Infrastructure.csproj", "Schedule.Infrastructure/"]
COPY ["Schedule.Web/Schedule.Web.csproj", "Schedule.Web/"]

RUN dotnet restore "Schedule.Api/Schedule.Api.csproj" && \
    dotnet restore "Schedule.Web/Schedule.Web.csproj"

COPY . .

RUN dotnet publish "Schedule.Api/Schedule.Api.csproj" \
        --configuration Release \
        --output /out/api \
        --no-restore \
        /p:UseAppHost=false && \
    dotnet publish "Schedule.Web/Schedule.Web.csproj" \
        --configuration Release \
        --output /out/web \
        --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /out/api ./
COPY --from=build /out/web/wwwroot ./wwwroot

RUN mkdir -p /var/lib/schedulesystem/keys && \
    chown -R "$APP_UID:$APP_UID" /var/lib/schedulesystem

USER $APP_UID

EXPOSE 5085

ENTRYPOINT ["dotnet", "Schedule.Api.dll"]
