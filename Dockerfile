FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_14.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

# restore
WORKDIR /src
COPY ["Btech.Sql.Console/Btech.Sql.Console.csproj", "Btech.Sql.Console/"]
RUN dotnet restore "Btech.Sql.Console/Btech.Sql.Console.csproj"
COPY . .

# build
WORKDIR "/src/Btech.Sql.Console"
RUN dotnet build "Btech.Sql.Console.csproj" -c Release -o /app/build

# publish
FROM build AS publish
RUN dotnet publish "Btech.Sql.Console.csproj" -c Release -o /app/publish

# final
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
EXPOSE 443
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Btech.Sql.Console.dll"]