﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using WebTyped.Abstractions;

namespace WebTyped.Elements
{
	public class TsEnum : TsModelBase {
		public TsEnum(INamedTypeSymbol modelType, TypeResolver typeResolver, Options options) : base(modelType, typeResolver, options) {}

		public override (string file, string content)? GenerateOutput() {
			//If external
			if (this.HasCustomMap) { return null; }
			var subClasses = new List<INamedTypeSymbol>();
			var sb = new StringBuilder();

			var level = 0;
			//var hasModule = false;
			//if (!string.IsNullOrEmpty(Module) && Options.TypingsScope == TypingsScope.Global) {
			//	hasModule = true;
			//	sb.AppendLine($"declare module {Module} {{");
			//	level++;
			//}
			//var declaration = "declare ";
			//if (hasModule) { declaration = ""; }
			//if(Options.TypingsScope == TypingsScope.Module) {
			//	declaration = "export ";
			//}
			//sb.AppendLine(level, $"export const enum {TypeSymbol.Name} {{");
			sb.AppendLine(level, $"export enum {TypeSymbol.Name} {{");
			foreach (var m in TypeSymbol.GetMembers()) {
				if (m.Kind == SymbolKind.Field) {
					var f = m as IFieldSymbol;
					sb.AppendLine(level + 1, $"{f.Name} = {f.ConstantValue},");
				}
			}
			sb.AppendLine(level, "}");
			//if (!string.IsNullOrEmpty(Module) && Options.TypingsScope == TypingsScope.Global) {
			//	sb.AppendLine("}");
			//}

			AppendKeysAndNames(sb);

			//File.WriteAllText(FileHelper.PathCombine(Options.ModelsDir, Filename), sb.ToString());
			//string file = FileHelper.PathCombine(Options.TypingsDir, Filename);
			string content = sb.ToString();
			return (OutputFilePath, content);
			//await FileHelper.WriteAsync(file, content);
			//return file;
		}

        public override OutputFileAbstraction GetAbstraction()
        {
            //If external
            if (this.HasCustomMap) { return null; }

            var result = new EnumAbstraction
            {
                Path = AbstractPath,
                Name = TypeSymbol.Name,
                Values = TypeSymbol
                    .GetMembers()
                    .Where(m => m.Kind == SymbolKind.Field)
                    .Select(m => m as IFieldSymbol)
                    .Select(f => new EnumValueAbstraction
                    {
                        Name = f.Name,
                        Value = (int)f.ConstantValue
                    }).ToList()
            };
            

            return result;
        }

        public static bool IsEnum(ITypeSymbol t) {
			if (t.BaseType?.SpecialType != SpecialType.System_Enum) { return false; }
			return true;
		}
	}
}
