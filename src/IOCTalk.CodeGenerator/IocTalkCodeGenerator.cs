using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace IOCTalk.CodeGenerator
{
    [Generator]
    public class IocTalkCodeGenerator : IIncrementalGenerator
    {
        private const string MethodNameRegisterRemoteService = "RegisterRemoteService";
        private const string MethodNameRegisterLocalSessionService = "RegisterLocalSessionService";

        Dictionary<ITypeSymbol, ITypeSymbol>? interfaceClassImplementationMappings = null;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
#if DEBUG
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
#endif

            // collect remote services
            var interfaceTypes = context.SyntaxProvider
                    .CreateSyntaxProvider(CollectRegisterRemoteMethods, GetProxyInterface)
                    .Where(type => type is not null)
                    .Collect();


            // collect target assembly name
            var assemblyName = context.CompilationProvider.Select(static (cp, _) => cp.AssemblyName);

            // collect own custom interface implementations
            var implementedInterfaces = context.SyntaxProvider
                .CreateSyntaxProvider(CollectImplementedInterfaceClasses, GetImplementedInterface)
                .Where(type => type is not null)
                 .Collect();


            var localSessionServiceTypes = context.SyntaxProvider
                        .CreateSyntaxProvider(CollectRegisterLocalSessionMethods, GetLocalSessionServiceInterface)
                        .Where(type => type is not null)
                        .Collect();

            var combined = interfaceTypes.Combine(assemblyName).Combine(implementedInterfaces).Combine(localSessionServiceTypes);

            context.RegisterSourceOutput(combined, ExecuteGenerateCode);
        }

        private bool CollectRegisterRemoteMethods(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            //Debug.WriteLine(syntaxNode.GetType().Name + ": " + syntaxNode.ToString());

            if (syntaxNode is InvocationExpressionSyntax invokeExp)
            {
                var genericName = invokeExp.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault();

                if (genericName != null)
                {
                    string genericNameStr = genericName.ToString();

                    if (genericNameStr.StartsWith(MethodNameRegisterRemoteService))
                    {
                        // ioc talk generic remote service registration
                        return true;
                    }
                }
            }

            return false;
        }



        private bool CollectRegisterLocalSessionMethods(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            //Debug.WriteLine(syntaxNode.GetType().Name + ": " + syntaxNode.ToString());

            if (syntaxNode is InvocationExpressionSyntax invokeExp)
            {
                var genericName = invokeExp.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault();

                if (genericName != null)
                {
                    string genericNameStr = genericName.ToString();

                    if (genericNameStr.StartsWith(MethodNameRegisterLocalSessionService))
                    {
                        // ioc talk local session service implementation
                        return true;
                    }
                }
            }

            return false;
        }


        private ITypeSymbol? GetProxyInterface(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var invokeExpr = (InvocationExpressionSyntax)context.Node;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invokeExpr.Expression, cancellationToken);

            //var t = symbolInfo.Symbol.ContainingType;
            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                if (method.IsGenericMethod
                    && method.Name.Equals(MethodNameRegisterRemoteService))
                {
                    var genericTypeArg0 = method.TypeArguments.FirstOrDefault();

                    if (genericTypeArg0 != null)
                    {
                        return genericTypeArg0;
                    }
                }
            }


            return null;
        }


        private ITypeSymbol? GetLocalSessionServiceInterface(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var invokeExpr = (InvocationExpressionSyntax)context.Node;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invokeExpr.Expression, cancellationToken);

            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                if (method.IsGenericMethod
                    && method.Name.Equals(MethodNameRegisterLocalSessionService))
                {
                    var genericTypeArg0 = method.TypeArguments.FirstOrDefault();

                    if (genericTypeArg0 != null)
                    {
                        return genericTypeArg0;
                    }
                }
            }


            return null;
        }



        private bool CollectImplementedInterfaceClasses(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            //Debug.WriteLine(syntaxNode.GetType().Name + ": " + syntaxNode.ToString());

            if (syntaxNode is ClassDeclarationSyntax cds)
            {
                return true;
            }
            return false;
        }

        private Dictionary<ITypeSymbol, ITypeSymbol>? GetImplementedInterface(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            if (interfaceClassImplementationMappings == null)
            {
                // collect all referenced implementations
                // execute only once (caching) - no better way of collecting referenced interface implementations found yet
                interfaceClassImplementationMappings = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
                var assemblyName = context.SemanticModel.Compilation.AssemblyName;
                if (assemblyName != null)
                {
                    assemblyName = GetMainNamespace(assemblyName);

                    var customTypes = context.SemanticModel.Compilation.SourceModule.ReferencedAssemblySymbols
                        .Where(ras => ras.Name.StartsWith(assemblyName))
                        .SelectMany(a =>
                        {
                            try
                            {
                                var main = a.Identity.Name.Split('.').Aggregate(a.GlobalNamespace, (s, c) => s.GetNamespaceMembers().Single(m => m.Name.Equals(c)));

                                Debug.WriteLine(main.ToDisplayString());

                                return GetAllClassTypes(main);
                            }
                            catch
                            {
                                return Enumerable.Empty<ITypeSymbol>();
                            }
                        });

                    foreach (var ct in customTypes)
                    {
                        if (ct.AllInterfaces.Any())
                        {
                            foreach (var interf in ct.AllInterfaces)
                            {
                                AddCustomMap(interf, ct);
                            }
                        }
                    }
                }
            }

            if (context.Node is ClassDeclarationSyntax cds)
            {
                // received class interface implementations
                // Use the semantic model to get the symbol for this type
                var typeNodeSymbol = context.SemanticModel.Compilation
                .GetSemanticModel(cds.SyntaxTree)
                .GetDeclaredSymbol(cds);

                if (typeNodeSymbol != null)
                {
                    var typeInfo = (ITypeSymbol)typeNodeSymbol;

                    if (typeInfo.IsAbstract == false
                        && typeInfo.AllInterfaces.Any())
                    {
                        foreach (var implInterf in typeInfo.AllInterfaces)
                        {
                            AddCustomMap(implInterf, typeInfo);
                        }
                    }
                }
            }

            return interfaceClassImplementationMappings;
        }

        private static string GetMainNamespace(string? assemblyName)
        {
            string[] namespaceParts = assemblyName.Split('.');
            if (namespaceParts.Length > 1)
            {
                // remove latest level
                var startLevelsOnly = new string[namespaceParts.Length - 1];
                Array.Copy(namespaceParts, startLevelsOnly, startLevelsOnly.Length);
                assemblyName = string.Join(".", startLevelsOnly);
            }

            return assemblyName;
        }

        private void AddCustomMap(ITypeSymbol interfaceType, ITypeSymbol implInterf)
        {
            if (interfaceType.ContainingNamespace == null
                || interfaceType.ContainingNamespace.ToString().StartsWith("System") == false)
            {
                interfaceClassImplementationMappings[interfaceType] = implInterf;
            }
        }

        private static IEnumerable<ITypeSymbol> GetAllClassTypes(INamespaceSymbol root)
        {
            foreach (var namespaceOrTypeSymbol in root.GetMembers())
            {
                if (namespaceOrTypeSymbol is INamespaceSymbol @namespace) foreach (var nested in GetAllClassTypes(@namespace)) yield return nested;

                else if (namespaceOrTypeSymbol is ITypeSymbol type
                    && type.IsAbstract == false
                    && type.TypeKind == TypeKind.Class) yield return type;
            }
        }


        //private void ExecuteGenerateCode(SourceProductionContext context, ((ImmutableArray<ITypeSymbol> proxyInterfaceTypes, string? assemblyName) input1, ImmutableArray<Dictionary<ITypeSymbol, ITypeSymbol>?> implementations) input)
        private void ExecuteGenerateCode(SourceProductionContext context, (((ImmutableArray<ITypeSymbol> proxyInterfaceTypes, string? assemblyName) proxyInterfaceAssembly, ImmutableArray<Dictionary<ITypeSymbol, ITypeSymbol>?> implementations) proxyImpl, ImmutableArray<ITypeSymbol> localSessionInterfaceTypes) input)
        {
            if (input.proxyImpl.proxyInterfaceAssembly.proxyInterfaceTypes.IsDefaultOrEmpty
                && input.localSessionInterfaceTypes.IsDefaultOrEmpty)
                return;


            List<string> porxyInterfaceClassNames = new List<string>(input.proxyImpl.proxyInterfaceAssembly.proxyInterfaceTypes.Length);

            List<ITypeSymbol> allDtoTypes = new List<ITypeSymbol>();
            List<string> allDtoImplementationNames = new List<string>();

            var allInterfaceImplementations = input.proxyImpl.implementations.FirstOrDefault();
            Dictionary<ITypeSymbol, ITypeSymbol> customInterfaceImplementations = new Dictionary<ITypeSymbol, ITypeSymbol>();

            foreach (var type in input.proxyImpl.proxyInterfaceAssembly.proxyInterfaceTypes.Distinct(SymbolEqualityComparer.Default)
                                             .Cast<ITypeSymbol>())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                string code = BuildProxyImplementationSource(type, out var dtoTypes, out string proxyClassName).ToString();
                porxyInterfaceClassNames.Add(proxyClassName);


                var typeNamespace = type.ContainingNamespace.IsGlobalNamespace
                       ? null
                       : $"{type.ContainingNamespace}.";

                context.AddSource($"{typeNamespace}{type.Name}AutoGenProxy.g.cs", code);


                foreach (var dtoType in dtoTypes)
                {
                    if (allDtoTypes.Contains(dtoType) == false)
                        allDtoTypes.Add(dtoType);
                }
            }


            // collect local session dto mappings
            // only nested types are collected because registration with class implmentation mapping is expected
            foreach (var localSessionServiceInterface in input.localSessionInterfaceTypes.Distinct(SymbolEqualityComparer.Default)
                                             .Cast<ITypeSymbol>())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                Debug.WriteLine($"{localSessionServiceInterface}");

                GetDtoTypesByForMembersRecursive(localSessionServiceInterface, allDtoTypes);
            }


            // create data transfer object implementations (implementation not found anywhere?)
            string containingNamespace = input.proxyImpl.proxyInterfaceAssembly.assemblyName;
            for (int dtoIndex = 0; dtoIndex < allDtoTypes.Count; dtoIndex++)
            {
                var dtoInterfType = allDtoTypes[dtoIndex];

                if (allInterfaceImplementations != null
                    && allInterfaceImplementations.TryGetValue(dtoInterfType, out ITypeSymbol actualImplType))
                {
                    // do not auto implement already custom implemented interface
                    allDtoTypes.RemoveAt(dtoIndex);
                    dtoIndex--;

                    // add to custom implmentation mapping
                    customInterfaceImplementations[dtoInterfType] = actualImplType;

                    // skip but check for nested interfaces
                    foreach (var member in dtoInterfType.GetMembers())
                    {
                        if (member is IPropertySymbol pi)
                            CheckForNestedDtoTypes(pi, allDtoTypes);
                    }
                    continue;
                }


                string dtoSource = CreateDtoImplementationSource(dtoInterfType, containingNamespace, allDtoTypes, out string className).ToString();
                allDtoImplementationNames.Add(className);

                var typeNamespace = dtoInterfType.ContainingNamespace.IsGlobalNamespace
                                   ? null
                                   : $"{dtoInterfType.ContainingNamespace}.";

                context.AddSource($"{typeNamespace}{GetImplName(dtoInterfType.Name)}AutoGenDto.g.cs", dtoSource);
            }



            // Build interface implementation helper class
            if (input.proxyImpl.proxyInterfaceAssembly.assemblyName != null)
            {
                string mappingSource = BuildInterfaceImplementationMappingsSource(input.proxyImpl.proxyInterfaceAssembly.proxyInterfaceTypes, input.proxyImpl.proxyInterfaceAssembly.assemblyName, porxyInterfaceClassNames, allDtoTypes, allDtoImplementationNames, customInterfaceImplementations).ToString();
                context.AddSource($"AutoGeneratedInterfaceImplementationMapping.g.cs", mappingSource);
            }

        }


        private StringBuilder BuildInterfaceImplementationMappingsSource(ImmutableArray<ITypeSymbol> proxyInterfaceTypes, string assemblyName, List<string> proxyClassNames, List<ITypeSymbol> dtoTypes, List<string> dtoImplementationNames, Dictionary<ITypeSymbol, ITypeSymbol>? interfImplementations)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using BSAG.IOCTalk.Common.Interface.Container;");
            sb.AppendLine("using BSAG.IOCTalk.Composition;");
            sb.AppendLine();
            sb.Append("namespace ");
            sb.AppendLine(assemblyName);
            sb.AppendLine("{");

            sb.AppendLine("internal static class AutoGeneratedProxyInterfaceImplementations { ");

            sb.AppendLine("     public static void RegisterAutoGeneratedProxyInterfaceMappings(this TalkCompositionHost ctx)");
            sb.AppendLine("     {");

            if (interfImplementations != null && interfImplementations.Any())
            {
                sb.AppendLine($"         // custom interface implementations (includes all assemblies starting with \"{GetMainNamespace(assemblyName)}\")");

                foreach (var customImpl in interfImplementations)
                {
                    if (proxyInterfaceTypes.Contains(customImpl.Key))
                        continue;   // do not map proxy interface

                    sb.AppendLine($"         ctx.MapInterfaceImplementationType<{customImpl.Key}, {customImpl.Value}>();");
                }
                sb.AppendLine();
            }


            if (proxyInterfaceTypes.Length > 0)
            {
                sb.AppendLine("         // remote proxy mappings");

                for (int i = 0; i < proxyInterfaceTypes.Length; i++)
                {
                    var proxyInterfType = proxyInterfaceTypes[i];
                    var proxyImplType = proxyClassNames[i];

                    sb.AppendLine($"         ctx.MapInterfaceImplementationType<{proxyInterfType}, {proxyInterfType.ContainingNamespace}.{proxyImplType}>();");
                }
            }

            sb.AppendLine();

            if (dtoTypes.Count > 0)
            {
                sb.AppendLine("         // DTO mappings");
                for (int i = 0; i < dtoTypes.Count; i++)
                {
                    var interfaceType = dtoTypes[i];
                    var implementationType = dtoImplementationNames[i];

                    sb.AppendLine($"         ctx.MapInterfaceImplementationType<{interfaceType}, {assemblyName}.{implementationType}>();");
                }
            }

            sb.AppendLine("     }");

            sb.AppendLine(" }");
            sb.AppendLine("}");

            return sb;
        }



        public static StringBuilder BuildProxyImplementationSource(ITypeSymbol interfaceType, out List<ITypeSymbol> dtoTypes, out string className)
        {
            if (!interfaceType.IsAbstract)
                throw new Exception("Type must be an interface!");

            className = GetImplName(interfaceType.Name) + "AutoGenProxy";

            StringBuilder source = new StringBuilder();

            source.AppendLine("using System;");
            source.AppendLine("using BSAG.IOCTalk.Common.Reflection;");
            source.AppendLine("using BSAG.IOCTalk.Common.Interface.Reflection;");
            source.AppendLine("using BSAG.IOCTalk.Common.Interface.Session;");
            source.AppendLine("using BSAG.IOCTalk.Common.Interface.Communication;");

            source.AppendLine();
            source.Append("namespace ");
            //source.AppendLine("AutoGeneratedProxiesNamespace ");
            source.AppendLine(interfaceType.ContainingNamespace.ToString());
            source.AppendLine("{");
            source.Append($" internal class {className} : {interfaceType.ContainingNamespace}.{interfaceType.Name}");
            source.AppendLine(" {");
            dtoTypes = new List<ITypeSymbol>();
            var methodInfoMemberNames = CreateProxyInterfaceMethodSourceCode(source, interfaceType, dtoTypes);

            // add communication service field
            source.AppendLine();
            //source.Append("     ");
            source.AppendLine("     private IGenericCommunicationService communicationService;");
            source.AppendLine("     private ISession session;");
            source.AppendLine();

            // add constructor
            source.AppendLine($"    public {className}(IGenericCommunicationService commService, ISession session)");
            source.AppendLine("     {");
            source.AppendLine("         this.communicationService = commService;");
            source.AppendLine("         this.session = session;");

            //todo: determine runtime async invoke state for each method

            source.AppendLine("     }");


            source.AppendLine(" }");
            source.AppendLine("}");
            return source;
        }

        private static List<string> CreateProxyInterfaceMethodSourceCode(StringBuilder mainSource, ITypeSymbol interfaceType, List<ITypeSymbol> dtoItems)
        {
            List<string> methodInfoMemberNames = new List<string>();
            string methodBodyIntention = "\t\t";
            string methodLineIntention = "\t\t\t";


            foreach (var member in interfaceType.GetMembers())
            {
                if (member is IMethodSymbol method)
                {
                    StringBuilder sbInvokeInfoMember = new StringBuilder();
                    sbInvokeInfoMember.AppendLine();

                    StringBuilder methodSource = new StringBuilder();

                    bool isReturnRequired;
                    string returnType;
                    bool isAsyncAwait = false;
                    bool containsAsyncResultValue = false;
                    if (method.ReturnsVoid)
                    {
                        isReturnRequired = false;
                        returnType = "void";
                    }
                    else
                    {
                        isReturnRequired = true;
                        isAsyncAwait = IsAsyncAwaitType(method.ReturnType, out containsAsyncResultValue);
                        if (isAsyncAwait)
                        {
                            isReturnRequired = containsAsyncResultValue;

                            if (isReturnRequired)
                                AddAsyncMethodReturnDtoItems(dtoItems, method);
                        }
                        returnType = GetSourceCodeTypeName(method.ReturnType);

                        if (method.ReturnType.TypeKind == TypeKind.Interface)
                        {
                            if (dtoItems.Contains(method.ReturnType) == false)
                                dtoItems.Add(method.ReturnType);
                        }
                    }

                    methodSource.AppendLine();
                    if (isAsyncAwait)
                    {
                        methodSource.AppendFormat("{0}public async {1} {2}(", methodBodyIntention, returnType, method.Name);
                    }
                    else
                        methodSource.AppendFormat("{0}public {1} {2}(", methodBodyIntention, returnType, method.Name);

                    string invokeInfoMemberName = string.Format("methodInfo{0}_", method.Name);

                    List<IParameterSymbol>? outParameters = null;
                    StringBuilder? sbParameterValues = null;
                    StringBuilder? sbParameterTypes = null;

                    var parameters = method.Parameters;

                    sbParameterTypes = new StringBuilder();
                    sbParameterTypes.Append("new Type[] { ");

                    if (parameters.Length > 0)
                    {
                        sbParameterValues = new StringBuilder();
                        sbParameterValues.Append(methodLineIntention);
                        sbParameterValues.Append("object[] parameterValues = new object[] { ");

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var param = parameters[i];

                            ITypeSymbol paramType = param.Type;
                            string? decoration = null;
                            //if (param.IsOut)
                            if (param.RefKind == RefKind.Out)
                            {
                                decoration = "out";
                                //paramType = paramType.GetElementType();

                                if (outParameters == null)
                                    outParameters = new List<IParameterSymbol>();

                                outParameters.Add(param);
                            }

                            //string parameterTypeString = GetSourceCodeTypeName(paramType);
                            string parameterTypeString = $"{paramType.ContainingNamespace}.{paramType.Name}";

                            methodSource.AppendFormat("{0} {1} {2}", decoration, parameterTypeString, param.Name);

                            string invokeInfoMemberTypeName = string.Concat(decoration, parameterTypeString);
                            invokeInfoMemberTypeName = Regex.Replace(invokeInfoMemberTypeName, @"[^a-zA-Z0-9]", "");    // remove invalid chars
                            //if (paramType.IsArray)
                            //{
                            //    invokeInfoMemberTypeName += "Arr";
                            //}
                            invokeInfoMemberName += invokeInfoMemberTypeName;

                            // add reference to parameter value array
                            if (param.RefKind == RefKind.Out)
                            {
                                // out parameter -> pass null
                                sbParameterValues.Append("null");
                            }
                            else
                            {
                                sbParameterValues.Append(param.Name);
                            }

                            // type array
                            //sbParameterTypes.AppendFormat("typeof({0}){1}", parameterTypeString, (param.IsOut ? ".MakeByRefType()" : null));
                            sbParameterTypes.AppendFormat("typeof({0}){1}", parameterTypeString, null);


                            if (i < parameters.Length - 1)
                            {
                                methodSource.Append(", ");
                                sbParameterValues.Append(", ");
                                sbParameterTypes.Append(", ");
                            }

                            if (param.Type.TypeKind == TypeKind.Interface)
                            {
                                if (dtoItems.Contains(param.Type) == false)
                                    dtoItems.Add(param.Type);
                            }
                        }

                        sbParameterValues.AppendLine(" };");

                    }


                    sbParameterTypes.Append(" } ");

                    methodSource.AppendLine(")");

                    methodSource.Append(methodBodyIntention);
                    methodSource.AppendLine("{");

                    if (sbParameterValues != null)
                    {
                        methodSource.Append(sbParameterValues);
                    }

                    sbInvokeInfoMember.Append(methodBodyIntention);
                    string iTypeFullStr = $"{interfaceType.ContainingNamespace}.{interfaceType.Name}";
                    sbInvokeInfoMember.AppendFormat("private static IInvokeMethodInfo {0} = new InvokeMethodInfo(typeof({1}), \"{2}\", {3});", invokeInfoMemberName, iTypeFullStr, method.Name, sbParameterTypes.ToString());
                    sbInvokeInfoMember.AppendLine();

                    methodSource.Append(methodLineIntention);

                    if (isReturnRequired)
                        methodSource.Append("object result = ");

                    if (isAsyncAwait)
                        methodSource.AppendFormat("await communicationService.InvokeMethodAsync(this, {0}, session, {1});", invokeInfoMemberName, (sbParameterValues != null ? "parameterValues" : "null"));
                    else
                        methodSource.AppendFormat("communicationService.InvokeMethod(this, {0}, session, {1});", invokeInfoMemberName, (sbParameterValues != null ? "parameterValues" : "null"));

                    methodSource.AppendLine();

                    if (outParameters != null)
                    {
                        // assign out parameters

                        for (int outParamIndex = 0; outParamIndex < outParameters.Count; outParamIndex++)
                        {
                            var outParam = outParameters[outParamIndex];
                            methodSource.AppendFormat("{0}{1} = ({2})parameterValues[{3}];", methodLineIntention, outParam.Name, GetSourceCodeTypeName(outParam.Type), outParamIndex);
                            methodSource.AppendLine();
                        }
                    }

                    if (isReturnRequired)
                    {
                        if (isAsyncAwait)
                        {
                            if (containsAsyncResultValue)
                            {
                                string genericTypeStr = GetSourceCodeTypeName(((INamedTypeSymbol)method.ReturnType).TypeArguments.First());

                                methodSource.Append($"{methodLineIntention}return ({genericTypeStr})result;");
                            }
                            // else: Task/ValueTask void return
                        }
                        else
                            methodSource.AppendFormat("{0}return ({1})result;", methodLineIntention, returnType);

                        methodSource.AppendLine();
                    }

                    methodSource.Append(methodBodyIntention);
                    methodSource.AppendLine("}");
                    methodSource.AppendLine();

                    // add to main source
                    mainSource.Append(sbInvokeInfoMember);
                    mainSource.Append(methodSource);

                    methodInfoMemberNames.Add(invokeInfoMemberName);
                }
            }

            return methodInfoMemberNames;
        }



        private static StringBuilder CreateDtoImplementationSource(ITypeSymbol interfaceType, string containingNamespace, List<ITypeSymbol> dtoTypes, out string className, ITypeSymbol? parentType = null)
        {
            //string assemblyName = $"IOCTalk.AutoGeneratedAssembly{interfaceType.Name}";

            StringBuilder code = new StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine();
            code.Append("namespace ");
            //code.Append(assemblyName);
            //code.AppendLine(interfaceType.ContainingNamespace.ToString());
            code.AppendLine(containingNamespace);
            code.AppendLine();
            code.AppendLine("{");
            className = $"{GetImplName(interfaceType.Name)}AutoGenDto";
            code.Append($"   internal class {className}");
            code.Append(" : ");
            if (parentType != null)
            {
                code.Append(GetSourceCodeTypeName(parentType));
                code.Append(", ");
                code.Append(GetSourceCodeTypeName(interfaceType));
            }
            else
            {
                code.Append(GetSourceCodeTypeName(interfaceType));
            }
            code.AppendLine();
            code.AppendLine("   {");
            code.AppendLine();
            // implement get/set properties
            foreach (var member in interfaceType.GetMembers())
            {
                if (member is IPropertySymbol pi)
                    AddPropertyCode(code, pi, dtoTypes);
            }

            // implement base interface properties
            foreach (var baseInterface in interfaceType.Interfaces)
            {
                foreach (var baseMember in baseInterface.GetMembers())
                {
                    if (baseMember is IPropertySymbol piBase)
                        AddPropertyCode(code, piBase, dtoTypes);
                }
            }

            code.AppendLine();
            code.AppendLine("   }");
            code.AppendLine("}");

            return code;
        }

        private static void AddPropertyCode(StringBuilder code, IPropertySymbol pi, List<ITypeSymbol> dtoTypes)
        {
            code.AppendLine($"      public {GetSourceCodeTypeName(pi.Type)} {pi.Name} {{get; set;}}");
            code.AppendLine();

            CheckForNestedDtoTypes(pi, dtoTypes);
        }

        private static void GetDtoTypesByForMembersRecursive(ITypeSymbol interfaceType, List<ITypeSymbol> dtoItems)
        {
            if (interfaceType.TypeKind == TypeKind.Interface)
            {
                foreach (var member in interfaceType.GetMembers())
                {
                    if (member is IMethodSymbol method)
                    {
                        if (method.ReturnsVoid == false)
                        {
                            if (IsAsyncAwaitType(method.ReturnType, out bool containsAsyncResultValue))
                            {
                                if (containsAsyncResultValue)
                                {
                                    AddAsyncMethodReturnDtoItems(dtoItems, method);
                                }
                            }
                            else if (method.ReturnType.TypeKind == TypeKind.Interface)
                            {
                                if (dtoItems.Contains(method.ReturnType) == false)
                                {
                                    dtoItems.Add(method.ReturnType);

                                    GetDtoTypesByForMembersRecursive(method.ReturnType, dtoItems);
                                }
                            }
                        }

                        if (method.Parameters.Length > 0)
                        {
                            foreach (var param in method.Parameters)
                            {
                                if (param.Type.TypeKind == TypeKind.Interface)
                                {
                                    if (dtoItems.Contains(param.Type) == false)
                                        dtoItems.Add(param.Type);
                                }
                            }
                        }
                    }
                    else if (member is IPropertySymbol pi)
                        CheckForNestedDtoTypes(pi, dtoItems);
                }

                foreach (var baseInterface in interfaceType.Interfaces)
                {
                    foreach (var baseMember in baseInterface.GetMembers())
                    {
                        if (baseMember is IPropertySymbol piBase)
                            CheckForNestedDtoTypes(piBase, dtoItems);
                    }
                }
            }
        }

        private static void AddAsyncMethodReturnDtoItems(List<ITypeSymbol> dtoItems, IMethodSymbol method)
        {
            foreach (var genericArgType in ((INamedTypeSymbol)method.ReturnType).TypeArguments)
            {
                if (genericArgType.TypeKind == TypeKind.Interface)
                {
                    if (dtoItems.Contains(genericArgType) == false)
                    {
                        dtoItems.Add(genericArgType);

                        GetDtoTypesByForMembersRecursive(genericArgType, dtoItems);
                    }
                }
            }
        }

        private static void CheckForNestedDtoTypes(IPropertySymbol pi, List<ITypeSymbol> dtoTypes)
        {
            if (pi.Type.IsAbstract
                && pi.Type.IsReferenceType
                && dtoTypes.Contains(pi.Type) == false)
            {
                dtoTypes.Add(pi.Type);
            }
        }

        private static string GetSourceCodeTypeName(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                return $"{GetSourceNamespace(arrayType.ElementType.ContainingNamespace)}{arrayType.ElementType.Name}[]";
            }
            else if (type is INamedTypeSymbol nts
                && nts.IsGenericType)
            {
                return $"{GetSourceNamespace(type.ContainingNamespace)}{type.Name}<{string.Join(", ", nts.TypeArguments.Select(ta => GetSourceCodeTypeName(ta)))}>";
            }
            else
                return $"{GetSourceNamespace(type.ContainingNamespace)}{type.Name}";
        }

        private static string GetSourceNamespace(INamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol.Name == "System" || namespaceSymbol.IsGlobalNamespace)
                return string.Empty;
            else
                return $"{namespaceSymbol}.";
        }

        private static string GetImplName(string interfaceName)
        {
            if (interfaceName.StartsWith("I"))
            {
                return interfaceName.Substring(1);
            }
            else
            {
                return interfaceName;
            }
        }


        /// <summary>
        /// Determines if the given type is an async await Task like type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsAsyncAwaitType(ITypeSymbol type, out bool containsResultValue)
        {

            bool isAsyncAwait = type.AllInterfaces.Where(nts => nts.Name == "IAsyncResult" && nts.ContainingNamespace.Name == "System").Any();

            if (isAsyncAwait)
            {
                bool isGenericType = ((INamedTypeSymbol)type).IsGenericType;

                containsResultValue = isGenericType;
            }
            else
                containsResultValue = false;

            return isAsyncAwait;
        }
    }
}
