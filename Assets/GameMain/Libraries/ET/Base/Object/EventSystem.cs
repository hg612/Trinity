﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ETModel
{
	public enum DLLType
	{
		Model,
		Hotfix,
		Editor,
	}

	public sealed class EventSystem
	{
		private readonly Dictionary<long, Component> allComponents = new Dictionary<long, Component>();

		private readonly Dictionary<DLLType, Assembly> assemblies = new Dictionary<DLLType, Assembly>();
		private readonly UnOrderMultiMap<Type, Type> types = new UnOrderMultiMap<Type, Type>();

		private readonly UnOrderMultiMap<Type, IAwakeSystem> awakeSystems = new UnOrderMultiMap<Type, IAwakeSystem>();

		private readonly UnOrderMultiMap<Type, IStartSystem> startSystems = new UnOrderMultiMap<Type, IStartSystem>();

		private readonly UnOrderMultiMap<Type, IDestroySystem> destroySystems = new UnOrderMultiMap<Type, IDestroySystem>();

		private readonly UnOrderMultiMap<Type, ILoadSystem> loadSystems = new UnOrderMultiMap<Type, ILoadSystem>();

		private readonly UnOrderMultiMap<Type, IUpdateSystem> updateSystems = new UnOrderMultiMap<Type, IUpdateSystem>();

		private readonly UnOrderMultiMap<Type, ILateUpdateSystem> lateUpdateSystems = new UnOrderMultiMap<Type, ILateUpdateSystem>();

		private readonly UnOrderMultiMap<Type, IChangeSystem> changeSystems = new UnOrderMultiMap<Type, IChangeSystem>();

		private Queue<long> updates = new Queue<long>();
		private Queue<long> updates2 = new Queue<long>();
		
		private readonly Queue<long> starts = new Queue<long>();

		private Queue<long> loaders = new Queue<long>();
		private Queue<long> loaders2 = new Queue<long>();

		private Queue<long> lateUpdates = new Queue<long>();
		private Queue<long> lateUpdates2 = new Queue<long>();

		public void Add(DLLType dllType, Assembly assembly)
		{
			this.assemblies[dllType] = assembly;
			this.types.Clear();
			foreach (Assembly value in this.assemblies.Values)
			{
				foreach (Type type in value.GetTypes())
				{
					object[] objects = type.GetCustomAttributes(typeof(BaseAttribute), false);
					if (objects.Length == 0)
					{
						continue;
					}

					BaseAttribute baseAttribute = (BaseAttribute) objects[0];
					this.types.Add(baseAttribute.AttributeType, type);
				}
			}

			this.awakeSystems.Clear();
			this.lateUpdateSystems.Clear();
			this.updateSystems.Clear();
			this.startSystems.Clear();
			this.loadSystems.Clear();
			this.changeSystems.Clear();

			foreach (Type type in types[typeof(ObjectSystemAttribute)])
			{
				object[] attrs = type.GetCustomAttributes(typeof(ObjectSystemAttribute), false);

				if (attrs.Length == 0)
				{
					continue;
				}

				object obj = Activator.CreateInstance(type);

				IAwakeSystem objectSystem = obj as IAwakeSystem;
				if (objectSystem != null)
				{
					this.awakeSystems.Add(objectSystem.Type(), objectSystem);
				}

				IUpdateSystem updateSystem = obj as IUpdateSystem;
				if (updateSystem != null)
				{
					this.updateSystems.Add(updateSystem.Type(), updateSystem);
				}

				ILateUpdateSystem lateUpdateSystem = obj as ILateUpdateSystem;
				if (lateUpdateSystem != null)
				{
					this.lateUpdateSystems.Add(lateUpdateSystem.Type(), lateUpdateSystem);
				}

				IStartSystem startSystem = obj as IStartSystem;
				if (startSystem != null)
				{
					this.startSystems.Add(startSystem.Type(), startSystem);
				}

				IDestroySystem destroySystem = obj as IDestroySystem;
				if (destroySystem != null)
				{
					this.destroySystems.Add(destroySystem.Type(), destroySystem);
				}

				ILoadSystem loadSystem = obj as ILoadSystem;
				if (loadSystem != null)
				{
					this.loadSystems.Add(loadSystem.Type(), loadSystem);
				}

				IChangeSystem changeSystem = obj as IChangeSystem;
				if (changeSystem != null)
				{
					this.changeSystems.Add(changeSystem.Type(), changeSystem);
				}
			}

			

			this.Load();
		}


		public Assembly Get(DLLType dllType)
		{
			return this.assemblies[dllType];
		}
		
		public List<Type> GetTypes(Type systemAttributeType)
		{
			if (!this.types.ContainsKey(systemAttributeType))
			{
				return new List<Type>();
			}
			return this.types[systemAttributeType];
		}

		public void Add(Component component)
		{
			this.allComponents.Add(component.InstanceId, component);

			Type type = component.GetType();

			if (this.loadSystems.ContainsKey(type))
			{
				this.loaders.Enqueue(component.InstanceId);
			}

			if (this.updateSystems.ContainsKey(type))
			{
				this.updates.Enqueue(component.InstanceId);
			}

			if (this.startSystems.ContainsKey(type))
			{
				this.starts.Enqueue(component.InstanceId);
			}

			if (this.lateUpdateSystems.ContainsKey(type))
			{
				this.lateUpdates.Enqueue(component.InstanceId);
			}
		}

		public void Remove(long instanceId)
		{
			this.allComponents.Remove(instanceId);
		}

		public Component Get(long id)
		{
			Component component = null;
			this.allComponents.TryGetValue(id, out component);
			return component;
		}

		public void Awake(Component component)
		{
			List<IAwakeSystem> iAwakeSystems = this.awakeSystems[component.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (IAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				
				IAwake iAwake = aAwakeSystem as IAwake;
				if (iAwake == null)
				{
					continue;
				}

				try
				{
					iAwake.Run(component);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

		public void Awake<P1>(Component component, P1 p1)
		{
			List<IAwakeSystem> iAwakeSystems = this.awakeSystems[component.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (IAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				
				IAwake<P1> iAwake = aAwakeSystem as IAwake<P1>;
				if (iAwake == null)
				{
					continue;
				}

				try
				{
					iAwake.Run(component, p1);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

		public void Awake<P1, P2>(Component component, P1 p1, P2 p2)
		{
			List<IAwakeSystem> iAwakeSystems = this.awakeSystems[component.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (IAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				
				IAwake<P1, P2> iAwake = aAwakeSystem as IAwake<P1, P2>;
				if (iAwake == null)
				{
					continue;
				}

				try
				{
					iAwake.Run(component, p1, p2);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

		public void Awake<P1, P2, P3>(Component component, P1 p1, P2 p2, P3 p3)
		{
			List<IAwakeSystem> iAwakeSystems = this.awakeSystems[component.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (IAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}

				IAwake<P1, P2, P3> iAwake = aAwakeSystem as IAwake<P1, P2, P3>;
				if (iAwake == null)
				{
					continue;
				}

				try
				{
					iAwake.Run(component, p1, p2, p3);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

		public void Change(Component component)
		{
			List<IChangeSystem> iChangeSystems = this.changeSystems[component.GetType()];
			if (iChangeSystems == null)
			{
				return;
			}

			foreach (IChangeSystem iChangeSystem in iChangeSystems)
			{
				if (iChangeSystem == null)
				{
					continue;
				}

				try
				{
					iChangeSystem.Run(component);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

		public void Load()
		{
			while (this.loaders.Count > 0)
			{
				long instanceId = this.loaders.Dequeue();
				Component component;
				if (!this.allComponents.TryGetValue(instanceId, out component))
				{
					continue;
				}
				if (component.IsDisposed)
				{
					continue;
				}
				
				List<ILoadSystem> iLoadSystems = this.loadSystems[component.GetType()];
				if (iLoadSystems == null)
				{
					continue;
				}

				this.loaders2.Enqueue(instanceId);

				foreach (ILoadSystem iLoadSystem in iLoadSystems)
				{
					try
					{
						iLoadSystem.Run(component);
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			}

			ObjectHelper.Swap(ref this.loaders, ref this.loaders2);
		}

		private void Start()
		{
			while (this.starts.Count > 0)
			{
				long instanceId = this.starts.Dequeue();
				Component component;
				if (!this.allComponents.TryGetValue(instanceId, out component))
				{
					continue;
				}

				List<IStartSystem> iStartSystems = this.startSystems[component.GetType()];
				if (iStartSystems == null)
				{
					continue;
				}
				
				foreach (IStartSystem iStartSystem in iStartSystems)
				{
					try
					{
						iStartSystem.Run(component);
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			}
		}

		public void Destroy(Component component)
		{
			List<IDestroySystem> iDestroySystems = this.destroySystems[component.GetType()];
			if (iDestroySystems == null)
			{
				return;
			}

			foreach (IDestroySystem iDestroySystem in iDestroySystems)
			{
				if (iDestroySystem == null)
				{
					continue;
				}

				try
				{
					iDestroySystem.Run(component);
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}

		public void Update()
		{
			this.Start();
			
			while (this.updates.Count > 0)
			{
				long instanceId = this.updates.Dequeue();
				Component component;
				if (!this.allComponents.TryGetValue(instanceId, out component))
				{
					continue;
				}
				if (component.IsDisposed)
				{
					continue;
				}
				
				List<IUpdateSystem> iUpdateSystems = this.updateSystems[component.GetType()];
				if (iUpdateSystems == null)
				{
					continue;
				}

				this.updates2.Enqueue(instanceId);

				foreach (IUpdateSystem iUpdateSystem in iUpdateSystems)
				{
					try
					{
						iUpdateSystem.Run(component);
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			}

			ObjectHelper.Swap(ref this.updates, ref this.updates2);
		}

		public void LateUpdate()
		{
			while (this.lateUpdates.Count > 0)
			{
				long instanceId = this.lateUpdates.Dequeue();
				Component component;
				if (!this.allComponents.TryGetValue(instanceId, out component))
				{
					continue;
				}
				if (component.IsDisposed)
				{
					continue;
				}

				List<ILateUpdateSystem> iLateUpdateSystems = this.lateUpdateSystems[component.GetType()];
				if (iLateUpdateSystems == null)
				{
					continue;
				}

				this.lateUpdates2.Enqueue(instanceId);

				foreach (ILateUpdateSystem iLateUpdateSystem in iLateUpdateSystems)
				{
					try
					{
						iLateUpdateSystem.Run(component);
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			}

			ObjectHelper.Swap(ref this.lateUpdates, ref this.lateUpdates2);
		}

		
	}
}