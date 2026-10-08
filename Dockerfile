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

ENTRYPOINT ["dotnet", "WebApp.dll"]
