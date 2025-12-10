FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
RUN mkdir -p /data

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/out

FROM base AS final
WORKDIR /app
COPY --from=build /app/out ./
ENV DB_PATH=/data/storing.db
EXPOSE 5001
ENTRYPOINT ["dotnet", "FileStoring.dll"]
