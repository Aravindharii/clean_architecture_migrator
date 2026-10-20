# ---------- Build Stage ----------
    FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

    WORKDIR /src
    
    COPY . .
    
    RUN dotnet restore server.csproj
    RUN dotnet publish server.csproj -c Release -o /app/publish
    
    # ---------- Runtime Stage ----------
    FROM mcr.microsoft.com/dotnet/aspnet:10.0
    
    WORKDIR /app
    
    COPY --from=build /app/publish .
    
    ENV ASPNETCORE_URLS=http://+:10000
    EXPOSE 10000
    
    ENTRYPOINT ["dotnet", "server.dll"]