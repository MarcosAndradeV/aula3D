# Instruções Globais e Arquitetura Detalhada - Aula3D

Este documento detalha o que compõe o projeto **Aula3D**, explicando a lógica, a arquitetura e as decisões matemáticas de cada módulo. A intenção é documentar a fundo as implementações do rastreamento de mão, a integração com OpenCV, e a manipulação 3D (Quaternions e Godot Physics Loop).

## 1. Visão Geral da Arquitetura

O projeto adota uma arquitetura fracamente acoplada ("Decoupled Architecture") entre os sistemas de visão computacional e o motor 3D. 

- O **Motor 3D (Godot, via `Aula3D.App`)** atua unicamente como consumidor de um estado. Ele não processa imagens da câmera, apenas recebe *posições (X, Y, Z)* e um estado da mão (*Aberta/Fechada*).
- O **Módulo de Visão (`Aula3D.VisionCore`)** processa as imagens da webcam em tempo real em uma Background Thread, isolando a performance computacional intensiva (OpenCV) da thread principal da Godot, prevenindo quedas bruscas de Frame Rate.
- O **Módulo de Console (`Aula3D.VisionConsole`)** é um utilitário CLI para desenvolvimento. Ele permite usar a câmera nativamente e visualizar as imagens modificadas através de janelas do SO para debugar a eficácia da iluminação do ambiente.

### Tolerância à Multiplataforma e Dependências Nativas (OS)
O projeto depende de binários densos em C++ (`OpenCvSharpExtern`). Para assegurar que rodem sem falhas (como `DllNotFoundException`) em sistemas diversificados:
- Os arquivos `.csproj` fixam os runtimes para Windows e Ubuntu. Para usuários fora do ecossistema Ubuntu (ex. Fedora ou macOS), vale a regra de compatibilidade natural da GLIBC e .NET.
- A fachada `GestorDeVisaoFacade` integra um `NativeLibrary.SetDllImportResolver` equipado com condicional de OS (`RuntimeInformation.IsOSPlatform`). Isso força o .NET a procurar por `libOpenCvSharpExtern.so` em distribuições Linux, mas varreduras em computadores de colegas utilizando Windows tentarão extrair a `OpenCvSharpExtern.dll`, sanando problemas com o Godot.

A comunicação entre a GUI (Godot) e a Visão Computacional ocorre através da interface `IGestureProvider`. Este contrato define os pontos de acesso aos dados, de forma que o script Godot pode aceitar perfeitamente o adaptador de mouse (`MouseMock.cs`) caso não haja necessidade de câmera.

---

## 2. Aula3D.App (O Motor Godot)

Este pacote contém o código de cena principal (`Main.tscn`) da aplicação. 

### a. `Objeto3D.cs`

É talvez a classe mais crucial em Godot para a lógica de manipulação. Ele se conecta através da injeção do provider (`IGestureProvider`) no `_Ready()`.

A cada update (no `_Process(double delta)`):
1. **Leitura**: Busca a posição `(X, Y)` mapeada em relação à tela e o estado da mão (Aberto/Fechado). 

2. **Mão Aberta (Rotação Livre com Quaternions)**:
   A movimentação da mão num espaço 2D `(Delta X e Delta Y)` é transposta para um eixo 3D.
   Usar as Transformações de Euler tradicionais `(X, Y, Z degrees)` resulta no **Gimbal Lock** (a perda de graus de liberdade ao rotacionar 90 graus), que causa comportamentos irrealistas do modelo. Portanto, a modelagem foi feita baseada na matemática de **Quaternions**, criando quaternions locais baseados nos vetores normais da câmera (`Vector3.Up` e `Vector3.Right`) multiplicados pela diferença de velocidade da mão. Esta abordagem une os eixos, permitindo uma rotação esférica e fluida de 360 graus do objeto instanciado na cena.

3. **Mão Fechada (Translação/Scala)**:
   Calcula-se o delta do espaço (distância e origem dos eixos da tela) para empurrar as coordenadas em direção a `Transform.Origin`, além de escalar modelos dependendo do eixo `Z` rastreado na câmera baseada na área da caixa delimitadora do contorno da mão.

### b. `GameManager.cs` e Interface

O gerenciador orquestra as interações nos botões baseados no "Painel Acadêmico". Quando um botão de UI carrega um arquivo GLB/GLTF local, o `GameManager` notifica o manipulador (`Objeto3D`) para destruir as instâncias anteriores e anexar os novos vértices processados dinamicamente na árvore Godot, enquanto salva e recarrega os mesmos Shaders.

### c. `ClippingShader.gdshader`

No carregamento procedural, o .NET varre cada MeshInstance e Material no modelo submetido e sobrepõe um material secundário (Shader Material puro). Um *clipping plane* numéricos em eixos (`X`, `Y`, `Z`) é despachado via parâmetros `SetShaderParameter`. Modelos interceptados pelas coordenadas de recorte descartam a renderização daqueles vértices `(discard;)`, expondo seu interior vazio aos usuários da aula.

---

## 3. Aula3D.VisionCore (Processamento Digital de Imagem)

O core de rastreamento abstrai as classes pesadas de OpenCV e lida inteiramente com C#.

### a. `GestorDeVisaoFacade.cs`

Implementa a thread secundária. A classe instancia o stream da câmera e aplica um loop infinito `while (IsRunning)`. O processamento é delegado aos vários Steps ("Etapas"). Se as operações custarem `30ms` de GPU/CPU, a Godot (que requer frames a cada `16ms`) continuará performando lida a variável atômica exposta publicamente. Adicionalmente, possui a injeção nativa de compatibilidade `.so`/`.dll` para sistemas não-Ubuntu.

### b. Pipeline de Processamento de Imagem (`FiltroEspacial.cs`)

O script `FiltroEspacial.cs` toma o Frame BGR gerado e aplica rigorosamente o clássico fluxo PDI:
1. **Filtro Bilateral**: Remove a textura microscópica da pele e manchas, mas segura geometricamente as arestas principais. Evita distorções para o cálculo de borda.
2. **CLAHE (Equalização de Histograma Local)**: Extrai o canal de contraste do padrão HSV (o Brilho) para tratar iluminação desigual entre os dois lados da mão.
3. **Máscara Binária (`inRange`) & Morfologia**: Peneira pixels entre um `LowerBound` e `UpperBound` e fecha os eventuais buracos contidos na máscara matemática com Dilação.
4. **Distance Transform (Separação do Pulso)**: Remove as raízes estendidas do braço limitando-se ao centro do raio perfeito da palma.
5. **Filtro Frequencial (FFT 2D) Passa-Baixa**: Extração da imagem de alta frequência do espectro complexo minimiza o ruído "granulado" captado em webcams por falta de sensores caros.
6. **Bordas de Canny**: As bordas reais são computadas do Filtro de Frequência, limitando precisamente onde o HSV deve focar através da intersecção.

### c. `ClassificadorDeGestos.cs` - (O Cálculo e Detecção do "Fechado")

Encontrar a pose baseia-se num sistema geométrico sem Redes Neurais. Ele detecta qual o maior contorno de uma imagem binária. 
1. **Casco Convexo (Convex Hull)**: Uma espécie de polígono que abraça externamente o contorno.
2. **Defeitos de Convexidade**: Analisa a depressão geométrica gerada pelos dedos (o espaço entre as pontas de cada dedo). O algoritmo cruza cada ponta (ponto inicial e final do buraco) com a cavidade mais profunda encontrada.
3. **Lei dos Cossenos na Detecção de Ângulo**: Ao recuperar os comprimentos do triângulo imaginário das pontas dos dedos à ponta do buraco (a teia digital da mão), a Lei dos Cossenos `cos(alpha) = (b^2 + c^2 - a^2) / 2bc` deduz o ângulo do espaço de cada dedo (se o ângulo é estreito, na casa dos ~90, o dedo é considerado como "aberto", criando os defeitos de convexidade lógicos).
Ao testar a quantidade desses dedos "abertos" presentes no polígono da caixa contra sua proporção geral, se tem a inferência binária de ABERTA vs FECHADA.

### d. `ExtratorHu.cs` (Para Expansão Futura)

Usa **Momentos Invariantes de Hu**. Baseados na variância de intensidade da escala de cinza e a matriz da área projetada de uma forma sólida. Produz um vetor (array) de 7 números (Momentos Hu) resistentes a transformações da imagem (como tamanho e rotação dos dedos fechados). O intuito da classe é armazenar o dataset de características sem precisão algorítmica geométrica para permitir a inserção de *Machine Learning* nas fases posteriores do sistema.

