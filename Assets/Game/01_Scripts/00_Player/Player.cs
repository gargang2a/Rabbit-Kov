using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    [Header("Component")]
    public Rigidbody rb;

    [Header("Player Info 정보")]
    [SerializeField] private int hp;      // 체력
    [SerializeField] private int stamina; // 스테미너
    public int MaxHp { get; private set; } = 100;
    public int MaxStamina { get; private set; } = 100;

    [field: SerializeField] public int Atk { get; private set; }   // 공격력
    [field: SerializeField] public int Def { get; private set; }    // 방어력
    [field: SerializeField] public int Shield { get; private set; } // 쉴드

    // Move
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float rotationSpeed = 3.0f;
    private Vector3 currentMoveInput = Vector3.zero;


    [Space]
    [Header("Condition 상태")]
    [SerializeField] private bool isBleed;
    [SerializeField] private bool isSlow;

    [Space]
    [Header("Inventory 인벤토리")]
    [SerializeField] private int coin;         // 돈
    [SerializeField] private string slotOne;   // 슬롯 1
    [SerializeField] private string slotTwo;   // 슬롯 2
    [SerializeField] private string slotThree; // 슬롯 3
    [SerializeField] private string slotFour;  // 슬롯 4

    public int Hp // HP 프로퍼티 (PascalCase)
    {
        get => hp; // { return hp; } 와 동일, 람다식으로 간결하게
        set
        {
            hp = Mathf.Clamp(value, 0, MaxHp); // if 조건문을 간결하게

            if (hp <= 0) // 사망 처리
            {
                hp = 0; // 0으로 고정
                Debug.Log("플레이어 사망");
            }
        }
    }
    public int Stamina // Stamina 프로퍼티 (PascalCase)
    {
        get => stamina;
        set
        {
            stamina = Mathf.Clamp(value, 0, MaxStamina);

            if (stamina <= 0)
            {
                stamina = 0; // 0으로 고정
                Debug.Log("스테미나 고갈");
            }
        }
    }

    private void Awake()
    {
        Hp = MaxHp;
        Stamina = MaxStamina;

        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 moveInput = new Vector3(h, 0, v);
        if (moveInput.magnitude > 1f) moveInput.Normalize();
        currentMoveInput = moveInput;
    }

    private void FixedUpdate()
    {
        Vector3 forwardMovement = transform.forward * currentMoveInput.z;
        Vector3 rightMovemnet = transform.right * currentMoveInput.x;

        Vector3 localMovementDirection = forwardMovement + rightMovemnet;

        if (localMovementDirection.magnitude > 1f) localMovementDirection.Normalize();

        Vector3 targetVelocity = localMovementDirection * moveSpeed;
        targetVelocity.y = rb.velocity.y;
        rb.velocity = targetVelocity;

        if (currentMoveInput != Vector3.zero) HandleRotation(currentMoveInput);
    }

    private void HandleRotation(Vector3 input)
    {
        Vector3 targetDirection = transform.right * input.x + transform.forward * input.z;
        targetDirection.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    
    }
}
