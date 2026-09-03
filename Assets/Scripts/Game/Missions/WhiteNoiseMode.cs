using UnityEngine;

namespace PlatinumQuestScripts
{
    public class WhiteNoiseMode : ISpecialGameMode
    {
        private readonly GameObject missionRoot;

        // -----------------------------------------------------------------
        // White Noise state
        // -----------------------------------------------------------------

        private bool buzzsawd = false;

        private float smbImpulse = 0f;
        private float smbUpwards = 0f;
        private float smbBlockUntil = -100000000f;

        private bool pendingSmbJump = false;

        public WhiteNoiseMode(GameObject missionRoot)
        {
            this.missionRoot = missionRoot;
        }

        // -----------------------------------------------------------------
        // Mission lifecycle
        // -----------------------------------------------------------------

        public void OnMissionLoad()
        {
            // Equivalent to initial Haxe field values.
            buzzsawd = false;

            smbImpulse = 0f;
            smbUpwards = 0f;
            smbBlockUntil = -100000000f;

            pendingSmbJump = false;
        }

        public void OnRestart()
        {
            buzzsawd = false;
            pendingSmbJump = false;

            smbImpulse = 0f;
            smbUpwards = 0f;
            smbBlockUntil = -100000000f;
        }

        public void OnRespawn()
        {
            buzzsawd = false;
            pendingSmbJump = false;
        }

        // -----------------------------------------------------------------
        // SMBTrigger
        // -----------------------------------------------------------------

        public void SmbTriggerEnter(
            float impulse,
            float upwards,
            float currentTime)
        {
            smbImpulse = impulse;
            smbUpwards = upwards;
            smbBlockUntil = currentTime + 0.3f;
        }

        public void SmbTriggerLeave(float currentTime)
        {
            if (currentTime < smbBlockUntil)
                return;

            smbImpulse = 0f;
            smbUpwards = 0f;
        }

        // -----------------------------------------------------------------
        // Jump
        // -----------------------------------------------------------------

        public void OnJump()
        {
            if (smbImpulse != 0f ||
                smbUpwards != 0f)
            {
                pendingSmbJump = true;
            }
        }

        // -----------------------------------------------------------------
        // Material contact
        //
        // Haxe:
        //
        // if (contact.otherObject is InteriorObject) {
        //     var igo = cast(contact.otherObject, InteriorObject);
        //     if (igo.interiorFile.indexOf("buzzsaw") != -1)
        //         buzzsawd = true;
        // }
        // -----------------------------------------------------------------

        public void ProcessMaterialContact(
            Marble marble,
            CollisionInfo contact)
        {
            if (marble == null)
                return;

            if (contact == null)
                return;

            if (contact.collider == null)
                return;

            Dif dif =
                contact.collider.GetComponentInParent<Dif>();

            if (dif == null)
                return;

            if (string.IsNullOrEmpty(dif.filePath))
                return;

            if (dif.filePath.IndexOf(
                "buzzsaw",
                System.StringComparison.OrdinalIgnoreCase) != -1)
            {
                buzzsawd = true;
            }
        }

        // -----------------------------------------------------------------
        // Update
        // -----------------------------------------------------------------

        public void Update()
        {
            Marble marble = Marble.instance;
            Movement movement = Movement.instance;

            if (marble == null || movement == null)
                return;

            // -------------------------------------------------------------
            // SMB jump
            //
            // Haxe:
            //
            // velocity += new h3d.Vector(0, 0, smbImpulse)
            //
            // velocity.z = max(velocity.z, smbUpwards)
            //
            // Therefore this MUST use Unity Z, not Y.
            // -------------------------------------------------------------

            if (pendingSmbJump)
            {
                pendingSmbJump = false;

                Vector3 velocity =
                    movement.marbleVelocity;

                if (smbImpulse != 0f)
                {
                    velocity += new Vector3(
                        0f,
                        smbImpulse,
                        0f
                    );
                }

                if (smbUpwards != 0f)
                {
                    velocity.y =
                        Mathf.Max(
                            velocity.y,
                            smbUpwards
                        );
                }

                movement.marbleVelocity = velocity;
            }

            // -------------------------------------------------------------
            // Buzzsaw
            // -------------------------------------------------------------

            if (buzzsawd)
            {
                buzzsawd = false;

                Transform startPos =
                    GetSpawnTransform();

                if (startPos == null)
                    return;

                // Haxe:
                //
                // setMarblePosition(...)
                // velocity.set(0, 0, 0)
                // omega.set(0, 0, 0)
                // gameplayClock = 0
                //
                // SetPosition already clears both linear and angular
                // velocity in your Movement implementation.
                movement.SetPosition(
                    startPos.position
                );

                movement.marbleVelocity =
                    Vector3.zero;

                movement.marbleAngularVelocity =
                    Vector3.zero;

                if (GameManager.instance != null)
                {
                    GameManager.instance.elapsedTime =
                        0f;
                }
            }
        }

        // -----------------------------------------------------------------
        // Spawn
        // -----------------------------------------------------------------

        private Transform GetSpawnTransform()
        {
            return GameManager.instance
                .startPad
                .transform
                .Find("Spawn");
        }
    }
}