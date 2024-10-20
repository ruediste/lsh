using System.Drawing;
using System.Runtime.InteropServices;

namespace lsh.LshMath;

public unsafe class NativeMemoryWrapper<T> : IDisposable
where T : unmanaged
{
    public readonly T* Data;

    public readonly int SizeInBytes;
    public NativeMemoryWrapper(int elementCount, nuint alignment)
    {
        SizeInBytes = elementCount * sizeof(T);
        Data = (T*)NativeMemory.AlignedAlloc((nuint)SizeInBytes, alignment);
    }

    public void Dispose()
    {
        NativeMemory.AlignedFree(Data);
    }

    public T this[int key]
    {
        get => Data[key];
        set => Data[key] = value;
    }

    public static T* operator +(NativeMemoryWrapper<T> wrapper, int offset)
    {
        return wrapper.Data + offset;
    }

    public static implicit operator T*(NativeMemoryWrapper<T> source)
    => source.Data;
}
