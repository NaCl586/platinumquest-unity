using UnityEngine;

namespace PlatinumQuestScripts
{
    public class SacredGroundMode : ISpecialGameMode
    {
        private readonly GameObject missionRoot;

        public SacredGroundMode(GameObject missionRoot)
        {
            this.missionRoot = missionRoot;
        }

        public void OnMissionLoad()
        {
        }

        public void OnRestart()
        {
        }

        public void OnRespawn()
        {
        }

        public void Update()
        {
        }

        public void OnJump()
        {
        }


        public void ProcessMaterialContact(
            Marble marble,
            CollisionInfo contact)
        {
            if (marble == null || contact == null)
                return;

            if (contact.collider == null)
                return;

            Dif dif = contact.collider.GetComponentInParent<Dif>();

            if (dif == null)
                return;

            if (string.IsNullOrEmpty(dif.filePath))
                return;

            if (dif.filePath.IndexOf(
                    "spike",
                    System.StringComparison.OrdinalIgnoreCase) < 0)
                return;

            GameManager.onOutOfBounds?.Invoke();
        }
    }
}
