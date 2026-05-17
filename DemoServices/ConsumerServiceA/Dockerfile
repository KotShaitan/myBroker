FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY MyBroker.Sdk/MyBroker.Sdk.csproj MyBroker.Sdk/
COPY DemoServices/ConsumerServiceA/ConsumerServiceA.csproj DemoServices/ConsumerServiceA/
RUN dotnet restore DemoServices/ConsumerServiceA/ConsumerServiceA.csproj

COPY MyBroker.Sdk/ MyBroker.Sdk/
COPY DemoServices/ConsumerServiceA/ DemoServices/ConsumerServiceA/
RUN dotnet publish DemoServices/ConsumerServiceA/ConsumerServiceA.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ConsumerServiceA.dll"]
