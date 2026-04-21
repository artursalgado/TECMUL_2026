using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class ZombieAI : MonoBehaviour
{
    public enum ZombieState
    {
        Patrol,
        Alert,
        Chase,
        Attack,
        Stunned
    }

    public enum ZombieVariant
    {
        Walker,
        Runner,
        Tank,
        Screamer,
        Crawler
    }

    [Header("Identity")]
    public ZombieVariant variant = ZombieVariant.Walker;

    [Header("Chase")]
    [FormerlySerializedAs("velocidade")]
    public float moveSpeed = 3f;

    [FormerlySerializedAs("velocidadeAtaque")]
    public float attackMoveSpeed = 1.5f;

    [FormerlySerializedAs("distanciaAtaque")]
    public float attackDistance = 2f;

    [FormerlySerializedAs("distanciaDetecao")]
    public float detectionDistance = 20f;

    [Header("Attack")]
    [FormerlySerializedAs("danoAtaque")]
    public int attackDamage = 10;

    [FormerlySerializedAs("cadenciaAtaque")]
    public float attackRate = 1.5f;

    [Header("Animation")]
    public Animator animator;

    private NavMeshAgent agent;
    private Transform player;
    private float nextAttackTime = 0f;
    private ZombieHealth health;
    private CapsuleCollider capsuleCollider;
    private float groundOffset = 0.02f;
    private float groundedY;
    private Vector3 patrolOrigin;
    private Vector3 patrolTarget;
    private float alertUntil;
    private float stunnedUntil;
    private ZombieState currentState = ZombieState.Patrol;
    private float hearingDistance = 16f;

    void Start()
    {
        attackDamage = Mathf.RoundToInt(attackDamage * GameConfig.ZombieDamageMultiplier);
        agent = GetComponent<NavMeshAgent>();
        
        // If NavMeshAgent exists but is disabled, enable it if on NavMesh
        if (agent != null && !agent.enabled) agent.enabled = true;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        health = GetComponent<ZombieHealth>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        patrolOrigin = transform.position;
        patrolTarget = patrolOrigin;
        ApplyVariantProfile();
        
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.speed = moveSpeed;
        }

        PlaceOnWalkableGround();
    }

    void Update()
    {
        if (health != null && health.IsDead()) return;
        if (player == null) return;

        UpdateAIState();
        ExecuteAIState();
        SyncAnimation();
    }

    void UpdateAIState()
    {
        if (Time.time < stunnedUntil)
        {
            currentState = ZombieState.Stunned;
            return;
        }

        Vector3 flatOffset = player.position - transform.position;
        flatOffset.y = 0f;
        float distanceToPlayer = flatOffset.magnitude;

        // Flee logic (Evasion) - if low health and not a Tank
        if (health != null && health.currentHealth < (health.maxHealth * 0.15f) && variant != ZombieVariant.Tank)
        {
            currentState = ZombieState.Patrol;
            FleeFromPlayer();
            return;
        }

        bool heardShot = Time.time - Shooting.LastShotTime < 1.2f
            && Vector3.Distance(transform.position, Shooting.LastShotPosition) <= hearingDistance;

        if (heardShot)
        {
            alertUntil = Time.time + 4.5f;
        }

        float actualDetectionDistance = detectionDistance;
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm != null && pm.IsCrouching()) actualDetectionDistance *= 0.65f;

        if (distanceToPlayer <= actualDetectionDistance)
        {
            currentState = distanceToPlayer <= attackDistance ? ZombieState.Attack : ZombieState.Chase;
        }
        else if (Time.time < alertUntil)
        {
            currentState = ZombieState.Alert;
        }
        else
        {
            currentState = ZombieState.Patrol;
        }
    }

    void ExecuteAIState()
    {
        switch (currentState)
        {
            case ZombieState.Attack:
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.speed = 0f;
                }
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackRate;
                    AttackPlayer();
                }
                break;

            case ZombieState.Chase:
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    agent.speed = moveSpeed;
                }
                else
                {
                    MoveTowardsPlayer();
                }
                break;

            case ZombieState.Alert:
                InvestigateSound();
                break;

            case ZombieState.Patrol:
                Patrol();
                break;

            case ZombieState.Stunned:
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                KeepGrounded();
                break;
        }
    }

    void SyncAnimation()
    {
        if (animator == null) return;
        
        float currentSpeed = 0f;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            currentSpeed = agent.velocity.magnitude;
        }
        else if (currentState != ZombieState.Attack && currentState != ZombieState.Stunned)
        {
            currentSpeed = moveSpeed;
        }

        animator.SetFloat("Velocidade", currentSpeed);
        animator.SetBool("Perto", currentState == ZombieState.Attack);
    }

    void MoveTowardsPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 moveDirection = direction.normalized;
        float speed = Vector3.Distance(transform.position, player.position) <= attackDistance
            ? 0f
            : moveSpeed;

        Move(moveDirection, speed);
    }

    void MoveTowardsPoint(Vector3 targetPoint, float speed)
    {
        Vector3 direction = targetPoint - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.25f)
        {
            return;
        }

        Move(direction.normalized, speed);
    }

    void Patrol()
    {
        if ((patrolTarget - transform.position).sqrMagnitude < 1.2f)
        {
            Vector2 randomOffset = Random.insideUnitCircle * 4.5f;
            patrolTarget = patrolOrigin + new Vector3(randomOffset.x, 0f, randomOffset.y);
            patrolTarget.y = groundedY;
        }

        MoveTowardsPoint(patrolTarget, moveSpeed * 0.45f);
    }

    void Move(Vector3 moveDirection, float speed)
    {
        Vector3 targetMove = moveDirection * speed * Time.deltaTime;
        
        // Primitive collision fallback (dont walk through walls)
        if (!Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.35f, moveDirection, out RaycastHit hit, targetMove.magnitude + 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            transform.position += targetMove;
        }
        
        KeepGrounded(true); // Active grounding
        
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDirection),
                8.5f * Time.deltaTime);
        }
    }

    void KeepGrounded(bool active = false)
    {
        if (active)
        {
            // Dynamic raycast grounding
            if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsWalkableSurface(hit.collider))
                {
                    groundedY = hit.point.y + groundOffset;
                }
            }
        }

        Vector3 position = transform.position;
        position.y = groundedY;
        transform.position = position;
    }

    void PlaceOnWalkableGround()
    {
        Vector3 rayStart = transform.position + Vector3.up * 8f;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        RaycastHit? groundHit = null;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider == capsuleCollider)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!IsWalkableSurface(hit.collider))
            {
                continue;
            }

            groundHit = hit;
            break;
        }

        if (!groundHit.HasValue)
        {
            return;
        }

        float targetY = groundHit.Value.point.y + groundOffset;
        Vector3 position = transform.position;
        position.y = targetY;
        transform.position = position;
        groundedY = targetY;
    }

    void KeepGrounded()
    {
        Vector3 position = transform.position;
        position.y = groundedY;
        transform.position = position;
    }

    bool IsWalkableSurface(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        // Logic sync with PlayerMovement
        int layerBit = 1 << hitCollider.gameObject.layer;
        if ((Physics.DefaultRaycastLayers & layerBit) != 0)
        {
            return true;
        }

        string surfaceName = hitCollider.gameObject.name;
        return surfaceName == "Ground"
            || surfaceName == "Outer Terrain"
            || surfaceName == "Main Street"
            || surfaceName == "Cross Road"
            || surfaceName.Contains("Road")
            || surfaceName.Contains("Terrain");
    }

    void AttackPlayer()
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }

        if (animator != null)
        {
            animator.SetTrigger("Atacar");
        }
    }

    public void ApplyStun(float duration)
    {
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration);
    }

    void ApplyVariantProfile()
    {
        switch (variant)
        {
            case ZombieVariant.Runner:
                moveSpeed = 2.7f;
                attackDamage = 6;
                detectionDistance = 17f;
                hearingDistance = 21f;
                transform.localScale = new Vector3(0.92f, 0.92f, 0.92f);
                break;
            case ZombieVariant.Tank:
                moveSpeed = 1.1f;
                attackDamage = 12;
                detectionDistance = 13f;
                hearingDistance = 12f;
                transform.localScale = new Vector3(1.18f, 1.18f, 1.18f);
                break;
            case ZombieVariant.Screamer:
                moveSpeed = 1.8f;
                attackDamage = 5;
                detectionDistance = 22f;
                hearingDistance = 28f;
                transform.localScale = new Vector3(0.95f, 1.02f, 0.95f);
                break;
            case ZombieVariant.Crawler:
                moveSpeed = 1.1f;
                attackDamage = 4;
                detectionDistance = 10f;
                hearingDistance = 10f;
                transform.localScale = new Vector3(1f, 0.58f, 1f);
                if (capsuleCollider != null)
                {
                    capsuleCollider.height = 0.95f;
                    capsuleCollider.center = new Vector3(0f, 0.45f, 0f);
                }
                break;
            default:
                moveSpeed = 1.7f;
                attackDamage = 5;
                detectionDistance = 13f;
                hearingDistance = 15f;
                transform.localScale = Vector3.one;
                break;
        }
    }
    void InvestigateSound()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(Shooting.LastShotPosition);
            agent.speed = moveSpeed * 0.7f;
        }
        else
        {
            MoveTowardsPoint(Shooting.LastShotPosition, moveSpeed * 0.7f);
        }
    }

    void FleeFromPlayer()
    {
        Vector3 fleeDirection = (transform.position - player.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDirection * 10f;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(fleeTarget);
            agent.speed = moveSpeed * 1.25f;
        }
        else
        {
            MoveTowardsPoint(fleeTarget, moveSpeed * 1.25f);
        }
    }
}