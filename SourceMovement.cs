using Network;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    Rigidbody rb;
    GameObject cam;
    
    public float distToGround = 1;
    
    float rotX = 0;
    float rotY = 0;
    float rotZ = 0;

    bool isCrouched;
    bool prevJumpInput;

    Vector3[] groundCheckVectors = 
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(0f, 0f, 0.2f),
        new Vector3(0f, 0f, -0.2f),
        
        new Vector3(0.2f, 0f, 0f),
        new Vector3(0.2f, 0f, 0.2f),
        new Vector3(0.2f, 0f, -0.2f),
        
        new Vector3(-0.2f, 0f, 0f),
        new Vector3(-0.2f, 0f, 0.2f),
        new Vector3(-0.2f, 0f, -0.2f),
    };

    bool inCollision;
    ContactPoint[] contacts;



    public bool forwardInput;
    public bool backInput;
    public bool rightInput;
    public bool leftInput;

    public bool lookUp;
    public bool lookDown;
    public bool lookRight;
    public bool lookLeft;

    public bool freeLook;

    public bool jump;
    public bool crouch;
    public bool run;
    public bool noclip;

    public float mouseX;
    public float mouseY;



    public float GetVelocity()
    {
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        return vel.magnitude;
    }

    public void ToggleNoclip()
    {
        noclip = !noclip;

        if (noclip)
            GetComponent<CapsuleCollider>().isTrigger = true;
        else
            GetComponent<CapsuleCollider>().isTrigger = false;
    }



    private void LookRight(bool input)
    {
        if (input)
            rotY += (float)Server.GetVar("sensitivity");
    }

    private void LookLeft(bool input)
    {
        if (input)
            rotY -= (float)Server.GetVar("sensitivity");
    }

    private void LookUp(bool input)
    {
        if (input)
            rotX = Mathf.Clamp(rotX + (float)Server.GetVar("sensitivity"), -85, 85);
    }

    private void LookDown(bool input)
    {
        if (input)
            rotX = Mathf.Clamp(rotX - (float)Server.GetVar("sensitivity"), -85, 85);
    }

    private void FreeLook()
    {
        rotY += (float)Server.GetVar("sensitivity") * mouseX;
        rotX = Mathf.Clamp(rotX + (float)Server.GetVar("sensitivity") * mouseY, -85, 85);
    }

    private bool CanJump(bool input)
    {
        if ((bool)Server.GetVar("sv_autojump"))
            return input;

        bool canJump = false;

        if (input && !prevJumpInput)
            canJump = true;

        prevJumpInput = input;

        return canJump;
    }

    private void Jump(bool input)
    {
        if (CanJump(input) && IsOnGround())
            rb.velocity += new Vector3(0, 0.4f * (float)Server.GetVar("sv_gravity"), 0);
    }

    private void Crouch(bool input)
    {
        if (input && !isCrouched)
        {
            GetComponent<CapsuleCollider>().height = 1;
            cam.transform.localPosition = new Vector3(0, 0.4f, 0);

            isCrouched = true;
        }
        else if (!input && isCrouched)
        {
            GetComponent<CapsuleCollider>().height = 2;
            cam.transform.localPosition = new Vector3(0, 0.8f, 0);

            isCrouched = false;
        }
    }

    private void CameraTilt(bool left, bool right)
    {
        if (!(bool)Server.GetVar("cameratilt"))
            return;

        if (!(left ^ right))
        {

            if (rotZ > 0)
                rotZ = Mathf.Max(rotZ - Time.deltaTime * 20, 0);

            if (rotZ < 0)
                rotZ = Mathf.Min(rotZ + Time.deltaTime * 20, 0);

            return;
        }

        if (left)
            rotZ = Mathf.Min(rotZ + Time.deltaTime * 20, 3);

        if (right)
            rotZ = Mathf.Max(rotZ - Time.deltaTime * 20, -3);
    }

    private Vector3 GetMoveDir()
    {
        Vector3 dir = Vector3.zero;

        if (forwardInput)
            dir += new Vector3(rb.transform.forward.x, 0, rb.transform.forward.z);

        if (backInput)
            dir -= new Vector3(rb.transform.forward.x, 0, rb.transform.forward.z);

        if (rightInput)
            dir += new Vector3(rb.transform.right.x, 0, rb.transform.right.z);

        if (leftInput)
            dir -= new Vector3(rb.transform.right.x, 0, rb.transform.right.z);

        //if (!IsOnGround())
        //{
        //    if (Input.GetAxis("Mouse X") < 0)
        //        dir -= cam.transform.right;
        //    else if (Input.GetAxis("Mouse X") > 0)
        //        dir += cam.transform.right;
        //}
        
        return dir.normalized;
    }

    private Vector3 GetNoclipDir()
    {
        Vector3 dir = Vector3.zero;

        if (forwardInput)
            dir += cam.transform.forward;

        if (backInput)
            dir -= cam.transform.forward;

        if (rightInput)
            dir += cam.transform.right;

        if (leftInput)
            dir -= cam.transform.right;
     
        return dir.normalized;
    }



    private float GetGroundSlope(Vector3 position)
    {
        float dist = isCrouched ? distToGround / 2 : distToGround;

        RaycastHit hit;

        if (!IsOnGround())
            return 0;

        if (Physics.Raycast(rb.transform.position + position, Vector3.down, out hit, dist + 0.5f) && !hit.collider.isTrigger)
            return Vector3.Angle(hit.normal, Vector3.up);

        return 0;
    }

    public bool IsOnGround()
    {
        float dist = isCrouched ? distToGround / 2 : distToGround;

        if (rb.velocity.y > 2)
            return false;

        for (int i = 0; i < groundCheckVectors.Length; i++)
            if (Physics.Raycast(rb.transform.position + groundCheckVectors[i], Vector3.down, dist + 0.001f, 1, QueryTriggerInteraction.Ignore))
                return true;

        return false;
    }

    private Vector3 Accelerate(Vector3 prevVel, Vector3 accelDir, float maxSpeed, float accel)
    {
        float projVel = Vector3.Dot(prevVel, accelDir);
        float addVel = accel * Time.fixedDeltaTime;

        if (projVel + addVel > maxSpeed)
            addVel = maxSpeed - projVel;

        return prevVel + accelDir * addVel;
    }

    private Vector3 MoveAir(Vector3 prevVel, Vector3 accelDir)
    {
        return Accelerate(prevVel, accelDir, (float)Server.GetVar("sv_airmaxspeed"), (float)Server.GetVar("sv_airaccelerate"));
    }

    private Vector3 MoveNoclip()
    {
        float speed = (float)Server.GetVar("sv_noclipmaxspeed");

        if (isCrouched)
            speed /= 2;
        else if (run)
            speed *= 2;

        return GetNoclipDir() * speed;
    }

    private Vector3 MoveGround(Vector3 prevVel,  Vector3 accelDir)
    {
        float maxSpeed = (float)Server.GetVar("sv_maxspeed");

        if (isCrouched)
            maxSpeed /= 2;
        else if (run)
            maxSpeed *= 2;

        float speed = prevVel.magnitude;

        if (speed != 0) 
        {
            float drop = speed * (float)Server.GetVar("sv_friction") * Time.fixedDeltaTime;
            prevVel *= Mathf.Max(speed - drop, 0) / speed; 
        }
        
        prevVel = Quaternion.AngleAxis(GetGroundSlope(rb.transform.position), accelDir) * prevVel;

        return Accelerate(prevVel, accelDir, maxSpeed, (float)Server.GetVar("sv_accelerate"));
    }

    private float GetStep(Vector3 dir)
    {
        RaycastHit hit;

        if (Physics.Raycast(rb.transform.position - new Vector3(0, isCrouched ? 0.49f : 0.99f, 0), dir.normalized, out hit, 1f))
            if (!hit.collider.isTrigger)
                if (Physics.Raycast(rb.transform.position + dir.normalized, Vector3.down, out hit, 1f))
                    if (!hit.collider.isTrigger && hit.normal == Vector3.up)
                        return Vector3.Distance(hit.point, rb.transform.position + dir.normalized - new Vector3(0, isCrouched ? 0.49f : 0.99f, 0)) + 0.1f;
                
        return 0;
    }

    private Vector3 ClipVelocity(Vector3 dir)
    {
        if (!inCollision || contacts == null)
            return dir;

        foreach (ContactPoint point in contacts)
        {
            if (point.normal == Vector3.up)
                continue;

            float dot = Vector3.Dot(dir, point.normal);

            if (dot < 0)
                return dir - point.normal * dot;
        }

        return dir;
    }



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = transform.Find("PlayerCamera").gameObject;
    }

    void Update()
    {
        if (!LocalClient.instance.IsServer())
            return;

        Jump(jump);
        Crouch(crouch);

        LookUp(lookUp);
        LookDown(lookDown);
        LookRight(lookRight);
        LookLeft(lookLeft);

        //CameraTilt(leftInput, rightInput);

        FreeLook();
    }

    void FixedUpdate()
    {
        if (!LocalClient.instance.IsServer())
            return;

        //cam.fieldOfView = (float)Server.GetVar("fov");
        //cam.transform.Find("WeaponCamera").GetComponent<Camera>().fieldOfView = (float)Server.GetVar("fov");

        rb.transform.rotation = Quaternion.Euler(0, rotY, 0);
        cam.transform.localRotation = Quaternion.Euler(-rotX, 0, rotZ);
        rb.angularVelocity = Vector3.zero;

        if (noclip)
        {
            rb.velocity = MoveNoclip();
            return;
        }

        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        if (IsOnGround())
        {
            vel = MoveGround(vel, GetMoveDir());
            vel = new Vector3(vel.x, 0, vel.z);
            
            float step = GetStep(vel);
          
            if (step > 0)
                transform.position += new Vector3(0, step, 0);
        }
        else
        { 
            vel = MoveAir(vel, GetMoveDir());
            float gravity = rb.velocity.y - (float)Server.GetVar("sv_gravity") * Time.fixedDeltaTime;
            vel = new Vector3(vel.x, gravity, vel.z);
        }

        rb.velocity = ClipVelocity(vel);
    }

    void OnCollisionEnter()
    {
        inCollision = true;
    }

    void OnCollisionExit()
    {
        inCollision = false;
    }

    void OnCollisionStay(Collision collision)
    {
        inCollision = true;
        contacts = collision.contacts;
    }

}
