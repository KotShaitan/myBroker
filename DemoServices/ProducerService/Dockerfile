FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY MyBroker.Sdk/MyBroker.Sdk.csproj MyBroker.Sdk/
COPY DemoServices/ProducerService/ProducerService.csproj DemoServices/ProducerService/
RUN dotnet restore DemoServices/ProducerService/ProducerService.csproj

COPY MyBroker.Sdk/ MyBroker.Sdk/
COPY DemoServices/ProducerService/ DemoServices/ProducerService/
RUN dotnet publish DemoServices/ProducerService/ProducerService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ProducerService.dll"]
