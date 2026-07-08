namespace SimpleBuildingSystem
{
    // Classe base de qualquer modo de construção (Placement, Destruction, Adjustment, Upgrade...).
    // Cria uma subclasse nova só se precisares de um modo diferente destes dois.
    public abstract class BuildingState
    {
        protected BuildingController controller;

        public BuildingState(BuildingController controller)
        {
            this.controller = controller;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick() { }

        public virtual void OnConfirm() { }
        public virtual void OnCancel() { }
        public virtual void OnRotate(float direction) { }
    }
}
