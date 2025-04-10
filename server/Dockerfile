FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5161

ENV ASPNETCORE_URLS=http://+:5161
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8
ENV TZ=America/Detroit
ENV ConfigPath=/etc/inkydesk

VOLUME /etc/inkydesk

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["InkyDesk.Server.csproj", "./"]
RUN dotnet restore "InkyDesk.Server.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "InkyDesk.Server.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "InkyDesk.Server.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InkyDesk.Server.dll"]
