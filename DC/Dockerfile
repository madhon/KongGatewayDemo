ARG VERSION=7.0-alpine

FROM mcr.microsoft.com/dotnet/sdk:$VERSION  AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:$VERSION

RUN apk add --upgrade --no-cache apk-tools openssl \
  && apk add --no-cache icu-libs

HEALTHCHECK --interval=5s --timeout=5s --retries=3 CMD wget http://127.0.0.1:80/healthz -q -O - > /dev/null 2>&1

WORKDIR /app
COPY --from=build-env /app/out .
ENV ASPNETCORE_URLSE=""http://+""
ENV ASPNETCORE_ENVIRONMENT="Development"
ENV DOTNET_RUNNING_IN_CONTAINER="true"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="1"
EXPOSE 80/tcp
EXPOSE 5000/tcp

ENTRYPOINT ["dotnet", "DC.dll"]



