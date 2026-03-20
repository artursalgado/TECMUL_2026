# TECMUL 2025/2026 — Trabalho Prático 1

## Grupo

| Nome | Número |
|------|--------|
| Artur Salgado | EI33385 |
| Tiago Costa | EI33379 |

---

## Tema: First Person Shooter (FPS)

Jogo em primeira pessoa onde o jogador explora um ambiente 3D, dispara sobre alvos/inimigos, gere a sua vida e tenta sobreviver o máximo de tempo possível. O jogo termina quando a vida chega a zero (Game Over) e o jogador pode reiniciar a partida.

---

## Versão do Unity

**Unity 6000.3.9f1** (versão pedida no enunciado)

---

## Descrição do jogo e funcionalidades implementadas

- Movimento do jogador em primeira pessoa (WASD + rato para olhar)
- Sistema de disparo com raycasting ou projéteis
- Inimigos/alvos com deteção de colisões por tags (`"Enemy"`, `"Pickup"`, `"Obstacle"`)
- Sistema de vida e dano — Game Over ao chegar a 0 HP
- Pontuação visível em HUD (UI)
- Condição de reinício — recarregar a cena ao morrer
- Câmera em primeira pessoa integrada no jogador
- Física com Rigidbody e Collider onde aplicável
- Lógica de física em `FixedUpdate`

---

## Jogabilidade

| Ação | Tecla/Input |
|------|-------------|
| Mover | `W` `A` `S` `D` |
| Olhar / Mirar | Rato |
| Disparar | `Botão esquerdo do rato` |
| Reiniciar (Game Over) | `R` ou botão no ecrã |

**Objetivo:** Eliminar inimigos/alvos e sobreviver o máximo de tempo possível sem perder toda a vida.

---

## Como abrir o projeto

1. Instalar o **Unity Hub** e a versão **6000.3.9f1** do Unity Editor
2. No Unity Hub, clicar em **"Add"** → selecionar a pasta `TECMUL` (raiz do repositório)
3. Abrir o projeto
4. No painel **Project**, navegar até `Assets/Scenes/` e abrir `SampleScene`
5. Clicar em **Play** para correr o jogo

---

## Estrutura de scripts (organização)

```
Assets/
├── Scripts/
│   ├── PlayerMovement.cs       — Movimento e input do jogador (Rigidbody)
│   ├── PlayerShooting.cs       — Disparo e interação com alvos
│   ├── CameraController.cs     — Câmera em primeira pessoa
│   ├── EnemyController.cs      — Comportamento dos inimigos
│   ├── CollisionHandler.cs     — Deteção de colisões com tags
│   └── GameManager.cs          — Game Over, pontuação, reinício
└── Scenes/
    └── SampleScene.unity
```

---

## Assets multimédia

> *(A preencher à medida que forem adicionados)*

| Tipo | Formato | Resolução / Bitrate | Justificação |
|------|---------|---------------------|--------------|
| Texturas | PNG / JPG | 512×512 ou 1024×1024 | Boa qualidade sem excesso de memória para o contexto FPS |
| Sons — efeitos | WAV / OGG | 44.1 kHz, 16-bit | Latência baixa para efeitos sonoros em tempo real |
| Sons — música | MP3 / OGG | 128–192 kbps | Boa qualidade com tamanho de ficheiro reduzido |

---

## Fluxo de trabalho Git — branches

Para evitar conflitos, cada elemento do grupo trabalha na sua própria branch:

```
main  ──────────────────────────────────── (versão estável)
         ↑                        ↑
feature/artur ── commits ── merge
feature/tiago ── commits ── merge
```

### Comandos a usar

**Criar a tua branch (uma vez):**
```bash
git checkout -b feature/artur
# ou
git checkout -b feature/tiago
```

**Guardar e enviar trabalho:**
```bash
git add -A
git commit -m "Descrição clara do que foi feito"
git push origin feature/artur
```

**Juntar ao main quando o trabalho estiver pronto:**
```bash
git checkout main
git pull origin main
git merge feature/artur
git push origin main
```

**Buscar as alterações do colega:**
```bash
git checkout feature/tiago
git pull origin main
```

---

## Entrega

- **Data:** 24 de abril de 2026
- **Tag de entrega:** `1.0`
- **Repositório:** [https://github.com/artursalgado/TECMUL_2026](https://github.com/artursalgado/TECMUL_2026)

Para criar e fazer push da tag de entrega:
```bash
git tag 1.0
git push origin 1.0
```

---

## Observações

- O projeto encontra-se em desenvolvimento — funcionalidades serão adicionadas progressivamente.
- Quaisquer lacunas ou decisões de design serão documentadas aqui ao longo do desenvolvimento.
