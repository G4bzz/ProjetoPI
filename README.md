# Trabalho prático GenericStuff 
### Bacharelado em Ciência da Computação - UFS
### Autor
- Gabriel de Oliveira Santos
	- gabriel.santos1@dcomp.ufs.br

### Professora
- Beatriz Trinchão Andrade

## Instruções sobre como compilar e executar o projeto
Para compilar e executar o projeto será necessário ter em mãos a pasta do projeto e instalar o SDK do DotNet, pois o mesmo foi feito em C#.


**Obs1.:** se atentar à versão do SDK a ser instalada, é imprescindível que seja instalada a versão 6.0, pois o projeto foi feito em cima dela.

**Obs2.:** para instalar em outras versões do Ubuntu, basta substituir o primeiro comando por algum destes:

- 22.04 (LTS): o repositório da MS já consta no sistema, basta executar o último comando;
- 20.04 (LTS): wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
- 18.04 (LTS): wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
- 16.04 (LTS): wget https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb


Para instalar o SDK do DotNet, basta executar os seguintes comandos no terminal:
```

$ wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb

$ sudo dpkg -i packages-microsoft-prod.deb

$ rm packages-microsoft-prod.deb

$ sudo apt-get update \&\& sudo apt-get install -y dotnet-sdk-6.0

```

Depois de instalar o SDK, ainda no cmd, navegue até o diretório raiz do projeto e siga as seguintes etapas, executando os comandos listados:

- compile o projeto: ```dotnet build```
- executando o projeto (modo interativo): ```dotnet run```

Também é possível executar o projeto via linha de comando e passar dois parâmetros de execução:
- para passar o diretório da imagem: ```-path DIRETORIO\_DA\_IMAGEM```
- para ativar o modo debug: ```-debugMode```
- exemplo de execução com os parâmetros (estando com o terminal aberto na raiz do projeto): ```$ bin/Debug/net6.0/ProjetoPI -debugMode -path ./exemplo1.pbm```


**Obs.:** o modo debug além de printar o resultado de algumas operações no console, exporta esses resultados para o diretório de onde você executou o projeto. Sendo assim, caso o debugMode seja usado, é interessante acessar este diretório, conferir esses resultados e depois apagá-los para não provocar confusão caso o comando seja executado com outra imagem.