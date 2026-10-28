FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PruebaBlazorZiur/PruebaBlazorZiur.csproj PruebaBlazorZiur/
RUN dotnet restore PruebaBlazorZiur/PruebaBlazorZiur.csproj

COPY PruebaBlazorZiur/ PruebaBlazorZiur/
RUN dotnet publish PruebaBlazorZiur/PruebaBlazorZiur.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
USER app

ENTRYPOINT ["dotnet", "PruebaBlazorZiur.dll"]
