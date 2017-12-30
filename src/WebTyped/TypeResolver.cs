﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebTyped.Elements;

namespace WebTyped {
	public class TypeResolver {
		public IEnumerable<Service> Services { get; private set; } = new List<Service>();
		public IEnumerable<TsModelBase> Models { get; private set; } = new List<TsModelBase>();
		public Dictionary<ITypeSymbol, ITsFile> TypeFiles { get; private set; } = new Dictionary<ITypeSymbol, ITsFile>();
		Options Options { get; set; }
		public TypeResolver(Options options) {
			this.Options = options;
		}
		//public void Add(Service service) {
		//	this.TypeFiles[service.TypeSymbol] = service;
		//	(this.Services as List<Service>).Add(service);
		//}
		//public void Add(Model model) {
		//	this.TypeFiles[model.TypeSymbol] = model;
		//	(this.Models as List<Model>).Add(model);
		//}

		public void Add(ITsFile file) {
			this.TypeFiles[file.TypeSymbol] = file;
			if (file is TsModelBase) {
				(this.Models as List<TsModelBase>).Add(file as TsModelBase);
			}
			if (file is Service) {
				(this.Services as List<Service>).Add(file as Service);
			}
		}
		public string Resolve(ITypeSymbol typeSymbol) {
			if (TypeFiles.TryGetValue(typeSymbol, out var tsType)) {
				return tsType.FullName;
			}

			var type = typeSymbol as INamedTypeSymbol;
			//tuples
			if (type.IsTupleType) {
				return $"{{{string.Join(", ", type.TupleElements.Select(te => $"{te.Name}: {Resolve(te.Type as INamedTypeSymbol)}"))}}}";
			}

			string name = type.Name;
			var members = type.GetTypeMembers();
			string typeName = type.Name;
			//In case of nested types
			var parent = type.ContainingType;
			while (parent != null) {
				//Check if parent type is controller > service
				var parentName = parent.Name;
				//Adjust to check prerequisites
				if (Service.IsService(parent)) {
					parentName = parentName.Replace("Controller", "Service");
				}
				//For now, we'll just check if ends with "Controller" suffix
				//if (parentName.EndsWith("Controller")) {
				//	parentName = parentName.Replace("Controller", "Service");
				//}

				typeName = $"{parentName}.{typeName}";
				parent = parent.ContainingType;
			}
			//Change type to ts type
			var tsTypeName = ToTsTypeName(type);
			//If contains "{" or "}" then it was converted to anonymous type, so no need to do anything more.
			if (tsTypeName.Contains("{")) {
				return tsTypeName;
			}
			//if (tsTypeName == "any") {
			//	return $"any/* {type.ToString()} */";
			//}

			string genericPart = "";
			//Generic
			if (type.IsGenericType) {
				if (string.IsNullOrEmpty(tsTypeName)) {
					tsTypeName = Resolve(type.TypeArguments[0]);
				} else {
					genericPart = $"<{string.Join(", ", type.TypeArguments.Select(t => Resolve(t as INamedTypeSymbol)))}>";
				}
			}
			//genericPart = genericPart.Replace("*", "");

			if (tsTypeName == "any" || tsTypeName.StartsWith("any/*")) {
				genericPart = genericPart.Replace("*", "");
				if (!string.IsNullOrEmpty(genericPart)) {
					genericPart = $"/*{genericPart}*/";
				}
				//return $"{tsTypeName}{(string.IsNullOrEmpty(genericPart) ? "" : $"")}";
			}
			if(tsTypeName == "Array" && string.IsNullOrWhiteSpace(genericPart)) {
				genericPart = "<any>";
			}
			return $"{tsTypeName}{genericPart}";
		}
		public bool IsNullable(ITypeSymbol t) {
			return (t as INamedTypeSymbol).ConstructedFrom.ToString() == "System.Nullable<T>";
		}

		string ToTsTypeName(INamedTypeSymbol original) {
			if (IsNullable(original)) { return ""; }
			switch (original.SpecialType) {
				case SpecialType.System_Boolean:
					return "boolean";
				case SpecialType.System_Byte:
				case SpecialType.System_Decimal:
				case SpecialType.System_Double:
				case SpecialType.System_Int16:
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
				case SpecialType.System_Single:
				case SpecialType.System_UInt16:
				case SpecialType.System_UInt32:
				case SpecialType.System_UInt64:
					return "number";
				case SpecialType.System_Char:
				case SpecialType.System_String:
				case SpecialType.System_DateTime:
					return "string";
				case SpecialType.System_Array:
				case SpecialType.System_Collections_Generic_ICollection_T:
				case SpecialType.System_Collections_Generic_IEnumerable_T:
				case SpecialType.System_Collections_Generic_IList_T:
				case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
				case SpecialType.System_Collections_Generic_IReadOnlyList_T:
				case SpecialType.System_Collections_IEnumerable:
					return "Array";
				case SpecialType.System_Void: return "void";
			}
			var constructedFrom = (original as INamedTypeSymbol).ConstructedFrom.ToString();

			//IQueryable<int> a;
			switch (constructedFrom) {
				//ase nameof(Boolean):
				//	return "boolean";
				//case nameof(DateTime):
				//case nameof(String):
				//	return "string";
				//case nameof(Int32):
				//case nameof(Int16):
				//case nameof(Int64):
				//	return "number";
				//case nameof(IEnumerable):
				case "System.Collections.Generic.IList<T>":
				case "System.Collections.Generic.List<T>":
				case "System.Collections.Generic.IEnumerable<T>":
				case "System.Collections.Generic.ICollection<T>":
				case "System.Linq.IQueryable<T>":
					return "Array";
				case "System.Threading.Tasks.Task":
					return "void";
				case "System.Threading.Tasks.Task<TResult>":
					return "";
				case "System.Collections.Generic.KeyValuePair<TKey, TValue>":
					var keyType = Resolve(original.TypeArguments[0]);
					var valType = Resolve(original.TypeArguments[1]);
					return Options.KeepPropsCase ?
					$"{{ Key: {keyType}, Value: {valType} }}"
					: $"{{ key: {keyType}, value: {valType} }}";
				//default: return typeName;
				default: return $"any/*{constructedFrom}*/";
			}
		}

		public async Task SaveAllAsync() {
			var currentFiles = Directory.GetFiles(Options.OutDir, "*.ts", SearchOption.AllDirectories);
			List<Task<string>> tasks = new List<Task<string>>();
			foreach (var m in Models) {
				tasks.Add(m.SaveAsync());
			}
			foreach (var s in Services) {
				tasks.Add(s.SaveAsync());
			}
			//Create indexes
			//Create root index
			var sbRootIndex = new StringBuilder();
			if (Options.ServiceMode == ServiceMode.Angular) {
				sbRootIndex.AppendLine("import { NgModule, ModuleWithProviders } from '@angular/core';");
				sbRootIndex.AppendLine("import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';");
				sbRootIndex.AppendLine("import { WebTypedEventEmitterService, WebTypedInterceptor } from '@guimabdo/webtyped-angular';");
			}

			//Create index for each module folder
			var serviceModules = Services
				.GroupBy(s => s.Module)
				.Distinct()
				.OrderBy(g => g.Key);
			var counter = 0;
			var services = new List<string>();
			foreach (var sm in serviceModules) {
				sbRootIndex.AppendLine($"import * as mdl{counter} from './{sm.Key.ToCamelCase()}'");
				var sbServiceIndex = string.IsNullOrEmpty(sm.Key) ? sbRootIndex : new StringBuilder();
				foreach (var s in sm.OrderBy(s => s.ClassName)) {
					sbServiceIndex.AppendLine($"export * from './{s.FilenameWithoutExtenstion}';");
					services.Add($"mdl{counter}.{s.ClassName}");
				}

				if (!string.IsNullOrEmpty(sm.Key)) {
					var servicesDir = Path.Combine(Options.OutDir, sm.Key.ToCamelCase());
					var serviceIndexFile = Path.Combine(servicesDir, "index.ts");
					//files.Add(serviceIndexFile);
					//await FileHelper.WriteAsync(serviceIndexFile, sbServiceIndex.ToString());
					tasks.Add(FileHelper.WriteAsync(serviceIndexFile, sbServiceIndex.ToString()));
				}
				counter++;
			}
			sbRootIndex.AppendLine("export var serviceTypes = [");
			sbRootIndex.AppendLine(1, string.Join($",{System.Environment.NewLine}	", services));
			sbRootIndex.AppendLine("]");
			if (Options.ServiceMode == ServiceMode.Angular) {
				//sbRootIndex.AppendLine("@NgModule({");
				//sbRootIndex.AppendLine(1, "imports: [ HttpClientModule ],");
				//sbRootIndex.AppendLine(1, "providers: [");
				//sbRootIndex.AppendLine(2, "WebTypedEventEmitterService,");
				//sbRootIndex.AppendLine(2, "...serviceTypes");
				//sbRootIndex.AppendLine(1, "]");
				//sbRootIndex.AppendLine("})");
				//sbRootIndex.AppendLine("export class WebTypedGeneratedModule {}");
				sbRootIndex.AppendLine("@NgModule({");
				sbRootIndex.AppendLine(1, "imports: [ HttpClientModule ]");
				sbRootIndex.AppendLine("})");
				sbRootIndex.AppendLine("export class WebTypedGeneratedModule {");
				sbRootIndex.AppendLine(1, "static forRoot(): ModuleWithProviders {");
				sbRootIndex.AppendLine(2, "return {");
				sbRootIndex.AppendLine(3, "ngModule: WebTypedGeneratedModule,");
				sbRootIndex.AppendLine(3, "providers: [");
				sbRootIndex.AppendLine(4, "{");
				sbRootIndex.AppendLine(5, "provide: HTTP_INTERCEPTORS,");
				sbRootIndex.AppendLine(5, "useClass: WebTypedInterceptor,");
				sbRootIndex.AppendLine(5, "multi: true");
				sbRootIndex.AppendLine(4, "},");
				sbRootIndex.AppendLine(4, "WebTypedEventEmitterService,");
				sbRootIndex.AppendLine(4, "...serviceTypes");
				sbRootIndex.AppendLine(3, "]");
				sbRootIndex.AppendLine(2, "};");
				sbRootIndex.AppendLine(1, "}");
				sbRootIndex.AppendLine("}");
			}

			var rootIndexFile = Path.Combine(Options.OutDir, "index.ts");
			//files.Add(rootIndexFile);
			//await FileHelper.WriteAsync(rootIndexFile, sbRootIndex.ToString());
			tasks.Add(FileHelper.WriteAsync(rootIndexFile, sbRootIndex.ToString()));


			if (Options.Clear) {
				Console.WriteLine("Webtyped - Clearing invalid files");
				//currentFiles.ToList().ForEach(f => Console.WriteLine(f));
				var files = new HashSet<string>();
				foreach (var t in tasks) {
					files.Add(await t);
				}
				//Console.Write("new files");
				//files.ToList().ForEach(f => Console.WriteLine(f));
				var delete = currentFiles.Except(files, StringComparer.InvariantCultureIgnoreCase);
				//delete.ToList().ForEach(f => Console.WriteLine(f));
				//Console.WriteLine($"celete: {string.Join(", ", delete)}");
				foreach (var f in delete) {
					var line = File.ReadLines(f).First();
					var mark = FileHelper.Mark.Replace(System.Environment.NewLine, "");
					//Console.WriteLine($"{line} == {FileHelper.Mark} - {line.CompareTo(FileHelper.Mark.Replace(System.Environment.NewLine, ""))}");
					//if (line.ToUpper().Contains(FileHelper.Mark.ToUpper())) {
					if (line == mark) {
						Console.WriteLine($"deleting {f}");
						File.Delete(f);
					}
				}
				DeleteEmptyDirs(Options.OutDir);
			}
		}

		static void DeleteEmptyDirs(string dir) {
			if (String.IsNullOrEmpty(dir))
				throw new ArgumentException(
					"Starting directory is a null reference or an empty string",
					"dir");

			try {
				foreach (var d in Directory.EnumerateDirectories(dir)) {
					DeleteEmptyDirs(d);
				}

				var entries = Directory.EnumerateFileSystemEntries(dir);

				if (!entries.Any()) {
					try {
						Directory.Delete(dir);
					} catch (UnauthorizedAccessException) { } catch (DirectoryNotFoundException) { }
				}
			} catch (UnauthorizedAccessException) { }
		}
	}
}
