﻿/*
 * Copyright 2007-2012 JetBrains s.r.o.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

// ReSharper disable IntroduceOptionalParameters.Global
// ReSharper disable UnusedParameter.Local
namespace JetBrains.Annotations {
    /// <summary>
    /// Indicates that marked method builds string by format pattern and (optional) arguments. 
    /// Parameter, which contains format string, should be given in constructor.
    /// The format string should be in <see cref="string.Format(IFormatProvider,string,object[])"/> -like form
    /// </summary>
    [AttributeUsage( AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false, Inherited = true )]
    sealed class StringFormatMethodAttribute : Attribute {
        /// <summary>
        /// Initializes new instance of StringFormatMethodAttribute
        /// </summary>
        /// <param name="formatParameterName">Specifies which parameter of an annotated method should be treated as format-string</param>
        public StringFormatMethodAttribute( string formatParameterName ) {
            FormatParameterName = formatParameterName;
        }


        /// <summary>
        /// Gets format parameter name
        /// </summary>
        [UsedImplicitly]
        public string FormatParameterName { get; private set; }
    }


    /// <summary>
    /// Indicates that the value of marked element could be <c>null</c> sometimes, so the check for <c>null</c> is necessary before its usage
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Delegate |
        AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    sealed class CanBeNullAttribute : Attribute {}


    /// <summary>
    /// Indicates that the value of marked element could never be <c>null</c>
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Delegate |
        AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    sealed class NotNullAttribute : Attribute {}


    /// <summary>
    /// Describes dependency between method input and output
    /// </summary>
    /// <syntax>
    /// <p>Function definition table syntax:</p>
    /// <list>
    /// <item>FDT      ::= FDTRow [;FDTRow]*</item>
    /// <item>FDTRow   ::= Input =&gt; Output | Output &lt;= Input</item>
    /// <item>Input    ::= ParameterName: Value [, Input]*</item>
    /// <item>Output   ::= [ParameterName: Value]* {halt|stop|void|nothing|Value}</item>
    /// <item>Value    ::= true | false | null | notnull | canbenull</item>
    /// </list>
    /// If method has single input parameter, it's name could be omitted. <br/>
    /// Using "halt" (or "void"/"nothing", which is the same) for method output means that methos doesn't return normally. <br/>
    /// "canbenull" annotation is only applicable for output parameters. <br/>
    /// You can use multiple [ContractAnnotation] for each FDT row, or use single attribute with rows separated by semicolon. <br/>
    /// </syntax>
    /// <examples>
    /// <list>
    /// <item>[ContractAnnotation("=> halt")] public void TerminationMethod()</item>
    /// <item>[ContractAnnotation("halt &lt;= condition: false")] public void Assert(bool condition, string text) // Regular Assertion method</item>
    /// <item>[ContractAnnotation("s:null => true")] public bool IsNullOrEmpty(string s) // String.IsNullOrEmpty</item>
    /// <item>[ContractAnnotation("null => null; notnull => notnull")] public object Transform(object data) // Method which returns null if parameter is null, and not null if parameter is not null</item>
    /// <item>[ContractAnnotation("s:null=>false; =>true,result:notnull; =>false, result:null")] public bool TryParse(string s, out Person result)</item>
    /// </list>
    /// </examples>
    [AttributeUsage( AttributeTargets.Method, AllowMultiple = true, Inherited = true )]
    sealed class ContractAnnotationAttribute : Attribute {
        public ContractAnnotationAttribute( [NotNull] string fdt )
            : this( fdt, false ) {}

        public ContractAnnotationAttribute( [NotNull] string fdt, bool forceFullStates ) {
            FDT = fdt;
            ForceFullStates = forceFullStates;
        }


        public string FDT { get; private set; }
        public bool ForceFullStates { get; private set; }
    }


    /// <summary>
    /// Indicates that the marked symbol is used implicitly (e.g. via reflection, in external library),
    /// so this symbol will not be marked as unused (as well as by other usage inspections)
    /// </summary>
    [AttributeUsage( AttributeTargets.All, AllowMultiple = false, Inherited = true )]
    sealed class UsedImplicitlyAttribute : Attribute {
        [UsedImplicitly]
        public UsedImplicitlyAttribute()
            : this( ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default ) {}


        [UsedImplicitly]
        public UsedImplicitlyAttribute( ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags ) {
            UseKindFlags = useKindFlags;
            TargetFlags = targetFlags;
        }


        [UsedImplicitly]
        public UsedImplicitlyAttribute( ImplicitUseKindFlags useKindFlags )
            : this( useKindFlags, ImplicitUseTargetFlags.Default ) {}


        [UsedImplicitly]
        public UsedImplicitlyAttribute( ImplicitUseTargetFlags targetFlags )
            : this( ImplicitUseKindFlags.Default, targetFlags ) {}


        [UsedImplicitly]
        public ImplicitUseKindFlags UseKindFlags { get; private set; }

        /// <summary>
        /// Gets value indicating what is meant to be used
        /// </summary>
        [UsedImplicitly]
        public ImplicitUseTargetFlags TargetFlags { get; private set; }
    }


    /// <summary>
    /// Should be used on attributes and causes ReSharper to not mark symbols marked with such attributes as unused (as well as by other usage inspections)
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
    sealed class MeansImplicitUseAttribute : Attribute {
        [UsedImplicitly]
        public MeansImplicitUseAttribute()
            : this( ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default ) {}


        [UsedImplicitly]
        public MeansImplicitUseAttribute( ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags ) {
            UseKindFlags = useKindFlags;
            TargetFlags = targetFlags;
        }


        [UsedImplicitly]
        public MeansImplicitUseAttribute( ImplicitUseKindFlags useKindFlags )
            : this( useKindFlags, ImplicitUseTargetFlags.Default ) {}


        [UsedImplicitly]
        public MeansImplicitUseAttribute( ImplicitUseTargetFlags targetFlags )
            : this( ImplicitUseKindFlags.Default, targetFlags ) {}


        [UsedImplicitly]
        public ImplicitUseKindFlags UseKindFlags { get; private set; }

        /// <summary>
        /// Gets value indicating what is meant to be used
        /// </summary>
        [UsedImplicitly]
        public ImplicitUseTargetFlags TargetFlags { get; private set; }
    }


    [Flags]
    enum ImplicitUseKindFlags {
        Default = Access | Assign | InstantiatedWithFixedConstructorSignature,

        /// <summary>
        /// Only entity marked with attribute considered used
        /// </summary>
        Access = 1,

        /// <summary>
        /// Indicates implicit assignment to a member
        /// </summary>
        Assign = 2,

        /// <summary>
        /// Indicates implicit instantiation of a type with fixed constructor signature.
        /// That means any unused constructor parameters won't be reported as such.
        /// </summary>
        InstantiatedWithFixedConstructorSignature = 4,

        /// <summary>
        /// Indicates implicit instantiation of a type
        /// </summary>
        InstantiatedNoFixedConstructorSignature = 8,
    }


    /// <summary>
    /// Specify what is considered used implicitly when marked with <see cref="MeansImplicitUseAttribute"/> or <see cref="UsedImplicitlyAttribute"/>
    /// </summary>
    [Flags]
    enum ImplicitUseTargetFlags {
        Default = Itself,

        Itself = 1,

        /// <summary>
        /// Members of entity marked with attribute are considered used
        /// </summary>
        Members = 2,

        /// <summary>
        /// Entity marked with attribute and all its members considered used
        /// </summary>
        WithMembers = Itself | Members
    }


    /// <summary>
    /// This attribute is intended to mark publicly available API which should not be removed and so is treated as used.
    /// </summary>
    [MeansImplicitUse]
    sealed class PublicAPIAttribute : Attribute {
        public PublicAPIAttribute() {}

        public PublicAPIAttribute( string comment ) {}
    }


    /// <summary>
    /// Tells code analysis engine if the parameter is completely handled when the invoked method is on stack. 
    /// If the parameter is delegate, indicates that delegate is executed while the method is executed.
    /// If the parameter is enumerable, indicates that it is enumerated while the method is executed.
    /// </summary>
    [AttributeUsage( AttributeTargets.Parameter, Inherited = true )]
    sealed class InstantHandleAttribute : Attribute {}


    /// <summary> Indicates that method doesn't contain observable side effects. </summary>
    [AttributeUsage( AttributeTargets.Method, Inherited = true )]
    sealed class PureAttribute : Attribute {}
}