# Aula 3D - Visualizador de Objetos 3D

Este projeto é um visualizador de objetos 3D desenvolvido na Godot Engine utilizando C#. Ele permite carregar modelos 3D em tempo de execução, manipulá-los (escala, translação, rotação) e aplicar shaders de corte (clipping) através de uma interface de usuário simples.

## Funcionalidades
- **Carregamento de Modelos:** Suporte em runtime para arquivos `.gltf` e `.glb`.
- **Manipulação:** Rotacione, posicione e escale o modelo selecionado utilizando os sliders da UI.
- **Shader de Corte (Clipping):** Remova seções do modelo 3D em diferentes eixos diretamente pelo slider de corte de shader.
- **Integração C#:** Todo o gerenciamento da lógica principal, incluindo a UI e transformações de eixo, usa scripts em C#.

## Pré-requisitos
Para compilar e rodar este projeto, você precisará de:
1. **Godot Engine:** Qualquer versão da Godot 4 (recomendado 4.2 ou superior) **com suporte ao .NET (Mono C#)**.
2. **.NET SDK:** Versão 8.0 ou superior para o código C#. (Certifique-se de que o SDK está no PATH do seu sistema).

## Como Fazer Build e Rodar

Você pode executar o projeto de duas formas: diretamente pelo editor ou utilizando a linha de comando.

### Pelo Editor Godot
1. Abra o **Godot Engine (versão .NET)**.
2. Clique em `Importar` e selecione o arquivo `project.godot` contido na pasta raiz deste projeto (`/home/marcos/Projects/aula-3d/project.godot`).
3. Com o projeto aberto, clique no ícone **Build** (ícone de martelo no canto superior direito) para compilar os scripts do .NET.
4. Após o build ter sucesso, clique no ícone de Play (ou pressione `F5`) para rodar o projeto.

### Pela Linha de Comando (Linux/macOS/Windows)
1. Navegue até o diretório do seu projeto:
   ```bash
   cd /caminho/para/seu/projeto/aula-3d
   ```

2. Se você baixou apenas o código-fonte, instancie os pacotes .NET (restore) e faça o build do C#:
   ```bash
   dotnet build
   ```

3. Para rodar o projeto, execute o binário do Godot na mesma pasta do arquivo `project.godot`. A execução pode variar de acordo com o seu sistema operacional:
   - **Linux:**
     ```bash
     godot-mono --path .
     # (O comando 'godot-mono' pode depender do nome real do executável do Godot no seu PATH, ex: 'godot' ou './Godot_v4.x.mono_linux_x86_64')
     ```
   - **Windows:**
     ```bash
     Godot.exe --path .
     ```
   - **macOS:**
     ```bash
     /Applications/Godot.app/Contents/MacOS/Godot --path .
     ```

## Estrutura de Pastas
- `src/Core/`: Scripts principais de manipulação como `GameManager.cs` e `ModelManager.cs`.
- `src/UI/`: Scripts responsáveis pela interação de interface como o `UIManager.cs`.
- `src/Shaders/`: Contém regras específicas de renderização customizada na GPU, como o `ClippingShader`.
- `Main.tscn`: Cena principal do app, configurada com a estrutura UI-World.
