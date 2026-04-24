# 🧟 KILL THEM ALL — TECMUL 2026

> **First-Person Shooter de Sobrevivência em Mundo Aberto**  
> Desenvolvido em Unity 6 para a unidade curricular TECMUL 2025/2026

---

## 👥 Grupo 05 — Tema A

| Nome | Número | 
|------|--------|
| Artur Salgado | EI33385 |
| Tiago Costa | EI33379 | 

**Repositório:** https://github.com/artursalgado/TECMUL_2026  
**Branch de entrega:** `final`  
**Tag de entrega:** `1.0`  
**Data de entrega:** 24 de abril de 2026

---

## 🎮 Descrição do Jogo

*Kill Them All* é um FPS de sobrevivência num ambiente pós-apocalíptico em mundo aberto. O jogador acorda numa zona infestada de zombies e tem de explorar 5 zonas distintas, cumprir os seus objetivos, recolher suprimentos e sobreviver a vagas progressivas de inimigos até conseguir ativar a zona de extracção e escapar.

O jogo termina de duas formas:
- 💀 **Game Over** — a vida do jogador chega a zero
- 🏆 **Vitória** — o jogador ativa a zona de extracção com sucesso

---

## 🔧 Requisitos Técnicos

| Componente | Versão / Especificação |
|------------|----------------------|
| Unity Editor | **6000.3.9f1** |
| Render Pipeline | Universal Render Pipeline (URP) |
| Pacotes Unity | AI Navigation, TextMeshPro, Input System |
| Plataforma alvo | PC (Windows) |
| Resolução recomendada | 1920×1080 |

---

## 🚀 Como Abrir o Projeto

1. Instalar o **Unity Hub** e o Unity Editor versão **6000.3.9f1**
2. Clonar o repositório:
   ```bash
   git clone https://github.com/artursalgado/TECMUL_2026.git
   git checkout final
   ```
3. No Unity Hub → **"Add"** → selecionar a pasta `TECMUL_2026`
4. Abrir o projeto (a primeira abertura pode demorar alguns minutos a importar)
5. Carregar em **Play**



---

## ✅ Funcionalidades Implementadas

### 🏃 Jogador
- Movimento FPS fluído com `CharacterController`: andar, correr, agachar, saltar
- Camera bob durante o movimento (efeito procedural)
- Sistema de `SnapToGround` para alinhamento ao terreno irregular
- Sensibilidade do rato e field of view configuráveis no menu
- Cursor bloqueado durante o jogo, libertado nos menus

### ⚔️ Sistema de Combate
- Disparo com `SphereCast` (Hitscan) — altamente optimizado para FPS
- Spread dinâmico: a precisão diminui ao correr ou saltar
- Recuo da arma animado proceduralmente (weapon kick)
- Tracer visual do projéctil
- Recarga com reserva de munições (`R`)
- **Machado** — ataque corpo-a-corpo, dano elevado, sem munições (`1`)
- **Pistola** — arma de fogo, 12 balas por carregador, alcance 100u (`2`)
- Troca rápida entre armas com as teclas `1` e `2`

### ❤️ Sistema de Vida
- Barra de vida com indicador visual de dano no ecrã (flash vermelho)
- Uso de medkit com a tecla `H`
- Regeneração de vida opcional (configurável no Inspector)
- Morte com transição para ecrã de Game Over

### 🎒 Inventário
- Sistema de inventário com peso máximo (22kg por defeito)
- Tipos de recursos: Munições, Medkits, Comida, Sucata, Combustível, Chaves
- Interacção com loot containers no mundo (`E`)
- Contador de suprimentos no HUD

### 🧟 Inimigos — Inteligência Artificial
- IA baseada em Máquina de Estados Finita (FSM) com `NavMeshAgent`
- 5 estados de comportamento: **Patrol → Alert → Chase → Attack → Stunned**
- **Detecção auditiva:** os zombies ouvem os tiros e investigam a origem do som, mesmo sem linha de visão direta
- **Detecção visual:** raio de detecção configurável por variante
- Sistema de patrulha autónoma quando não há jogador próximo
- Barra de vida 3D acima do zombie
- Hit zones: headshot com multiplicador ×2.5 (`ZombieHitZone`)

### 🧬 Variantes de Zombie

| Variante | Velocidade | Vida | Comportamento Especial |
|----------|-----------|------|----------------------|
| Walker | Normal | Normal | Zombie padrão |
| Runner | Alta | Baixa | Muito rápido, difícil de acertar |
| Tank | Baixa | Muito Alta | Absorve muitos tiros, dano elevado |
| Screamer | Normal | Normal | Ao detetar o jogador, alerta zombies próximos |
| Crawler | Baixa | Normal | Rasteja pelo chão, perfil muito baixo |

### 🗺️ Zonas de Missão

| Zona | Objectivo | Zombies |
|------|-----------|---------|
| Residential Block | Recuperar a chave do abrigo | 6 (Walker, Runner, Crawler, Screamer) |
| Shelter Yard | Selar a entrada do abrigo | 4 (Walker, Crawler, Runner, Screamer) |
| Clinic | Encontrar medicamentos de emergência | 5 (Screamer, Walker, Runner, Crawler) |
| Warehouse | Recuperar peças do gerador | 5 (Tank, Walker, Runner, Crawler) |
| Fuel Depot | Restaurar a alimentação da extracção | 4 (Screamer, Runner, Walker) |
| Extraction Point | Zona de extracção — condição de vitória | — |

Cada zona possui: trigger de detecção de entrada, objectivos dinâmicos, loot containers interactivos, terminal de objectivo interactível (`E`) e zombies pré-posicionados.

### 🖥️ Interface (HUD e Menus)
- **HUD** em jogo: barra de vida, munições, score, wave, zona activa, objectivos, suprimentos recolhidos
- **Menu principal** com configuração de dificuldade (Fácil / Normal / Difícil) e modo noturno
- **Menu de pausa** (`Escape`) com opções de retomar, ir ao menu ou sair
- **Ecrã de Game Over** com pontuação final, tempo de jogo e botões Restart / Menu
- **Ecrã de Vitória** com pontuação final e tempo de jogo
- Mira (crosshair) procedural com flash vermelho ao acertar num inimigo
- Mensagens de feedback no ecrã: headshot, zona limpa, loot raro, etc.

### 🌊 Sistema de Waves
- Spawn dinâmico de waves de zombies (configurável no Inspector)
- Multiplicador de dificuldade por wave (`waveMultiplier = 1.5`)
- Integrado com o `GameManager` via `autoSpawnWaves`

---

## 🕹️ Controlos

| Acção | Tecla / Input |
|------|--------------|
| Mover | `W` `A` `S` `D` |
| Olhar / Mirar | Rato |
| Correr | `Shift` (segurar) |
| Agachar | `C` (segurar) |
| Saltar | `Espaço` |
| Disparar / Atacar | Botão esquerdo do rato |
| Recarregar | `R` |
| Usar Medkit | `H` |
| Interagir (loot / extracção / objectivos) | `E` |
| Equipar Machado | `1` |
| Equipar Pistola | `2` |
| Pausar | `Escape` |

---

## 🔫 Configurar o Sistema de Armas (WeaponSystem)

Os scripts `WeaponSystem.cs`, `Axe.cs` e `Pistol.cs` já estão criados. Para configurar no Unity:

1. **Seleciona o Player** na Hierarchy → confirma que tem a Tag `"Player"`
2. **Cria os objetos das armas** — clica com botão direito na Main Camera → `Create Empty`:
   - `Objeto_Machado`
   - `Objeto_Pistola`
3. **Adiciona os scripts**:
   - No `Player` → adiciona `WeaponSystem`
   - No `Objeto_Machado` → adiciona `Axe`
   - No `Objeto_Pistola` → adiciona `Pistol`
4. **Liga as referências** no Inspector do `WeaponSystem`:
   - Campo **Machado** → arrasta `Objeto_Machado`
   - Campo **Pistola** → arrasta `Objeto_Pistola`
5. **Ajusta os valores** (sugerido): Axe: Dano=`40`, Alcance=`2.5` | Pistol: Dano=`25`, Munição Máx=`12`, Alcance=`100`

---

## 📁 Estrutura de Scripts

```
Assets/Scripts/
├── ── Jogador ──────────────────────────────────────────────────
│   ├── PlayerMovement.cs         Movimento, sprint, agachar, saltar, camera bob
│   ├── PlayerHealth.cs           Vida, dano, morte, regeneração opcional
│   ├── PlayerInventory.cs        Inventário com peso máximo e tipos de recursos
│   ├── PlayerInteractor.cs       Interacção com objetos IInteractable (E)
│   ├── Shooting.cs               Disparo, spread, recarga, tracer, weapon kick
│   ├── WeaponSystem.cs           Gestão de armas equipadas (troca 1/2)
│   ├── Axe.cs                    Ataque corpo-a-corpo com raycast
│   └── Pistol.cs                 Disparo, munição, recarga
│
├── ── Inimigos ─────────────────────────────────────────────────
│   ├── ZombieAI.cs               IA FSM: Patrol, Alert, Chase, Attack, Stunned
│   ├── ZombieHealth.cs           Vida, dano, morte, barra de vida 3D
│   └── ZombieHitZone.cs          Hit zones com multiplicador (headshot x2.5)
│
├── ── Gestores ─────────────────────────────────────────────────
│   ├── GameManager.cs            Score, waves, zonas, objectivos, Game Over
│   ├── UIManager.cs              HUD completo (vida, municoes, score, objectivos)
│   ├── AudioManager.cs           Gestão de áudio (música, SFX, volumes)
│   └── GameConfig.cs             Configuração global (dificuldade, volumes, FOV)
│
├── ── Interface ────────────────────────────────────────────────
│   ├── GameOverScreen.cs         Ecrã de Game Over e Vitória
│   ├── MainMenuManager.cs        Menu principal com configuração
│   ├── PauseMenu.cs              Menu de pausa
│   ├── UIController.cs           Controlador geral de UI
│   ├── Crosshair.cs              Mira procedural com flash ao acertar
│   └── MenuHoverScale.cs         Animação de hover nos botões
│
├── ── Mundo ────────────────────────────────────────────────────
│   ├── LootContainer.cs          Supply caches interactivos por zona
│   ├── ExtractionZone.cs         Zona de extracção (IInteractable)
│   ├── WinCondition.cs           Trigger de vitória
│   ├── ZoneTrigger.cs            Detecção de entrada em zona
│   ├── ObjectiveInteractable.cs  Terminais de objectivos interactivos
│   └── OpenWorldVisualBuilder.cs Construtor visual do mundo open world
│
├── ── Bootstrap / Sistema ──────────────────────────────────────
│   ├── SceneBootstrapper.cs                Bootstrap procedural da cena
│   ├── OpenWorldBootstrap.cs               Bootstrap do mapa Open World
│   ├── RuntimePlayModeGuard.cs             Garante flow correcto no Play Mode
│   ├── RuntimeOpenWorldBootstrapGuard.cs   Garante bootstrap no mapa OW
│   ├── RuntimeCameraGuard.cs               Garante câmara activa
│   └── PrototypeSceneMarker.cs             Marcador de versão da cena
│
└── IInteractable.cs              Interface para todos os objetos interactivos
```

---

## 🎵 Assets Multimédia

| Tipo | Formato | Resolução / Bitrate | Justificação |
|------|---------|---------------------|--------------|
| Texturas de ambiente (chão, paredes, edifícios) | PNG | 1024×1024 | Boa qualidade sem excesso de memória |
| Som — disparo (`gunshot.wav`) | WAV | 44.1 kHz, 16-bit, mono | Latência mínima para feedback imediato |
| Som — dano jogador (`player_hurt.wav`) | WAV | 44.1 kHz, 16-bit, mono | Resposta sonora rápida ao receber dano |
| Som — morte zombie (`zombie_death.wav`) | WAV | 44.1 kHz, 16-bit, mono | Confirmação auditiva de kill |
| Som — zombie dano (`zombie_hurt.wav`) | WAV | 44.1 kHz, 16-bit, mono | Feedback de acerto no inimigo |
| Música de fundo (`bgm_main.mp3`) | MP3 | 128–192 kbps, stereo | Boa qualidade com ficheiro compacto para loop |
| Modelo zombie (ZombieMale_AAB) | FBX + Prefab | — | Modelo rigged com animações de andar/atacar |
| Nature Kit (Stylized Nature Kit Lite) | Prefabs + Terrain layers | — | Árvores, rochas, vegetação para o Open World |

---

## 🌿 Git — Fluxo de Trabalho

### Branches
```
main     ──── versão estável base
final    ──── branch de entrega (scripts + cenas, sem assets pesados)
```

```

## 📐 Arquitectura Técnica

### Padrões de Design Utilizados

| Padrão | Onde | Descrição |
|--------|------|-----------|
| **Singleton** | `GameManager`, `UIManager`, `AudioManager` | Instância única global acessível de qualquer script |
| **FSM** | `ZombieAI` | Máquina de estados finita para comportamento de IA |
| **SRP** | Todos os scripts | Cada script tem uma única responsabilidade |
| **Observer** | `GameManager` ↔ `UIManager` | GameManager notifica o HUD de alterações de estado |
| **Strategy** | `WeaponSystem` + `Axe`/`Pistol` | Armas intercambiáveis com interface comum |

### Sistemas Técnicos Chave
- **NavMeshAgent** — navegação autónoma dos zombies com contorno de obstáculos
- **CharacterController** — movimento do jogador sem física rígida, mais controlável
- **Physics.SphereCast** — detecção de impacto de disparo com tolerância configurável
- **RuntimeInitializeOnLoadMethod** — bootstrap automático da cena sem depender de GameObjects
- **TextMeshPro** — todo o texto do HUD e menus
- **DontDestroyOnLoad** — `GameManager` persiste entre cenas

---

##  Configuração de Dificuldade

| Parâmetro | Fácil | Normal | Difícil |
|-----------|-------|--------|---------|
| Vida do zombie | ×0.60 | ×1.00 | ×1.65 |
| Dano do zombie | ×0.50 | ×1.00 | ×2.00 |
| Vida do jogador | ×1.50 | ×1.00 | ×0.75 |

---

