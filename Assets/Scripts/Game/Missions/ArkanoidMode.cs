using System.Collections.Generic;
using UnityEngine;

namespace PlatinumQuestScripts
{
    public class ArkanoidMode : ISpecialGameMode
    {
        private readonly GameObject missionRoot;

        private readonly List<FadePlatform> bricks =
            new List<FadePlatform>();

        private bool finished;

        public ArkanoidMode(GameObject missionRoot)
        {
            this.missionRoot = missionRoot;
        }

        public void OnMissionLoad()
        {
            bricks.Clear();
            finished = false;

            if (missionRoot != null)
            {
                FadePlatform[] platforms =
                    missionRoot.GetComponentsInChildren<FadePlatform>(true);

                bricks.AddRange(platforms);
            }

            Debug.Log(
                $"ArkanoidMode: Found {bricks.Count} bricks."
            );
        }

        public void OnRestart()
        {
            finished = false;
        }

        public void OnRespawn()
        {

        }

        public void Update()
        {
            if (finished)
                return;

            if (bricks.Count == 0)
            {
                Finish();
                return;
            }

            foreach (FadePlatform brick in bricks)
            {
                if (brick == null)
                    continue;

                if (brick.CurrentOpacity != 0f)
                    return;
            }

            Finish();
        }

        public void OnJump()
        {
        }


        private void Finish()
        {
            if (finished)
                return;

            finished = true;

            Debug.Log(
                "ArkanoidMode: All bricks destroyed."
            );

            GameManager.onFinish?.Invoke();
        }

        public void ProcessMaterialContact(Marble marble, CollisionInfo contact)
        {

        }
    }
}
