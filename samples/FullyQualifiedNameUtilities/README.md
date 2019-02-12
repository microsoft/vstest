## FullyQualifiedNameUtilities

This sample project (with accompanying tests) demonstrates how to generate Fully Qualified Name properties that conform to the [spec](https://github.com/Microsoft/vstest-docs/blob/master/RFCs/0017-Managed-TestCase-Properties.md).

The code in this sample...

* Converts `System.Reflection.MethodBase` to `ManagedType`/`ManagedMethod` string values and vice versa.
* Converts `Microsoft.CodeAnalysis.INamedTypeSymbol` & `IMethodSymbol` to `ManagedType`/`ManagedMethod` string values and vice versa.

This code can be integrated into vstest adapters to ensure that they are compatible with each other and with the Visual Studio Test Explorer.
