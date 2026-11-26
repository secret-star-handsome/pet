namespace XiaosuPet
{
    public interface IPlatformWindow
    {
        void Initialize();
        void Tick();
        void Shutdown();
        void OnDragStarted();
        void OnDragEnded();
    }
}
