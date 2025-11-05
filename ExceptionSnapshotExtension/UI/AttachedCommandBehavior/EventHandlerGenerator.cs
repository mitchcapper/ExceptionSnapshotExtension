using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;
using System.Linq;
namespace AttachedCommandBehavior {
	/// <summary>
	/// Generates delegates according to the specified signature on runtime
	/// </summary>
	public static class EventHandlerGenerator {


		/// <summary>
		/// Generates a delegate with a matching signature of the supplied eventHandlerType.
		/// This method only supports Events that have a delegate of type void.
		/// </summary>
		/// <param name="eventHandlerType">The delegate type to wrap. Note that this must always be a void delegate.</param>
		/// <param name="methodToInvoke">The method to invoke. This method is expected to take no arguments.</param>
		/// <param name="instance">The instance (if not static) to call the method on</param>
		/// <returns>Returns a delegate with the same signature as eventHandlerType that calls the methodToInvoke inside.</returns>
		public static Delegate CreateDelegate(Type eventHandlerType, MethodInfo methodToInvoke, object instance) {
			if (eventHandlerType == null) {
				throw new ArgumentNullException(nameof(eventHandlerType));
			}
			if (!typeof(Delegate).IsAssignableFrom(eventHandlerType)) {
				throw new ArgumentException("The supplied type must be a delegate type.", nameof(eventHandlerType));
			}
			if (methodToInvoke == null) {
				throw new ArgumentNullException(nameof(methodToInvoke));
			}
			if (!methodToInvoke.IsStatic && instance == null) {
				throw new ArgumentNullException(nameof(instance), "An instance method requires a non-null invoker instance.");
			}
			if (methodToInvoke.GetParameters().Length > 0) {
				throw new ArgumentException("The methodToInvoke is expected to have zero arguments.", nameof(methodToInvoke));
			}
			MethodInfo eventHandlerInvoke = eventHandlerType.GetMethod("Invoke") ?? throw new ArgumentException("Unable to resolve the Invoke method on the delegate type.", nameof(eventHandlerType));
			if (eventHandlerInvoke.ReturnType != typeof(void)) {
				throw new ApplicationException("This only supports event handlers that return void.");
			}
			ParameterInfo[] delegateParams = eventHandlerInvoke.GetParameters();
			ParameterExpression[] handlerParameters = delegateParams.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

			Expression instanceExpression = null;
			if (!methodToInvoke.IsStatic) {
				if (!methodToInvoke.DeclaringType.IsInstanceOfType(instance)) {
					throw new ArgumentException("The provided methodInvoker is not compatible with the method to invoke.", nameof(instance));
				}
				instanceExpression = Expression.Constant(instance, methodToInvoke.DeclaringType);
			}

			MethodCallExpression methodCall = methodToInvoke.IsStatic
				? Expression.Call(methodToInvoke)
				: Expression.Call(instanceExpression, methodToInvoke);

			LambdaExpression lambda = Expression.Lambda(
				eventHandlerType,
				methodCall,
				handlerParameters
			);

			return lambda.Compile();
		}

	}
}
