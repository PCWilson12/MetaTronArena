using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using NETWORK_ENGINE;
using Unity.VisualScripting;

public class NetworkPlayerController : HighLevelEntity
{

    public Vector2 LastInput;
    public Vector2 AimVector;
    public Vector3 AimPosition;
    public Vector3 AimDirection;
    public Rigidbody myRig;
    public float speed = 5;
    public float Overheat = 0;
    public bool canShoot = true;
    public bool lastFire=false;
    public bool passiveActive = false;
    public string pname;
    RaycastHit hit;
    public Vector3 SpawnLoc;

    public Vector2 ParseV2(string v)
    {
        Vector2 temp = new Vector2();
        string[] args = v.Trim('(').Trim(')').Split(',');
        temp.x = float.Parse(args[0]); 
        temp.y = float.Parse(args[1]);
        return temp;
    }
    public Vector3 ParseV3(string v)
    {
        Vector3 temp = new Vector3();
        string[] args = v.Trim('(').Trim(')').Split(',');
        temp.x = float.Parse(args[0]);
        temp.y = float.Parse(args[1]);
        temp.z = float.Parse(args[2]);
        return temp;
    }

    public void ActionHandler(InputAction.CallbackContext c)
    {
       if(IsLocalPlayer)
        {
            if (c.started || c.performed)
            {
                //Send input
                SendCommand("MVC", c.ReadValue<Vector2>().ToString("F2"));
            }
            else if (c.canceled)
            {
                //Send vector 2.zero.
                SendCommand("MVC", Vector2.zero.ToString("F2"));
            }
        }
    }
    public void Aiming(InputAction.CallbackContext a)
    {
        if(IsLocalPlayer)
        {
            SendCommand("AIM", a.ReadValue<Vector2>().ToString());
        }
    }
    public void Shoot(InputAction.CallbackContext s)
    {
        if (IsLocalPlayer)
        {
            if (s.started)
            {
                SendCommand("FIRE", "True");
                
            }

            else if (s.canceled)
            {
                SendCommand("FIRE", "False");
            }
        }
    }
    public void HeatVent(InputAction.CallbackContext h)
    {
        if (IsLocalPlayer && Overheat<100) 
        {
            SendCommand("RELOAD", "''");
        }
    }
    public IEnumerator Reload()
    {
        yield return new WaitForSeconds(2);
        canShoot = true;
        Overheat = 0;
        SendUpdate("OH", Overheat.ToString());
        SendUpdate("CANSHOOT", canShoot.ToString());
       
    }
    public IEnumerator ROF()
    {
        if (Overheat < 100)
        {
            yield return new WaitForSeconds(.25f);
            canShoot = true;
            SendUpdate("CANSHOOT", canShoot.ToString());
        }
    }
    public IEnumerator OH()
    {
        yield return new WaitForSeconds(4);
        Overheat= 0;
        canShoot= true;
        SendUpdate("CANSHOOT", canShoot.ToString());
        SendUpdate("OH", Overheat.ToString());
    }
    
    public override void HandleMessage(string flag, string value)
    {
        base.HandleMessage(flag, value);
        if (IsClient && flag == "SHIELD")
        {
            OverShield = int.Parse(value);
        }
        if (IsClient && flag == "HP")
        {
            HP = int.Parse(value);
        }
        if (IsServer && flag == "MVC")
        {
            LastInput = ParseV2(value);
        }
        if(IsServer && flag == "AIM")
        {
            AimVector = ParseV2(value);
        }
        if(IsServer && flag == "FIRE")
        {
            lastFire = bool.Parse(value);
            
        }
        if(IsClient && flag == "CANSHOOT")
        {
            canShoot= bool.Parse(value);
            
        }
        if(IsClient && flag == "OH")
        {
            Overheat = float.Parse(value);
         
        }
        if(IsServer && flag == "RELOAD")
        {
            canShoot = false;
            SendUpdate("CANSHOOT", "False");
            StartCoroutine(Reload());
        }
        if(IsServer && flag == "AP")
        {
            AimPosition = ParseV3(value);
            
        }
        if(IsServer && flag == "AD")
        {
            AimDirection= ParseV3(value);
            
        }
        
        if(flag == "PN")
        {
            pname = value;
        }
    }

    public override void NetworkedStart()
    {
      
    }

    public override IEnumerator SlowUpdate()
    {
        while(IsServer)
        {
            if(lastFire && canShoot)
            {
                
                if(Physics.Raycast(AimPosition, AimDirection,out hit))
                {
                        if(hit.collider.tag=="Entity")
                    {
                        hit.transform.GetComponent<HighLevelEntity>().Damage(this.DamageScalar);
                    }
                    
                }
                Overheat = Overheat + 5;
                SendUpdate("OH", Overheat.ToString());
                canShoot = false;
                SendUpdate("CANSHOOT", canShoot.ToString());
                StartCoroutine(ROF());
            }
            if (Overheat >= 100)
            {
                canShoot = false;
                SendUpdate("CANSHOOT", canShoot.ToString());
                StartCoroutine(OH());
            }
            if (!lastFire && Overheat > 0 && Overheat<100)
            {
                Overheat= Overheat - .1f;
                SendUpdate("OH", Overheat.ToString());
            }
            if (IsDirty)
            {
                SendUpdate("PN", pname);
                SendUpdate("MVC", myRig.velocity.ToString());
                IsDirty= false;
            }
            if (HP <= 0)
            {
                StartCoroutine(Respawn());
            }
            yield return new WaitForSeconds(.1f);
        }
        while (IsLocalPlayer)
        {
            SendCommand("AP", Camera.main.transform.position.ToString());
            SendCommand("AD", Camera.main.transform.forward.ToString());
            yield return new WaitForSeconds(.1f);
        }
    }

    // Start is called before the first frame update
    public void Start()
    {
        myRig = GetComponent<Rigidbody>();
        SpawnLoc= myRig.position;
        
    }

    // Update is called once per frame
    public void Update()
    {
        if (IsServer && HP>0)
        {
            myRig.velocity = transform.forward * LastInput.y * speed + transform.right * LastInput.x *speed;
            
            myRig.angularVelocity = new Vector3(0, AimVector.x, 0);
            
           
            
            

        }
        if (IsLocalPlayer)
        {
            Camera.main.transform.position = transform.position + transform.forward * .5f + this.transform.up;
            Camera.main.transform.forward = transform.forward;
            



        }
    }
    
    public IEnumerator Respawn()
    {
        yield return new WaitForSeconds(3);
        myRig.position= SpawnLoc;
        HP = 100;
        OverShield=50;
        SendUpdate("SHIELD", OverShield.ToString());
        SendUpdate("HP", HP.ToString());
    }
}