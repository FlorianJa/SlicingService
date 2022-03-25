#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM florianjasche/prusaslicerindocker AS base
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN rm packages-microsoft-prod.deb

RUN apt-get update
RUN apt-get install -y apt-transport-https
RUN apt-get update
RUN apt-get install -y aspnetcore-runtime-6.0
RUN apt-get install -y dotnet-runtime-6.0
RUN apt-get install -y dotnet-sdk-6.0

WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SlicingServiceAPI/SlicingServiceAPI.csproj", "SlicingServiceAPI/"]
COPY ["SlicingServiceCommon/SlicingServiceCommon.csproj", "SlicingServiceCommon/"]
COPY ["SlicerCLIWrapper/SlicerCLIWrapper.csproj", "SlicerCLIWrapper/"]
RUN dotnet restore "SlicingServiceAPI/SlicingServiceAPI.csproj"
COPY . .
WORKDIR "/src/SlicingServiceAPI"
RUN dotnet build "SlicingServiceAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SlicingServiceAPI.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app
#RUN dotnet dev-certs https
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SlicingServiceAPI.dll"]
