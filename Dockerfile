FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BankaExtreAnaliz.csproj", "./"]
RUN dotnet restore "BankaExtreAnaliz.csproj"
COPY . .
RUN dotnet publish "BankaExtreAnaliz.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BankaExtreAnaliz.dll"]
