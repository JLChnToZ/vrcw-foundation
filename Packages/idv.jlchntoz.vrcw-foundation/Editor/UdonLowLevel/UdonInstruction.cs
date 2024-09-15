namespace JLChnToZ.VRC.Foundation.UdonLowLevel {
    public abstract class UdonInstruction {
        public abstract uint Size { get; }
        internal UdonInstruction next;
        internal uint offset;
    }

    public class NopInstruction : UdonInstruction {
        public const uint SIZE = 4U;
        public override uint Size => SIZE;

        public override string ToString() => "NOP";
    }

    public class PopInstruction : UdonInstruction {
        public const uint SIZE = 4U;
        public override uint Size => SIZE;

        public override string ToString() => "POP";
    }

    public class CopyInstruction : UdonInstruction {
        public const uint SIZE = 4U;
        public override uint Size => SIZE;

        public override string ToString() => "COPY";
    }

    public class PushInstruction : UdonInstruction {
        public const uint SIZE = 8U;
        public override uint Size => SIZE;
        public VariableName dest;

        public PushInstruction(VariableName dest) => this.dest = dest;

        public override string ToString() => $"PUSH, {dest}";
    }

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

    public class JumpInstruction : JumpInstructionBase {
        public JumpInstruction(UdonInstruction destination) : base(destination) {}

        public JumpInstruction(uint destinationAddr) : base(destinationAddr) {}

        public override string ToString() => $"JUMP, 0x{DestinationAddr:X8}";
    }

    public class JumpIndirectInstruction : UdonInstruction {
        public const uint SIZE = 8U;
        public override uint Size => SIZE;
        public readonly VariableName varName;

        public JumpIndirectInstruction(VariableName varName) => this.varName = varName;

        public override string ToString() => $"JUMP_INDIRECT, {varName}";
    }

    public class JumpIfFalseInstruction : JumpInstruction {
        public JumpIfFalseInstruction(UdonInstruction destination) : base(destination) {}

        public JumpIfFalseInstruction(uint destinationAddr) : base(destinationAddr) {}

        public override string ToString() => $"JUMP_IF_FALSE, 0x{DestinationAddr:X8}";
    }

    public class ExternInstruction : UdonInstruction {
        public const uint SIZE = 8U;
        public override uint Size => SIZE;
        public readonly string methodName;

        public ExternInstruction(string methodName) => this.methodName = methodName;

        public override string ToString() => $"EXTERN, \"{methodName}\"";
    }
}