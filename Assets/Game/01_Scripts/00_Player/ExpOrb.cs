using UnityEngine;

public class ExpOrb : MonoBehaviour
{
    [Header("Basic Settings")]
    public int expAmount = 10;
    public float detectRange = 5f;

    [Header("Magnet Settings")]
    public float smoothTime = 0.3f;
    public float maxSpeed = 20f;

    [Header("Audio")]
    public AudioClip expSound; // ★ [추가] 효과음 파일 넣을 곳

    private Transform playerTrans;
    private bool isFollowing = false;
    private bool isMagnetMode = false;

    private Vector3 currentVelocity = Vector3.zero;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTrans = playerObj.transform;
        }
    }

    void Update()
    {
        if (playerTrans == null) return;

        if (!isFollowing)
        {
            float distance = Vector3.Distance(transform.position, playerTrans.position);
            if (isMagnetMode || distance < detectRange)
            {
                isFollowing = true;
            }
        }

        if (isFollowing)
        {
            Vector3 targetPos = playerTrans.position + Vector3.up * 1.0f;
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime, maxSpeed);
        }
    }

    public void ActivateMagnet()
    {
        isMagnetMode = true;
        isFollowing = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Collect(other.gameObject);
        }
    }

    void Collect(GameObject playerObj)
    {
        Player player = playerObj.GetComponent<Player>();
        if (player != null)
        {
            player.GainExp(expAmount);
        }

        // ★ [추가] 사운드 매니저에게 소리 재생 요청
        // (SoundManager가 없거나 소리 파일이 없으면 오류 안 나게 체크)
        if (SoundManager.instance != null && expSound != null)
        {
            SoundManager.instance.PlaySFX(expSound);
        }

        Destroy(gameObject);
    }
}