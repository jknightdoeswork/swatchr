// Copyright (c) 2013 Daniel Grunwald
// 
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SmartWeakEvent
{
  /// <summary>
	/// A class for managing a weak event.
	/// See http://www.codeproject.com/Articles/29922/Weak-Events-in-C
	/// </summary>
	/// <typeparam name="T">The delegate type of the event handlers.</typeparam>
	public sealed class FastSmartWeakEvent<T> where T : class
	{
		static FastSmartWeakEvent()
		{
			if (!typeof(T).IsSubclassOf(typeof(Delegate)))
				throw new ArgumentException("T must be a delegate type");
			MethodInfo invoke = typeof(T).GetMethod("Invoke");
			if (invoke == null)
				throw new ArgumentException("T must be a delegate type");
			if (invoke.ReturnType != typeof(void))
				throw new ArgumentException("The delegate return type must be void.");
			foreach (var p in invoke.GetParameters()) {
				if (p.IsOut && !p.IsIn)
					throw new ArgumentException("The delegate type must not have out-parameters");
			}
		}
		
		class HandlerEntry
		{
			public readonly FastSmartWeakEvent<T> ParentEventSource;
			readonly WeakReference weakReference;
			public readonly MethodInfo TargetMethod;
			public Delegate WrappingDelegate;
			
			public HandlerEntry(FastSmartWeakEvent<T> parentEventSource, object targetInstance, MethodInfo targetMethod)
			{
				this.ParentEventSource = parentEventSource;
				this.weakReference = new WeakReference(targetInstance);
				this.TargetMethod = targetMethod;
			}
			
			// This property is accessed by the generated IL method
			public object TargetInstance {
				get { return weakReference.Target; }
			}
			
			// This method is called by the generated IL method
			public void CalledWhenDead()
			{
				ParentEventSource.RemoveFromRaiseDelegate(this.WrappingDelegate);
			}
			
			/*
			 A wrapper method like this is generated using IL.Emit and attached to this object.
			 The signature of the method depends on the delegate type T.
			this.WrappingDelegate = delegate(object sender, EventArgs e)
			{
				object target = this.TargetInstance;
				if (target == null)
					this.CalledWhenDead();
				else
					((TargetType)target).TargetMethod(sender, e);
			}
			 */
		}
		
		volatile Delegate _raiseDelegate;
		
		Delegate GetRaiseDelegateInternal()
		{
			return _raiseDelegate;
		}
		
		#pragma warning disable 420 // CS0420 - a reference to a volatile field will not be treated as volatile
		// can be ignored because CompareExchange() treats the reference as volatile
		void AddToRaiseDelegate(Delegate d)
		{
			Delegate oldDelegate, newDelegate;
			do {
				oldDelegate = _raiseDelegate;
				newDelegate = Delegate.Combine(oldDelegate, d);
			} while (Interlocked.CompareExchange(ref _raiseDelegate, newDelegate, oldDelegate) != oldDelegate);
		}
		
		void RemoveFromRaiseDelegate(Delegate d)
		{
			Delegate oldDelegate, newDelegate;
			do {
				oldDelegate = _raiseDelegate;
				newDelegate = Delegate.Remove(oldDelegate, d);
			} while (Interlocked.CompareExchange(ref _raiseDelegate, newDelegate, oldDelegate) != oldDelegate);
		}
		#pragma warning restore 420
		
		public void Add(T eh)
		{
			if (eh != null) {
				Delegate d = (Delegate)(object)eh;
				RemoveDeadEntries();
				object targetInstance = d.Target;
				if (targetInstance != null) {
					MethodInfo targetMethod = d.Method;
					var wd = new HandlerEntry(this, targetInstance, targetMethod);
					var dynamicMethod = GetInvoker(targetMethod);
					wd.WrappingDelegate = dynamicMethod.CreateDelegate(typeof(T), wd);
					AddToRaiseDelegate(wd.WrappingDelegate);
				} else {
					// delegate to static method: use directly without wrapping delegate
					AddToRaiseDelegate(d);
				}
			}
		}

		
		
		/// <summary>
		/// Removes dead entries from the handler list.
		/// You normally do not need to invoke this method manually, as dead entry removal runs automatically as part of the normal operation of the FastSmartWeakEvent.
		/// </summary>
		public void RemoveDeadEntries()
		{
			Delegate raiseDelegate = GetRaiseDelegateInternal();
			if (raiseDelegate == null)
				return;
			foreach (var d in raiseDelegate.GetInvocationList()) {
				var wd = d.Target as HandlerEntry;
				if (wd != null && wd.TargetInstance == null) {
					RemoveFromRaiseDelegate(d);
				}
			}
		}
		
		public void Remove(T eh)
		{
			if (eh == null)
				return;
			Delegate d = (Delegate)(object)eh;
			object targetInstance = d.Target;
			if (targetInstance == null) {
				// delegate to static method: use directly without wrapping delegate
				RemoveFromRaiseDelegate(d);
				return;
			}
			MethodInfo targetMethod = d.Method;
			// Find+Remove the last copy of a delegate pointing to targetInstance/targetMethod
			Delegate raiseDelegate = GetRaiseDelegateInternal();
			if (raiseDelegate == null)
				return;
			Delegate[] invocationList = raiseDelegate.GetInvocationList();
			for (int i = invocationList.Length - 1; i >= 0; i--) {
				var wrappingDelegate = invocationList[i];
				var weakDelegate = wrappingDelegate.Target as HandlerEntry;
				if (weakDelegate == null)
					continue;
				object target = weakDelegate.TargetInstance;
				if (target == null) {
					RemoveFromRaiseDelegate(wrappingDelegate);
				} else if (target == targetInstance && weakDelegate.TargetMethod == targetMethod) {
					RemoveFromRaiseDelegate(wrappingDelegate);
					break;
				}
			}
		}
		
		/// <summary>
		/// Retrieves a delegate that can be used to raise the event.
		/// The delegate will contain a copy of the current invocation list. If handlers are added/removed from the event, GetRaiseDelegate() must be called
		/// again to retrieve a delegate that invokes the up-to-date invocation list.
		/// 
		/// If the invocation list is empty, this method will return null.
		/// </summary>
		public T GetRaiseDelegate()
		{
			return (T)(object)GetRaiseDelegateInternal();
		}
		
		/// <summary>
		/// Gets whether the event has listeners that were not cleaned up yet.
		/// </summary>
		public bool HasListeners {
			get { return GetRaiseDelegateInternal() != null; }
		}
		
		#region Code Generation
		static readonly MethodInfo getTargetMethod = typeof(HandlerEntry).GetMethod("get_TargetInstance");
		static readonly MethodInfo calledWhileDeadMethod = typeof(HandlerEntry).GetMethod("CalledWhenDead");
		
		static readonly Dictionary<MethodInfo, DynamicMethod> invokerMethods = new Dictionary<MethodInfo, DynamicMethod>();
		
		static DynamicMethod GetInvoker(MethodInfo method)
		{
			DynamicMethod dynamicMethod;
			lock (invokerMethods) {
				if (invokerMethods.TryGetValue(method, out dynamicMethod))
					return dynamicMethod;
			}
			
			if (method.DeclaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length != 0)
				throw new ArgumentException("Cannot create weak event to anonymous method with closure.");
			
			var parameters = method.GetParameters();
			var dynamicMethodParameterTypes = new Type[parameters.Length + 1];
			dynamicMethodParameterTypes[0] = typeof(HandlerEntry);
			for (int i = 0; i < parameters.Length; i++) {
				dynamicMethodParameterTypes[i + 1] = parameters[i].ParameterType;
			}
			
			dynamicMethod = new DynamicMethod("FastSmartWeakEvent", typeof(void), dynamicMethodParameterTypes, typeof(HandlerEntry), true);
			ILGenerator il = dynamicMethod.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.EmitCall(OpCodes.Call, getTargetMethod, null);
			il.Emit(OpCodes.Dup);
			Label label = il.DefineLabel();
			// Exit if target is null (was garbage-collected)
			il.Emit(OpCodes.Brtrue, label);
			il.Emit(OpCodes.Pop); // pop the duplicate null target
			il.Emit(OpCodes.Ldarg_0);
			il.EmitCall(OpCodes.Call, calledWhileDeadMethod, null);
			il.Emit(OpCodes.Ret);
			il.MarkLabel(label);
			il.Emit(OpCodes.Castclass, method.DeclaringType);
			for (int i = 0; i < parameters.Length; i++) {
				il.Emit(OpCodes.Ldarg, i + 1);
			}
			il.EmitCall(OpCodes.Call, method, null);
			il.Emit(OpCodes.Ret);
			
			lock (invokerMethods) {
				invokerMethods[method] = dynamicMethod;
			}
			return dynamicMethod;
		}
		#endregion
	}
	
	/// <summary>
	/// Strongly-typed raise methods for FastSmartWeakEvent
	/// </summary>
	public static class FastSmartWeakEventRaiseExtensions
	{
		public static void Raise(this FastSmartWeakEvent<EventHandler> ev, object sender, EventArgs e)
		{
			var d = ev.GetRaiseDelegate();
			if (d != null)
				d(sender, e);
		}
		
		public static void Raise<T>(this FastSmartWeakEvent<EventHandler<T>> ev, object sender, T e) where T : EventArgs
		{
			var d = ev.GetRaiseDelegate();
			if (d != null)
				d(sender, e);
		}
	}
}
