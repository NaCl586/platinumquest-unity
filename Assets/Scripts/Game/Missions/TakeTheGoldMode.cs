using UnityEngine;

namespace PlatinumQuestScripts
{
    public class TakeTheGoldMode : ISpecialGameMode
    {
        private readonly GameObject missionRoot;

        private PhysModTrigger finishGravity;
        private MovingPlatform mustChange;

        public TakeTheGoldMode(GameObject missionRoot)
        {
            this.missionRoot = missionRoot;
        }

        public void OnMissionLoad()
        {
            GameObject finishGravityObject =
                FindObject("finishgravity");

            if (finishGravityObject != null)
            {
                finishGravity =
                    finishGravityObject.GetComponent<PhysModTrigger>();

                if (finishGravity == null)
                {
                    Debug.LogWarning(
                        "TakeTheGoldMode: finishgravity "
                        + "does not have a PhysModTrigger."
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "TakeTheGoldMode: finishgravity not found."
                );
            }

            GameObject mustChangeObject =
                FindObject("mustchange");

            if (mustChangeObject != null)
            {
                mustChange =
                    mustChangeObject.GetComponent<MovingPlatform>();

                if (mustChange == null)
                {
                    Debug.LogWarning(
                        "TakeTheGoldMode: mustchange "
                        + "does not have a MovingPlatform."
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "TakeTheGoldMode: mustchange not found."
                );
            }
        }

        public void OnRestart()
        {
            UpdateFinishGravity();
        }

        public void OnRespawn()
        {
            UpdateFinishGravity();
        }

        public void Update()
        {
            UpdateFinishGravity();
        }

        public void OnJump()
        {
        }

        public void OnGemPickup()
        {
            if (GameManager.instance == null)
                return;

            if (GameManager.instance.CurrentGems !=
                GameManager.instance.TotalGems)
            {
                return;
            }

            if (mustChange == null)
                return;

            mustChange.GoToTime(4.001f);
        }

        public void ProcessMaterialContact(
            Marble marble,
            CollisionInfo contact)
        {
        }

        private void UpdateFinishGravity()
        {
            if (finishGravity == null ||
                GameManager.instance == null)
            {
                return;
            }

            finishGravity.Disabled =
                GameManager.instance.CurrentGems !=
                GameManager.instance.TotalGems;
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
    }
}