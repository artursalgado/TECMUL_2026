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
    private float baseMoveSpeed;
    private int baseAttackDamage;
    private float baseDetectionDistance;
    private float baseHearingDistance;
    private float hordeSpeedMultiplier = 1f;
    private float hordeDamageMultiplier = 1f;
    private float hordeDetectionMultiplier = 1f;
    private float flankUntil;
    private float nextFlankDecisionTime;
    private float flankSideSign = 1f;
    private float flankOffsetDistance = 4f;
    private float flankForwardLead = 2f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // If NavMeshAgent exists but is disabled, enable it if on NavMesh
        if (agent != null && !agent.enabled) agent.enabled = true;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        health = GetComponent<ZombieHealth>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        patrolOrigin = transform.position;
        patrolTarget = patrolOrigin;
        flankSideSign = Random.value > 0.5f ? 1f : -1f;
        ApplyVariantProfile();
        ApplyDynamicModifiers();
        
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.speed = moveSpeed;
            agent.baseOffset = 0f;
        }

        PlaceOnWalkableGround();
    }

    void Update()
    {
        if (health != null && health.IsDead()) return;
        if (player == null) return;

        UpdateAIState();
        ExecuteAIState();
        MaintainGroundLock();
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
                Vector3 chaseTarget = GetChaseTarget();
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(chaseTarget);
                    agent.speed = moveSpeed;
                }
                else
                {
                    MoveTowardsPoint(chaseTarget, moveSpeed);
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
                else
                {
                    KeepGrounded();
                }
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

    Vector3 GetChaseTarget()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;

        if (direction.sqrMagnitude <= 0.001f || distance <= attackDistance + 0.2f)
        {
            return player.position;
        }

        if (Time.time >= nextFlankDecisionTime)
        {
            nextFlankDecisionTime = Time.time + Random.Range(1.1f, 2.3f);
            bool canFlank = distance > attackDistance + 4f && variant != ZombieVariant.Tank;
            if (canFlank && Random.value < 0.38f)
            {
                flankUntil = Time.time + Random.Range(1.2f, 2.8f);
                flankSideSign = Random.value > 0.5f ? 1f : -1f;
                flankOffsetDistance = Mathf.Clamp(distance * 0.34f, 2.5f, 8.5f);
                flankForwardLead = Random.Range(1.2f, 3.8f);
            }
        }

        if (Time.time < flankUntil)
        {
            Vector3 dir = direction / distance;
            Vector3 side = Vector3.Cross(Vector3.up, dir) * flankSideSign;
            Vector3 flankTarget = player.position + side * flankOffsetDistance + dir * flankForwardLead;
            flankTarget.y = transform.position.y;
            return flankTarget;
        }

        return player.position;
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
            TryResolveGroundY(transform.position, out groundedY);
        }

        Vector3 position = transform.position;
        position.y = groundedY;
        transform.position = position;
    }

    void PlaceOnWalkableGround()
    {
        if (!TryResolveGroundY(transform.position, out float targetY))
        {
            return;
        }

        Vector3 position = transform.position;
        position.y = targetY;
        transform.position = position;
        groundedY = targetY;
    }

    void MaintainGroundLock()
    {
        if (TryResolveGroundY(transform.position, out float targetY))
        {
            groundedY = Mathf.Lerp(groundedY, targetY, 0.85f);
            Vector3 pos = transform.position;
            pos.y = groundedY;
            transform.position = pos;
        }
    }

    bool TryResolveGroundY(Vector3 aroundPosition, out float targetY)
    {
        targetY = aroundPosition.y;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            targetY = agent.nextPosition.y + groundOffset;
            return true;
        }

        if (NavMesh.SamplePosition(aroundPosition, out NavMeshHit navHit, 16f, NavMesh.AllAreas))
        {
            targetY = navHit.position.y + groundOffset;
            return true;
        }

        Vector3 rayStart = aroundPosition + Vector3.up * 120f;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 420f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider == capsuleCollider)
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

            targetY = hit.point.y + groundOffset;
            return true;
        }

        return false;
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

    public void ApplyHordeModifiers(float speedMultiplier, float damageMultiplier, float detectionMultiplier)
    {
        hordeSpeedMultiplier = Mathf.Max(0.4f, speedMultiplier);
        hordeDamageMultiplier = Mathf.Max(0.4f, damageMultiplier);
        hordeDetectionMultiplier = Mathf.Max(0.4f, detectionMultiplier);
        ApplyDynamicModifiers();
    }

    void ApplyDynamicModifiers()
    {
        moveSpeed = baseMoveSpeed * hordeSpeedMultiplier;
        attackDamage = Mathf.RoundToInt(baseAttackDamage * GameConfig.ZombieDamageMultiplier * hordeDamageMultiplier);
        detectionDistance = baseDetectionDistance * hordeDetectionMultiplier;
        hearingDistance = baseHearingDistance * hordeDetectionMultiplier;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.speed = moveSpeed;
        }
    }

    void SetBaseStats(float move, int damage, float detect, float hearing, Vector3 localScale)
    {
        baseMoveSpeed = move;
        baseAttackDamage = damage;
        baseDetectionDistance = detect;
        baseHearingDistance = hearing;
        transform.localScale = localScale;
    }

    void ApplyVariantProfile()
    {
        switch (variant)
        {
            case ZombieVariant.Runner:
                SetBaseStats(2.7f, 6, 17f, 21f, new Vector3(0.92f, 0.92f, 0.92f));
                break;
            case ZombieVariant.Tank:
                SetBaseStats(1.1f, 12, 13f, 12f, new Vector3(1.18f, 1.18f, 1.18f));
                break;
            case ZombieVariant.Screamer:
                SetBaseStats(1.8f, 5, 22f, 28f, new Vector3(0.95f, 1.02f, 0.95f));
                break;
            case ZombieVariant.Crawler:
                SetBaseStats(1.1f, 4, 10f, 10f, new Vector3(1f, 0.58f, 1f));
                if (capsuleCollider != null)
                {
                    capsuleCollider.height = 0.95f;
                    capsuleCollider.center = new Vector3(0f, 0.45f, 0f);
                }
                break;
            default:
                SetBaseStats(1.7f, 5, 13f, 15f, Vector3.one);
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
