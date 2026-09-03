using UnityEngine;

namespace PlatinumQuestScripts
{
    public class BlastToTheBeatMode : ISpecialGameMode
    {
        private readonly GameObject missionRoot;

        public BlastToTheBeatMode(GameObject missionRoot)
        {
            this.missionRoot = missionRoot;
        }

        public void OnMissionLoad()
        {
            if (missionRoot == null)
                return;
        }

        public void OnRestart()
        {
            GameManager.instance.PlayBassPunchAudio();
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
        public void ProcessMaterialContact(Marble marble, CollisionInfo contact)
        {

        }
    }
}
