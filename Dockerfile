FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY WebApp/WebApp.csproj WebApp/
RUN dotnet restore WebApp/WebApp.csproj

COPY WebApp/ WebApp/

WORKDIR /src/WebApp
RUN dotnet publish WebApp.csproj -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5172
EXPOSE 6061

ENV ASPNETCORE_URLS=http://+:5172

# La BD SQLite (webapp.db) y los backups se guardan en el directorio de trabajo.
# Se usa /app/data para poder montarlo como volumen y no perder datos al redesplegar.
ENV ASPNETCORE_CONTENTROOT=/app
WORKDIR /app/data
VOLUME /app/data

ENTRYPOINT ["dotnet", "/app/WebApp.dll"]
