namespace MagicVillageDash.Input
{
    public interface IRunnerInputController
    {

        void Activate();   
        void Deactivate(); 

        bool IsActive { get; }
    }
}