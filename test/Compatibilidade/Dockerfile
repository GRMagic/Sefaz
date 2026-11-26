# Testa o pacote rmdev.sefaz num projeto consumidor limpo, isolado da máquina local:
# o container tem um único SDK/runtime e o cache do NuGet começa vazio.
# Para rodar todos os cenários, use testar.ps1 (o contexto do build é a raiz do repositório).

ARG SDK_CONSUMIDOR=10.0

# Etapa 1: gera o .nupkg a partir do código atual
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS pacote
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
WORKDIR /src
COPY README.md ./
COPY src/Sefaz.Core/ src/Sefaz.Core/
# O projeto usa GeneratePackageOnBuild, então o build (como no CI) já gera o .nupkg; dotnet pack falharia com NU5026
RUN dotnet build src/Sefaz.Core/Sefaz.Core.csproj --configuration Release -p:PackageOutputPath=/pacotes

# Etapa 2: consome o pacote com o SDK/runtime do cenário e, opcionalmente, com versões próprias das dependências
FROM mcr.microsoft.com/dotnet/sdk:${SDK_CONSUMIDOR} AS consumidor
ARG TFM=net10.0
ARG PACOTES_EXTRAS=""
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
WORKDIR /consumidor
COPY --from=pacote /pacotes /pacotes
COPY test/Compatibilidade/Consumidor/ ./
RUN sed -i "s|<TargetFramework>.*</TargetFramework>|<TargetFramework>${TFM}</TargetFramework>|" Consumidor.csproj
RUN for p in ${PACOTES_EXTRAS}; do dotnet add Consumidor.csproj package "${p%@*}" --version "${p#*@}" --no-restore || exit 1; done
# Avisos viram erro: conflito de versão (NU1605/NU1608), vulnerabilidade conhecida (NU190x) ou framework sem suporte
RUN dotnet build --configuration Release -warnaserror
RUN dotnet run --configuration Release --no-build
