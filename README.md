# TECMUL 2025/2026 — Trabalho Prático 1

## Grupo

| Nome | Número |
|------|--------|
| Artur Salgado | EI33385 |
| Tiago Costa | EI33379 |

---

## Tema: First Person Shooter (FPS)

Jogo em primeira pessoa ambientado num cenário pós-apocalíptico. O jogador explora zonas infetadas, elimina zombies, recolhe suprimentos e tenta chegar à zona de extração. O jogo termina quando a vida chega a zero (Game Over) ou quando o jogador extrai com sucesso (Vitória).

---

## Versão do Unity

**Unity 6000.3.9f1** (versão pedida no enunciado)

---

## Descrição do jogo e funcionalidades implementadas

- Movimento em primeira pessoa: andar, correr, agachar e saltar (CharacterController)
- Sistema de disparo com raycasting, spread dinâmico e tracer visual
- Recuo da arma animado e modelo procedural gerado por código
- Inimigos zombie com IA (estados: Patrol, Alert, Chase, Attack, Stunned)
- 5 variantes de zombie com perfis distintos: Walker, Runner, Tank, Screamer, Crawler
- Hit zones com dano multiplicado na cabeça (headshot)
- Sistema de vida do jogador com indicador de dano no ecrã
- Regeneração de vida opcional (configurável no Inspector)
- Sistema de inventário com peso máximo (munições, medkits, comida, sucata, combustível, chaves)
- Uso de medkit com a tecla H
- Recarregamento com reserva de munições
- Sistema de pontuação e HUD completo (vida, munições, score, objetivos, suprimentos)
- Mensagens de feedback no ecrã (headshot, zona limpa, loot raro, etc.)
- Loot containers interativos por zona
- Zonas definidas com objetivos dinâmicos por zona
- Sistema de extração com condição de vitória
- Ecrã de Game Over e de Vitória com pontuação final e tempo de jogo
- Reinício da cena após Game Over
- Spawn dinâmico de waves de zombies (opcional, configurável)
- Mira (crosshair) procedural com flash vermelho ao acertar


---

## Controlos

| Ação | Tecla / Input |
|------|--------------|
| Mover | `W` `A` `S` `D` |
| Olhar / Mirar | Rato |
| Correr | `Shift` (segurar) |
| Agachar | `C` (segurar) |
| Saltar | `Espaço` |
| Disparar | `Botão esquerdo do rato` |
| Recarregar | `R` |
| Usar Medkit | `H` |
| Interagir (loot / extração) | `E` |
| Sair | `Escape` |

**Objetivo:** Eliminar os zombies em cada zona, recolher suprimentos e chegar à zona de extração para vencer.

---

## Como abrir o projeto

1. Instalar o **Unity Hub** e a versão **6000.3.9f1** do Unity Editor
2. No Unity Hub, clicar em **"Add"** → selecionar a pasta `TECMUL` (raiz do repositório)
3. Abrir o projeto
4. No painel **Project**, navegar até `Assets/Scenes/`
5. Abrir `Assets/Mapa_EXT01.unity` (mapa open world principal)
6. Clicar em **Play** para correr o jogo

---

## Estrutura de scripts

```
Assets/Scripts/
├── Player/
│   ├── PlayerMovement.cs       — Movimento, sprint, agachamento, salto (CharacterController)
│   ├── PlayerHealth.cs         — Vida, dano, morte, regeneração opcional
│   ├── PlayerInventory.cs      — Inventário com peso (munições, medkits, comida, etc.)
│   ├── PlayerInteractor.cs     — Interação com objetos IInteractable (E)
│   └── Shooting.cs             — Disparo, spread, recarga, tracer, modelo de arma
├── Enemy/
│   ├── ZombieAI.cs             — IA com estados: Patrol, Alert, Chase, Attack, Stunned
│   ├── ZombieHealth.cs         — Vida, dano, morte, barra de vida 3D, visual procedural
│   └── ZombieHitZone.cs        — Hit zones com multiplicador (headshot x2.5)
├── Managers/
│   ├── GameManager.cs          — Score, waves, zonas, objetivos, extração, Game Over
│   └── UIManager.cs            — HUD: vida, munições, score, wave, inventário, objetivos
├── UI/
│   ├── GameOverScreen.cs       — Ecrã de Game Over e Vitória com botões Restart / Quit
│   └── Crosshair.cs            — Mira procedural com flash ao acertar
├── World/
│   ├── LootContainer.cs        — Supply caches interativos por zona
│   ├── ExtractionZone.cs       — Zona de extração (IInteractable)
│   ├── WinCondition.cs         — Trigger de vitória com verificação de suprimentos
│   ├── ZoneTrigger.cs          — Deteção de entrada em zona
│   └── ObjectiveInteractable.cs — Objetivos interativos na cena
├── IInteractable.cs            — Interface para objetos interagíveis
├── SceneBootstrapper.cs        — Legado (desativado)
└── PrototypeSceneMarker.cs     — Marcador para o gerador de cena (Editor only)
```


---

## Assets multimédia

| Tipo | Ficheiro(s) | Formato | Resolução / Bitrate | Justificação |
|------|-------------|---------|---------------------|--------------|
| Texturas de ambiente | Chão, paredes, céu | PNG | 1024×1024 | Boa qualidade visual sem excesso de memória no contexto FPS |
| Sons — disparo | gunshot.wav | WAV | 44.1 kHz, 16-bit, mono | Latência mínima para feedback imediato de disparo |
| Sons — dano jogador | player_hurt.wav | WAV | 44.1 kHz, 16-bit, mono | Resposta sonora rápida ao receber dano |
| Sons — morte zombie | zombie_death.wav | WAV | 44.1 kHz, 16-bit, mono | Confirmação auditiva de kill |
| Sons — zombie (dano) | zombie_hurt.wav | WAV | 44.1 kHz, 16-bit, mono | Feedback de acerto no inimigo |
| Música de fundo | bgm_main.mp3 | MP3 | 128–192 kbps, stereo | Boa qualidade com ficheiro compacto para loop contínuo |

---

## Fluxo de trabalho Git — branches

```
main  ──────────────────────────────────── (versão estável)
         ↑                        ↑
feature/artur ── commits ── merge
feature/tiago ── commits ── merge
```

**Criar a tua branch (uma vez):**
```bash
git checkout -b feature/artur
```

**Guardar e enviar trabalho:**
```bash
git add -A
git commit -m "feat: descrição do que foi feito"
git push origin feature/artur
```

**Juntar ao main quando pronto:**
```bash
git checkout main
git pull origin main
git merge feature/artur
git push origin main
```

---

## Entrega

- **Data:** 24 de abril de 2026
- **Tag de entrega:** `1.0`
- **Repositório:** [https://github.com/artursalgado/TECMUL_2026](https://github.com/artursalgado/TECMUL_2026)

```bash
git tag 1.0
git push origin 1.0
```
