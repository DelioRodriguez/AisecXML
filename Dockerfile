FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY global.json NominaPrueba.sln ./
COPY NominaPrueba/NominaPrueba.csproj NominaPrueba/
RUN dotnet restore NominaPrueba/NominaPrueba.csproj
COPY NominaPrueba/ NominaPrueba/
RUN dotnet publish NominaPrueba/NominaPrueba.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "NominaPrueba.dll"]
