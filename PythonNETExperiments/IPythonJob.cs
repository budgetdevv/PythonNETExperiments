namespace PythonNETExperiments
{
    public interface IPythonJobBase
    {
        public static abstract string[] Packages { get; }
        
        public static abstract void StaticInitialize();

        public void Initialize();
    }
    
    public interface IPythonJob: IPythonJobBase
    {
        public void Run();
    }
}
