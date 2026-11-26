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

Na versão 1.5:
- Os pacotes `System.ServiceModel.Duplex`, `System.ServiceModel.NetTcp` e `System.ServiceModel.Security` deixaram de ser dependências, porque a biblioteca não os usa. Se o seu projeto usa tipos desses pacotes, adicione uma referência direta a eles.
- A versão mínima do `System.Security.Cryptography.Xml` passou a ser a 8.0.5, a primeira versão corrigida entre as linhas que ainda têm suporte. Se o seu projeto referencia uma versão menor diretamente, o build vai acusar NU1605 até você atualizá-la.
- Se o seu projeto usa WCF 6 ou mais novo (`System.ServiceModel.Primitives`, `NetTcp` etc.), referencie também o `System.ServiceModel.Http` na mesma versão. Sem isso, os pacotes WCF novos se misturam com os 4.x usados por esta biblioteca e as chamadas falham em tempo de execução.