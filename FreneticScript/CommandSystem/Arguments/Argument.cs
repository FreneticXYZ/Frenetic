//
// This file is created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;
using System.Reflection;
using System.Reflection.Emit;

namespace FreneticScript.CommandSystem.Arguments
{
    /// <summary>
    /// An Argument in the command system.
    /// </summary>
    public class Argument
    {
        /// <summary>
        /// Empty argument bit array.
        /// </summary>
        public static readonly ArgumentBit[] EMPTY_BITS = new ArgumentBit[0];

        /// <summary>
        /// Raw value of the parts that build up the argument.
        /// </summary>
        public ArgumentBit[] RawBits = EMPTY_BITS;

        /// <summary>
        /// The parts that build up the argument.
        /// </summary>
        public ArgumentBit[] Bits
        {
            get
            {
                return RawBits;
            }
            set
            {
                RawBits = value;
                if (value.Length != 0)
                {
                    FirstBit = value[0];
                }
            }
        }

        /// <summary>
        /// The first bit in <see cref="Bits"/>.
        /// </summary>
        public ArgumentBit FirstBit;

        /// <summary>
        /// Whether the argument was input with "quotes" around it.
        /// </summary>
        public bool WasQuoted = true;
        
        /// <summary>
        /// Gets the resultant type of this argument.
        /// </summary>
        /// <param name="values">The relevant variable set.</param>
        /// <returns>The tag type.</returns>
        public TagType ReturnType(CILAdaptationValues values)
        {
            if (Bits.Length == 1)
            {
                return Bits[0].ReturnType(values);
            }
            else
            {
                return Bits[0].CommandSystem.TagSystem.Types.Type_Text;
            }
        }

        private static long IDINCR = 0;
        
        /// <summary>
        /// All valid duplicator calls known. Null values indicate that there is known to not be a duplicator.
        /// </summary>
        public static Dictionary<Type, MethodInfo> DuplicatorCalls = new Dictionary<Type, MethodInfo>(128);

        /// <summary>
        /// Automatically duplicates an object, if duplication is required.
        /// </summary>
        /// <param name="ilgen">The IL generator object.</param>
        /// <param name="tab">The text argument bit.</param>
        public static void Compile_DuplicateIfNeeded(CILAdaptationValues.ILGeneratorTracker ilgen, TextArgumentBit tab)
        {
            Type tx = tab.InputValue.GetType();
            if (!DuplicatorCalls.TryGetValue(tx, out MethodInfo metinf)) // TODO: Store this early in TagType
            {
                metinf = tx.GetMethod("RequiredDuplicate", BindingFlags.Instance | BindingFlags.Public);
                DuplicatorCalls.Add(tx, metinf);
            }
            if (metinf == null)
            {
                return;
            }
            ilgen.Emit(OpCodes.Call, metinf);
        }

        /// <summary>
        /// Compiles the argument.
        /// </summary>
        public void Compile()
        {
            string tname = "__script_argument__" + IDINCR++;
            AssemblyName asmname = new AssemblyName(tname) { Name = tname };
            AssemblyBuilder asmbuild = AppDomain.CurrentDomain.DefineDynamicAssembly(asmname,
#if NET_4_5
                    AssemblyBuilderAccess.RunAndCollect
#else
                    AssemblyBuilderAccess.Run
#endif
                    );
            ModuleBuilder modbuild = asmbuild.DefineDynamicModule(tname);
            TypeBuilder typebuild_c = modbuild.DefineType(tname + "__CENTRAL", TypeAttributes.Class | TypeAttributes.Public, typeof(Argument));
            MethodBuilder methodbuild_c = typebuild_c.DefineMethod("Parse", MethodAttributes.Public | MethodAttributes.Virtual, typeof(TemplateObject), new Type[] { typeof(Action<string>), typeof(CompiledCommandStackEntry) });
            CILAdaptationValues.ILGeneratorTracker ilgen = new CILAdaptationValues.ILGeneratorTracker() { Internal = methodbuild_c.GetILGenerator() };
            if (Bits.Length == 0) // Empty argument
            {
                ilgen.Emit(OpCodes.Ldstr, ""); // Load an empty string
                ilgen.Emit(OpCodes.Newobj, TextTag_CTOR); // Construct a texttag of the empty string
                // Note: have to construct new text tag every time, as text tag value can be modified (via Set call).
            }
            else if (Bits.Length == 1) // One argument input
            {
                int tab_tracker_loc = 0;
                int object_result_loc = 1;
                if (FirstBit is TagArgumentBit)
                {
                    tab_tracker_loc = ilgen.DeclareLocal(typeof(TagArgumentBit)); // Declare variable of type TagArgumentBit as local-0
                    object_result_loc = ilgen.DeclareLocal(typeof(TemplateObject)); // Declare variable of type TemplateObject as local-1
                }
                ilgen.Emit(OpCodes.Ldarg_0); // Load the argument object
                ilgen.Emit(OpCodes.Ldfld, Argument_FirstBit); // Load the only argument bit
                if (FirstBit is TextArgumentBit textab)
                {
                    ilgen.Emit(OpCodes.Ldfld, TextArgumentBit_InputValue); // Load the textab's input value directly
                    Compile_DuplicateIfNeeded(ilgen, textab);
                }
                else if (FirstBit is TagArgumentBit tab)
                {
                    tab.GenerateCall(ilgen, tab_tracker_loc, OpCodes.Ldarg_1, OpCodes.Ldarg_2, object_result_loc); // Call the tag - takes TAB on stack and adds a TemplateObject result onto it
                    ilgen.Emit(OpCodes.Ldloc, object_result_loc); // Load the object result
                }
                else
                {
                    ilgen.Emit(OpCodes.Ldarg_1); // Load Action<string> 'error'
                    ilgen.Emit(OpCodes.Ldarg_2); // Load CompiledCommandStackEntry 'cse'
                    ilgen.Emit(OpCodes.Callvirt, ArgumentBit_Parse); // Generic call to virtual parse method, for unknown argument bit types.
                }
            }
            else // Complex argument input
            {
                int result_string_loc = ilgen.DeclareLocal(typeof(StringBuilder)); // Declare variable of type StringBuilder as local-0
                bool hasTag = false;
                for (int i = 0; i < Bits.Length; i++)
                {
                    if (Bits[i] is TagArgumentBit tab)
                    {
                        hasTag = true;
                        break;
                    }
                }
                int object_result_loc = ilgen.DeclareLocal(typeof(TemplateObject)); // Declare variable of type TemplateObject as local-1
                int tab_tracker_loc = 0;
                if (hasTag)
                {
                    tab_tracker_loc = ilgen.DeclareLocal(typeof(TagArgumentBit)); // Declare variable of type TagArgumentBit as local-2
                }
                int cx = 2;
                for (int i = 0; i < Bits.Length; i++)
                {
                    cx += Bits[i].ToString().Length + 1;
                }
                ilgen.Emit(OpCodes.Ldc_I4, cx); // Load the integer value of the approximated length (to set an estimated capacity for the stringbuilder)
                ilgen.Emit(OpCodes.Newobj, StringBuilder_CTOR); // Construct an empty stringbuilder with that length
                ilgen.Emit(OpCodes.Stloc, result_string_loc); // Store the stringbuilder to a local variable
                for (int i = 0; i < Bits.Length; i++)
                {
                    if (!(Bits[i] is TagArgumentBit))
                    {
                        ilgen.Emit(OpCodes.Ldloc, result_string_loc); // Load the local variable containing the string builder
                    }
                    ilgen.Emit(OpCodes.Ldarg_0); // Load the argument object
                    ilgen.Emit(OpCodes.Ldfld, Argument_RawBits); // Load the bits array
                    ilgen.Emit(OpCodes.Ldc_I4, i); // Load the current index (will be a constant integer load at runtime)
                    ilgen.Emit(OpCodes.Ldelem_Ref); // Load the ArgumentBit
                    if (Bits[i] is TagArgumentBit tab)
                    {
                        tab.GenerateCall(ilgen, tab_tracker_loc, OpCodes.Ldarg_1, OpCodes.Ldarg_2, object_result_loc); // Call the tag - takes TAB on stack and adds a TemplateObject result onto it
                        ilgen.Emit(OpCodes.Ldloc, result_string_loc); // Load the local variable containing the string builder
                        ilgen.Emit(OpCodes.Ldloc, object_result_loc); // Load the object result
                    }
                    else if (Bits[i] is TextArgumentBit)
                    {
                        ilgen.Emit(OpCodes.Ldfld, TextArgumentBit_InputValue); // Load the tab's input value directly
                    }
                    else
                    {
                        ilgen.Emit(OpCodes.Ldarg_1); // Load Action<string> 'error'
                        ilgen.Emit(OpCodes.Ldarg_2); // Load CompiledCommandStackEntry 'cse'
                        ilgen.Emit(OpCodes.Callvirt, ArgumentBit_Parse); // Generic call to virtual parse method, for unknown argument bit types.
                    }
                    ilgen.Emit(OpCodes.Callvirt, Object_ToString); // Compress the result to a string
                    ilgen.Emit(OpCodes.Call, StringBuilder_Append); // Append it to the string builder
                    ilgen.Emit(OpCodes.Pop); // Remove the stringbuilder.append return value from the stack, as it's not needed (final assembly code should automatically discard the return push-n-pop)
                }
                ilgen.Emit(OpCodes.Ldloc, result_string_loc); // Load the stringbuilder
                ilgen.Emit(OpCodes.Callvirt, Object_ToString); // ToString it (maybe this doesn't need to be virtual?)
                ilgen.Emit(OpCodes.Newobj, TextTag_CTOR); // Construct a texttag of the full complex input
            }
            ilgen.Emit(OpCodes.Ret); // Return the resultant texttag or tag return value
            typebuild_c.DefineMethodOverride(methodbuild_c, Argument_Parse);
            Type t_c = typebuild_c.CreateType();
            TrueForm = Activator.CreateInstance(t_c) as Argument;
            TrueForm.Bits = Bits;
            TrueForm.WasQuoted = WasQuoted;
        }
        
        /// <summary>
        /// The <see cref="Object.ToString"/> method.
        /// </summary>
        public static MethodInfo Object_ToString = typeof(object).GetMethod(nameof(Object.ToString), BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);

        /// <summary>
        /// The <see cref="StringBuilder(int)"/> constructor.
        /// </summary>
        public static ConstructorInfo StringBuilder_CTOR = typeof(StringBuilder).GetConstructor(new Type[] { typeof(int) });

        /// <summary>
        /// The <see cref="StringBuilder.Append(string)"/> method.
        /// </summary>
        public static MethodInfo StringBuilder_Append = typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(string) }, null);

        /// <summary>
        /// The <see cref="TextTag(string)"/> constructor.
        /// </summary>
        public static ConstructorInfo TextTag_CTOR = typeof(TextTag).GetConstructor(new Type[] { typeof(string) });

        /// <summary>
        /// The <see cref="RawBits"/> field.
        /// </summary>
        public static FieldInfo Argument_RawBits = typeof(Argument).GetField(nameof(RawBits));

        /// <summary>
        /// The <see cref="FirstBit"/> field.
        /// </summary>
        public static FieldInfo Argument_FirstBit = typeof(Argument).GetField(nameof(FirstBit));

        /// <summary>
        /// The <see cref="Parse(Action{string}, CompiledCommandStackEntry)"/> method.
        /// </summary>
        public static MethodInfo Argument_Parse = typeof(Argument).GetMethod(nameof(Parse));

        /// <summary>
        /// The <see cref="ArgumentBit.Parse(Action{string}, CompiledCommandStackEntry)"/> method.
        /// </summary>
        public static MethodInfo ArgumentBit_Parse = typeof(ArgumentBit).GetMethod(nameof(ArgumentBit.Parse));

        /// <summary>
        /// The <see cref="TextArgumentBit.InputValue"/> field.
        /// </summary>
        public static FieldInfo TextArgumentBit_InputValue = typeof(TextArgumentBit).GetField(nameof(TextArgumentBit.InputValue));

        /// <summary>
        /// The "true form" of this argument.
        /// </summary>
        public Argument TrueForm;

        /// <summary>
        /// Parse the argument, reading any tags or other special data.
        /// </summary>
        /// <param name="error">What to invoke if there is an error.</param>
        /// <param name="cse">The command stack entry.</param>
        /// <returns>The parsed final text.</returns>
        public virtual TemplateObject Parse(Action<string> error, CompiledCommandStackEntry cse)
        {
            if (TrueForm == null)
            {
                Compile();
            }
            return TrueForm.Parse(error, cse);
        }
        
        /// <summary>
        /// Returns the argument as plain input text.
        /// </summary>
        /// <returns>The plain input text.</returns>
        public override string ToString()
        {
            if (Bits.Length == 1)
            {
                return Bits[0].ToString();
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Bits.Length; i++)
            {
                sb.Append(Bits[i].ToString());
            }
            return sb.ToString();
        }
    }
}
