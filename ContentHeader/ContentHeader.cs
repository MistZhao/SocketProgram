using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContentHeader
{
    /// <summary>
    /// 作为协议头的结构体必须使用相同大小的数据类型，防止 内存对齐 导致的错误
    /// </summary>
    public struct StuContentHeader // C#编译器会自动在上面运用[StructLayout(LayoutKind.Sequential)]，内存对齐时按最大的大小对齐
    {
        public Int32 iHeaderSize;
    }

    /// <summary>
    /// 作为协议头的结构体必须使用相同大小的数据类型，防止 内存对齐 导致的错误
    /// </summary>
    public struct StuTest // C#编译器会自动在上面运用[StructLayout(LayoutKind.Sequential)]，内存对齐时按最大的大小对齐
    {
        public short sTest1;
        public short sTest2;
    }

    /// <summary>
    /// 作为协议头的结构体必须使用相同大小的数据类型，防止 内存对齐 导致的错误
    /// </summary>
    public struct StuTest1 // C#编译器会自动在上面运用[StructLayout(LayoutKind.Sequential)]，内存对齐时按最大的大小对齐
    {
        public Int32 iTest;
        public StuTest sTest;
    }
}
