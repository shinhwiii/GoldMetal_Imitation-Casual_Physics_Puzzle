using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("----[ Core ]")]
    public int score;
    public int maxLevel;
    public bool isOver;

    [Header("----[ Object Pooling ]")]
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public List<Dongle> donglePool;
    public GameObject effectPrefab;
    public Transform effectGroup;
    public List<ParticleSystem> effectPool;
    [Range(1, 30)] // OnDisable 함수를 통해 초기화
    public int poolSize;
    public int poolCursor;
    public Dongle lastDongle;

    [Header("----[ Audio ]")]
    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayer;
    public AudioClip[] sfxClips;
    public enum Sfx { LevelUp, Next, Attach, Button, Over };
    int sfxCursor;

    [Header("----[ UI ]")]
    public GameObject startGroup;
    public GameObject endGroup;
    public Text scoreText;
    public Text maxScoreText;
    public Text subScoreText;
    public Text bestScoreText;

    [Header("----[ ETC ]")]
    public GameObject line;
    public GameObject bottom;

    void Awake()
    {
        Application.targetFrameRate = 60; // targetFrameRate : 프레임 설정 속성

        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();

        for(int i = 0; i < poolSize; i++)        
            MakeDongle();

        if (!PlayerPrefs.HasKey("MaxScore"))
            PlayerPrefs.SetInt("MaxScore", 0);

        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();
    }

    public void GameStart()
    {
        // 인게임에 필요한 오브젝트 활성화
        startGroup.SetActive(false);
        line.SetActive(true);
        bottom.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);

        // 사운드 플레이
        bgmPlayer.Play();
        SfxPlay(Sfx.Button);

        // 게임 시작 (동글 생성)
        Invoke("NextDongle", 1.5f);
    }

    Dongle MakeDongle()
    {
        // Effect Pool 생성
        GameObject instantEffect = Instantiate(effectPrefab, effectGroup);
        instantEffect.name = "Effect " + effectPool.Count;
        ParticleSystem effect = instantEffect.GetComponent<ParticleSystem>();
        effectPool.Add(effect);

        // Dongle Pool 생성
        GameObject instantDongle = Instantiate(donglePrefab, dongleGroup);
        instantDongle.name = "Dongle " + donglePool.Count;
        Dongle dongle = instantDongle.GetComponent<Dongle>();
        dongle.manager = this;
        dongle.effect = effect;
        donglePool.Add(dongle);

        return dongle;
    }

    Dongle GetDongle() 
    {
        for(int i = 0; i < donglePool.Count; i++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count;

            if (!donglePool[poolCursor].gameObject.activeSelf) // 만들어진 풀에서 비활성화 되어있는 오브젝트를 반환
                return donglePool[poolCursor];
        }

        // 모든 풀이 활성화 되어있다면 새로운 풀을 다시 생성
        return MakeDongle();
    }

    void NextDongle()
    {
        if (isOver)
            return;

        lastDongle = GetDongle();
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        SfxPlay(Sfx.Next);

        StartCoroutine(WaitNext());
    }

    IEnumerator WaitNext()
    {
        while (lastDongle != null)
            yield return null;

        yield return new WaitForSeconds(2.5f);

        NextDongle();
    }

    public void TouchDown()
    {
        if (lastDongle == null)
            return;

        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;

        lastDongle.Drop();

        lastDongle = null;
    }

    public void GameOver()
    {
        if (isOver)
            return;

        isOver = true;

        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        // 장면 안에 있는 활성화 되어있는 모든 Dongle 가져오기
        Dongle[] dongles = FindObjectsOfType<Dongle>();

        // 지우기 전에 모든 Dongle의 물리 효과 비활성화
        for (int i = 0; i < dongles.Length; i++)
        {
            dongles[i].rigid.simulated = false;
        }

        // Dongle에 하나씩 접근해서 지우기
        for (int i = 0; i < dongles.Length; i++)
        {
            dongles[i].Hide(Vector3.up * 100);
            dongles[i].EffectPlay();

            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);

        // 최고 점수 갱신
        int maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxScore"));
        if (maxScore > PlayerPrefs.GetInt("MaxScore")) // 최고 점수라면 Best 활성화
            bestScoreText.gameObject.SetActive(true);
        PlayerPrefs.SetInt("MaxScore", maxScore);

        // 게임오버 UI 표시
        subScoreText.text = "점수 : " + scoreText.text;
        endGroup.SetActive(true);

        bgmPlayer.Stop();
        SfxPlay(Sfx.Over);
    }

    public void Restart()
    {
        SfxPlay(Sfx.Button);
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("Main");
    }

    public void SfxPlay(Sfx type)
    {
        switch(type)
        {
            case Sfx.LevelUp:
                sfxPlayer[sfxCursor].clip = sfxClips[Random.Range(0, 3)];
                break;
            case Sfx.Next:
                sfxPlayer[sfxCursor].clip = sfxClips[3];
                break;
            case Sfx.Attach:
                sfxPlayer[sfxCursor].clip = sfxClips[4];
                break;
            case Sfx.Button:
                sfxPlayer[sfxCursor].clip = sfxClips[5];
                break;
            case Sfx.Over:
                sfxPlayer[sfxCursor].clip = sfxClips[6];
                break;
        }
        sfxPlayer[sfxCursor].Play();
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length;
    }

    void Update()
    {
        if(Input.GetButtonDown("Cancel"))
        {
            Application.Quit();
        }
    }

    void LateUpdate()
    {
        scoreText.text = score.ToString();
    }
}
