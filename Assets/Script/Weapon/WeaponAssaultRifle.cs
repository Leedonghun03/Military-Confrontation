using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AmmoEvent : UnityEngine.Events.UnityEvent<int, int> { }
[System.Serializable]
public class MagazineEvent : UnityEngine.Events.UnityEvent<int> { }

public class WeaponAssaultRifle : MonoBehaviour
{
    [HideInInspector]
    public AmmoEvent onAmmoEvent = new AmmoEvent();
    [HideInInspector]
    public MagazineEvent onMagazineEvent = new MagazineEvent();

    [Header("Fire Effects")]
    [SerializeField]
    private GameObject muzzleFlashEffect;   // 총구 이펙트

    [Header("Spawn Points")]
    [SerializeField]
    private Transform casingSpawnPoint;     //탄피 생성 위치
    [SerializeField]
    private Transform bulletSpawnPoint;     //총알 생성 위치

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip audioClipTakeOutWeapon;   //무기 견착 사운드
    [SerializeField]
    private AudioClip audioClipFire;            //공격 사운드
    [SerializeField]
    private AudioClip audioClipReload;          //재장전 사운드

    [Header("Weapon Setting")]
    [SerializeField]
    private WeaponSetting weaponSetting;    //무기 설정

    [Header("Aim UI")]
    [SerializeField]
    private Image imageAim;

    private float lastAttackTime = 0;       //마지막 발사시간 체크
    private bool isReload = false;          //재장전 중인지 확인
    private bool isAttak = false;           //공격 여부 체크
    private bool isModeChange = false;      //모드 전환 여부 체크
    private float defaultModeFOV = 60;      //기본모드 카메라 FOV
    private float aimModeFOV = 30;          //Aim 모드 카메라 FOV

    private AudioSource audioSource;                //사운드 재생 컴포넌트
    private PlayerAnimatorController animator;      //애니메이션 재생 제어
    private CasingMemortPool casingMemortPool;      //탄피 생성 후 활성/비활성 관리
    private ImpactMemotyPool impactMemotyPool;      //공격 효과 생성후 활성/비활성 관리
    private Camera mainCamera;                      //광선 발사

    //외부에서 필요한 정보를 열람하기 위해 정의한 Get Property's
    public WeaponName WeaponName => weaponSetting.weaponName;
    public int CurrentMagazine => weaponSetting.currentMagazine;
    public int MaxMagazine => weaponSetting.maxMagazine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponentInParent<PlayerAnimatorController>();
        casingMemortPool = GetComponent<CasingMemortPool>();
        impactMemotyPool = GetComponent<ImpactMemotyPool>();
        mainCamera = Camera.main;

        weaponSetting.currentMagazine = weaponSetting.maxMagazine;
        weaponSetting.currentAmmo = weaponSetting.maxAmmo;
    }

    private void OnEnable()
    {
        PlaySound(audioClipTakeOutWeapon);  //무기 장착 사운드 재생
        muzzleFlashEffect.SetActive(false); //총구 이펙트 비활성화

        onMagazineEvent.Invoke(weaponSetting.currentMagazine);                      //무기가 활성화될 때 해당 무기 탄창 정보를 갱신
        onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);       //무기가 활성화될 때 해당 무기 탄 수 정보를 갱신

        ResetVariables();
    }

    public void StartWeaponAction(int type = 0)
    {
        if (isReload == true) return;                   //재장전 중이면 무기 액션 불가능

        if (isModeChange == true) return;               //모드 전환중이면 무기 액선 불가능

        if(type == 0)
        {
            //연속 공격
            if(weaponSetting.isAutomaticAttack == true)
            {
                isAttak = true;
                StartCoroutine("OnAttackLoop");
            }
            else    //단발 공격
            {
                OnAttack();
            }
        }
        else
        {
            if (isAttak == true) return;

            StartCoroutine("OnModeChange");
        }
    }

    public void StopWeaponAction(int type = 0)
    {
        //마우스 왼쪽 클릭 (공격 종료)
        if(type == 0)
        {
            isAttak = false;
            StopCoroutine("OnAttackLoop");
        }
    }

    public void StartReload()
    {

        //현재 재장전 중이면 재장전 불가
        if (isReload == true || weaponSetting.currentMagazine <= 0) return;

        //무기 액션중 'R'키를 눌러 재장전을 하면 무기 액션 중지 후 재장전
        StopWeaponAction();

        StartCoroutine("OnReload1");
    }

    private IEnumerator OnAttackLoop()
    {
        while (true)
        {
            OnAttack();

            yield return null;
        }
    }

    public void OnAttack()
    {
        if(Time.time - lastAttackTime > weaponSetting.attackRate)
        {
            //뛰고있을 때는 공격할 수 없다.
            if(animator.MoveSpeed > 0.5f)
            {
                return;
            }

            //공격주기가 되어야 공격할 수 있도록 하기 위한 현재 시간 저장
            lastAttackTime = Time.time;
            
            //탄 수가 없으면 공격 불가능
            if(weaponSetting.currentAmmo <= 0)
            {
                return;
            }
            //공격시 커렌트애모를 1 감소
            weaponSetting.currentAmmo --;
            onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

            //무기 애니메이션 재생 (모드에 따라 애니메이션 재생)
            string animation = animator.AimModeIs == true ? "AimFire" : "Fire";
            animator.Play(animation, -1, 0);

            //총구 이펙트 재생 조준 안할떄만 재생
            if (animator.AimModeIs == false)
                StartCoroutine("OnMuzzleFlashEffect");

            //공격 사운드 재생
            PlaySound(audioClipFire);

            //탄피 생성
            casingMemortPool.SpawnCasing(casingSpawnPoint.position, transform.right);

            //광선을 발사해 원하는 위치 공격
            TwoStepRaycast();
        }
    }

    private IEnumerator OnMuzzleFlashEffect()
    {
        muzzleFlashEffect.SetActive(true);

        yield return new WaitForSeconds(weaponSetting.attackRate * 0.3f);

        muzzleFlashEffect.SetActive(false);
    }

    private IEnumerator OnReload1()
    {
        isReload = true;

        //재장전 애니메이션, 사운드 재생
        animator.OnReload();
        PlaySound(audioClipReload);

        while(true)
        {
            if(audioSource.isPlaying == false && (animator.CurrentAnimationIs("Movement") || animator.CurrentAnimationIs("AimFirePose")))
            {
                isReload = false;

                weaponSetting.currentMagazine--;
                onMagazineEvent.Invoke(weaponSetting.currentMagazine);

                weaponSetting.currentAmmo = weaponSetting.maxAmmo;
                onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

                yield break;
            }

            yield return null;
        }
    }

    private void TwoStepRaycast()
    {
        Ray ray;
        RaycastHit hit;
        Vector3 targetPoint = Vector3.zero;

        //화면 중앙 좌표 에임 기준으로 발사
        ray = mainCamera.ViewportPointToRay(Vector2.one * 0.5f);

        //attackDistance 안에 부딪히는 오브젝트가 있으면 targetPoint는 광선에 부딪힌 위치
        if(Physics.Raycast(ray, out hit, weaponSetting.attackDistance))
        {
            targetPoint = hit.point;
        }
        else  //공격 사거리 안에 부딪히는 오브젝트가 없으면 targetpoint는 최대 거리
        {
            targetPoint = ray.origin + ray.direction * weaponSetting.attackDistance;
        }
        Debug.DrawRay(ray.origin, ray.direction * weaponSetting.attackDistance, Color.red);

        //첫번째 Raycast연산으로 얻어진 targetPoint를 목표지점으로 설정하고, 총구를 시작지점으로 하여 Raycast 연산
        Vector3 attacklDirection = (targetPoint - bulletSpawnPoint.position).normalized;
        if(Physics.Raycast(bulletSpawnPoint.position, attacklDirection, out hit, weaponSetting.attackDistance))
        {
            impactMemotyPool.SpawnImpact(hit);
        }
        Debug.DrawRay(bulletSpawnPoint.position, attacklDirection * weaponSetting.attackDistance, Color.blue);
    }

    private IEnumerator OnModeChange()
    {
        float current = 0;
        float percent = 0;
        float time = 0;

        animator.AimModeIs = !animator.AimModeIs;
        imageAim.enabled = !imageAim.enabled;

        float start = mainCamera.fieldOfView;
        float end = animator.AimModeIs == true ? aimModeFOV : defaultModeFOV;

        isModeChange = true;

        while(percent < 1)
        {
            current += Time.deltaTime;
            percent = current / time;

            mainCamera.fieldOfView = Mathf.Lerp(start, end, percent);

            yield return null;
        }

        isModeChange = false;
    }

    private void ResetVariables()
    {
        isReload = false;
        isAttak = false;
        isModeChange = false;
    }

    private void PlaySound(AudioClip clip)
    {
        audioSource.Stop();             //재생중인 사운드가 있으면 사운드를 정지.
        audioSource.clip = clip;        //새로운 사운드 clip으로 교체 후
        audioSource.Play();             //사운드 재생
    }
}
