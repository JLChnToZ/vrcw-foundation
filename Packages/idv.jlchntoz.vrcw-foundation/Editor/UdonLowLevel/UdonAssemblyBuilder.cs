using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Graph;
using VRC.Udon.Editor;
using VRC.Udon.EditorBindings;

namespace JLChnToZ.VRC.Foundation.UdonLowLevel {
    public sealed class UdonAssemblyBuilder {
        static readonly Regex autoVariableStripper = new Regex("[\\W_]+", RegexOptions.Compiled);
        static readonly object nullObj = new object();
        public const uint ReturnAddress = uint.MaxValue - 7;
        readonly Dictionary<UdonInstruction, string> entryPoints = new Dictionary<UdonInstruction, string>();
        readonly Dictionary<VariableName, VariableDefinition> variableDefs = new Dictionary<VariableName, VariableDefinition>();
        readonly HashSet<string> uniqueExterns = new HashSet<string>();
        readonly Dictionary<object, VariableName> constants = new Dictionary<object, VariableName>();
        UdonInstruction firstInstruction, lastInstruction;
        string nextEntryPointName;
        string assembly;
        UdonEditorInterface editorInterface;
        IUdonProgram program;

        public uint Size => lastInstruction != null ? lastInstruction.offset + lastInstruction.Size : 0;

        public UdonInstruction LastInstruction => lastInstruction;

        public readonly bool typeCheck;

        public UdonAssemblyBuilder() : this(true) {}

        public UdonAssemblyBuilder(bool typeCheck) {
            this.typeCheck = typeCheck;
        }

        public void DefineVariable<T>(
            VariableName variableName,
            VariableAttributes attributes = VariableAttributes.None,
            T value = default
        ) => DefineVariable(variableName, new VariableDefinition(typeof(T), attributes, value));

        public void DefineVariable(
            VariableName variableName,
            Type type = null,
            VariableAttributes attributes = VariableAttributes.None,
            object value = null
        ) => DefineVariable(variableName, new VariableDefinition(type, attributes, value));

        public void DefineVariable(VariableName variableName, VariableDefinition definition) {
            if (!variableName.IsValid)
                throw new ArgumentNullException(nameof(variableName));
            var fixedType = variableName.GetPredefinedType();
            if (fixedType != null) {
                definition.type = fixedType;
                definition.attributes &= VariableAttributes.Public;
            }
            if (definition.type != null && definition.type != typeof(void) && (definition.value == null ? definition.type.IsValueType : !definition.type.IsInstanceOfType(definition.value)))
                throw new ArgumentException("Type mismatch");
            if (definition.attributes.HasFlag(VariableAttributes.Constant)) {
                var value2 = definition.value ?? nullObj;
                if (!constants.ContainsKey(value2)) constants[value2] = variableName;
            }
            if (definition.type == null) definition.type = typeof(object);
            if (variableDefs.TryGetValue(variableName, out var existing) &&
                existing.attributes.HasFlag(VariableAttributes.Constant) &&
                (definition.type != existing.type || definition.value != existing.value))
                throw new ArgumentException("Attempt to modify existing constant type.");
            variableDefs[variableName] = definition;
            assembly = null;
        }

        public bool TryGetVariable(VariableName variableName, out VariableDefinition result) {
            if (variableDefs.TryGetValue(variableName, out result)) return true;
            var fixedType = variableName.GetPredefinedType();
            if (fixedType != null) {
                DefineVariable(variableName, fixedType);
                result = variableDefs[variableName];
                return true;
            }
            return false;
        }

        public void DefineEvent(string name) => nextEntryPointName = name;

        public void DefineEvent(UdonInstruction instruction, string name) {
            entryPoints[instruction] = name;
            assembly = null;
        }

        private VariableName GetConstant(object value) {
            if (value == null) value = nullObj;
            if (!constants.TryGetValue(value, out var varName)) {
                Type type;
                string baseName;
                if (value == nullObj) {
                    type = typeof(void);
                    baseName = "__const_nil";
                    value = null;
                } else {
                    type = value.GetType();
                    var temp = autoVariableStripper.Replace($"{type.Name}_{value}", "_").TrimEnd();
                    if (temp.Length > 64) temp = temp.Substring(0, 64);
                    baseName = "__const_" + temp;
                }
                varName = baseName;
                int i = 0;
                while (variableDefs.ContainsKey(varName))
                    varName = $"{baseName}_{i++}";
                DefineVariable(varName, type, VariableAttributes.Constant, value);
            }
            return varName;
        }

        public void Emit(UdonInstruction instruction) {
            if (instruction == null || instruction.next != null)
                throw new ArgumentException("Instruction is null or already connected.");
            if (!string.IsNullOrEmpty(nextEntryPointName))
                DefineEvent(instruction, nextEntryPointName);
            nextEntryPointName = null;
            assembly = null;
            if (lastInstruction == null) {
                firstInstruction = lastInstruction = instruction;
            } else {
                instruction.offset = lastInstruction.offset + lastInstruction.Size;
                lastInstruction.next = instruction;
                lastInstruction = instruction;
            }
        }

        public NopInstruction EmitNop() {
            var instruction = new NopInstruction();
            Emit(instruction);
            return instruction;
        }

        public CopyInstruction EmitCopy() {
            var instruction = new CopyInstruction();
            Emit(instruction);
            return instruction;
        }

        public PopInstruction EmitPop() {
            var instruction = new PopInstruction();
            Emit(instruction);
            return instruction;
        }

        public CopyInstruction EmitCopy(object varFrom, VariableName varTo) {
            var varFromName = varFrom is VariableName name ? name.key : GetConstant(varFrom);
            EmitPush(varFromName, null);
            if (typeCheck) DoTypeCheck(varTo, variableDefs[varFromName].type);
            EmitPush(varTo, null);
            return EmitCopy();
        }

        public CopyInstruction EmitCopyOffset(VariableName dest, int relativeOffset = 0) {
            const uint INSTRUCTION_SIZE = PushInstruction.SIZE * 2 + CopyInstruction.SIZE;
            if (!variableDefs.ContainsKey(dest)) DefineVariable<uint>(dest);
            return EmitCopy((uint)(INSTRUCTION_SIZE + Size + relativeOffset), dest);
        }

        public PushInstruction EmitPush(object parameter) =>
            EmitPush(parameter is VariableName name ? name.key : GetConstant(parameter), null);

        public PushInstruction EmitPush(VariableName variableName) =>
            EmitPush(variableName, null);

        private PushInstruction EmitPush(VariableName variableName, Type type, bool strict = false) {
            DoTypeCheck(variableName, type, strict);
            var instruction = new PushInstruction(variableName);
            Emit(instruction);
            return instruction;
        }

        public JumpInstruction EmitJump(UdonInstruction dest) {
            var instruction = new JumpInstruction(dest);
            Emit(instruction);
            return instruction;
        }

        public JumpInstruction EmitJump(uint dest = ReturnAddress) {
            var instruction = new JumpInstruction(dest);
            Emit(instruction);
            return instruction;
        }

        public JumpIndirectInstruction EmitJumpIndirect(VariableName address) {
            DoTypeCheck(address, typeof(uint), true);
            var instruction = new JumpIndirectInstruction(address);
            Emit(instruction);
            return instruction;
        }

        public JumpIfFalseInstruction EmitJumpIfFalse(UdonInstruction dest) {
            var instruction = new JumpIfFalseInstruction(dest);
            Emit(instruction);
            return instruction;
        }

        public JumpIfFalseInstruction EmitJumpIfFalse(uint dest = ReturnAddress) {
            var instruction = new JumpIfFalseInstruction(dest);
            Emit(instruction);
            return instruction;
        }

        public JumpIfFalseInstruction EmitJumpIfFalse(VariableName paramName, UdonInstruction dest) {
            EmitPush(paramName, typeof(bool), true);
            return EmitJumpIfFalse(dest);
        }

        public JumpIfFalseInstruction EmitJumpIfFalse(VariableName paramName, uint dest = ReturnAddress) {
            EmitPush(paramName, typeof(bool), true);
            return EmitJumpIfFalse(dest);
        }

        private void DoTypeCheck(VariableName variableName, Type type, bool strict = false) {
            if (!variableName.IsValid) throw new ArgumentNullException(nameof(variableName));
            if (variableDefs.TryGetValue(variableName, out var def)) {
                if (typeCheck && type != null && (strict ? def.type != type : !TypeHelper.IsTypeCompatable(def.type, type)))
                    throw new ArgumentException($"Type mismatch: expected variable `{variableName}` to be `{type}` but got `{def.type}`.");
            } else DefineVariable(variableName, type);
        }

        public ExternInstruction EmitExtern(string methodName) {
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));
            if (typeCheck && UdonEditorManager.Instance.GetNodeDefinition(methodName) == null)
                throw new ArgumentException($"Method `{methodName}` does not exists!");
            uniqueExterns.Add(methodName);
            var instruction = new ExternInstruction(methodName);
            Emit(instruction);
            return instruction;
        }

        public ExternInstruction EmitExtern(string methodName, params object[] parameters) {
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));
            if (typeCheck) {
                var nodeDef = UdonEditorManager.Instance.GetNodeDefinition(methodName);
                if (nodeDef == null)
                    throw new ArgumentException($"Method `{methodName}` does not exists!");
                if (parameters == null ? nodeDef.parameters.Count != 0 : parameters.Length != nodeDef.parameters.Count)
                    throw new ArgumentException($"Invalid number of parameters for `{methodName}`: expected {nodeDef.parameters.Count} but got {parameters?.Length ?? 0}.");
                if (parameters != null)
                    for (int i = 0; i < parameters.Length; i++) {
                        var parameter = parameters[i];
                        Type type;
                        VariableDefinition varDef;
                        if (parameter is VariableName name) {
                            if (variableDefs.TryGetValue(name, out varDef)) type = varDef.type;
                            else {
                                type = parameter?.GetType();
                                variableDefs.Add(name, varDef = new VariableDefinition(type, value: parameter));
                            }
                        } else {
                            name = GetConstant(parameter);
                            varDef = variableDefs[name];
                            type = varDef.type;
                        }
                        var paramDef = nodeDef.parameters[i];
                        switch (paramDef.parameterType) {
                            case UdonNodeParameter.ParameterType.IN:
                                if (!TypeHelper.IsTypeAssignable(paramDef.type, type))
                                    throw new ArgumentException($"Type mismatch: Expected parameter {i} of `{methodName}` to be `{paramDef.type}` but got `{type}`.");
                                break;
                            case UdonNodeParameter.ParameterType.OUT:
                                if (varDef.attributes.HasFlag(VariableAttributes.Constant))
                                    throw new ArgumentException($"Parameter {i} of `{methodName}` is an output but assigned a constant `{parameter}`.");
                                if (type == null) {
                                    varDef.type = paramDef.type;
                                    variableDefs[name] = varDef;
                                } else if (!TypeHelper.IsTypeCompatable(paramDef.type, type))
                                    throw new ArgumentException($"Type mismatch: Expected parameter {i} of `{methodName}` to be `{paramDef.type}` but got `{type}`.");
                                break;
                            case UdonNodeParameter.ParameterType.IN_OUT:
                                if (varDef.attributes.HasFlag(VariableAttributes.Constant))
                                    throw new ArgumentException($"Parameter {i} of `{methodName}` is a reference but assigned a constant `{parameter}`.");
                                if (type == null) {
                                    varDef.type = paramDef.type;
                                    variableDefs[name] = varDef;
                                } else if (type != paramDef.type)
                                    throw new ArgumentException($"Type mismatch: Expected parameter {i} of `{methodName}` to be `{paramDef.type}` but got `{type}`.");
                                break;
                        }
                        parameters[i] = name;
                    }
            }
            if (parameters != null) foreach (var parameter in parameters) EmitPush(parameter);
            return EmitExtern(methodName);
        }

        public string Compile() {
            if (string.IsNullOrEmpty(assembly)) {
                var sb = new StringBuilder();
                sb.AppendLine(".data_start");
                foreach (var kv in variableDefs) {
                    if (kv.Value.attributes.HasFlag(VariableAttributes.Public))
                        sb.AppendLine($".export {kv.Key}");
                    switch (kv.Value.attributes & VariableAttributes.Sync) {
                        case VariableAttributes.SyncNone:
                            sb.AppendLine($".sync {kv.Key}, none");
                            break;
                        case VariableAttributes.SyncLinear:
                            sb.AppendLine($".sync {kv.Key}, linear");
                            break;
                        case VariableAttributes.SyncSmooth:
                            sb.AppendLine($".sync {kv.Key}, smooth");
                            break;
                    }
                }
                foreach (var kv in variableDefs)
                    sb.AppendLine($"{kv.Key}: %{kv.Value.type.GetUdonTypeName(true)}, {(kv.Value.attributes.HasFlag(VariableAttributes.DefaultThis) ? "this" : "null")}");
                sb.AppendLine(".data_end");
                sb.AppendLine(".code_start");
                for (var instruction = firstInstruction; instruction != null; instruction = instruction.next) {
                    if (entryPoints.TryGetValue(instruction, out var exportName)) {
                        sb.AppendLine($".export {exportName}");
                        sb.AppendLine($"{exportName}:");
                    }
                    sb.AppendLine(instruction.ToString());
                }
                sb.AppendLine(".code_end");
                assembly = sb.ToString();
            }
            return assembly;
        }

        public IUdonProgram Assemble() {
            Compile();
            editorInterface = new UdonEditorInterface(
                null, new HeapFactory((uint)(variableDefs.Count + uniqueExterns.Count)),
                null, null, null, null, null, null, null
            );
            program = editorInterface.Assemble(assembly);
            var symbolTable = program.SymbolTable;
            var heap = program.Heap;
            foreach (var kv in variableDefs)
                if (!kv.Value.attributes.HasFlag(VariableAttributes.DefaultThis) && kv.Value.type != typeof(void))
                    heap.SetHeapVariable(symbolTable.GetAddressFromSymbol(kv.Key.key), kv.Value.value, kv.Value.type);
            return program;
        }

        public string[] Disassemble() {
            if (editorInterface == null || program == null) Assemble();
            return editorInterface.DisassembleProgram(program);
        }
    }
}