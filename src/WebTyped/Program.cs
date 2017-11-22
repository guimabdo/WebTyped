﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WebTyped {
	public class Program {
		public static int Main(params string[] args) {
			var cmd = new CommandLineApplication() {
				Name = "WebTyped",
				Description = "Generate typescript services and model definitions based on your webApis"
			};
			cmd.VersionOption("--version", "0.1.0");
			cmd.HelpOption("-?|-h|--help");

			cmd.Command("generate", target => {
				var sourceFiles = target.Option("-sf | --sourceFiles", "C# source files (glob supported)", CommandOptionType.MultipleValue);
				var outDir = target.Option("-od | --outDir", "", CommandOptionType.SingleValue);
				var trims = target.Option("-t | --trim", "These module names will be removed when generating ts code", CommandOptionType.MultipleValue);
				var clear = target.Option("-c | --clear", "Clears folder", CommandOptionType.NoValue);

				target.HelpOption("-?|-h|--help");
				target.OnExecute(async () => {
					var matcher = new Matcher();
					foreach (var val in sourceFiles.Values) {
						matcher.AddInclude(val);
					}
					var csFiles = matcher.GetResultsInFullPath(Directory.GetCurrentDirectory());
					var trees = new List<SyntaxTree>();
					var tasks = new List<Task>();
					foreach (var csFile in csFiles) {
						tasks.Add(File.ReadAllTextAsync(csFile)
							.ContinueWith(tsk => {
								trees.Add(CSharpSyntaxTree.ParseText(tsk.Result));
							}));
						//trees.Add(CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(csFile)));
					}
					////Wait all task
					//while (tasks.Any()) {
					//	tasks.RemoveAll(t => t.IsCompleted);
					//	//var completed = tasks.FirstOrDefault(t => t.IsCompleted);
					//	//if (completed != null) {
					//	//	//trees.Add(CSharpSyntaxTree.ParseText(await completed));
					//	//	tasks.Remove(completed);
					//	//}
					//}

					//References
					var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
					foreach(var task in tasks) { await task; }
					var compilation = CSharpCompilation.Create("Comp", trees, new[] { mscorlib });

					//1200ms

					var semanticModels = trees.ToDictionary(t => t, t => compilation.GetSemanticModel(t));
					var options = new Options(outDir.Value(), clear.HasValue(), trims.Values);
					var typeResolver = new TypeResolver(options);

					//1330ms
					tasks = new List<Task>();
					var namedTypeSymbols = new List<INamedTypeSymbol>();
					foreach (var t in trees) {
						tasks.Add(t.GetRootAsync().ContinueWith(tks => {
							var root = tks.Result;
							foreach (var @class in root.DescendantNodes().OfType<ClassDeclarationSyntax>()) {
								var sm = semanticModels[t];
								namedTypeSymbols.Add(sm.GetDeclaredSymbol(@class));

								//if (Service.CanBeService(dsClass)) {
								//	var svc = new Service(dsClass, typeResolver, options);
								//	continue;
								//}

								//if (Model.CanBeModel(dsClass)) {
								//	var model = new Model(dsClass, typeResolver, options);
								//	continue;
								//}
							}
						}));
					}
					foreach(var tsk in tasks) { await tsk; }
					foreach(var s in namedTypeSymbols) {
						if (Service.CanBeService(s)) {
							var svc = new Service(s, typeResolver, options);
							continue;
						}

						if (Model.CanBeModel(s)) {
							var model = new Model(s, typeResolver, options);
							continue;
						}
					}
					//return 0;

					await typeResolver.SaveAllAsync();
					//var saveTask = typeResolver.SaveAllAsync();
					return 0;
				});
			});

			cmd.OnExecute(() => {
				cmd.ShowHelp();
				return 0;
			});

			return cmd.Execute(args);
		}
	}
}
