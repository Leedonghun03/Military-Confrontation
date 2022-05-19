using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Input keyCodes")]
    [SerializeField]
    private KeyCode keyCodeRun = KeyCode.LeftShift; //달리는 키 LeftShift
    [SerializeField]
    private KeyCode keyCodeJump = KeyCode.Space; //점프 키 Space
    [SerializeField]
    private KeyCode keyCodeReload = KeyCode.R;  //재장전 키 R

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip audioClipWalk;    //걷는 사운드
    [SerializeField]
    private AudioClip audioClipRun;     //달리는 사운드

    private RotateToMouse rotateToMouse;            //마우스 이동으로 카메라 회전
    private MovementCharacterController movement;   // 키보드 입력으로 플레이어 이동, 점프
    private Status status;                          //이동속도 등의 플레이어 정보
    private PlayerAnimatorController animator;      //애니메이션 재생 제어
    private AudioSource audioSource;                //사운드 재생제어
    private WeaponAssaultRifle weapon;              //무기를 이용한 공격 제어

    private void Awake()
    {
        Cursor.visible = false; //마우스 커서 false
        Cursor.lockState = CursorLockMode.Locked;

        rotateToMouse = GetComponent<RotateToMouse>();
        movement = GetComponent<MovementCharacterController>();
        status = GetComponent<Status>();
        animator = GetComponent<PlayerAnimatorController>();
        audioSource = GetComponent<AudioSource>();
        weapon = GetComponentInChildren<WeaponAssaultRifle>();
    }

    private void Update()
    {
        UpdateRotate();
        UpdateMove();
        UpdateJump();
        UpdateWeaponAction();
    }

    private void UpdateRotate()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        rotateToMouse.UpdateRotation(mouseX, mouseY);
    }

    private void UpdateMove()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        if (x != 0 || z != 0)    //이동중 일 때(걷기 or 뛰기)
        {
            bool isRun = false;

            //옆이나 뒤로 이동은 할 수 없다.
            if (z > 0) isRun = Input.GetKey(keyCodeRun);    //z값이 커지면 isRun이 True로 변함 옆으로 이동은 안그럼

            movement.MoveSpeed = isRun == true ? status.RunSpeed : status.WalkSpeed;
            animator.MoveSpeed = isRun == true ? 1 : 0.5f;
            audioSource.clip = isRun == true ? audioClipRun : audioClipWalk;

            if(audioSource.isPlaying == false)  //방향키 입력 여부는 매 프레임 확인하기 때문에 재생중일 때는 다시 재생하지 않도록 isPlaying으로 체크 후 재생
            {
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else   //제자리에 멈춰있는 경우
        {
            movement.MoveSpeed = 0;
            animator.MoveSpeed = 0;

            if(audioSource.isPlaying == true)   //멈췄을 때 사운드가 재생중이면 정지한다.
            {
                audioSource.Stop();
            }
        }

        movement.MoveTo(new Vector3(x, 0, z));
    }

    private void UpdateJump()
    {
        if (Input.GetKeyDown(keyCodeJump))
        {
            movement.Jump();
        }
    }

    private void UpdateWeaponAction()
    {
        if (Input.GetMouseButtonDown(0))
        {
            weapon.StartWeaponAction();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            weapon.StopWeaponAction();
        }

        if(Input.GetKeyDown(keyCodeReload))
        {
            Debug.Log("R 입력됬음");
            weapon.StartReload();
        }
    }
}
