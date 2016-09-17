using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace ProductiveRage.CompilableTypeConverter.QueryableExtensions.ProjectionConverterHelpers
{
	public class AnonymousTypeCreator
	{
		public static AnonymousTypeCreator DefaultInstance = new AnonymousTypeCreator("DefaultAnonymousTypeCreatorAssembly");

		private readonly ModuleBuilder _moduleBuilder;
		public AnonymousTypeCreator(string assemblyName)
		{
			if (string.IsNullOrWhiteSpace(assemblyName))
				throw new ArgumentException("Null/blank assemblyName specified");

			var assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(
				new AssemblyName(assemblyName),
				AssemblyBuilderAccess.Run
			);
			_moduleBuilder = assemblyBuilder.DefineDynamicModule(
				assemblyBuilder.GetName().Name,
				false // emitSymbolInfo (not required here)
			);
		}

		public Type Get(AnonymousTypePropertyInfoSet properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			var typeName = "<>AnonymousType-" + Guid.NewGuid().ToString("N");
			var typeBuilder = _moduleBuilder.DefineType(
				typeName,
				TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout
			);

			var ctorBuilder = typeBuilder.DefineConstructor(
				MethodAttributes.Public,
				CallingConventions.Standard,
				Type.EmptyTypes // constructor parameters
			);
			var ilCtor = ctorBuilder.GetILGenerator();
			ilCtor.Emit(OpCodes.Ldarg_0);
			ilCtor.Emit(OpCodes.Call, typeBuilder.BaseType.GetConstructor(Type.EmptyTypes));
			ilCtor.Emit(OpCodes.Ret);

			foreach (var property in properties)
			{
				// Prepare the property we'll add get and/or set accessors to
				var propBuilder = typeBuilder.DefineProperty(
					property.Name,
					PropertyAttributes.None,
					property.PropertyType,
					Type.EmptyTypes
				);
				var backingField = typeBuilder.DefineField(
					property.Name,
					property.PropertyType,
					FieldAttributes.Private
				);

				// Define get method
				var getFuncBuilder = typeBuilder.DefineMethod(
					"get_" + property.Name,
					MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
					property.PropertyType,
					Type.EmptyTypes
				);
				var ilGetFunc = getFuncBuilder.GetILGenerator();
				ilGetFunc.Emit(OpCodes.Ldarg_0);
				ilGetFunc.Emit(OpCodes.Ldfld, backingField);
				ilGetFunc.Emit(OpCodes.Ret);
				propBuilder.SetGetMethod(getFuncBuilder);

				// Define set method
				var setFuncBuilder = typeBuilder.DefineMethod(
					"set_" + property.Name,
					MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual,
					null,
					new Type[] { property.PropertyType }
				);
				var ilSetFunc = setFuncBuilder.GetILGenerator();
				ilSetFunc.Emit(OpCodes.Ldarg_0);
				ilSetFunc.Emit(OpCodes.Ldarg_1);
				ilSetFunc.Emit(OpCodes.Stfld, backingField);
				ilSetFunc.Emit(OpCodes.Ret);
				propBuilder.SetSetMethod(setFuncBuilder);
			}

			return typeBuilder.CreateType();
		}

		private static MethodInfo MethodInfoInvokeMember = typeof(Type).GetMethod(
			"InvokeMember",
			new[]
            {
                typeof(string),
                typeof(BindingFlags),
                typeof(Binder),
                typeof(object),
                typeof(object[])
            }
		);
	}
}
