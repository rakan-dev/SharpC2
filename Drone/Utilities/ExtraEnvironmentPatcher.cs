using System;
using System.Runtime.InteropServices;

namespace Drone.Utilities;

public sealed class ExtraEnvironmentPatcher
{
    private const int PEB_BASE_ADDRESS_OFFSET = 0x10;

    private IntPtr _pOriginalPebBaseAddress;
    private IntPtr _pPEBBaseAddr;

    private IntPtr _newPEBaseAddress;

    public ExtraEnvironmentPatcher(IntPtr newPEBaseAddress)
    {
        _newPEBaseAddress = newPEBaseAddress;
    }

    public bool PerformExtraEnvironmentPatches()
    {
        return PatchPebBaseAddress();
    }

    private bool PatchPebBaseAddress()
    {
        _pPEBBaseAddr = (IntPtr)(Helpers.GetPointerToPeb().ToInt64() + PEB_BASE_ADDRESS_OFFSET);
        _pOriginalPebBaseAddress = Marshal.ReadIntPtr(_pPEBBaseAddr);

        return Helpers.PatchAddress(_pPEBBaseAddr, _newPEBaseAddress);
    }

    public bool RevertExtraPatches()
    {
        return Helpers.PatchAddress(_pPEBBaseAddr, _pOriginalPebBaseAddress);
    }
}