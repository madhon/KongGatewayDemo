ARG VERSION=7.0-alpine

FROM mcr.microsoft.com/dotnet/aspnet:$VERSION AS base
ENV ASPNETCORE_URLS=""http://+""
ENV ASPNETCORE_ENVIRONMENT="Development"
ENV DOTNET_RUNNING_IN_CONTAINER="true"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="0"
EXPOSE 80/tcp
EXPOSE 5000/tcp

RUN apk add --upgrade --no-cache apk-tools openssl \
  && apk add --no-cache icu-libs tzdata

#HEALTHCHECK --interval=5s --timeout=5s --retries=3 CMD wget http://127.0.0.1:80/healthz -q -O - > /dev/null 2>&1


FROM mcr.microsoft.com/dotnet/sdk:$VERSION  AS build
WORKDIR /src

COPY "KongGatewayDemo.sln" "KongGatewayDemo.sln"
COPY "Directory.Packages.props" "Directory.Packages.props"
COPY "Directory.Build.props" "Directory.Build.props"
COPY "nuget.config" "nuget.config"
COPY "docker-compose.dcproj" "docker-compose.dcproj"
COPY "DC/DC.csproj" "DC/DC.csproj"
COPY "Marvel/Marvel.csproj" "Marvel/Marvel.csproj"
COPY "WebStatus/WebStatus.csproj" "WebStatus/WebStatus.csproj"

RUN dotnet restore "KongGatewayDemo.sln"

COPY . .
WORKDIR /src/DC
RUN dotnet publish --no-restore -c Release -o /app


FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DC.dll"]