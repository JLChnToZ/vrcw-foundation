namespace JLChnToZ.VRC.Foundation.UdonLowLevel {
    /// <summary>Bases for Udon instructions.</summary>
    public abstract class UdonInstruction {
        public abstract uint Size { get; }
        internal UdonInstruction next;
        internal uint offset;
    }

    /// <summary>NOP instruction. Does nothing.</summary>
    public class NopInstruction : UdonInstruction {
        public const uint SIZE = 4U;
        public override uint Size => SIZE;

        public override string ToString() => "NOP";
    }

    /// <summary>POP instruction. Pops the top value from the stack.</summary>
    public class PopInstruction : UdonInstruction {
        public const uint SIZE = 4U;
        public override uint Size => SIZE;

        public override string ToString() => "POP";
    }

    /// <summary>COPY instruction. Pops 2 variables from the stack and assigns the value of the first to the second.</summary>
    public class CopyInstruction : UdonInstruction {
        public const uint SIZE = 4U;
        public override uint Size => SIZE;

        public override string ToString() => "COPY";
    }

    /// <summary>LOAD instruction. Pushes a variable to the stack.</summary>
    public class PushInstruction : UdonInstruction {
        public const uint SIZE = 8U;
        public override uint Size => SIZE;
        public VariableName dest;

        public PushInstruction(VariableName dest) => this.dest = dest;

        public override string ToString() => $"PUSH, {dest}";
    }

    /// <summary>Base class for jump instructions.</summary>
    public abstract class JumpInstructionBase : UdonInstruction {
        public const uint SIZE = 8U;
        public override uint Size => SIZE;
        public UdonInstruction destination;
        public uint destinationAddr;

        protected JumpInstructionBase(UdonInstruction destination) =>
            this.destination = destination;

        protected JumpInstructionBase(uint destinationAddr) =>
            this.destinationAddr = destinationAddr;

        public uint DestinationAddr => destination != null ? destination.offset + destination.Size : destinationAddr;
    }

    /// <summary>JUMP instruction. Conditionless jump to a specific instruction.</summary>
    public class JumpInstruction : JumpInstructionBase {
        public JumpInstruction(UdonInstruction destination) : base(destination) {}

        public JumpInstruction(uint destinationAddr) : base(destinationAddr) {}

        public override string ToString() => $"JUMP, 0x{DestinationAddr:X8}";
    }

    /// <summary>JUMP_INDIRECT instruction. Jumps to an instruction at the address of a variable.</summary>
    public class JumpIndirectInstruction : UdonInstruction {
        public const uint SIZE = 8U;
        public override uint Size => SIZE;
        public readonly VariableName varName;

        public JumpIndirectInstruction(VariableName varName) => this.varName = varName;

        public override string ToString() => $"JUMP_INDIRECT, {varName}";
    }

    /// <summary>JUMP_IF_FALSE instruction. Jumps to a specific instruction if the top value of the stack is false.</summary>
    public class JumpIfFalseInstruction : JumpInstruction {
        public JumpIfFalseInstruction(UdonInstruction destination) : base(destination) {}

        public JumpIfFalseInstruction(uint destinationAddr) : base(destinationAddr) {}

        public override string ToString() => $"JUMP_IF_FALSE, 0x{DestinationAddr:X8}";
    }

    /// <summary>EXTERN instruction. Calls a method with arguments from the stack.</summary>
    public class ExternInstruction : UdonInstruction {
        public const uint SIZE = 8U;
        public override uint Size => SIZE;
        public readonly string methodName;

        public ExternInstruction(string methodName) => this.methodName = methodName;

        public override string ToString() => $"EXTERN, \"{methodName}\"";
    }
}