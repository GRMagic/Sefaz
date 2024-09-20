# Sefaz
Biblioteca para download de notas fiscais e conhecimentos de transportes

| Pacote |  Versão | Downloads |
| ------- | ----- | ----- |
| `rmdev.sefaz` | [![NuGet](https://img.shields.io/nuget/v/rmdev.sefaz.svg)](https://nuget.org/packages/rmdev.sefaz) | [![Nuget](https://img.shields.io/nuget/dt/rmdev.sefaz.svg)](https://nuget.org/packages/rmdev.sefaz) |

## Exemplos:
### Usar sefaz de produção
```csharp
using var sefaz = new Sefaz.Core.Sefaz(caminhoCertificado, senhaCertificado);
```

### Fazer um manifesto
```csharp
await sefaz.ManifestarNFeAsync(cnpj, chaveNFe, Core.Models.NFe.TEventoInfEventoDetEventoDescEvento.CienciaDaOperacao);
```

### Baixar uma nota fiscal pela chave nfe (com valor fiscal)
```csharp
var doc = await sefaz.BaixarNFeAsync(cUF, cnpj, chaveNFe);
doc.SalvarArquivo("arquivo.xml");
```

### Baixar notas apenas pelo CNPJ do interessado
```charp
var documentos = await sefaz.ConsultarNFeCNPJAsync(cUF, cnpj);
```
Mais exemplos em https://github.com/GRMagic/Sefaz/blob/main/src/Sefaz.Exemplo/Program.cs

## Notas Importantes de Versão

Na versão 1.4 desse pacote o campo NSU do Documento retornado pela Sefaz se tornou nullable!