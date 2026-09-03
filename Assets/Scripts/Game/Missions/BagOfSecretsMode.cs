using UnityEngine;

namespace PlatinumQuestScripts
{
    public class BagOfSecretsMode : ISpecialGameMode
    {
        private readonly GameObject missionRoot;

        public BagOfSecretsMode(GameObject missionRoot)
        {
            this.missionRoot = missionRoot;
        }

        public void OnMissionLoad()
        {
            Debug.Log("BagOfSecretsMode: OnMissionLoad");

            GameObject gravityItem =
                FindObject("secretgravityitem1");

            if (gravityItem != null)
                gravityItem.SetActive(false);
            else
                Debug.LogWarning(
                    "BagOfSecretsMode: secretgravityitem1 not found."
                );

            GameObject timeTravel =
                FindObject("topareatimetravel");

            if (timeTravel != null)
                timeTravel.SetActive(false);
            else
                Debug.LogWarning(
                    "BagOfSecretsMode: topareatimetravel not found."
                );

            GameObject catapult =
                FindObject("secretcatapult");

            if (catapult != null)
            {
                MovingPlatform movingPlatform =
                    catapult.GetComponent<MovingPlatform>();

                if (movingPlatform != null)
                    movingPlatform.GoToTime(0f);
                else
                    Debug.LogWarning(
                        "BagOfSecretsMode: secretcatapult "
                        + "does not have a MovingPlatform component."
                    );
            }
            else
            {
                Debug.LogWarning(
                    "BagOfSecretsMode: secretcatapult not found."
                );
            }
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

        private GameObject FindObject(string objectName)
        {
            if (missionRoot == null)
                return null;

            Transform[] transforms =
                missionRoot.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in transforms)
            {
                if (string.Equals(
                    t.gameObject.name,
                    objectName,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    return t.gameObject;
                }
            }

            return null;
        }

        public void ProcessMaterialContact(Marble marble, CollisionInfo contact)
        {

        }
    }
}
