FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Server/Server.csproj Server/
COPY Framework/Framework.csproj     Framework/
COPY ORM/ORM.csproj     ORM/
COPY TemplateEngine/TemplateEngine.csproj   TemplateEngine/

RUN dotnet restore Server/Server.csproj

COPY . ./
RUN dotnet publish Server/Server.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV SERVER_PORT=9999
EXPOSE 9999

ENTRYPOINT ["dotnet", "Server.dll"]