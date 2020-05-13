FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /app
COPY ./src/*.fsproj ./src/
COPY *.* ./

# https://stackoverflow.com/questions/61167032/error-netsdk1064-package-dnsclient-1-2-0-was-not-found
# appears latest 3.1 is broken, 18-04-2020
RUN dotnet restore

COPY ./src/. ./src/

WORKDIR /app/src/
RUN dotnet publish -o ../out -c Release

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime

WORKDIR /app
COPY --from=build /app/out/*.* ./

ENTRYPOINT [ "dotnet", "Foto.dll" ]