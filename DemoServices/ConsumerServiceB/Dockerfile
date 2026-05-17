FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY MyBroker.Sdk/MyBroker.Sdk.csproj MyBroker.Sdk/
COPY DemoServices/ConsumerServiceB/ConsumerServiceB.csproj DemoServices/ConsumerServiceB/
RUN dotnet restore DemoServices/ConsumerServiceB/ConsumerServiceB.csproj

COPY MyBroker.Sdk/ MyBroker.Sdk/
COPY DemoServices/ConsumerServiceB/ DemoServices/ConsumerServiceB/
RUN dotnet publish DemoServices/ConsumerServiceB/ConsumerServiceB.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ConsumerServiceB.dll"]
