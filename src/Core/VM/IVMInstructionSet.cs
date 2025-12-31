namespace PhantomExe.Core.VM
{
    public enum IVMOpcode : byte
    {
        NOP = 0x00,
        LD_I4 = 0x01,
        LD_I8 = 0x02,
        LD_R4 = 0x03,
        LD_R8 = 0x04,
        LD_STR = 0x05,
        LDARG = 0x06,
        CALL = 0x07,
        NEWOBJ = 0x08,
        RET = 0x09,
        ADD = 0x0A,
        SUB = 0x0B,
        MUL = 0x0C,
        DIV = 0x0D,
        TRAP = 0xFF
    }
}