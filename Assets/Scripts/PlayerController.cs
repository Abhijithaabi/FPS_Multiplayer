using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField] Transform viewPoint;
    public float mouseSensitivity = 1f;
    float verticalRotStore;
    [SerializeField]Vector2 mouseInput;
    [SerializeField] float moveSpeed=5f,runSpeed = 8f;
    float activeSpeed;
    Vector3 moveDir,movement;
    [SerializeField] CharacterController charCon;
    Camera cam;
    [SerializeField] float jumpForce=5f;
    [SerializeField] float gravityMod=2.5f;
    [SerializeField] Transform groundCheckPoint;
    bool isGrounded;
    [SerializeField] LayerMask groundLayers;
    [SerializeField] GameObject bulletImpact;
    [SerializeField] GameObject playerHitImpact;
    [SerializeField] Animator anim;
    [SerializeField] GameObject playerModel;
    [SerializeField] Material[] allSkins;
    [SerializeField] float adsSpeed=5f;
    [SerializeField] AudioSource footstepSlow,footstepFast;
    //[SerializeField] float timeBetweenShots=0.1f;
    float shotCounter;
    public float maxHeat=10f,/*heatPerShot=1f,*/coolRate=4f,overHeatCoolRate=5f;
    float heatCounter;
    bool overHeated;
    public Gun[] allGuns;
    int selectedGun;
    [SerializeField] float muzzleFlashTime;
    [SerializeField] Transform modelGunPoint,gunHolder;
    float muuzleCounter;
    public int maxHealth=100;
    int currentHealth;
   
    void Start()
    {
        Cursor.lockState=CursorLockMode.Locked;
        cam=Camera.main;
        UIController.Instance.weaponTempSlider.maxValue=maxHeat;
        currentHealth=maxHealth;
        
        // Transform newTrans = SpawnManager.instance.GetSpawnPoint();
        // transform.position=newTrans.position;
        // transform.rotation = newTrans.rotation;
        photonView.RPC("SetGun",RpcTarget.All,selectedGun);
        if(photonView.IsMine)
        {
            playerModel.SetActive(false);
            UIController.Instance.healthSlider.maxValue=maxHealth;
            UIController.Instance.healthSlider.value=currentHealth;
        }
        else
        {
            gunHolder.transform.parent=modelGunPoint.transform;
            gunHolder.localPosition=Vector3.zero;
            gunHolder.localRotation=Quaternion.identity;
        }
        playerModel.GetComponent<Renderer>().material=allSkins[photonView.Owner.ActorNumber % allSkins.Length];//% is used to so that it will never go above skins array length 
       
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }
        CameraMovement();
        PlayerMovement();
        CursorUpdate();
        if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
        {
            muuzleCounter -= Time.deltaTime;
            if (muuzleCounter <= 0)
            {
                allGuns[selectedGun].muzzleFlash.gameObject.SetActive(false);
            }
        }

        if (!overHeated)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }
            if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
            {
                shotCounter -= Time.deltaTime;
                if (shotCounter <= 0)
                {
                    Shoot();
                }
            }
            heatCounter -= coolRate * Time.deltaTime;
        }
        else
        {
            heatCounter -= overHeatCoolRate * Time.deltaTime;
            if (heatCounter <= 0)
            {
                overHeated = false;
                UIController.Instance.weaponOverheatTxt.gameObject.SetActive(false);
            }
        }
        if (heatCounter < 0)
        {
            heatCounter = 0;
        }
        UIController.Instance.weaponTempSlider.value = heatCounter;
        WeaponScroll();
        PlayerAnimations();
        if(Input.GetMouseButton(1))
        {
            cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,allGuns[selectedGun].adsZoom,adsSpeed*Time.deltaTime);
        }
        else
        {
            cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,60,adsSpeed*Time.deltaTime);//60 is default feild of view
        }

    }

    private void PlayerAnimations()
    {
        anim.SetBool("grounded", isGrounded);
        anim.SetFloat("speed", moveDir.magnitude);
    }

    private void WeaponScroll()
    {
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            selectedGun++;
            if (selectedGun >= allGuns.Length)
            {
                selectedGun = 0;
                //SwitchWeapon();
                photonView.RPC("SetGun",RpcTarget.All,selectedGun);
            }
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            selectedGun--;
            if (selectedGun <= 0)
            {
                selectedGun = allGuns.Length - 1;
            }
            //SwitchWeapon();
            photonView.RPC("SetGun",RpcTarget.All,selectedGun);
        }
        for(int i=0;i<allGuns.Length;i++)
        {
            if(Input.GetKeyDown((i+1).ToString()))
            {
                selectedGun=i;
                //SwitchWeapon();
                photonView.RPC("SetGun",RpcTarget.All,selectedGun);
            }
        }
    }

    private static void CursorUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0) && !UIController.Instance.OptionsScreen.activeInHierarchy)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    void LateUpdate()
    {
        if(photonView.IsMine)
        {
            if(MatchManager.instance.state == MatchManager.GameState.Playing)
            {
                cam.transform.position=viewPoint.position;
                cam.transform.rotation=viewPoint.rotation;
            }
            else
            {
                cam.transform.position=MatchManager.instance.mapCamPoint.transform.position;
                cam.transform.rotation=MatchManager.instance.mapCamPoint.transform.rotation;
            }
             
        }
        
    }
    void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f,0.5f,0));
        ray.origin=cam.transform.position;
        if(Physics.Raycast(ray,out RaycastHit hit))
        {

            if(hit.collider.gameObject.tag=="Player")
            {
                Debug.Log("Hit: "+hit.collider.gameObject.GetPhotonView().Owner.NickName);
                PhotonNetwork.Instantiate(playerHitImpact.name,hit.point,Quaternion.identity);
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage",RpcTarget.All,photonView.Owner.NickName,allGuns[selectedGun].shotDamage,PhotonNetwork.LocalPlayer.ActorNumber);    
            }
            else
            {
                GameObject bulletImpactObj = Instantiate(bulletImpact,hit.point+(hit.normal*0.002f),Quaternion.LookRotation(hit.normal,Vector3.up));
                Destroy(bulletImpactObj,10f);
            }
        }
        shotCounter = allGuns[selectedGun].timebetweenShots;
        heatCounter += allGuns[selectedGun].heatPerShot;
        if(heatCounter>=maxHeat)
        {
            overHeated=true;
            UIController.Instance.weaponOverheatTxt.gameObject.SetActive(true);
        }
        allGuns[selectedGun].muzzleFlash.gameObject.SetActive(true);
        muuzleCounter=muzzleFlashTime;
        allGuns[selectedGun].shotSound.Stop();
        allGuns[selectedGun].shotSound.Play();
    }
    [PunRPC]
    public void DealDamage(string damager,int damageAmount,int actor)
    {
        TakeDamage(damager,damageAmount,actor);
    }
    public void TakeDamage(string damager,int damageAmount,int actor)
    {
        //Debug.Log(photonView.Owner.NickName+ " has been Hit by :"+damager);
        if(photonView.IsMine)
        {
            currentHealth-=damageAmount;
            UIController.Instance.healthSlider.value=currentHealth;
            if(currentHealth<=0)
            {
                currentHealth=0;
                PlayerSpawner.instance.Die(damager);
                MatchManager.instance.UpdateStatsSend(actor,0,1);
            }
            
        }
        
    }

    private void PlayerMovement()
    {
        moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeSpeed=runSpeed;
            if(!footstepFast.isPlaying && moveDir!=Vector3.zero)
            {
                footstepFast.Play();
                footstepSlow.Stop();
            }
        }
        else
        {
            activeSpeed=moveSpeed;
            if(!footstepSlow.isPlaying && moveDir!=Vector3.zero)
            {
                footstepFast.Stop();
                footstepSlow.Play();
            }
        }
        if(moveDir==Vector3.zero || !isGrounded)
        {
            footstepFast.Stop();
            footstepSlow.Stop();
        }
        float yVel = movement.y;
        movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeSpeed;
        movement.y=yVel;
        if(charCon.isGrounded)
        {
            movement.y=0;
            
        }
        isGrounded = Physics.Raycast(groundCheckPoint.position,Vector3.down,0.25f,groundLayers);
        if(Input.GetButtonDown("Jump")&& isGrounded)
        {
            movement.y=jumpForce;
        }
        movement.y += Physics.gravity.y*Time.deltaTime*gravityMod;
        charCon.Move(movement * Time.deltaTime);
    }

    private void CameraMovement()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);
        verticalRotStore += mouseInput.y;
        verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);
        viewPoint.rotation = Quaternion.Euler(-verticalRotStore, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
    }
    void SwitchWeapon()
    {
        foreach(Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }
        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }
    [PunRPC]
    public void SetGun(int weaponToSwitch)
    {
        if(weaponToSwitch<allGuns.Length)
        {
            selectedGun=weaponToSwitch;
            SwitchWeapon();
        }
    }
}
