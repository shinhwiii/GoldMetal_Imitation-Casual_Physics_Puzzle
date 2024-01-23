using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dongle : MonoBehaviour
{
    public GameManager manager;
    public ParticleSystem effect;

    public int level;
    public bool isDrag;
    public bool isMerge;
    public bool isAttach;

    public Rigidbody2D rigid;

    CircleCollider2D circleCollider;
    Animator anim;
    SpriteRenderer spriteRenderer;

    float deadTime;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        anim.SetInteger("Level", level);
    }

    void OnDisable() // 비활성화 될 때 모두 초기화
    {
        // Dongle 속성 초기화
        level = 0;
        isDrag = false;
        isMerge = false;
        isAttach = false;

        // Dongle Transform 초기화
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.zero;

        // Dongle 물리 초기화
        rigid.simulated = false;
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;
        circleCollider.enabled = true;
    }

    void Update()
    {
        if(isDrag) // 마우스를 드래그할 때 이동
        {
            // 스크린 좌표(UI 상으로 보이는 좌표)와 월드 좌표(게임 내에서 보이는 좌표)가 다르기 떄문에 스크린 좌표를 월드 좌표로 바꾸어 계산함
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float leftBorder = -3.9f + transform.localScale.x / 2f; // 벽과 동글의 반지을 더해서 경계를 만들기
            float rightBorder = 3.9f - transform.localScale.x / 2f;

            if (mousePos.x < leftBorder)
                mousePos.x = leftBorder;
            else if (mousePos.x > rightBorder)
                mousePos.x = rightBorder;

            mousePos.y = 7.5f;
            mousePos.z = 0f;
            transform.position = Vector3.Lerp(transform.position, mousePos, 0.2f); // Vector3.Lerp : 목표지점으로 부드럽게 이동
        }
        
    }

    public void Drag()
    {
        isDrag = true;
    }

    public void Drop()
    {
        isDrag = false;
        rigid.simulated = true;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        StartCoroutine(AttachRoutine());
    }

    IEnumerator AttachRoutine()
    {
        if (isAttach)
            yield break;

        isAttach = true;
        manager.SfxPlay(GameManager.Sfx.Attach);

        yield return new WaitForSeconds(0.2f);
        isAttach = false;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Dongle")
        {
            Dongle other = collision.gameObject.GetComponent<Dongle>();

            if(level == other.level && !isMerge && !other.isMerge && level < 7) // 동글 합치기 (합치는 도중에 개입되는 것 방지)
            {
                // 자신과 상대편 위치 가져오기
                float meX = transform.position.x;
                float meY = transform.position.y;
                float otherX = other.transform.position.x;
                float otherY = other.transform.position.y;

                // 1. 내가 아래에 있을 때
                // 2. 동일한 높이일 때, 내가 오른쪽에 있을 때
                if(meY < otherY || (meY == otherY && meX > otherX))
                {
                    // 상대방은 숨기기
                    other.Hide(transform.position);
                    // 나는 레벨업
                    LevelUp();
                }

            }
            else if(level == other.level && !isMerge && !other.isMerge && level == 7)
            {
                other.LevelUp();
                LevelUp();
            }
        }
    }

    public void Hide(Vector3 targetPos)
    {
        isMerge = true;

        rigid.simulated = false;
        circleCollider.enabled = false;

        StartCoroutine(HideRoutine(targetPos));
    }

    IEnumerator HideRoutine(Vector3 targetPos)
    {
        int frameCount = 0;

        while(frameCount < 20)
        {
            frameCount++;

            if (targetPos == Vector3.up * 100) // 레벨업하지 않는 경우 (게임오버일 때 호출) 
            { 
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);
            }
            else 
                transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);

            yield return null;
        }

        manager.score += (int)Mathf.Pow(2, level); // Pow : 지정 숫자의 거듭제곱

        isMerge = false;
        gameObject.SetActive(false);
    }

    void LevelUp()
    {
        isMerge = true;

        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0f;

        StartCoroutine(LevelUpRoutine());
    }

    IEnumerator LevelUpRoutine()
    {
        if (level == 7)
        {
            gameObject.SetActive(false);
            EffectPlay();            

            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            anim.SetInteger("Level", level + 1);
            EffectPlay();

            yield return new WaitForSeconds(0.3f);

            level++;

            if(level != 7)
                manager.maxLevel = Mathf.Max(level, manager.maxLevel);
        }        
        isMerge = false;
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.tag == "Finish")
        {
            deadTime += Time.deltaTime;

            if(deadTime > 2)
                spriteRenderer.color = new Color(0.9f, 0.2f, 0.2f);

            if (deadTime > 5)
                manager.GameOver();
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.tag == "Finish")
        {
            deadTime = 0;
            spriteRenderer.color = Color.white;
        }
    }

    public void EffectPlay()
    {
        effect.transform.position = transform.position;
        effect.transform.localScale = transform.localScale;
        manager.SfxPlay(GameManager.Sfx.LevelUp);
        effect.Play();
    }
}
