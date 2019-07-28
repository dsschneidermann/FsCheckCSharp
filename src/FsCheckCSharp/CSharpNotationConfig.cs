// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System.Diagnostics.CodeAnalysis;

namespace FsCheckCSharp
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage("ReSharper", "ArgumentsStyleLiteral")]
    public class CSharpNotationConfig
    {
        /// <summary>Create an instance.</summary>
        /// <param name="_PreferObjectInitialization">
        ///     Prefer object initialization even if a constructor for the type is available.
        /// </param>
        /// <param name="_IncludeParameterNames">
        ///     Include names for parameters in constructor invocations.
        /// </param>
        /// <param name="_IncludeFullTypeNames">
        ///     Include full type name and assembly name.
        /// </param>
        /// <param name="_SkipCreateAssignment">
        ///     Do not create the "var data = " prefix for each serialized object.
        /// </param>
        public CSharpNotationConfig(
            bool _PreferObjectInitialization, bool _IncludeParameterNames, bool _IncludeFullTypeNames,
            bool _SkipCreateAssignment)
        {
            this._PreferObjectInitialization = _PreferObjectInitialization;
            this._IncludeParameterNames = _IncludeParameterNames;
            this._IncludeFullTypeNames = _IncludeFullTypeNames;
            this._SkipCreateAssignment = _SkipCreateAssignment;
        }

        /// <summary>
        ///     Returns the default configuration (all settings false).
        /// </summary>
        public static CSharpNotationConfig Setup => Default;

        /// <summary>
        ///     Returns the default configuration (all settings false).
        /// </summary>
        public static CSharpNotationConfig Default => new CSharpNotationConfig(false, false, false, false);

        /// <summary>
        ///     Prefer object initialization even if a constructor for the type is available.
        /// </summary>
        public bool _PreferObjectInitialization { get; }

        /// <summary>
        ///     Include names for parameters in constructor invocations.
        /// </summary>
        public bool _IncludeParameterNames { get; }

        /// <summary>
        ///     Include full type name and assembly name.
        /// </summary>
        public bool _IncludeFullTypeNames { get; }

        /// <summary>
        ///     Do not create the "var data = " prefix for each serialized object.
        /// </summary>
        public bool _SkipCreateAssignment { get; }

        /// <summary>
        ///     Include full type name and assembly name.
        /// </summary>
        public CSharpNotationConfig IncludeFullTypeNames()
        {
            return With(_IncludeFullTypeNames: true);
        }

        /// <summary>
        ///     Include names for parameters in constructor invocations.
        /// </summary>
        public CSharpNotationConfig IncludeParameterNames()
        {
            return With(_IncludeParameterNames: true);
        }

        /// <summary>
        ///     Prefer object initialization even if a constructor for the type is available.
        /// </summary>
        public CSharpNotationConfig PreferObjectInitialization()
        {
            return With(_PreferObjectInitialization: true);
        }

        /// <summary>
        ///     Do not create the "var data = " prefix for each serialized object.
        /// </summary>
        public CSharpNotationConfig SkipCreateAssignment()
        {
            return With(_SkipCreateAssignment: true);
        }

        /// <summary>Return a new instance with the properties.</summary>
        /// <param name="_PreferObjectInitialization">
        ///     Prefer object initialization even if a constructor for the type is available.
        /// </param>
        /// <param name="_IncludeParameterNames">
        ///     Include names for parameters in constructor invocations.
        /// </param>
        /// <param name="_IncludeFullTypeNames">
        ///     Include full type name and assembly name.
        /// </param>
        /// <param name="_SkipCreateAssignment">
        ///     Do not create the "var data = " prefix for each serialized object.
        /// </param>
        public CSharpNotationConfig With(
            bool? _PreferObjectInitialization = null, bool? _IncludeParameterNames = null,
            bool? _IncludeFullTypeNames = null, bool? _SkipCreateAssignment = null)
        {
            return new CSharpNotationConfig(
                _PreferObjectInitialization ?? this._PreferObjectInitialization,
                _IncludeParameterNames ?? this._IncludeParameterNames,
                _IncludeFullTypeNames ?? this._IncludeFullTypeNames, _SkipCreateAssignment ?? this._SkipCreateAssignment
            );
        }
    }
}
