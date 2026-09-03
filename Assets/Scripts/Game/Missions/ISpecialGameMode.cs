namespace PlatinumQuestScripts
{
    public interface ISpecialGameMode
    {
        void OnMissionLoad();
        void OnRestart();
        void OnRespawn();
        void Update();
        void OnJump();

        void ProcessMaterialContact(
        Marble marble,
        CollisionInfo contact);
    }
}
