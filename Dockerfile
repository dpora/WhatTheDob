# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore WhatTheDob.sln
RUN dotnet publish src/WhatTheDob.Web/WhatTheDob.Web.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DataStorage__DataDirectory=/app/datastorage \
    Serilog__LogDirectory=/app/logstorage
RUN mkdir -p /app/datastorage /app/logstorage
EXPOSE 8080
ENTRYPOINT ["dotnet", "WhatTheDob.Web.dll"]