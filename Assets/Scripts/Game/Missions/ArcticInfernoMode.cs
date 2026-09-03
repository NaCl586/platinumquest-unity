using System.Collections.Generic;
using UnityEngine;

namespace PlatinumQuestScripts
{
    public class ArcticInfernoMode : ISpecialGameMode
    {
        // Original PQ:
        // Normal Scale = 0.18975
        // Mega Scale   = 0.6666
        //
        // 0.6666 / 0.18975 = ~3.5115
        private const float MEGA_MARBLE_SCALE = 3.511f;

        private readonly GameManager gameManager;

        private readonly List<IceShard> destroyedShards =
            new List<IceShard>();

        private readonly HashSet<IceShard> destroyedShardSet =
            new HashSet<IceShard>();

        private int score;

        private Vector3 originalMarbleScale;
        private float originalMarbleRadius;
        private bool originalScaleCaptured;

        public int Score => score;

        public IReadOnlyList<IceShard> DestroyedShards =>
            destroyedShards;

        public ArcticInfernoMode(GameManager gameManager)
        {
            this.gameManager = gameManager;
        }

        // ============================================================
        // Mission
        // ============================================================

        public void OnMissionLoad()
        {
            ResetState();
            EnableMegaMarble();
        }

        public void OnRestart()
        {
            ResetState();
            EnableMegaMarble();
        }

        public void OnRespawn()
        {
            // Arctic Inferno always uses Mega Marble.
            EnableMegaMarble();
        }

        // ============================================================
        // Update
        // ============================================================

        public void Update()
        {
        }

        // ============================================================
        // Jump
        // ============================================================

        public void OnJump()
        {
        }

        // ============================================================
        // Material Contact
        // ============================================================

        public void ProcessMaterialContact(
            Marble marble,
            CollisionInfo contact)
        {
        }

        // ============================================================
        // Mega Marble
        // ============================================================

        private void EnableMegaMarble()
        {
            Marble marble = Marble.instance;

            if (marble == null)
                return;

            /*
             * Capture the normal scale only once.
             *
             * We deliberately do not do:
             *
             *     marble.transform.localScale *= 3.511f;
             *
             * because OnRespawn() can be called repeatedly.
             */

            if (!originalScaleCaptured)
            {
                originalMarbleScale =
                    marble.transform.localScale;

                originalMarbleRadius = marble.movement.marbleRadius;

                originalScaleCaptured = true;
            }

            marble.transform.localScale =
                originalMarbleScale * MEGA_MARBLE_SCALE;

            marble.movement.marbleRadius = originalMarbleRadius * MEGA_MARBLE_SCALE;
        }

        // ============================================================
        // Ice Shard
        // ============================================================

        public void OnIceShardDestroyed(IceShard shard)
        {
            if (shard == null)
                return;

            /*
             * An Ice Shard can only award its points once.
             */
            if (!destroyedShardSet.Add(shard))
                return;

            destroyedShards.Add(shard);

            /*
             * IceShard already owns the point value.
             *
             * Red      = 1
             * Yellow   = 2
             * Blue     = 5
             * Platinum = 10
             *
             * ArcticInfernoMode simply consumes that value.
             */

            score += shard.Points;
            GameUIManager.instance.DisplayGemMessage(
                shard.message,
                shard.messageColor
            );
            Debug.Log(score);
            UpdateScoreUI();
        }

        // ============================================================
        // Score
        // ============================================================

        public int GetScore()
        {
            return score;
        }

        private void UpdateScoreUI()
        {
            if (GameUIManager.instance == null)
                return;

            GameUIManager.instance.SetCurrentMadnessHuntGem(score);
        }

        // ============================================================
        // Reset
        // ============================================================

        private void ResetState()
        {
            score = 0;

            destroyedShards.Clear();
            destroyedShardSet.Clear();

            UpdateScoreUI();
        }

        public void ResetScore()
        {
            score = 0;

            destroyedShards.Clear();
            destroyedShardSet.Clear();

            UpdateScoreUI();
        }

        // ============================================================
        // Queries
        // ============================================================

        public bool HasDestroyedShard(IceShard shard)
        {
            return shard != null &&
                   destroyedShardSet.Contains(shard);
        }
    }
}