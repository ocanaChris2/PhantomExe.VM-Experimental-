using System;
using System.Collections.Generic;

namespace PhantomExe.Core.VM
{
    public interface IVMContext : IDisposable
    {
        object? Execute(object?[]? args);
    }
}