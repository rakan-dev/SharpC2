using System;
using System.Runtime.InteropServices;

using Drone.Interop;

namespace Drone.Utilities;

public sealed class PEMapper
{
    private IntPtr _codebase;
    private PELoader _pe;

    public void MapPEIntoMemory(byte[] unpacked, out PELoader peLoader, out long currentBase)
    {
        _pe = peLoader = new PELoader(unpacked);

        Methods.NtAllocateVirtualMemory(
            new IntPtr(-1),
            (int)_pe.OptionalHeader64.SizeOfImage,
            Data.MEMORY_ALLOCATION.MEM_COMMIT,
            Data.MEMORY_PROTECTION.PAGE_READWRITE,
            ref _codebase);

        currentBase = _codebase.ToInt64();

        // Copy Sections
        for (var i = 0; i < _pe.FileHeader.NumberOfSections; i++)
        {
            var y = (IntPtr)(currentBase + _pe.ImageSectionHeaders[i].VirtualAddress);

            Methods.NtAllocateVirtualMemory(new IntPtr(-1),
                (int)_pe.ImageSectionHeaders[i].SizeOfRawData,
                Data.MEMORY_ALLOCATION.MEM_COMMIT,
                Data.MEMORY_PROTECTION.PAGE_READWRITE,
                ref y);

            if (_pe.ImageSectionHeaders[i].SizeOfRawData > 0)
            {
                Marshal.Copy(_pe.RawBytes, (int)_pe.ImageSectionHeaders[i].PointerToRawData, y,
                    (int)_pe.ImageSectionHeaders[i].SizeOfRawData);
            }
        }

        // Perform Base Relocation
        var delta = currentBase - (long)_pe.OptionalHeader64.ImageBase;

        // Modify Memory Based On Relocation Table
        var relocationTable = (IntPtr)(currentBase + (int)_pe.OptionalHeader64.BaseRelocationTable.VirtualAddress);
        var relocationEntry = Marshal.PtrToStructure<Data.IMAGE_BASE_RELOCATION>(relocationTable);

        var imageSizeOfBaseRelocation = Marshal.SizeOf(typeof(Data.IMAGE_BASE_RELOCATION));
        var nextEntry = relocationTable;
        var sizeofNextBlock = (int)relocationEntry.SizeOfBlock;
        var offset = relocationTable;

        while (true)
        {
            var pRelocationTableNextBlock = (IntPtr)(relocationTable.ToInt64() + sizeofNextBlock);
            var relocationNextEntry = Marshal.PtrToStructure<Data.IMAGE_BASE_RELOCATION>(pRelocationTableNextBlock);
            var pRelocationEntry = (IntPtr)(currentBase + relocationEntry.VirtualAdress);

            for (var i = 0; i < (int)((relocationEntry.SizeOfBlock - imageSizeOfBaseRelocation) / 2); i++)
            {
                var value = (ushort)Marshal.ReadInt16(offset, 8 + 2 * i);
                var type = (ushort)(value >> 12);
                var fixup = (ushort)(value & 0xfff);

                switch (type)
                {
                    case 0x0:
                        break;

                    case 0xA:
                    {
                        var patchAddress = (IntPtr)(pRelocationEntry.ToInt64() + fixup);
                        var originalAddr = Marshal.ReadInt64(patchAddress);
                        Marshal.WriteInt64(patchAddress, originalAddr + delta);

                        break;
                    }
                }
            }

            offset = (IntPtr)(relocationTable.ToInt64() + sizeofNextBlock);
            sizeofNextBlock += (int)relocationNextEntry.SizeOfBlock;
            relocationEntry = relocationNextEntry;
            nextEntry = (IntPtr)(nextEntry.ToInt64() + sizeofNextBlock);

            if (relocationNextEntry.SizeOfBlock == 0)
                break;
        }
    }

    public void ClearPE()
    {
        var size = _pe.OptionalHeader64.SizeOfImage;

        Helpers.ZeroOutMemory(_codebase, (int)size);
        Helpers.FreeMemory(_codebase);
    }

    public void SetPagePermissions()
    {
        for (var i = 0; i < _pe.FileHeader.NumberOfSections; i++)
        {
            var execute = ((uint)_pe.ImageSectionHeaders[i].Characteristics & Data.IMAGE_SCN_MEM_EXECUTE) != 0;
            var read = ((uint)_pe.ImageSectionHeaders[i].Characteristics & Data.IMAGE_SCN_MEM_READ) != 0;
            var write = ((uint)_pe.ImageSectionHeaders[i].Characteristics & Data.IMAGE_SCN_MEM_WRITE) != 0;

            var protection = Data.MEMORY_PROTECTION.PAGE_EXECUTE_READWRITE;

            if (execute && read && write)
            {
                protection = Data.MEMORY_PROTECTION.PAGE_EXECUTE_READWRITE;
            }
            else if (!execute && read && write)
            {
                protection = Data.MEMORY_PROTECTION.PAGE_READWRITE;
            }
            else if (!write && execute && read)
            {
                protection = Data.MEMORY_PROTECTION.PAGE_EXECUTE_READ;
            }
            else if (!execute && !write && read)
            {
                protection = Data.MEMORY_PROTECTION.PAGE_READONLY;
            }
            else if (execute && !read && !write)
            {
                protection = Data.MEMORY_PROTECTION.PAGE_EXECUTE;
            }
            else if (!execute && !read && !write)
            {
                protection = Data.MEMORY_PROTECTION.PAGE_NOACCESS;
            }

            // var y = NativeDeclarations.VirtualProtect((IntPtr) (_codebase.ToInt64() + _pe.ImageSectionHeaders[i].VirtualAddress),
            //     (UIntPtr) _pe.ImageSectionHeaders[i].SizeOfRawData, protection, out _);

            Methods.NtProtectVirtualMemory(
                new IntPtr(-1),
                (IntPtr)(_codebase.ToInt64() + _pe.ImageSectionHeaders[i].VirtualAddress),
                (int)_pe.ImageSectionHeaders[i].SizeOfRawData,
                protection,
                out _);
        }
    }
}