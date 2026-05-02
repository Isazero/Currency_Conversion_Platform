FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Currency_Conversion_Platform/Currency_Conversion_Platform.csproj", "Currency_Conversion_Platform/"]
RUN dotnet restore "Currency_Conversion_Platform/Currency_Conversion_Platform.csproj"
COPY . .
WORKDIR "/src/Currency_Conversion_Platform"
RUN dotnet build "./Currency_Conversion_Platform.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Currency_Conversion_Platform.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Currency_Conversion_Platform.dll"]
