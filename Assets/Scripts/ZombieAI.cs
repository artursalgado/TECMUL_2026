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
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        health = GetComponent<ZombieHealth>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        patrolOrigin = transform.position;
        patrolTarget = patrolOrigin;
        ApplyVariantProfile();
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        PlaceOnWalkableGround();
    }

    void Update()
    {
        if (health != null && health.IsDead())
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (Time.time < stunnedUntil)
        {
            currentState = ZombieState.Stunned;
            KeepGrounded();
            return;
        }

        Vector3 flatOffset = player.position - transform.position;
        flatOffset.y = 0f;
        float distanceToPlayer = flatOffset.magnitude;
        bool heardShot = Time.time - Shooting.LastShotTime < 1.2f
            && Vector3.Distance(transform.position, Shooting.LastShotPosition) <= hearingDistance;

        if (heardShot)
        {
            alertUntil = Time.time + 2.5f;
        }

        if (distanceToPlayer <= detectionDistance)
        {
            currentState = distanceToPlayer <= attackDistance ? ZombieState.Attack : ZombieState.Chase;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(player.position);
            }
            else
            {
                MoveTowardsPlayer();
            }

            if (distanceToPlayer <= attackDistance)
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.speed = 0f;
                }

                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackRate;
                    AttackPlayer();
                }
            }
            else
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.speed = attackMoveSpeed;
                }
            }
        }
        else if (Time.time < alertUntil)
        {
            currentState = ZombieState.Alert;
            MoveTowardsPoint(Shooting.LastShotPosition, moveSpeed * 0.8f);
        }
        else
        {
            currentState = ZombieState.Patrol;
            Patrol();
        }

        if (animator != null)
        {
            float currentSpeed = agent != null && agent.isOnNavMesh
                ? agent.velocity.magnitude
                : (distanceToPlayer <= attackDistance ? 0f : moveSpeed);
            animator.SetFloat("Velocidade", currentSpeed);
            animator.SetBool("Perto", distanceToPlayer <= attackDistance);
        }

        KeepGrounded();
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
        transform.position += moveDirection * speed * Time.deltaTime;
        KeepGrounded();
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(moveDirection),
            8f * Time.deltaTime);
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

        string surfaceName = hitCollider.gameObject.name;
        return surfaceName == "Ground"
            || surfaceName == "Outer Terrain"
            || surfaceName == "Main Street"
            || surfaceName == "Cross Road";
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
}
