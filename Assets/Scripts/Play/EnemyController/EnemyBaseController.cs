using UnityChan;
using UnityEngine;
using UnityEngine.AI;

// 基底クラス。ゾンビに貼り付ける
// ゾンビを親として各エネミーの行動を定義
public class EnemyBaseController : MonoBehaviour
{
    protected Animator animator;
    protected NavMeshAgent agent;
    protected Transform myTransform;
    protected Transform targetTransform; // プレイヤー位置

    [SerializeField] protected AudioSource weaponAudio;

    // Managerクラスから参照する可能性があるので　public
    public string Name { get; protected set; }
    public int AttackPoint { get; protected set; } // 攻撃力
    public float WalkSpeed { get; protected set; } // 歩行速度
    public float RunSpeed { get; protected set; }  // 走行速度
    public float RandomWanderRadius { get; protected set; } // Wander状態のときの移動範囲
    public float CanSeePlayerRadius { get; protected set; } // プレイヤー視認可能範囲

    private float idleTimer = 0.0f;
    private const float idleInterval = 4.0f;
    protected void Awake()
    {
        myTransform = transform;
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        weaponAudio = GetComponent<AudioSource>();
    }

    protected EnemyBaseController(
        string name = "ゾンビ",           // 名前、
        int at = 5,                //攻撃力、
        float walk_s = 4f,          //歩行速度
        float run_s = 6f,           //走行速度、
        float rnd_wanderRad = 5f,   //巡回可能範囲
        float canSee_playerRad = 6f)//プレイヤー認知可能範囲
    {
        Name = name;
        AttackPoint = at;
        WalkSpeed = walk_s;
        RunSpeed = run_s;
        RandomWanderRadius = rnd_wanderRad;
        CanSeePlayerRadius = canSee_playerRad;
    }

    public enum STATE
    {
        IDLE,
        CHASE,
        WALK,
        ATTACK,
        DEAD
    };

    protected STATE current_enemy_state = STATE.IDLE;


    protected void Attack()
    {
        Vector3 dis = targetTransform.position - transform.position;

        if (dis.magnitude < 1.5f) // 攻撃が当たる範囲
            targetTransform.GetComponent<UnityChanController>().TakeHit(AttackPoint);
    }

    protected void OnAttackAnimEnd()
    {

        if (animator.HasState(0, Animator.StringToHash("Idle")))
            animator.SetBool("Idle", true);
    }

    protected float DistanceToPlayer()
    {
        if (GameState.gameOver)
        {
            return Mathf.Infinity;
        }

        return Vector3.Distance(targetTransform.position, myTransform.position);
    }


    protected bool CanSeePlayer()
    {
        // 距離
        if (DistanceToPlayer() <= CanSeePlayerRadius)
        {
            Vector3 direction = targetTransform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, direction);

            // 視野角に入っているか
            if (angle <= 30f)
            {
                return true;
            }
        }

        return false;
    }

    protected bool ReachToPlayer()
    {
        if (DistanceToPlayer() <= agent.stoppingDistance)
        {
            return true;
        }

        return false;
    }

    private void SetAnimationState(STATE state)
    {
        // すべてのアニメーションのブール値をfalseに設定
        animator.SetBool("Idle", false);
        animator.SetBool("Walk", false);
        animator.SetBool("Run", false);
        animator.SetBool("Attack", false);

        // 新しい状態に基づいて適切なアニメーションのブール値をtrueに設定
        switch (state)
        {
            case STATE.IDLE:
                animator.SetBool("Idle", true);
                break;
            case STATE.WALK:
                animator.SetBool("Walk", true);
                break;
            case STATE.CHASE:
                animator.SetBool("Run", true);
                break;
            case STATE.ATTACK:
                animator.SetBool("Attack", true);
                break;
        }

        // 現在のアニメーションの状態を更新
        current_enemy_state = state;
    }


    protected void Update()
    {
        switch (current_enemy_state)
        {
            // アイドル状態のとき
            case STATE.IDLE:

                agent.enabled = false;

                // 追跡開始
                if (CanSeePlayer())
                {
                    SoundEffectManager.instance.Play(SoundEffectManager.Select.NoticePlayer);
                    current_enemy_state = STATE.CHASE;
                }
                
                else
                {
                    // 毎フレーム更新されてしまうため、タイマーでタイミングを制御

                    idleTimer += Time.deltaTime;

                    if(idleTimer > idleInterval)
                    {
                        idleTimer = 0;

                        // 一定確率で巡回開始
                        if (Random.Range(0, 100) > 70)
                        {
                            current_enemy_state = STATE.WALK;
                        }
                    }                   
                }

                break;

            // 彷徨っているとき
            case STATE.WALK:

                agent.enabled = true;

                // プレイヤーを見つけていなければ
                if (!agent.hasPath)
                {
                    agent.speed = WalkSpeed;

                    float x = transform.position.x + Random.Range(-RandomWanderRadius, RandomWanderRadius);    // 横方向にランダム移動
                    float z = transform.position.z + Random.Range(-RandomWanderRadius, RandomWanderRadius);    // 奥方向にランダム移動
                    
                    Vector3 newPos = new Vector3(x, transform.position.y, z);

                    Ray ray = new Ray(newPos , Vector3.down);
                    RaycastHit hit;

                    // ランダム巡回地点に床があれば移動する
                    if (Physics.Raycast(ray, out hit, 1.0f))
                    {
                        agent.SetDestination(newPos);

                        SetAnimationState(STATE.WALK);
                    }

                    break;
                }

                if (CanSeePlayer())
                {
                    SoundEffectManager.instance.Play(SoundEffectManager.Select.NoticePlayer);
                    current_enemy_state = STATE.CHASE;
                }
                else if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
                {
                    current_enemy_state = STATE.IDLE;
                }

                // 地面に接地していなければ
                if (IsObstacleAhead() || !IsGroundAhead())
                {
                    SetAnimationState(STATE.IDLE); // 歩くアニメーションを停止
                }

                break;

            // プレイヤーを追いかけている時
            case STATE.CHASE:

                agent.enabled = true;

                SetAnimationState(STATE.CHASE);

                agent.SetDestination(targetTransform.position);
                agent.speed = RunSpeed;

                transform.LookAt(new Vector3(targetTransform.position.x, transform.position.y, targetTransform.position.z));

                // プレイヤーを見失ったら彷徨い始める   ゲームオーバーの場合は永遠に彷徨い始める
                if (GameState.gameOver || !CanSeePlayer())
                {
                    current_enemy_state = STATE.IDLE;
                }

                if (ReachToPlayer())
                {
                    current_enemy_state = STATE.ATTACK;
                }

                break;

            // プレイヤーを攻撃している時
            case STATE.ATTACK:

                if (!GameState.gameOver)
                {
                    agent.enabled = false;

                    transform.LookAt(new Vector3(targetTransform.position.x, transform.position.y, targetTransform.position.z));

                    SetAnimationState(STATE.ATTACK);

                    if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 &&
                        animator.IsInTransition(0))
                    {
                        animator.SetBool("Attack", false);
                    }
                }
                else
                {
                    current_enemy_state = STATE.WALK;
                    return;
                }

                if (!ReachToPlayer())
                    current_enemy_state = STATE.CHASE;      // 離れたらまた追いかけ始める
                break;
        }
    }


    private bool IsObstacleAhead()
    {
        RaycastHit hit;
        Vector3 forward = transform.TransformDirection(Vector3.forward);

        if (Physics.Raycast(transform.position, forward, out hit, 1.0f))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                return true;
            }
        }
        return false;
    }


    private bool IsGroundAhead()
    {
        RaycastHit hit;
        Vector3 down = transform.TransformDirection(Vector3.down);
        Vector3 forwardOffset = transform.position + Vector3.forward * 0.1f;

        if (Physics.Raycast(forwardOffset, down, out hit, 1.0f))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                return true;
            }
        }
        return false;
    }
}