
namespace AcquireInterfaceLibrary
{
    public interface IDevice
    {
        bool InitialDevice();
        bool StartAcquire();
        bool StopAcquire();
        bool ReleaseDevice();
    }
}
