// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace FsCheckCSharp
{
    [ExcludeFromCodeCoverage]
    public class CSharpNotationSerializer
    {
        private const string singleIndent = "  ";
        private static readonly string newLine = Environment.NewLine;

        private class Context
        {
            public Context(int IndentLevel, CSharpNotationConfig Config)
            {
                this.IndentLevel = IndentLevel;
                this.Config = Config;
            }

            public int IndentLevel { get; }
            public CSharpNotationConfig Config { get; }

            public Context AddIndent()
            {
                return With(IndentLevel + 1);
            }

            private Context With(int? IndentLevel = null, CSharpNotationConfig Config = null)
            {
                return new Context(IndentLevel ?? this.IndentLevel, Config ?? this.Config);
            }
        }

        public static string Serialize(object item, CSharpNotationConfig config = null)
        {
            return SerializeEach(new[] {item}, config);
        }

        public static string SerializeEach(IEnumerable<object> objects, CSharpNotationConfig config = null)
        {
            var context = new Context(0, config ?? CSharpNotationConfig.Default);

            var list = objects.ToList();

            return string.Join(
                $"{newLine}", list.Zip(
                    Enumerable.Range(0, list.Count), (x, i) => {
                        var assignmentPart = context.Config._SkipCreateAssignment ? string.Empty :
                            list.Count == 1 ? "var data = " : $"var data{i} = ";

                        return $"{assignmentPart}{GetCSharpString(x, context)};";
                    }
                )
            );
        }

        private static string CreateObject(object o, Context context)
        {
            var indents = GetIndents(context);

            var members = o.GetType().GetProperties();
            var sb = new StringBuilder();

            var foundMatchingCtor = false;
            if (!context.Config._PreferObjectInitialization)
            {
                var matchingCtor = o.GetType()
                    .GetConstructors()
                    .FirstOrDefault(
                        x => x.GetParameters()
                            .All(p => members.Any(m => p.Name == m.Name && p.ParameterType == m.PropertyType))
                    );

                if (matchingCtor != null)
                {
                    foundMatchingCtor = true;
                    var parameters = members.Select(x => (name: x.Name, val: x.GetValue(o)));

                    sb.Append($"{indents}new {GetClassName(o, context)}(");
                    var strParameters = parameters.Select(
                        x => context.Config._IncludeParameterNames ? $"{x.name}: {x.val}" : x.val
                    );
                    sb.Append(string.Join(", ", strParameters));
                    sb.Append(")");
                }
            }

            if (!foundMatchingCtor)
            {
                sb.Append($"{indents}new {GetClassName(o, context)} {newLine}{indents}{{{newLine}");
                foreach (var property in members)
                {
                    var value = property.GetValue(o);
                    if (value != null)
                    {
                        sb.Append(
                            $"{indents}{singleIndent}{property.Name} = {GetCSharpString(value, context.AddIndent())},{newLine}"
                        );
                    }
                }

                sb.Append($"{indents}}}");
            }

            return sb.ToString();
        }

        private static string GetClassName(object o, Context context)
        {
            var type = o.GetType();
            var name = GetTypeName(type);

            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments().Select(GetTypeName).ToList();
                name = name.Replace($"{type.Name}`{args.Count}", $"{type.Name}<{string.Join(", ", args)}>");
            }

            return name;

            string GetTypeName(Type t)
            {
                return context.Config._IncludeFullTypeNames
                    ? t.FullName
                    : // Remove namespace and for nested types, convert + into dots.
                    t.FullName?.Replace(t.Namespace + ".", string.Empty).Replace("+", ".");
            }
        }

        private static string GetCSharpString(object o, Context context)
        {
            var indents = GetIndents(context);
            switch (o)
            {
                // Generic cases
                case bool _:
                    return $"{o.ToString().ToLower()}";
                case string _:
                    return $"\"{o}\"";
                case int _:
                    return $"{o}";
                case decimal _:
                    return $"{o}m";
                case DateTime _:
                    return $"DateTime.Parse(\"{o}\")";
                case DateTimeOffset _:
                    return $"DateTimeOffset.Parse(\"{o}\")";
                case Enum _:
                    return $"{o.GetType().FullName}.{o}";
                case IEnumerable items:
                    return $"{indents}new[] {{{newLine}{GetItems(items, context)}}}";
                default:
                    return CreateObject(o, context);
            }
        }

        private static string GetIndents(Context context)
        {
            return string.Concat(Enumerable.Range(0, context.IndentLevel).Select(_ => singleIndent));
        }

        private static string GetItems(IEnumerable items, Context context)
        {
            var indents = GetIndents(context);
            var sb = new StringBuilder();
            var strItems = items.Cast<object>().Select(x => $"{indents}{GetCSharpString(x, context.AddIndent())}");
            sb.Append(string.Join($",{newLine}", strItems));
            sb.Append($"{newLine}{indents}"); // no comma before newline at list end
            return sb.ToString();
        }
    }
}
